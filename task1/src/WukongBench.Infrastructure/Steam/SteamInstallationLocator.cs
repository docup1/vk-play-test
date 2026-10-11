namespace WukongBench.Infrastructure.Steam;

using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Configuration;

/// <summary>
/// Finds the benchmark tool through the local Steam installation:
/// registry -> <c>libraryfolders.vdf</c> -> <c>appmanifest_&lt;id&gt;.acf</c> -> install folder.
/// An explicit executable path in the configuration always wins.
/// </summary>
public sealed class SteamInstallationLocator : ISteamInstallationLocator
{
    private const string SteamRegistryKey = @"Software\Valve\Steam";

    private readonly SteamLocatorOptions _options;

    public SteamInstallationLocator(IOptions<SteamLocatorOptions> options)
    {
        _options = options.Value;
    }

    public SteamInstallation? Locate(int steamAppId, out string diagnostics)
    {
        var log = new StringBuilder();

        var overridePath = _options.ExecutablePathOverride;
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (File.Exists(overridePath))
            {
                log.AppendLine($"Using the configured executable path '{overridePath}'.");
                diagnostics = log.ToString();
                return BuildFromExecutableOverride(steamAppId, overridePath, log);
            }

            log.AppendLine($"The configured executable path '{overridePath}' does not exist.");
        }

        var steamRoots = EnumerateSteamRoots(log).ToArray();
        if (steamRoots.Length == 0)
        {
            log.AppendLine("No Steam installation was found (registry and default folders).");
            diagnostics = log.ToString();
            return null;
        }

        foreach (var steamRoot in steamRoots)
        {
            log.AppendLine($"Steam root: {steamRoot}");

            foreach (var library in EnumerateLibraries(steamRoot, log))
            {
                var steamApps = Path.Combine(library, "steamapps");
                var manifestPath = Path.Combine(steamApps, $"appmanifest_{steamAppId}.acf");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                log.AppendLine($"Manifest found: {manifestPath}");

                var manifest = ValveKeyValues.ParseFile(manifestPath);
                var installFolderName = manifest["installdir"];
                if (string.IsNullOrWhiteSpace(installFolderName))
                {
                    log.AppendLine("The manifest does not contain an 'installdir' entry.");
                    continue;
                }

                var installDirectory = Path.Combine(steamApps, "common", installFolderName);
                if (!Directory.Exists(installDirectory))
                {
                    log.AppendLine($"Install directory '{installDirectory}' does not exist.");
                    continue;
                }

                var stateFlags = manifest["StateFlags"] ?? string.Empty;
                if (stateFlags.Length > 0 && stateFlags != "4")
                {
                    log.AppendLine($"StateFlags={stateFlags}: Steam reports the app as not fully installed (4 = installed).");
                }

                var executable = FindExecutable(installDirectory, log);
                if (executable is null)
                {
                    log.AppendLine($"No benchmark executable was found below '{installDirectory}'.");
                    continue;
                }

                diagnostics = log.ToString();
                return new SteamInstallation(
                    steamAppId,
                    manifest["name"] ?? "Black Myth: Wukong Benchmark Tool",
                    steamRoot,
                    library,
                    installDirectory,
                    executable,
                    manifestPath,
                    $"steam://rungameid/{steamAppId}",
                    manifest["buildid"] ?? string.Empty,
                    stateFlags);
            }
        }

        log.AppendLine($"Steam app {steamAppId} was not found in any library.");
        diagnostics = log.ToString();
        return null;
    }

    private SteamInstallation BuildFromExecutableOverride(int steamAppId, string executablePath, StringBuilder log)
    {
        var fullPath = Path.GetFullPath(executablePath);
        var installDirectory = Path.GetDirectoryName(fullPath) ?? fullPath;

        // When the configured path points at the inner UE binary, walk up to the
        // folder that contains the launcher so the report shows a useful root.
        var probe = installDirectory;
        for (var i = 0; i < 4 && !string.IsNullOrEmpty(probe); i++)
        {
            if (File.Exists(Path.Combine(probe, "b1_benchmark.exe")))
            {
                installDirectory = probe;
                break;
            }

            probe = Path.GetDirectoryName(probe);
        }

        var library = FindLibraryRoot(installDirectory) ?? installDirectory;
        var steamRoot = FindSteamRootFor(library) ?? string.Empty;

        return new SteamInstallation(
            steamAppId,
            "Black Myth: Wukong Benchmark Tool (configured path)",
            steamRoot,
            library,
            installDirectory,
            fullPath,
            string.Empty,
            $"steam://rungameid/{steamAppId}",
            string.Empty,
            "override");
    }

    private static string? FindLibraryRoot(string path)
    {
        var probe = path;
        for (var i = 0; i < 6 && !string.IsNullOrEmpty(probe); i++)
        {
            if (string.Equals(Path.GetFileName(probe), "common", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Path.GetFileName(Path.GetDirectoryName(probe) ?? string.Empty), "steamapps", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetDirectoryName(Path.GetDirectoryName(probe));
            }

            probe = Path.GetDirectoryName(probe);
        }

        return null;
    }

    private static string? FindSteamRootFor(string library)
    {
        var steamExe = Path.Combine(library, "steam.exe");
        return File.Exists(steamExe) ? library : null;
    }

    private IEnumerable<string> EnumerateSteamRoots(StringBuilder log)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(_options.SteamRootOverride))
        {
            candidates.Add(_options.SteamRootOverride);
        }

        foreach (var fromRegistry in ReadSteamRootsFromRegistry(log))
        {
            candidates.Add(fromRegistry);
        }

        candidates.Add(@"C:\Program Files (x86)\Steam");
        candidates.Add(@"C:\Program Files\Steam");

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            string full;
            try
            {
                full = Path.GetFullPath(candidate);
            }
            catch (Exception exception)
            {
                log.AppendLine($"Invalid Steam path '{candidate}': {exception.Message}");
                continue;
            }

            if (Directory.Exists(full))
            {
                yield return full;
            }
        }
    }

    private static IEnumerable<string> ReadSteamRootsFromRegistry(StringBuilder log)
    {
        var results = new List<string>();

        void TryRead(RegistryKey root, string subKey, string valueName)
        {
            try
            {
                using var key = root.OpenSubKey(subKey);
                if (key?.GetValue(valueName) is string value && !string.IsNullOrWhiteSpace(value))
                {
                    results.Add(value);
                }
            }
            catch (Exception exception)
            {
                log.AppendLine($"Registry read failed ({subKey}): {exception.Message}");
            }
        }

        TryRead(Registry.CurrentUser, SteamRegistryKey, "SteamPath");
        TryRead(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
        TryRead(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath");

        return results;
    }

    private static IEnumerable<string> EnumerateLibraries(string steamRoot, StringBuilder log)
    {
        yield return steamRoot;

        foreach (var vdfPath in new[]
                 {
                     Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf"),
                     Path.Combine(steamRoot, "config", "libraryfolders.vdf"),
                 })
        {
            if (!File.Exists(vdfPath))
            {
                continue;
            }

            log.AppendLine($"Library file: {vdfPath}");

            ValveKeyValues document;
            try
            {
                document = ValveKeyValues.ParseFile(vdfPath);
            }
            catch (Exception exception)
            {
                log.AppendLine($"Could not parse '{vdfPath}': {exception.Message}");
                continue;
            }

            foreach (var entry in document.ChildValues())
            {
                var path = entry["path"];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var normalised = path.Replace("\\\\", "\\");
                if (Directory.Exists(normalised) &&
                    !normalised.Equals(steamRoot, StringComparison.OrdinalIgnoreCase))
                {
                    yield return normalised;
                }
            }
        }
    }

    /// <summary>
    /// Locates the benchmark executable: the launcher in the install root is
    /// preferred (it is what Steam itself starts), then the Unreal binary under
    /// <c>b1\Binaries\Win64</c>.
    /// </summary>
    private static string? FindExecutable(string installDirectory, StringBuilder log)
    {
        var launcher = Path.Combine(installDirectory, "b1_benchmark.exe");
        if (File.Exists(launcher))
        {
            return launcher;
        }

        log.AppendLine("b1_benchmark.exe was not found in the install root, falling back to the Unreal binary.");

        var binaries = Path.Combine(installDirectory, "b1", "Binaries", "Win64");
        if (Directory.Exists(binaries))
        {
            var preferred = new[] { "b1-Win64-Shipping.exe", "b1.exe" };
            foreach (var name in preferred)
            {
                var candidate = Path.Combine(binaries, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            try
            {
                var any = Directory.EnumerateFiles(binaries, "*.exe").FirstOrDefault();
                if (any is not null)
                {
                    return any;
                }
            }
            catch (Exception exception)
            {
                log.AppendLine($"Could not enumerate '{binaries}': {exception.Message}");
            }
        }

        return null;
    }
}
