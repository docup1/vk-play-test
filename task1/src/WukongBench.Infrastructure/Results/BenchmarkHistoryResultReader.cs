namespace WukongBench.Infrastructure.Results;

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Configuration;

/// <summary>
/// Reads the benchmark history written by the tool.
///
/// After every completed pass the Benchmark Tool writes a file named after the
/// Unix timestamp of the run (no extension) into
/// <c>%TEMP%\b1\BenchMarkHistory\Tool\</c>. The file is JSON and contains the FPS
/// block (<c>FPSAvg</c>, <c>FPSMin</c>, <c>FPSMax</c>, <c>FPS95</c>), the VRAM
/// usage (<c>VideoMem</c>, in GB), the machine data and the graphics settings
/// that were actually used, plus a per-frame <c>Records</c> array from which the
/// 1% low and the average frame times are derived.
/// </summary>
public sealed class BenchmarkHistoryResultReader : IBenchmarkResultReader
{
    private const string HistoryFolderName = "BenchMarkHistory";
    private const string ToolFolderName = "Tool";

    /// <summary>Top level keys that describe the settings of the run rather than the machine.</summary>
    private static readonly string[] SettingKeys =
    {
        "ScreenMode", "ScreenResolution", "QualityLevel", "ImageQuality", "ViewDistance",
        "AntiAliasing", "PostProcessing", "ShadowQuality", "TextureQuality", "MaterialQuality",
        "VegetationQuality", "MotionBlur", "Rtx", "Dlss", "InsertFrame", "Dx12",
        "SuperResolutionSampling", "Vsync", "LockFrameRate",
    };

    private readonly ResultReaderOptions _options;
    private readonly IToolLogger _logger;

    public BenchmarkHistoryResultReader(IOptions<ResultReaderOptions> options, IToolLogger logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<BenchmarkMetrics?> TryReadLatestAsync(
        SteamInstallation installation,
        DateTimeOffset notOlderThan,
        CancellationToken cancellationToken)
    {
        var files = FindResultFiles(installation, notOlderThan);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var metrics = Parse(file);
                if (metrics is not null)
                {
                    return Task.FromResult<BenchmarkMetrics?>(metrics);
                }
            }
            catch (IOException exception)
            {
                // The tool may still be writing the file; the caller polls again.
                _logger.Debug($"'{file}' is not readable yet: {exception.Message}");
            }
            catch (JsonException exception)
            {
                _logger.Debug($"'{file}' is not valid JSON yet: {exception.Message}");
            }
        }

        return Task.FromResult<BenchmarkMetrics?>(null);
    }

    public IReadOnlyList<string> GetWatchedLocations(SteamInstallation installation)
    {
        var locations = new List<string> { ResolveHistoryDirectory() };
        locations.AddRange(_options.AdditionalDirectories.Where(Directory.Exists));
        return locations;
    }

    public IReadOnlyList<string> FindExistingResultFiles(SteamInstallation installation, DateTimeOffset notOlderThan) =>
        FindResultFiles(installation, notOlderThan);

    public IReadOnlyList<string> FindExistingLogFiles(SteamInstallation installation)
    {
        var files = new List<string>();
        foreach (var candidate in _options.LogFileCandidates)
        {
            var path = Path.Combine(installation.InstallDirectory, candidate);
            if (File.Exists(path))
            {
                files.Add(path);
            }
        }

        return files;
    }

    private string ResolveHistoryDirectory() =>
        string.IsNullOrWhiteSpace(_options.HistoryDirectoryOverride)
            ? Path.Combine(Path.GetTempPath(), "b1", HistoryFolderName, ToolFolderName)
            : Path.GetFullPath(_options.HistoryDirectoryOverride);

    private IReadOnlyList<string> FindResultFiles(SteamInstallation installation, DateTimeOffset notOlderThan)
    {
        var directories = new List<string> { ResolveHistoryDirectory() };
        directories.AddRange(_options.AdditionalDirectories);

        var threshold = notOlderThan.UtcDateTime;
        var results = new List<FileInfo>();

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            try
            {
                results.AddRange(Directory
                    .EnumerateFiles(directory)
                    .Select(path => new FileInfo(path))
                    .Where(info => info.LastWriteTimeUtc >= threshold));
            }
            catch (Exception exception)
            {
                _logger.Debug($"Could not enumerate '{directory}': {exception.Message}");
            }
        }

        return results
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .Select(info => info.FullName)
            .ToArray();
    }

    private BenchmarkMetrics? Parse(string path)
    {
        string json;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream))
        {
            json = reader.ReadToEnd();
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var average = ReadDouble(root, "FPSAvg");
        if (average is null || average <= 0)
        {
            // An unfinished file: the tool writes the summary last.
            return null;
        }

        var records = ReadRecords(root);
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.Number or JsonValueKind.String or JsonValueKind.True or JsonValueKind.False)
            {
                raw[property.Name] = property.Value.ToString();
            }

            if (SettingKeys.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                settings[property.Name] = property.Value.ToString();
            }
        }

        var videoMemGb = ReadDouble(root, "VideoMem");

        return new BenchmarkMetrics(
            averageFps: average,
            minimumFps: ReadDouble(root, "FPSMin"),
            maximumFps: ReadDouble(root, "FPSMax"),
            percentile95Fps: ReadDouble(root, "FPS95"),
            percentile1LowFps: Percentile1Low(records),
            averageFrameTimeMs: records.Count > 0 ? records.Average(record => record.FrameTimeMs) : 1000.0 / average.Value,
            averageCpuFrameTimeMs: records.Count > 0 && records.Any(r => r.CpuFrameTimeMs.HasValue)
                ? records.Where(r => r.CpuFrameTimeMs.HasValue).Average(r => r.CpuFrameTimeMs!.Value)
                : null,
            averageGpuFrameTimeMs: records.Count > 0 && records.Any(r => r.GpuFrameTimeMs.HasValue)
                ? records.Where(r => r.GpuFrameTimeMs.HasValue).Average(r => r.GpuFrameTimeMs!.Value)
                : null,
            maximumVramUsageMb: videoMemGb.HasValue ? videoMemGb * 1024.0 : null,
            averageVramUsageMb: records.Count > 0 && records.Any(r => r.VideoMemoryUsage.HasValue)
                ? records.Where(r => r.VideoMemoryUsage.HasValue).Average(r => r.VideoMemoryUsage!.Value)
                : null,
            vramSizeLabel: ReadString(root, "VideoMemSize"),
            cpuUtilizationPercent: ReadDouble(root, "CPUAvg"),
            gpuUtilizationPercent: ReadDouble(root, "GPUAvg"),
            gpuLeakPercent: ReadDouble(root, "GPULeak"),
            resolution: ReadString(root, "ScreenResolution"),
            sourcePath: path,
            reportedSystem: new ReportedSystemInfo(
                ReadString(root, "CPUModel"),
                ReadString(root, "GPUModel"),
                ReadString(root, "GpuDriverVer"),
                ReadString(root, "SysMem"),
                ReadString(root, "SysVer"),
                ReadString(root, "GameVer"),
                ReadString(root, "BenckMarkVersion")),
            recordedSettings: settings,
            rawValues: raw);
    }

    private static double? Percentile1Low(IReadOnlyList<FrameRecord> records)
    {
        if (records.Count == 0)
        {
            return null;
        }

        var ordered = records.Select(record => record.FrameRate).Where(rate => rate > 0).OrderBy(rate => rate).ToArray();
        if (ordered.Length == 0)
        {
            return null;
        }

        var count = Math.Max(1, (int)Math.Floor(ordered.Length * 0.01));
        return ordered.Take(count).Average();
    }

    private List<FrameRecord> ReadRecords(JsonElement root)
    {
        var records = new List<FrameRecord>();
        if (!root.TryGetProperty("Records", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return records;
        }

        foreach (var element in array.EnumerateArray())
        {
            var rate = ReadDouble(element, "FrameRate");
            if (rate is null or <= 0)
            {
                continue;
            }

            records.Add(new FrameRecord(
                rate.Value,
                1000.0 / rate.Value,
                ReadDouble(element, "CPUFrameTime"),
                ReadDouble(element, "GPUFrameTime"),
                ReadDouble(element, "VideoMemoryUsage")));
        }

        return records;
    }

    private static double? ReadDouble(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String when double.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static string ReadString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    }

    private sealed record FrameRecord(double FrameRate, double FrameTimeMs, double? CpuFrameTimeMs, double? GpuFrameTimeMs, double? VideoMemoryUsage);
}
