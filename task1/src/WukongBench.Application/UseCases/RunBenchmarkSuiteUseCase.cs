namespace WukongBench.Application.UseCases;

using System.Text;
using Microsoft.Extensions.Options;
using WukongBench.Application.Automation;
using WukongBench.Application.Configuration;
using WukongBench.Application.Models;
using WukongBench.Application.Scenarios;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;

/// <summary>
/// The main scenario of the tool: apply the CPU settings and run the benchmark,
/// apply the GPU settings and run it again, then hand back one report that
/// contains the machine, both results and both setting sets.
/// </summary>
public sealed class RunBenchmarkSuiteUseCase
{
    private readonly ISteamInstallationLocator _installationLocator;
    private readonly IGraphicsSettingsStoreFactory _settingsStoreFactory;
    private readonly IBenchmarkProcessLauncher _launcher;
    private readonly IBenchmarkReadinessProbe _readinessProbe;
    private readonly IKeyboardUiAutomation _uiAutomation;
    private readonly IBenchmarkResultReader _resultReader;
    private readonly IArtifactCollector _artifactCollector;
    private readonly IHardwareInfoProvider _hardwareInfoProvider;
    private readonly IPowerSchemeManager _powerSchemeManager;
    private readonly IBenchmarkWorkspace _workspace;
    private readonly IClock _clock;
    private readonly IToolLogger _logger;
    private readonly BenchmarkScenarioFactory _scenarioFactory;
    private readonly UiScriptCatalog _scriptCatalog;
    private readonly WukongBenchOptions _options;

    public RunBenchmarkSuiteUseCase(
        ISteamInstallationLocator installationLocator,
        IGraphicsSettingsStoreFactory settingsStoreFactory,
        IBenchmarkProcessLauncher launcher,
        IBenchmarkReadinessProbe readinessProbe,
        IKeyboardUiAutomation uiAutomation,
        IBenchmarkResultReader resultReader,
        IArtifactCollector artifactCollector,
        IHardwareInfoProvider hardwareInfoProvider,
        IPowerSchemeManager powerSchemeManager,
        IBenchmarkWorkspace workspace,
        IClock clock,
        IToolLogger logger,
        IOptions<WukongBenchOptions> options,
        BenchmarkScenarioFactory scenarioFactory,
        UiScriptCatalog scriptCatalog)
    {
        _installationLocator = installationLocator;
        _settingsStoreFactory = settingsStoreFactory;
        _launcher = launcher;
        _readinessProbe = readinessProbe;
        _uiAutomation = uiAutomation;
        _resultReader = resultReader;
        _artifactCollector = artifactCollector;
        _hardwareInfoProvider = hardwareInfoProvider;
        _powerSchemeManager = powerSchemeManager;
        _workspace = workspace;
        _clock = clock;
        _logger = logger;
        _options = options.Value;
        _scenarioFactory = scenarioFactory;
        _scriptCatalog = scriptCatalog;
    }

    /// <summary>Runs every enabled pass and returns the report.</summary>
    public async Task<BenchmarkReport> ExecuteAsync(BenchmarkSuiteRequest request, CancellationToken cancellationToken)
    {
        var notes = new List<string>();
        var artifactPaths = new List<string>();
        var timings = BenchmarkTimingsMapper.ToDomain(_options.Timings);

        _logger.Info("Reading hardware profile ...");
        var hardware = await _hardwareInfoProvider.ReadAsync(cancellationToken);
        _logger.Info($"CPU : {hardware.Cpu.Name} ({hardware.Cpu.PhysicalCores}C/{hardware.Cpu.LogicalProcessors}T)");
        _logger.Info($"GPU : {hardware.Gpu.Name} (driver {hardware.Gpu.DriverVersion})");
        _logger.Info($"RAM : {hardware.Memory.TotalBytes} bytes");

        var installation = _installationLocator.Locate(_options.SteamAppId, out var discoveryDiagnostics);
        if (installation is null)
        {
            throw new InvalidOperationException(
                $"Black Myth: Wukong Benchmark Tool (Steam app {_options.SteamAppId}) was not found. {discoveryDiagnostics}");
        }

        var installationSummary =
            $"{installation.AppName} (app {installation.AppId}, build {installation.BuildId}) in {installation.InstallDirectory}";
        _logger.Info($"Installation: {installationSummary}");

        var settingsStore = _settingsStoreFactory.Create(installation);
        _logger.Info($"Settings file: {settingsStore.SettingsFilePath}");
        if (!settingsStore.Exists)
        {
            notes.Add(
                "The benchmark tool settings file did not exist before this run. Start the benchmark tool once, " +
                "set the graphics options, close it and run this tool again so the original file can be backed up.");
        }

        var scenarios = _scenarioFactory.Create(hardware, request);
        if (scenarios.Count == 0)
        {
            throw new InvalidOperationException("No benchmark pass is enabled - nothing to do.");
        }

        var suiteDirectory = _workspace.CreateRunDirectory($"{_clock.UtcNow:yyyyMMdd-HHmmss}-suite");
        artifactPaths.Add(suiteDirectory);

        var backupPath = Path.Combine(suiteDirectory, "GameUserSettings.original.ini");
        var backupCreated = settingsStore.TryBackup(backupPath, out var backupMessage);
        _logger.Info(backupMessage);
        if (backupCreated)
        {
            artifactPaths.Add(backupPath);
        }
        else if (settingsStore.Exists)
        {
            notes.Add($"Could not back up the settings file: {backupMessage}");
        }

        var previousPowerScheme = string.Empty;
        var powerSchemeChanged = false;
        if (request.UseHighPerformancePowerScheme)
        {
            previousPowerScheme = await _powerSchemeManager.GetActiveSchemeGuidAsync(cancellationToken);
            if (string.Equals(previousPowerScheme, PowerSchemes.HighPerformance, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info("Power scheme is already 'High performance'.");
            }
            else if (await _powerSchemeManager.TryActivateAsync(PowerSchemes.HighPerformance, cancellationToken))
            {
                powerSchemeChanged = true;
                _logger.Info("Switched the power scheme to 'High performance' for the duration of the suite.");
            }
            else
            {
                notes.Add("Could not switch to the 'High performance' power scheme. CPU results may be less stable.");
            }
        }

        BenchmarkRun? cpuRun = null;
        BenchmarkRun? gpuRun = null;
        var toolLogTail = string.Empty;

        try
        {
            foreach (var scenario in scenarios)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var run = await RunScenarioAsync(scenario, installation, settingsStore, request, timings, notes, cancellationToken);
                artifactPaths.AddRange(run.ArtifactPaths);
                if (!string.IsNullOrWhiteSpace(run.ApplicationLogTail))
                {
                    toolLogTail = run.ApplicationLogTail;
                }

                if (scenario.Kind == BenchmarkScenarioKind.Cpu)
                {
                    cpuRun = run;
                }
                else
                {
                    gpuRun = run;
                }

                if (!run.Succeeded)
                {
                    notes.Add($"The {scenario.DisplayName} did not produce a result: {run.StatusMessage}");
                }
            }
        }
        finally
        {
            if (powerSchemeChanged && !string.IsNullOrWhiteSpace(previousPowerScheme))
            {
                await _powerSchemeManager.TryActivateAsync(previousPowerScheme, CancellationToken.None);
                _logger.Info("Restored the previous power scheme.");
            }

            if (request.KeepModifiedSettings)
            {
                notes.Add("The benchmark tool settings were intentionally left at the values of the last pass (--keep-settings).");
            }
            else if (backupCreated)
            {
                if (settingsStore.TryRestore(backupPath, out var restoreMessage))
                {
                    _logger.Info($"Restored the original benchmark settings ({restoreMessage}).");
                }
                else
                {
                    notes.Add($"Could not restore the original settings file: {restoreMessage}");
                }
            }
        }

        return new BenchmarkReport(
            _options.ToolName,
            _options.ToolVersion,
            hardware,
            cpuRun,
            gpuRun,
            _clock.UtcNow,
            installationSummary,
            settingsStore.SettingsFilePath,
            toolLogTail,
            artifactPaths.Distinct().ToArray(),
            notes);
    }

    private async Task<BenchmarkRun> RunScenarioAsync(
        BenchmarkScenario scenario,
        SteamInstallation installation,
        IGraphicsSettingsStore settingsStore,
        BenchmarkSuiteRequest request,
        BenchmarkTimings timings,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(scenario.DisplayName);

        var startedAt = _clock.UtcNow;
        var artifacts = new List<string>();
        var failures = new List<string>();
        var logTail = string.Empty;
        var processStarted = false;
        var processExitedCleanly = false;
        BenchmarkMetrics? metrics = null;

        var runDirectory = _workspace.CreateRunDirectory(
            $"{startedAt:HHmmss}-{scenario.Kind.ToString().ToLowerInvariant()}-{scenario.Settings.Name}");
        artifacts.Add(runDirectory);

        // 1. Apply the settings of the pass.
        _logger.Info($"Applying settings profile '{scenario.Settings.Name}' ({scenario.Settings.Values.Count} ini keys, " +
                     $"{scenario.Settings.UiSettings.Count} game settings entries).");
        settingsStore.Apply(scenario.Settings);

        var appliedDumpPath = Path.Combine(runDirectory, "settings-applied.txt");
        await WriteSettingsDumpAsync(appliedDumpPath, scenario, settingsStore, cancellationToken);
        artifacts.Add(appliedDumpPath);

        IBenchmarkSession? session = null;
        try
        {
            // 2. Start the tool.
            session = await _launcher.StartAsync(
                installation,
                scenario.LaunchArguments,
                request.LaunchThroughSteam,
                cancellationToken);
            processStarted = true;
            _logger.Info($"Started benchmark process (pid {session.ProcessId}): {session.CommandLine}");

            if (!await session.WaitForWindowAsync(timings.LaunchTimeoutSeconds, cancellationToken))
            {
                failures.Add($"The benchmark window did not appear within {timings.LaunchTimeoutSeconds}s.");
            }
            else
            {
                _logger.Info($"Benchmark window is up: '{session.WindowTitle}'.");

                var readiness = await _readinessProbe.WaitUntilReadyAsync(session, timings, cancellationToken);
                _logger.Info($"Readiness: {readiness}");

                if (request.UiAutomationEnabled && _options.UiAutomation.Enabled)
                {
                    var startScript = _scriptCatalog.CreateStartScript();
                    _logger.Info($"Running the start script '{startScript.Name}' ({startScript.Steps.Count} steps).");
                    if (!await _uiAutomation.RunAsync(session, startScript, cancellationToken))
                    {
                        failures.Add("The benchmark window could not be focused for the start script.");
                    }
                }
                else
                {
                    _logger.Warning("UI automation is disabled: start the benchmark manually in the tool window.");
                    notes.Add("UI automation was disabled, so the benchmark had to be started manually.");
                }

                // 3. Wait for the result file of this pass.
                metrics = await WaitForMetricsAsync(installation, session, startedAt, timings, cancellationToken);
                if (metrics is null)
                {
                    failures.Add(
                        $"No result file appeared within {timings.BenchmarkCompletionTimeoutSeconds}s in " +
                        $"{string.Join(", ", _resultReader.GetWatchedLocations(installation))}.");
                }
                else
                {
                    _logger.Info($"Result parsed from {metrics.SourcePath}.");
                }
            }

            // 4. Collect evidence and close the tool.
            var collected = await _artifactCollector.CollectAsync(
                installation,
                runDirectory,
                startedAt.AddMinutes(-5),
                cancellationToken);
            artifacts.AddRange(collected);

            logTail = await session.ReadApplicationLogTailAsync(120, cancellationToken);

            if (request.UiAutomationEnabled && _options.UiAutomation.Enabled && session.IsRunning)
            {
                await _uiAutomation.RunAsync(session, _scriptCatalog.CreateExitScript(), cancellationToken);
            }

            if (session.IsRunning)
            {
                session.RequestClose();
                processExitedCleanly = await session.WaitForExitAsync(timings.ProcessExitTimeoutSeconds, cancellationToken);
                if (!processExitedCleanly)
                {
                    _logger.Warning("The benchmark tool did not close in time - terminating it.");
                    await session.KillAsync(timings.ProcessKillGraceSeconds, cancellationToken);
                }
            }
            else
            {
                processExitedCleanly = true;
            }
        }
        catch (OperationCanceledException)
        {
            if (session is not null)
            {
                await session.KillAsync(timings.ProcessKillGraceSeconds, CancellationToken.None);
            }

            throw;
        }
        catch (Exception exception)
        {
            _logger.Error($"The {scenario.DisplayName} failed: {exception.Message}", exception);
            failures.Add(exception.Message);
            if (session is not null)
            {
                await session.KillAsync(timings.ProcessKillGraceSeconds, CancellationToken.None);
            }
        }
        finally
        {
            if (session is not null)
            {
                await session.DisposeAsync();
            }
        }

        if (!string.IsNullOrWhiteSpace(logTail))
        {
            var logPath = Path.Combine(runDirectory, "benchmark-tool.log.txt");
            await _workspace.WriteAllTextAsync(logPath, logTail, cancellationToken);
            artifacts.Add(logPath);
        }

        string status;
        if (metrics is not null && failures.Count == 0)
        {
            status = $"Completed: {metrics.AverageFps:0.#} FPS average.";
            VerifyRecordedSettings(scenario, metrics, notes);
        }
        else
        {
            status = failures.Count > 0 ? string.Join(" ", failures) : "The benchmark did not produce a result.";
        }

        return new BenchmarkRun(
            scenario,
            metrics,
            metrics is not null && failures.Count == 0,
            status,
            startedAt,
            _clock.UtcNow,
            artifacts.Distinct().ToArray(),
            processStarted,
            processExitedCleanly,
            logTail);
    }

    private async Task<BenchmarkMetrics?> WaitForMetricsAsync(
        SteamInstallation installation,
        IBenchmarkSession session,
        DateTimeOffset since,
        BenchmarkTimings timings,
        CancellationToken cancellationToken)
    {
        var deadline = _clock.UtcNow.AddSeconds(timings.BenchmarkCompletionTimeoutSeconds);
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, timings.ResultPollIntervalSeconds));

        while (_clock.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metrics = await _resultReader.TryReadLatestAsync(installation, since, cancellationToken);
            if (metrics is not null)
            {
                // The tool may still be appending to the file: re-read after a settle delay.
                await Task.Delay(TimeSpan.FromSeconds(timings.ResultSettleSeconds), cancellationToken);
                var confirmed = await _resultReader.TryReadLatestAsync(installation, since, cancellationToken);
                return confirmed ?? metrics;
            }

            if (!session.IsRunning)
            {
                _logger.Warning("The benchmark process exited before a result file was written.");
                return await _resultReader.TryReadLatestAsync(installation, since, cancellationToken);
            }

            _logger.Debug("Waiting for the benchmark result file ...");
            await Task.Delay(pollInterval, cancellationToken);
        }

        return await _resultReader.TryReadLatestAsync(installation, since, cancellationToken);
    }

    private static void VerifyRecordedSettings(BenchmarkScenario scenario, BenchmarkMetrics metrics, List<string> notes)
    {
        if (metrics.RecordedSettings.Count == 0)
        {
            return;
        }

        foreach (var requested in scenario.Settings.UiSettings)
        {
            if (!metrics.RecordedSettings.TryGetValue(requested.Key, out var recorded))
            {
                continue;
            }

            if (!string.Equals(recorded.Trim(), requested.Value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                notes.Add(
                    $"The tool recorded {requested.Key}='{recorded}' while '{requested.Value}' was requested for the " +
                    $"{scenario.DisplayName}. Verify the graphics keys with --diagnostics.");
            }
        }
    }

    private async Task WriteSettingsDumpAsync(
        string path,
        BenchmarkScenario scenario,
        IGraphicsSettingsStore settingsStore,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {scenario.DisplayName} - settings profile '{scenario.Settings.Name}'");
        builder.AppendLine($"# Rationale: {scenario.Settings.Rationale}");
        builder.AppendLine();

        foreach (var group in settingsStore.ReadAll().GroupBy(value => value.Section))
        {
            builder.AppendLine(group.Key);
            foreach (var value in group)
            {
                builder.AppendLine($"  {value.Key} = {value.Value}");
            }

            builder.AppendLine();
        }

        await _workspace.WriteAllTextAsync(path, builder.ToString(), cancellationToken);
    }
}
