namespace WukongBench.Tests;

using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Configuration;
using WukongBench.Infrastructure.Settings;
using WukongBench.Tests.Fakes;
using Xunit;

/// <summary>
/// The settings file is the input side of the automation, so these tests pin
/// down the exact behaviour: standard UE keys are written, the tool's own
/// UISettingData tuple is merged without losing unrelated entries, and a backup
/// can be restored byte for byte.
/// </summary>
public sealed class UeGameUserSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "wukongbench-tests", Guid.NewGuid().ToString("N"));

    private const string OriginalSettings = """
        [/Script/Engine.GameUserSettings]
        bUseVSync=True
        ResolutionSizeX=2560
        ResolutionSizeY=1440
        FullscreenMode=1
        FrameRateLimit=60.000000

        [/Script/GSGameSettings.GSGameUserSettings]
        UISettingData=(("ScreenMode", "1"),("ScreenResolution", "0"),("ImageQuality", "720"),("SuperResolutionSampling", "3"),("Vsync", "1"),("LockFrameRate", "1"),("MotionBlur", "1"),("InsertFrame", "1"),("Rtx", "0"),("QualityLevel", "3"),("ViewDistance", "3"),("GlobalIllumination", "3"),("ReflectionQuality", "3"),("Brightness", "50"))
        bUseHDRDisplayOutput=False

        [ScalabilityGroups]
        sg.ResolutionQuality=67
        sg.ViewDistanceQuality=2
        """;

    public UeGameUserSettingsStoreTests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsPath, OriginalSettings);
    }

    private string SettingsPath => Path.Combine(_directory, "GameUserSettings.ini");

    [Fact]
    public void DefaultCandidateList_PointsAtTheInstallFolderInsideSteam()
    {
        var candidates = new SettingsStoreOptions().SettingsFileCandidates;

        Assert.Contains(candidates, candidate =>
            candidate.Replace('\\', '/').EndsWith("b1/Saved/Config/Windows/GameUserSettings.ini", StringComparison.OrdinalIgnoreCase));
    }

    private UeGameUserSettingsStore CreateStore(SettingsStoreOptions? options = null)
    {
        options ??= new SettingsStoreOptions { SettingsFileOverride = SettingsPath };
        var installation = new SteamInstallation(
            3132990, "Black Myth: Wukong Benchmark Tool", _directory, _directory, _directory,
            Path.Combine(_directory, "b1_benchmark.exe"), string.Empty, "steam://rungameid/3132990", "1", "4");

        return new UeGameUserSettingsStore(installation, options, new RecordingLogger());
    }

    [Fact]
    public void Apply_WritesStandardKeys_AndPreservesTheRestOfTheFile()
    {
        var store = CreateStore();
        var profile = new GraphicsSettingsProfile(
            "cpu-bound",
            "test",
            new[]
            {
                new GraphicsSettingValue("[/Script/Engine.GameUserSettings]", "bUseVSync", "False", "vsync"),
                new GraphicsSettingValue("[/Script/Engine.GameUserSettings]", "ResolutionSizeX", "1280", "width"),
                new GraphicsSettingValue("[ScalabilityGroups]", "sg.ResolutionQuality", "25.000000", "scale"),
                new GraphicsSettingValue("[ScalabilityGroups]", "sg.ShadowQuality", "0", "shadows"),
            },
            Array.Empty<UISettingValue>());

        store.Apply(profile);
        var text = File.ReadAllText(SettingsPath);

        Assert.Contains("bUseVSync=False", text);
        Assert.Contains("ResolutionSizeX=1280", text);
        Assert.Contains("sg.ResolutionQuality=25.000000", text);
        Assert.Contains("sg.ShadowQuality=0", text);

        // Untouched content must survive.
        Assert.Contains("ResolutionSizeY=1440", text);
        Assert.Contains("FrameRateLimit=60.000000", text);
        Assert.Contains("bUseHDRDisplayOutput=False", text);
        Assert.Contains("Brightness", text);
    }

    [Fact]
    public void Apply_MergesUiSettingData_AndKeepsUnknownEntries()
    {
        var store = CreateStore();
        var profile = new GraphicsSettingsProfile(
            "gpu-bound",
            "test",
            Array.Empty<GraphicsSettingValue>(),
            new[]
            {
                new UISettingValue("QualityLevel", "5", "preset"),
                new UISettingValue("Rtx", "4", "ray tracing"),
                new UISettingValue("InsertFrame", "0", "frame generation"),
                new UISettingValue("BrandNewKey", "42", "new entry"),
            });

        store.Apply(profile);
        var text = File.ReadAllText(SettingsPath);

        Assert.Contains("(\"QualityLevel\", \"5\")", text);
        Assert.Contains("(\"Rtx\", \"4\")", text);
        Assert.Contains("(\"InsertFrame\", \"0\")", text);
        Assert.Contains("(\"BrandNewKey\", \"42\")", text);

        // Entries that were not requested are untouched.
        Assert.Contains("(\"Brightness\", \"50\")", text);
        Assert.Contains("(\"ScreenResolution\", \"0\")", text);

        // Exactly one UISettingData key exists.
        Assert.Equal(1, CountOccurrences(text, "UISettingData="));
    }

    [Fact]
    public void Apply_CreatesTheFileAndSection_WhenNothingExists()
    {
        File.Delete(SettingsPath);
        var store = CreateStore();

        store.Apply(new GraphicsSettingsProfile(
            "cpu-bound",
            "test",
            new[] { new GraphicsSettingValue("[ScalabilityGroups]", "sg.ResolutionQuality", "25.000000", "scale") },
            new[] { new UISettingValue("QualityLevel", "1", "preset") }));

        Assert.True(File.Exists(SettingsPath));
        var text = File.ReadAllText(SettingsPath);
        Assert.Contains("[ScalabilityGroups]", text);
        Assert.Contains("sg.ResolutionQuality=25.000000", text);
        Assert.Contains("[/Script/GSGameSettings.GSGameUserSettings]", text);
        Assert.Contains("(\"QualityLevel\", \"1\")", text);
    }

    [Fact]
    public void BackupAndRestore_ReturnsTheFileToItsOriginalState()
    {
        var store = CreateStore();
        var backup = Path.Combine(_directory, "backup.ini");

        Assert.True(store.TryBackup(backup, out _));

        store.Apply(new GraphicsSettingsProfile(
            "cpu-bound",
            "test",
            new[] { new GraphicsSettingValue("[/Script/Engine.GameUserSettings]", "bUseVSync", "False", "vsync") },
            Array.Empty<UISettingValue>()));
        Assert.NotEqual(OriginalSettings, File.ReadAllText(SettingsPath));

        Assert.True(store.TryRestore(backup, out _));
        Assert.Equal(OriginalSettings, File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void ReadAll_ReturnsEveryKeyOfTheFile()
    {
        var store = CreateStore();
        var entries = store.ReadAll();

        Assert.Contains(entries, entry => entry.Key == "bUseVSync" && entry.Value == "True");
        Assert.Contains(entries, entry => entry.Key == "sg.ResolutionQuality" && entry.Value == "67");
        Assert.Contains(entries, entry => entry.Key == "UISettingData");
    }

    private static int CountOccurrences(string text, string needle)
    {
        var count = 0;
        var index = text.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = text.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }
}
