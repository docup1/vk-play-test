namespace WukongBench.Tests;

using Microsoft.Extensions.Options;
using WukongBench.Infrastructure.Configuration;
using WukongBench.Infrastructure.Steam;
using WukongBench.Tests.Fakes;
using Xunit;

/// <summary>
/// Steam discovery: the tool walks registry -> libraryfolders.vdf ->
/// appmanifest_&lt;id&gt;.acf -> install folder, so a fake library is enough to
/// verify the whole chain.
/// </summary>
public sealed class SteamInstallationLocatorTests : IDisposable
{
    private readonly string _steamRoot = Path.Combine(Path.GetTempPath(), "wukongbench-tests", Guid.NewGuid().ToString("N"), "Steam");
    private readonly string _libraryRoot;

    private const int AppId = 3132990;

    public SteamInstallationLocatorTests()
    {
        _libraryRoot = Path.Combine(Path.GetDirectoryName(_steamRoot)!, "SteamLibrary");
        Directory.CreateDirectory(Path.Combine(_steamRoot, "steamapps"));
        Directory.CreateDirectory(Path.Combine(_libraryRoot, "steamapps", "common"));

        File.WriteAllText(Path.Combine(_steamRoot, "steam.exe"), string.Empty);

        File.WriteAllText(
            Path.Combine(_steamRoot, "steamapps", "libraryfolders.vdf"),
            $$"""
            "libraryfolders"
            {
            	"0"
            	{
            		"path"		"{{_steamRoot.Replace("\\", "\\\\")}}"
            		"label"		""
            	}
            	"1"
            	{
            		"path"		"{{_libraryRoot.Replace("\\", "\\\\")}}"
            		"label"		"games"
            	}
            }
            """);

        var installDirectory = Path.Combine(_libraryRoot, "steamapps", "common", "Black Myth Wukong Benchmark Tool");
        Directory.CreateDirectory(Path.Combine(installDirectory, "b1", "Binaries", "Win64"));
        File.WriteAllText(Path.Combine(installDirectory, "b1_benchmark.exe"), string.Empty);

        File.WriteAllText(
            Path.Combine(_libraryRoot, "steamapps", $"appmanifest_{AppId}.acf"),
            $$"""
            "AppState"
            {
            	"appid"		"{{AppId}}"
            	"name"		"Black Myth: Wukong Benchmark Tool"
            	"StateFlags"		"4"
            	"installdir"		"Black Myth Wukong Benchmark Tool"
            	"buildid"		"15365379"
            }
            """);
    }

    private SteamInstallationLocator CreateLocator(string? executableOverride = null) =>
        new(Options.Create(new SteamLocatorOptions
        {
            SteamRootOverride = _steamRoot,
            ExecutablePathOverride = executableOverride,
        }));

    [Fact]
    public void Locate_FindsTheApplicationThroughTheLibraryConfiguration()
    {
        var installation = CreateLocator().Locate(AppId, out var diagnostics);

        Assert.NotNull(installation);
        Assert.Equal(AppId, installation!.AppId);
        Assert.Equal("Black Myth: Wukong Benchmark Tool", installation.AppName);
        Assert.Equal("15365379", installation.BuildId);
        Assert.Equal("4", installation.StateFlags);
        Assert.Equal($"steam://rungameid/{AppId}", installation.LaunchUri);
        Assert.EndsWith("b1_benchmark.exe", installation.ExecutablePath);
        Assert.Equal(_libraryRoot, installation.LibraryPath);
        Assert.Contains("libraryfolders.vdf", diagnostics);
    }

    [Fact]
    public void Locate_ReturnsNull_WhenTheAppIsNotInstalled()
    {
        var installation = CreateLocator().Locate(999999, out var diagnostics);

        Assert.Null(installation);
        Assert.Contains("999999", diagnostics);
    }

    [Fact]
    public void Locate_HonoursTheConfiguredExecutablePath()
    {
        var explicitPath = Path.Combine(_libraryRoot, "steamapps", "common", "Black Myth Wukong Benchmark Tool", "b1_benchmark.exe");

        var installation = CreateLocator(explicitPath).Locate(AppId, out _);

        Assert.NotNull(installation);
        Assert.Equal(Path.GetFullPath(explicitPath), installation!.ExecutablePath);
    }

    public void Dispose()
    {
        try
        {
            var root = Path.GetDirectoryName(_steamRoot);
            if (root is not null && Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }
}
