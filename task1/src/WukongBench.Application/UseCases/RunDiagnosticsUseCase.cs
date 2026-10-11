namespace WukongBench.Application.UseCases;

using Microsoft.Extensions.Options;
using WukongBench.Application.Configuration;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;

/// <summary>
/// "Check this machine" mode. It never starts the benchmark: it only reports the
/// located installation, the hardware, the settings keys that are currently in
/// the tool's ini file and the folders where results will be looked for. This is
/// the quick way to confirm the graphics key names on a machine before the first
/// real run.
/// </summary>
public sealed class RunDiagnosticsUseCase
{
    private readonly ISteamInstallationLocator _installationLocator;
    private readonly IGraphicsSettingsStoreFactory _settingsStoreFactory;
    private readonly IBenchmarkResultReader _resultReader;
    private readonly IHardwareInfoProvider _hardwareInfoProvider;
    private readonly IToolLogger _logger;
    private readonly WukongBenchOptions _options;

    public RunDiagnosticsUseCase(
        ISteamInstallationLocator installationLocator,
        IGraphicsSettingsStoreFactory settingsStoreFactory,
        IBenchmarkResultReader resultReader,
        IHardwareInfoProvider hardwareInfoProvider,
        IOptions<WukongBenchOptions> options,
        IToolLogger logger)
    {
        _installationLocator = installationLocator;
        _settingsStoreFactory = settingsStoreFactory;
        _resultReader = resultReader;
        _hardwareInfoProvider = hardwareInfoProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DiagnosticsSnapshot> ExecuteAsync(CancellationToken cancellationToken)
    {
        var notes = new List<string>();

        _logger.Info("Reading hardware profile ...");
        var hardware = await _hardwareInfoProvider.ReadAsync(cancellationToken);

        var installation = _installationLocator.Locate(_options.SteamAppId, out var diagnostics);
        if (installation is null)
        {
            notes.Add($"The benchmark tool could not be located: {diagnostics}");
        }

        var settingsFilePath = string.Empty;
        var settingsFileExists = false;
        var currentSettings = new List<GraphicsSettingValue>();
        var watched = new List<string>();
        var existingResults = new List<string>();
        var existingLogs = new List<string>();

        if (installation is not null)
        {
            var store = _settingsStoreFactory.Create(installation);
            settingsFilePath = store.SettingsFilePath;
            settingsFileExists = store.Exists;
            if (settingsFileExists)
            {
                currentSettings.AddRange(store.ReadAll());
            }
            else
            {
                notes.Add(
                    "The settings file does not exist yet. Run the benchmark tool once, change the graphics options " +
                    "and close it, then repeat --diagnostics to see the real key names of your tool build.");
            }

            watched.AddRange(_resultReader.GetWatchedLocations(installation));
            existingResults.AddRange(_resultReader.FindExistingResultFiles(installation, DateTimeOffset.MinValue));
            existingLogs.AddRange(_resultReader.FindExistingLogFiles(installation));

            if (existingResults.Count == 0)
            {
                notes.Add("No benchmark result file was found: the benchmark has probably never been completed on this machine.");
            }
        }

        notes.Add("Add or correct key names under WukongBench:CpuScenario/GpuScenario:ExtraSettings and :ExtraUiSettings in appsettings.json.");

        return new DiagnosticsSnapshot(
            installation,
            hardware,
            settingsFilePath,
            settingsFileExists,
            currentSettings,
            watched,
            existingResults,
            existingLogs,
            notes);
    }
}
