namespace WukongBench.Domain.Models;

/// <summary>
/// Result of the "check my machine" mode: what the tool found, what it would
/// change, and where it will look for benchmark results. Useful on a new PC
/// where the Wukong-specific ini key names must be confirmed once.
/// </summary>
public sealed class DiagnosticsSnapshot
{
    public DiagnosticsSnapshot(
        SteamInstallation? installation,
        HardwareProfile hardware,
        string settingsFilePath,
        bool settingsFileExists,
        IReadOnlyList<GraphicsSettingValue> currentSettings,
        IReadOnlyList<string> watchedResultLocations,
        IReadOnlyList<string> existingResultFiles,
        IReadOnlyList<string> benchmarkLogFiles,
        IReadOnlyList<string> notes)
    {
        Installation = installation;
        Hardware = hardware;
        SettingsFilePath = settingsFilePath;
        SettingsFileExists = settingsFileExists;
        CurrentSettings = currentSettings;
        WatchedResultLocations = watchedResultLocations;
        ExistingResultFiles = existingResultFiles;
        BenchmarkLogFiles = benchmarkLogFiles;
        Notes = notes;
    }

    public SteamInstallation? Installation { get; }
    public HardwareProfile Hardware { get; }
    public string SettingsFilePath { get; }
    public bool SettingsFileExists { get; }

    /// <summary>Every key currently present in the settings file ("/script/..." style inventory).</summary>
    public IReadOnlyList<GraphicsSettingValue> CurrentSettings { get; }

    /// <summary>Directories that are scanned for benchmark result files.</summary>
    public IReadOnlyList<string> WatchedResultLocations { get; }

    /// <summary>Result files that already exist on disk.</summary>
    public IReadOnlyList<string> ExistingResultFiles { get; }

    /// <summary>Benchmark tool log files that already exist on disk.</summary>
    public IReadOnlyList<string> BenchmarkLogFiles { get; }

    public IReadOnlyList<string> Notes { get; }
}
