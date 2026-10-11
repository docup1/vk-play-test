namespace WukongBench.Tests;

using Microsoft.Extensions.Options;
using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Configuration;
using WukongBench.Infrastructure.Results;
using WukongBench.Tests.Fakes;
using Xunit;

/// <summary>
/// The result reader is the output side of the automation: it has to find the
/// newest history file of the tool and turn it into metrics.
/// </summary>
public sealed class BenchmarkHistoryResultReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "wukongbench-tests", Guid.NewGuid().ToString("N"));

    private const string SampleResult = """
        {
          "TimeStamp": 1755000000,
          "BenckMarkVersion": "1.0.3.14649",
          "GameVer": "1.0.3.14649",
          "SysVer": "Windows 11 Pro 10.0.22631",
          "CPUModel": "AMD Ryzen 7 5800X3D 8-Core Processor",
          "GPUModel": "NVIDIA GeForce RTX 4070",
          "GpuDriverVer": "552.22",
          "SysMem": "32GB",
          "FPSAvg": 120,
          "FPSMin": 44,
          "FPSMax": 201,
          "FPS95": 61,
          "CPUAvg": 74.5,
          "GPUAvg": 98.2,
          "GPULeak": 0.3,
          "VideoMem": 6.5,
          "VideoMemSize": "12GB",
          "ScreenMode": 1,
          "ScreenResolution": "1280 × 720",
          "QualityLevel": 1,
          "ImageQuality": 180,
          "ViewDistance": 1,
          "AntiAliasing": 1,
          "PostProcessing": 1,
          "ShadowQuality": 1,
          "TextureQuality": 1,
          "MaterialQuality": 1,
          "VegetationQuality": 1,
          "MotionBlur": 0,
          "Rtx": 0,
          "Dlss": 0,
          "InsertFrame": 0,
          "Dx12": 1,
          "Records": [
            { "TimeStamp": 1, "FrameRate": 5,   "CPUUsage": 60, "GPUUsage": 40, "CPUFrameTime": 8.1, "GPUFrameTime": 4.2, "VideoMemoryUsage": 1024 },
            { "TimeStamp": 2, "FrameRate": 60,  "CPUUsage": 70, "GPUUsage": 80, "CPUFrameTime": 16.6, "GPUFrameTime": 14.0, "VideoMemoryUsage": 2048 },
            { "TimeStamp": 3, "FrameRate": 120, "CPUUsage": 80, "GPUUsage": 95, "CPUFrameTime": 8.3, "GPUFrameTime": 7.9, "VideoMemoryUsage": 3072 }
          ]
        }
        """;

    public BenchmarkHistoryResultReaderTests() => Directory.CreateDirectory(_directory);

    private SteamInstallation Installation => new(
        3132990, "Black Myth: Wukong Benchmark Tool", _directory, _directory, _directory,
        Path.Combine(_directory, "b1_benchmark.exe"), string.Empty, "steam://rungameid/3132990", "15365379", "4");

    private BenchmarkHistoryResultReader CreateReader() =>
        new(Options.Create(new ResultReaderOptions { HistoryDirectoryOverride = _directory }), new RecordingLogger());

    private string WriteResult(string content, DateTimeOffset timestamp)
    {
        // The tool names the file after the Unix timestamp of the run and adds no extension.
        var path = Path.Combine(_directory, timestamp.ToUnixTimeSeconds().ToString());
        File.WriteAllText(path, content);
        File.SetLastWriteTimeUtc(path, timestamp.UtcDateTime);
        return path;
    }

    [Fact]
    public async Task TryReadLatest_ParsesTheSummaryBlock()
    {
        WriteResult(SampleResult, DateTimeOffset.UtcNow);

        var metrics = await CreateReader().TryReadLatestAsync(Installation, DateTimeOffset.UtcNow.AddMinutes(-5), CancellationToken.None);

        Assert.NotNull(metrics);
        Assert.Equal(120d, metrics!.AverageFps!.Value, 3);
        Assert.Equal(44d, metrics.MinimumFps!.Value, 3);
        Assert.Equal(201d, metrics.MaximumFps!.Value, 3);
        Assert.Equal(61d, metrics.Percentile95Fps!.Value, 3);
        Assert.Equal(74.5d, metrics.CpuUtilizationPercent!.Value, 3);
        Assert.Equal(98.2d, metrics.GpuUtilizationPercent!.Value, 3);
        Assert.Equal("1280 × 720", metrics.Resolution);

        // VideoMem is reported in GB and is exposed in MB.
        Assert.Equal(6.5 * 1024, metrics.MaximumVramUsageMb!.Value, 3);
        Assert.Equal("12GB", metrics.VramSizeLabel);
    }

    [Fact]
    public async Task TryReadLatest_DerivesFrameTimeAndOnePercentLowFromTheRecords()
    {
        WriteResult(SampleResult, DateTimeOffset.UtcNow);

        var metrics = await CreateReader().TryReadLatestAsync(Installation, DateTimeOffset.UtcNow.AddMinutes(-5), CancellationToken.None);

        Assert.NotNull(metrics);
        Assert.Equal(5d, metrics!.Percentile1LowFps!.Value, 3); // lowest 1% of the three records
        Assert.Equal((8.1 + 16.6 + 8.3) / 3, metrics.AverageCpuFrameTimeMs!.Value, 3);
        Assert.Equal((4.2 + 14.0 + 7.9) / 3, metrics.AverageGpuFrameTimeMs!.Value, 3);
        Assert.Equal(2048d, metrics.AverageVramUsageMb!.Value, 3);
    }

    [Fact]
    public async Task TryReadLatest_CapturesTheSettingsRecordedByTheTool()
    {
        WriteResult(SampleResult, DateTimeOffset.UtcNow);

        var metrics = await CreateReader().TryReadLatestAsync(Installation, DateTimeOffset.UtcNow.AddMinutes(-5), CancellationToken.None);

        Assert.NotNull(metrics);
        Assert.Equal("1", metrics!.RecordedSettings["QualityLevel"]);
        Assert.Equal("0", metrics.RecordedSettings["Rtx"]);
        Assert.Equal("180", metrics.RecordedSettings["ImageQuality"]);
        Assert.Equal("AMD Ryzen 7 5800X3D 8-Core Processor", metrics.ReportedSystem.CpuModel);
        Assert.Equal("1.0.3.14649", metrics.ReportedSystem.BenchmarkToolVersion);
    }

    [Fact]
    public async Task TryReadLatest_IgnoresResultsOlderThanTheRequestedPointInTime()
    {
        WriteResult(SampleResult, DateTimeOffset.UtcNow.AddHours(-2));

        var metrics = await CreateReader().TryReadLatestAsync(Installation, DateTimeOffset.UtcNow.AddMinutes(-5), CancellationToken.None);

        Assert.Null(metrics);
    }

    [Fact]
    public async Task TryReadLatest_ReturnsNull_ForAnIncompleteFile()
    {
        var incomplete = SampleResult.Replace("\"FPSAvg\": 120", "\"FPSAvg\": 0");
        WriteResult(incomplete, DateTimeOffset.UtcNow);

        var metrics = await CreateReader().TryReadLatestAsync(Installation, DateTimeOffset.UtcNow.AddMinutes(-5), CancellationToken.None);

        Assert.Null(metrics);
    }

    [Fact]
    public async Task TryReadLatest_PicksTheNewestFile()
    {
        WriteResult(SampleResult, DateTimeOffset.UtcNow.AddMinutes(-3));
        var newest = WriteResult(SampleResult.Replace("\"FPSAvg\": 120", "\"FPSAvg\": 240"), DateTimeOffset.UtcNow);

        var metrics = await CreateReader().TryReadLatestAsync(Installation, DateTimeOffset.UtcNow.AddMinutes(-5), CancellationToken.None);

        Assert.NotNull(metrics);
        Assert.Equal(240d, metrics!.AverageFps!.Value, 3);
        Assert.Equal(newest, metrics.SourcePath);
    }

    [Fact]
    public void WatchedLocations_ContainsTheConfiguredAndTheDefaultHistoryFolder()
    {
        var configured = CreateReader().GetWatchedLocations(Installation);
        Assert.Contains(_directory, configured);

        var withDefault = new BenchmarkHistoryResultReader(
            Options.Create(new ResultReaderOptions()),
            new RecordingLogger());

        var locations = withDefault.GetWatchedLocations(Installation);
        Assert.Contains(locations, location => location.Contains("BenchMarkHistory", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(locations, location => location.EndsWith("Tool", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }
}
