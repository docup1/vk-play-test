namespace WukongBench.Tests;

using Microsoft.Extensions.Options;
using WukongBench.Application.Configuration;
using WukongBench.Application.Models;
using WukongBench.Application.Scenarios;
using WukongBench.Domain.Models;
using Xunit;

/// <summary>
/// The scenario factory is the "settings selection" part of the task: these
/// tests pin down that the CPU pass really strips the GPU load away and that the
/// GPU pass really maximises it.
/// </summary>
public sealed class BenchmarkScenarioFactoryTests
{
    private static readonly HardwareProfile Hardware = new(
        new SystemInfo("PC", "Windows 11 Pro", "10.0.22631", "22631", "64-bit", "ASUS", "ASUS", "TUF", "1.0", "NVMe 2TB", "High performance (8c5e...)", "no"),
        new CpuInfo("AMD Ryzen 7 5800X3D", "AMD", 8, 16, "3400 MHz", "3400 MHz", "64-bit"),
        new GpuInfo("NVIDIA GeForce RTX 4070", "NVIDIA", "552.22", "2024-05-15", "12884901888 bytes (12 GB)", "GeForce", "2560 x 1440 x 144 hertz", "144"),
        new MemoryInfo("34359738368 bytes (32 GB)", "2 x 16 GB", "3600", "4096 MB"));

    private static IReadOnlyList<BenchmarkScenario> CreateScenarios(Action<WukongBenchOptions>? configure = null)
    {
        var options = new WukongBenchOptions();
        configure?.Invoke(options);

        var factory = new BenchmarkScenarioFactory(Options.Create(options));
        return factory.Create(Hardware, new BenchmarkSuiteRequest(true, true, false, true, false, true));
    }

    [Fact]
    public void CreatesTheCpuPassFirst_ThenTheGpuPass()
    {
        var scenarios = CreateScenarios();

        Assert.Equal(2, scenarios.Count);
        Assert.Equal(BenchmarkScenarioKind.Cpu, scenarios[0].Kind);
        Assert.Equal(BenchmarkScenarioKind.Gpu, scenarios[1].Kind);
        Assert.True(scenarios[0].Order < scenarios[1].Order);
    }

    [Fact]
    public void CpuPass_RemovesEverySourceOfGpuLoad()
    {
        var cpu = CreateScenarios()[0];

        Assert.Equal("1280", Value(cpu, "[/Script/Engine.GameUserSettings]", "ResolutionSizeX"));
        Assert.Equal("720", Value(cpu, "[/Script/Engine.GameUserSettings]", "ResolutionSizeY"));
        Assert.Equal("False", Value(cpu, "[/Script/Engine.GameUserSettings]", "bUseVSync"));
        Assert.Equal("0.000000", Value(cpu, "[/Script/Engine.GameUserSettings]", "FrameRateLimit"));
        Assert.Equal("25.000000", Value(cpu, "[ScalabilityGroups]", "sg.ResolutionQuality"));
        Assert.Equal("0", Value(cpu, "[ScalabilityGroups]", "sg.ShadowQuality"));
        Assert.Equal("0", Value(cpu, "[ScalabilityGroups]", "sg.GlobalIlluminationQuality"));
        Assert.Equal("2", Value(cpu, "[/Script/Engine.GameUserSettings]", "FullscreenMode")); // windowed
    }

    [Fact]
    public void CpuPass_UsesTheLowestPresetAndNoUpscalingTricks()
    {
        var cpu = CreateScenarios()[0];

        Assert.Equal("1", UiValue(cpu, "QualityLevel"));   // in-game preset 1..5
        Assert.Equal("0", Value(cpu, "[ScalabilityGroups]", "sg.ShadowQuality"));
        Assert.Equal("180", UiValue(cpu, "ImageQuality")); // 720 * 25%
        Assert.Equal("0", UiValue(cpu, "Rtx"));
        Assert.Equal("0", UiValue(cpu, "InsertFrame"));
        Assert.Equal("0", UiValue(cpu, "SuperResolutionSampling"));
        Assert.Equal("0", UiValue(cpu, "Vsync"));
    }

    [Fact]
    public void GpuPass_MaximisesTheLoadAndUsesTheNativeResolution()
    {
        var gpu = CreateScenarios()[1];

        Assert.Equal("2560", Value(gpu, "[/Script/Engine.GameUserSettings]", "ResolutionSizeX"));
        Assert.Equal("1440", Value(gpu, "[/Script/Engine.GameUserSettings]", "ResolutionSizeY"));
        Assert.Equal("0", Value(gpu, "[/Script/Engine.GameUserSettings]", "FullscreenMode")); // exclusive fullscreen
        Assert.Equal("100.000000", Value(gpu, "[ScalabilityGroups]", "sg.ResolutionQuality"));
        Assert.Equal("4", Value(gpu, "[ScalabilityGroups]", "sg.ShadowQuality")); // Cinematic = scalability 4
        Assert.Equal("5", UiValue(gpu, "QualityLevel")); // in-game preset 5
        Assert.Equal("4", UiValue(gpu, "Rtx"));
        Assert.Equal("1440", UiValue(gpu, "ImageQuality"));
        Assert.Equal("0", UiValue(gpu, "InsertFrame")); // frame generation off: real frames only
        Assert.Equal("0", UiValue(gpu, "Vsync"));
    }

    [Fact]
    public void GpuPass_FallsBackToTheConfiguredResolution_WhenNoDisplayIsDetected()
    {
        var options = new WukongBenchOptions();
        options.GpuScenario.ResolutionWidth = 1920;
        options.GpuScenario.ResolutionHeight = 1080;

        var hardware = new HardwareProfile(
            Hardware.System,
            Hardware.Cpu,
            new GpuInfo("GPU", "Vendor", "1", "2024", "8 GB", "", string.Empty, string.Empty),
            Hardware.Memory);

        var factory = new BenchmarkScenarioFactory(Options.Create(options));
        var scenarios = factory.Create(hardware, new BenchmarkSuiteRequest(true, true, false, true, false, true));

        Assert.Equal("1920", Value(scenarios[1], "[/Script/Engine.GameUserSettings]", "ResolutionSizeX"));
        Assert.Equal("1080", Value(scenarios[1], "[/Script/Engine.GameUserSettings]", "ResolutionSizeY"));
    }

    [Fact]
    public void ExtraSettings_OverrideTheDefaults()
    {
        var scenarios = CreateScenarios(options =>
        {
            options.GpuScenario.ExtraSettings.Add(new ExtraSettingOptions
            {
                Section = "[/Script/Engine.GameUserSettings]",
                Key = "FrameRateLimit",
                Value = "120.000000",
                Meaning = "cap",
            });
            options.CpuScenario.ExtraUiSettings.Add(new ExtraUiSettingOptions
            {
                Key = "Dlss",
                Value = "2",
                Meaning = "DLSS on",
            });
        });

        var cpu = scenarios[0];
        var gpu = scenarios[1];

        Assert.Equal("2", UiValue(cpu, "Dlss"));
        Assert.Contains(gpu.Settings.Values, value => value.Key == "FrameRateLimit" && value.Value == "120.000000");
    }

    [Fact]
    public void DisabledScenarios_AreNotCreated()
    {
        var scenarios = CreateScenarios(options => options.GpuScenario.Enabled = false);

        Assert.Single(scenarios);
        Assert.Equal(BenchmarkScenarioKind.Cpu, scenarios[0].Kind);
    }

    private static string? Value(BenchmarkScenario scenario, string section, string key) =>
        scenario.Settings.Values
            .FirstOrDefault(value => value.Section == section && value.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            ?.Value;

    private static string? UiValue(BenchmarkScenario scenario, string key) =>
        scenario.Settings.UiSettings
            .FirstOrDefault(value => value.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            ?.Value;
}
