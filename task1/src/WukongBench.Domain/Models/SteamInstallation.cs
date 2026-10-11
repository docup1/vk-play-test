namespace WukongBench.Domain.Models;

/// <summary>
/// A Steam application installed on this machine, resolved to a concrete folder
/// and executable. Produced by <c>ISteamInstallationLocator</c>.
/// </summary>
public sealed class SteamInstallation
{
    public SteamInstallation(
        int appId,
        string appName,
        string steamRootPath,
        string libraryPath,
        string installDirectory,
        string executablePath,
        string manifestPath,
        string launchUri,
        string buildId,
        string stateFlags)
    {
        AppId = appId;
        AppName = appName;
        SteamRootPath = steamRootPath;
        LibraryPath = libraryPath;
        InstallDirectory = installDirectory;
        ExecutablePath = executablePath;
        ManifestPath = manifestPath;
        LaunchUri = launchUri;
        BuildId = buildId;
        StateFlags = stateFlags;
    }

    /// <summary>Steam application id (3132990 for the Benchmark Tool).</summary>
    public int AppId { get; }

    /// <summary>Human readable application name taken from the app manifest.</summary>
    public string AppName { get; }

    /// <summary>Root folder of the Steam client (contains steam.exe).</summary>
    public string SteamRootPath { get; }

    /// <summary>Steam library folder that actually holds the app.</summary>
    public string LibraryPath { get; }

    /// <summary>Install directory of the application (<c>...\steamapps\common\&lt;dir&gt;</c>).</summary>
    public string InstallDirectory { get; }

    /// <summary>Full path of the shipping executable found inside the install directory.</summary>
    public string ExecutablePath { get; }

    /// <summary>Full path of <c>appmanifest_&lt;appid&gt;.acf</c>.</summary>
    public string ManifestPath { get; }

    /// <summary>URI that starts the app through the Steam client (<c>steam://rungameid/&lt;id&gt;</c>).</summary>
    public string LaunchUri { get; }

    /// <summary>Steam build id of the installed version (useful for the report).</summary>
    public string BuildId { get; }

    /// <summary>Raw <c>StateFlags</c> value from the manifest (4 = fully installed).</summary>
    public string StateFlags { get; }
}
