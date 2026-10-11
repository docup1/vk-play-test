namespace WukongBench.Infrastructure.Configuration;

/// <summary>
/// Steam discovery configuration. Infrastructure keeps its own option types so
/// that it never has to know the Application layer's configuration model;
/// the Presentation layer maps the application options onto these.
/// </summary>
public sealed class SteamLocatorOptions
{
    /// <summary>Explicit Steam root folder; skips the registry lookup.</summary>
    public string? SteamRootOverride { get; set; }

    /// <summary>
    /// Explicit path of the benchmark executable (or of its launcher). When set,
    /// the Steam library scan is skipped completely.
    /// </summary>
    public string? ExecutablePathOverride { get; set; }
}

/// <summary>Configuration for the settings file adapter.</summary>
public sealed class SettingsStoreOptions
{
    /// <summary>Explicit path of <c>GameUserSettings.ini</c>; otherwise derived from the installation.</summary>
    public string? SettingsFileOverride { get; set; }

    /// <summary>Make the ini read-only while the benchmark runs, so the tool cannot rewrite it.</summary>
    public bool LockFileDuringRun { get; set; }

    /// <summary>Files to probe below the install directory, in order of preference.</summary>
    public List<string> SettingsFileCandidates { get; set; } = new()
    {
        @"b1\Saved\Config\Windows\GameUserSettings.ini",
        @"b1\Saved_WeGame\Config\Windows\GameUserSettings.ini",
        @"Engine\Saved\Config\Windows\GameUserSettings.ini",
    };
}

/// <summary>Configuration for the benchmark result reader.</summary>
public sealed class ResultReaderOptions
{
    /// <summary>Override of the result directory (defaults to <c>%TEMP%\b1\BenchMarkHistory\Tool</c>).</summary>
    public string? HistoryDirectoryOverride { get; set; }

    /// <summary>Additional directories that are scanned for result files.</summary>
    public List<string> AdditionalDirectories { get; set; } = new();

    /// <summary>Locations of Unreal log files that are copied as evidence (searched when they exist).</summary>
    public List<string> LogFileCandidates { get; set; } = new()
    {
        @"b1\Saved\Logs\b1.log",
        @"b1\Saved\Logs\b1_benchmark.log",
    };
}
