namespace WukongBench.Tests;

using Microsoft.Extensions.Options;
using WukongBench.Application.Automation;
using WukongBench.Application.Configuration;
using WukongBench.Application.Models;
using WukongBench.Application.Scenarios;
using WukongBench.Application.UseCases;
using WukongBench.Domain.Models;
using WukongBench.Tests.Fakes;
using Xunit;

/// <summary>
/// End to end test of the main scenario with fake infrastructure ports: the use
/// case has to apply the CPU settings, run a pass, apply the GPU settings, run a
/// second pass, read both results and restore the original settings.
/// </summary>
public sealed class RunBenchmarkSuiteUseCaseTests : IDisposable
{
    private readonly TempWorkspace _workspace = new();
    private readonly FakeSettingsStoreFactory _settingsStore = new();
    private readonly FakeProcessLauncher _launcher = new();
    private readonly FakeUiAutomation _uiAutomation = new();
    private readonly FakeArtifactCollector _artifacts = new();
    private readonly FakeHardwareInfoProvider _hardware = new();
    private readonly FakePowerSchemeManager _power = new();
    private readonly RecordingLogger _logger = new();

    private readonly DateTimeOffset _start = new(2025, 8, 12, 18, 0, 0, TimeSpan.Zero);

    private static BenchmarkMetrics Metrics(double averageFps, string source) => new(
        averageFps, averageFps * 0.5, averageFps * 1.5, averageFps * 0.7, averageFps * 0.4,
        1000 / averageFps, 5, 6, 4096, 3800, "12GB", 60, 99, 0.1, "2560 × 1440",
        source,
        new ReportedSystemInfo("CPU", "GPU", "1.0", "32GB", "Windows 11", "1.0", "1.0"),
        new Dictionary<string, string> { ["QualityLevel"] = "1" },
        new Dictionary<string, string>());

    private RunBenchmarkSuiteUseCase CreateUseCase(WukongBenchOptions? options = null)
    {
        options ??= new WukongBenchOptions
        {
            Timings = new BenchmarkTimingsOptions
            {
                ResultSettleSeconds = 0,
                ResultPollIntervalSeconds = 1,
                MainMenuSettleSeconds = 0,
            },
        };

        var installation = new SteamInstallation(
            3132990, "Black Myth: Wukong Benchmark Tool", @"C:\Steam", @"C:\Steam", @"C:\Steam\common\wukong",
            @"C:\Steam\common\wukong\b1_benchmark.exe", @"C:\Steam\appmanifest_3132990.acf",
            "steam://rungameid/3132990", "15365379", "4");

        var reader = new FakeResultReader(call => call < 2 ? Metrics(240, "cpu.json") : Metrics(61, "gpu.json"));

        return new RunBenchmarkSuiteUseCase(
            new FakeInstallationLocator(installation),
            _settingsStore,
            _launcher,
            new FakeReadinessProbe(),
            _uiAutomation,
            reader,
            _artifacts,
            _hardware,
            _power,
            _workspace,
            new FakeClock(_start),
            _logger,
            Options.Create(options),
            new BenchmarkScenarioFactory(Options.Create(options)),
            new UiScriptCatalog(Options.Create(options)));
    }

    [Fact]
    public async Task ExecuteAsync_RunsBothPasses_AndBuildsTheReport()
    {
        var report = await CreateUseCase().ExecuteAsync(
            new BenchmarkSuiteRequest(true, true, false, true, true, true),
            CancellationToken.None);

        Assert.NotNull(report.CpuRun);
        Assert.NotNull(report.GpuRun);
        Assert.True(report.CpuRun!.Succeeded);
        Assert.True(report.GpuRun!.Succeeded);
        Assert.Equal(240d, report.CpuRun.Metrics!.AverageFps!.Value, 3);
        Assert.Equal(61d, report.GpuRun.Metrics!.AverageFps!.Value, 3);

        // Both scenarios were applied, in the CPU -> GPU order.
        Assert.Equal(2, _settingsStore.Applied.Count);
        Assert.Equal("cpu-bound", _settingsStore.Applied[0].Name);
        Assert.Equal("gpu-bound", _settingsStore.Applied[1].Name);

        // The tool was started twice and the UI scripts ran twice.
        Assert.Equal(2, _launcher.Arguments.Count);
        Assert.Equal(2, _uiAutomation.Scripts.Count(script => script == "start-benchmark"));
        Assert.All(_launcher.Session.WaitedForWindow, timeout => Assert.True(timeout > 0));

        // The high performance power scheme was activated and rolled back.
        Assert.Contains(PowerSchemes.HighPerformance, _power.Activated);
        Assert.Equal(PowerSchemes.Balanced, _power.Current);

        // The original settings were backed up and restored.
        Assert.Equal(1, _settingsStore.BackupCount);
        Assert.Equal(1, _settingsStore.RestoreCount);

        // Evidence is recorded for both passes.
        Assert.Equal(2, report.ArtifactPaths.Count(path => path.Contains("cpu-bound")));
    }

    [Fact]
    public async Task ExecuteAsync_KeepsTheModifiedSettings_WhenAsked()
    {
        var report = await CreateUseCase().ExecuteAsync(
            new BenchmarkSuiteRequest(true, true, true, true, false, true),
            CancellationToken.None);

        Assert.NotNull(report.CpuRun);
        Assert.Equal(0, _settingsStore.RestoreCount);
        Assert.Contains(report.Notes, note => note.Contains("--keep-settings", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_ReportsAFailure_WhenNoResultAppears()
    {
        var options = new WukongBenchOptions
        {
            Timings = new BenchmarkTimingsOptions
            {
                ResultSettleSeconds = 0,
                ResultPollIntervalSeconds = 1,
                BenchmarkCompletionTimeoutSeconds = 2,
                MainMenuSettleSeconds = 0,
            },
        };

        // A reader that never returns a result: the pass must fail cleanly.
        var reader = new FakeResultReader(_ => null);
        var installation = new SteamInstallation(
            3132990, "tool", @"C:\Steam", @"C:\Steam", @"C:\Steam\common\wukong",
            @"C:\Steam\common\wukong\b1_benchmark.exe", string.Empty, "steam://rungameid/3132990", "1", "4");

        var failing = new RunBenchmarkSuiteUseCase(
            new FakeInstallationLocator(installation),
            _settingsStore,
            _launcher,
            new FakeReadinessProbe(),
            _uiAutomation,
            reader,
            _artifacts,
            _hardware,
            _power,
            _workspace,
            new FakeClock(_start),
            _logger,
            Options.Create(options),
            new BenchmarkScenarioFactory(Options.Create(options)),
            new UiScriptCatalog(Options.Create(options)));

        var report = await failing.ExecuteAsync(
            new BenchmarkSuiteRequest(true, false, false, true, false, true),
            CancellationToken.None);

        Assert.NotNull(report.CpuRun);
        Assert.False(report.CpuRun!.Succeeded);
        Assert.Null(report.CpuRun.Metrics);
        Assert.Contains("No result file appeared", report.CpuRun.StatusMessage);
        Assert.NotEmpty(report.Notes);

    }

    [Fact]
    public async Task ExecuteAsync_Throws_WhenTheToolIsNotInstalled()
    {
        var options = new WukongBenchOptions();
        var useCase = new RunBenchmarkSuiteUseCase(
            new FakeInstallationLocator(null),
            _settingsStore,
            _launcher,
            new FakeReadinessProbe(),
            _uiAutomation,
            new FakeResultReader(_ => null),
            _artifacts,
            _hardware,
            _power,
            _workspace,
            new FakeClock(_start),
            _logger,
            Options.Create(options),
            new BenchmarkScenarioFactory(Options.Create(options)),
            new UiScriptCatalog(Options.Create(options)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new BenchmarkSuiteRequest(true, true, false, true, false, true), CancellationToken.None));
    }

    public void Dispose() => _workspace.Dispose();
}
