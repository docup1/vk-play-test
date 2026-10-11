namespace WukongBench.Domain.Abstractions;


/// <summary>
/// Finds a Steam application on the local machine by walking the Steam library
/// configuration and the app manifest. Implemented in Infrastructure.
/// </summary>
public interface ISteamInstallationLocator
{
    /// <summary>
    /// Locates the application. Returns <c>null</c> when Steam or the app is not
    /// installed / not fully downloaded.
    /// </summary>
    SteamInstallation? Locate(int steamAppId, out string diagnostics);
}
