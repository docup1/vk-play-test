namespace WukongBench.Application.Scenarios;

using System.Globalization;
using Microsoft.Extensions.Options;
using WukongBench.Application.Configuration;
using WukongBench.Application.Models;
using WukongBench.Domain.Models;

/// <summary>
/// Builds the two benchmark passes required by the task.
///
/// CPU pass  - the engine renders at a quarter of 720p, every quality group is
///             dropped to Low, ray tracing, upscaling, frame generation and
///             VSync are off and the frame rate is uncapped. The GPU finishes
///             each frame long before the CPU does, so the resulting frame rate
///             is limited by game logic, animation and draw-call submission,
///             i.e. by the processor.
///
/// GPU pass  - native monitor resolution, every quality group at Cinematic,
///             temporal upscaling at 100% screen percentage (TSR/DLAA) and full
///             ray tracing at Ultra. VSync and the frame cap are disabled so the
///             card is never idling while waiting for the display.
/// </summary>
public sealed class BenchmarkScenarioFactory
{
    private readonly WukongBenchOptions _options;

    public BenchmarkScenarioFactory(IOptions<WukongBenchOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>Creates the enabled scenarios, ordered (CPU first, GPU second).</summary>
    public IReadOnlyList<BenchmarkScenario> Create(HardwareProfile hardware, BenchmarkSuiteRequest request)
    {
        var scenarios = new List<BenchmarkScenario>();

        if (request.RunCpuPass && _options.CpuScenario.Enabled)
        {
            var cpu = _options.CpuScenario;
            scenarios.Add(CreateScenario(
                BenchmarkScenarioKind.Cpu,
                "CPU-bound pass",
                "Windowed 720p with a 25% internal render scale, every quality group at Low, " +
                "no ray tracing, no upscaling, no frame generation, VSync off and no frame cap. " +
                "The GPU has almost nothing to render, so the measured frame rate is dictated by " +
                "CPU performance (simulation, animation, draw call submission, driver overhead).",
                cpu,
                cpu.ResolutionWidth,
                cpu.ResolutionHeight,
                order: 1));
        }

        if (request.RunGpuPass && _options.GpuScenario.Enabled)
        {
            var gpu = _options.GpuScenario;
            var (width, height) = ResolveGpuResolution(hardware, gpu);
            var detectedRefresh = hardware.Gpu.CurrentRefreshRateHz;

            scenarios.Add(CreateScenario(
                BenchmarkScenarioKind.Gpu,
                "GPU-bound pass",
                $"Fullscreen {width}x{height} at 100% render scale, every quality group at Cinematic, " +
                $"temporal upscaling at DLAA quality and full ray tracing at {gpu.RayTracing}. " +
                "Frame generation is off on purpose: it fabricates intermediate frames and would " +
                "inflate the numbers instead of measuring the card. VSync is off" +
                (string.IsNullOrEmpty(detectedRefresh) ? "." : $" (display refresh: {detectedRefresh} Hz)."),
                gpu,
                width,
                height,
                order: 2));
        }

        return scenarios;
    }

    private static (int Width, int Height) ResolveGpuResolution(HardwareProfile hardware, GpuScenarioOptions scenario)
    {
        if (scenario.UseNativeResolution)
        {
            var parsed = TryParseResolution(hardware.Gpu.CurrentResolution);
            if (parsed is { } value && value.Width > 0 && value.Height > 0)
            {
                return value;
            }
        }

        return (scenario.ResolutionWidth, scenario.ResolutionHeight);
    }

    /// <summary>
    /// Parses the WMI video mode description, which looks like
    /// <c>1920 x 1080 x 60 hertz</c>, or a plain <c>2560x1440</c> string.
    /// </summary>
    private static (int Width, int Height)? TryParseResolution(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Split(new[] { 'x', 'X', '*' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var widthText = new string(parts[0].Where(char.IsDigit).ToArray());
        var heightText = new string(parts[1].Where(char.IsDigit).ToArray());

        if (int.TryParse(widthText, out var width) && int.TryParse(heightText, out var height))
        {
            return (width, height);
        }

        return null;
    }

    private static BenchmarkScenario CreateScenario(
        BenchmarkScenarioKind kind,
        string displayName,
        string rationale,
        CpuScenarioOptions scenario,
        int width,
        int height,
        int order)
    {
        var profile = new GraphicsSettingsProfile(
            name: kind == BenchmarkScenarioKind.Cpu ? "cpu-bound" : "gpu-bound",
            rationale: rationale,
            values: BuildIniValues(scenario, width, height),
            uiSettings: BuildUiSettings(scenario, width, height));

        return new BenchmarkScenario(
            kind,
            displayName,
            rationale,
            profile,
            scenario.ExtraLaunchArguments.ToArray(),
            order);
    }

    private static IReadOnlyList<GraphicsSettingValue> BuildIniValues(CpuScenarioOptions scenario, int width, int height)
    {
        const string gus = WukongSettingsKeys.GameUserSettingsSection;
        const string sg = WukongSettingsKeys.ScalabilityGroupsSection;
        const string wu = WukongSettingsKeys.WukongSection;

        var quality = (int)scenario.Quality;
        var renderHeight = (int)Math.Round(height * (scenario.ResolutionQualityPercent / 100.0));
        var renderWidth = (int)Math.Round(width * (scenario.ResolutionQualityPercent / 100.0));

        var values = new List<GraphicsSettingValue>
        {
            // ---- resolution / window ------------------------------------------------
            new(gus, WukongSettingsKeys.ResolutionSizeX, width.ToString(CultureInfo.InvariantCulture), $"Back buffer width ({width}px)"),
            new(gus, WukongSettingsKeys.ResolutionSizeY, height.ToString(CultureInfo.InvariantCulture), $"Back buffer height ({height}px)"),
            new(gus, WukongSettingsKeys.LastUserConfirmedResolutionSizeX, width.ToString(CultureInfo.InvariantCulture), "Confirms the resolution so the tool does not prompt again"),
            new(gus, WukongSettingsKeys.LastUserConfirmedResolutionSizeY, height.ToString(CultureInfo.InvariantCulture), "Confirms the resolution so the tool does not prompt again"),
            new(gus, WukongSettingsKeys.DesiredScreenWidth, width.ToString(CultureInfo.InvariantCulture), "Requested window width"),
            new(gus, WukongSettingsKeys.DesiredScreenHeight, height.ToString(CultureInfo.InvariantCulture), "Requested window height"),
            new(gus, WukongSettingsKeys.FullscreenMode, ((int)scenario.WindowMode).ToString(CultureInfo.InvariantCulture), $"Window mode ({scenario.WindowMode})"),
            new(gus, WukongSettingsKeys.LastConfirmedFullscreenMode, ((int)scenario.WindowMode).ToString(CultureInfo.InvariantCulture), "Confirms the window mode"),
            new(gus, WukongSettingsKeys.PreferredFullscreenMode, ((int)scenario.WindowMode).ToString(CultureInfo.InvariantCulture), "Window mode used on the next start"),

            // ---- synchronisation / frame limits -------------------------------------
            new(gus, WukongSettingsKeys.UseVSync, scenario.VSync ? "True" : "False", "VSync must be off, otherwise the frame rate is clamped to the display"),
            new(gus, WukongSettingsKeys.UseDynamicResolution, "False", "Dynamic resolution would silently change the render scale during the run"),
            new(gus, WukongSettingsKeys.FrameRateLimit, scenario.FrameRateLimit.ToString("0.000000", CultureInfo.InvariantCulture), "0 = uncapped frame rate"),
            new(gus, WukongSettingsKeys.ResolutionScaleNormalized, (scenario.ResolutionQualityPercent / 100.0).ToString("0.######", CultureInfo.InvariantCulture), "Normalised render scale"),

            // ---- render scale -------------------------------------------------------
            new(sg, WukongSettingsKeys.SgResolutionQuality, scenario.ResolutionQualityPercent.ToString("0.000000", CultureInfo.InvariantCulture), $"Screen percentage = {scenario.ResolutionQualityPercent}% ({renderWidth}x{renderHeight})"),

            // ---- quality groups -----------------------------------------------------
            new(sg, WukongSettingsKeys.SgViewDistanceQuality, quality.ToString(CultureInfo.InvariantCulture), $"View distance quality ({scenario.Quality})"),
            new(sg, WukongSettingsKeys.SgAntiAliasingQuality, quality.ToString(CultureInfo.InvariantCulture), $"Anti-aliasing quality ({scenario.Quality})"),
            new(sg, WukongSettingsKeys.SgShadowQuality, quality.ToString(CultureInfo.InvariantCulture), $"Shadow quality ({scenario.Quality})"),
            new(sg, WukongSettingsKeys.SgGlobalIlluminationQuality, quality.ToString(CultureInfo.InvariantCulture), $"Global illumination quality ({scenario.Quality})"),
            new(sg, WukongSettingsKeys.SgReflectionQuality, quality.ToString(CultureInfo.InvariantCulture), $"Reflection quality ({scenario.Quality})"),
            new(sg, WukongSettingsKeys.SgPostProcessQuality, quality.ToString(CultureInfo.InvariantCulture), $"Post process quality ({scenario.Quality})"),
            new(sg, WukongSettingsKeys.SgTextureQuality, quality.ToString(CultureInfo.InvariantCulture), $"Texture quality ({scenario.Quality})"),
            new(sg, WukongSettingsKeys.SgEffectsQuality, quality.ToString(CultureInfo.InvariantCulture), $"Effects quality ({scenario.Quality})"),
            new(sg, WukongSettingsKeys.SgFoliageQuality, quality.ToString(CultureInfo.InvariantCulture), $"Foliage quality ({scenario.Quality})"),
            new(sg, WukongSettingsKeys.SgShadingQuality, quality.ToString(CultureInfo.InvariantCulture), $"Shading quality ({scenario.Quality})"),

            // ---- tool's own section (mirrors UISettingData) --------------------------
            new(wu, WukongSettingsKeys.UiDx12, "1", "DirectX 12 (required by ray tracing)"),
        };

        foreach (var extra in scenario.ExtraSettings)
        {
            if (string.IsNullOrWhiteSpace(extra.Key))
            {
                continue;
            }

            values.Add(new GraphicsSettingValue(
                string.IsNullOrWhiteSpace(extra.Section) ? wu : extra.Section,
                extra.Key,
                extra.Value,
                string.IsNullOrWhiteSpace(extra.Meaning) ? "Configured extra setting" : extra.Meaning));
        }

        return values;
    }

    private static IReadOnlyList<UISettingValue> BuildUiSettings(CpuScenarioOptions scenario, int width, int height)
    {
        // The in-game preset is 1-based (1 = lowest, 5 = Cinematic), while the UE
        // scalability level is 0-based - hence "level + 1" here.
        var preset = (int)scenario.Quality + 1;
        var renderHeight = (int)Math.Round(height * (scenario.ResolutionQualityPercent / 100.0));

        var uiSettings = new List<UISettingValue>
        {
            new(WukongSettingsKeys.UiQualityLevel, preset.ToString(CultureInfo.InvariantCulture), $"In-game quality preset ({scenario.Quality})"),
            new(WukongSettingsKeys.UiViewDistance, preset.ToString(CultureInfo.InvariantCulture), "View distance"),
            new(WukongSettingsKeys.UiAntiAliasing, preset.ToString(CultureInfo.InvariantCulture), "Anti-aliasing"),
            new(WukongSettingsKeys.UiShadowQuality, preset.ToString(CultureInfo.InvariantCulture), "Shadow quality"),
            new(WukongSettingsKeys.UiPostProcessing, preset.ToString(CultureInfo.InvariantCulture), "Post processing"),
            new(WukongSettingsKeys.UiTextureQuality, preset.ToString(CultureInfo.InvariantCulture), "Texture quality"),
            new(WukongSettingsKeys.UiMaterialQuality, preset.ToString(CultureInfo.InvariantCulture), "Material quality"),
            new(WukongSettingsKeys.UiVegetationQuality, preset.ToString(CultureInfo.InvariantCulture), "Vegetation quality"),
            new(WukongSettingsKeys.UiGlobalIllumination, preset.ToString(CultureInfo.InvariantCulture), "Global illumination"),
            new(WukongSettingsKeys.UiReflectionQuality, preset.ToString(CultureInfo.InvariantCulture), "Reflection quality"),
            new(WukongSettingsKeys.UiScreenMode, ((int)scenario.WindowMode).ToString(CultureInfo.InvariantCulture), $"Screen mode ({scenario.WindowMode})"),
            new(WukongSettingsKeys.UiImageQuality, renderHeight.ToString(CultureInfo.InvariantCulture), $"Internal render height in pixels ({renderHeight}p)"),
            new(WukongSettingsKeys.UiSuperResolutionSampling, MapUpscaling(scenario.Upscaling).ToString(CultureInfo.InvariantCulture), $"Upscaling mode ({scenario.Upscaling})"),
            new(WukongSettingsKeys.UiVsync, scenario.VSync ? "1" : "0", "VSync"),
            new(WukongSettingsKeys.UiLockFrameRate, scenario.FrameRateLimit > 0 ? "1" : "0", "Frame rate cap"),
            new(WukongSettingsKeys.UiMotionBlur, scenario.MotionBlur ? "1" : "0", "Motion blur"),
            new(WukongSettingsKeys.UiInsertFrame, ((int)scenario.FrameGeneration).ToString(CultureInfo.InvariantCulture), $"Frame generation ({scenario.FrameGeneration})"),
            new(WukongSettingsKeys.UiRtx, ((int)scenario.RayTracing).ToString(CultureInfo.InvariantCulture), $"Full ray tracing ({scenario.RayTracing})"),
            new(WukongSettingsKeys.UiDx12, "1", "DirectX 12"),
        };

        foreach (var extra in scenario.ExtraUiSettings)
        {
            if (string.IsNullOrWhiteSpace(extra.Key))
            {
                continue;
            }

            uiSettings.Add(new UISettingValue(
                extra.Key,
                extra.Value,
                string.IsNullOrWhiteSpace(extra.Meaning) ? "Configured extra setting" : extra.Meaning));
        }

        return uiSettings;
    }

    /// <summary>
    /// Maps the upscaling mode onto the tool's <c>SuperResolutionSampling</c>
    /// value. 2 = DLSS and 3 = TSR were observed in a real settings file; the
    /// remaining values are ordered by elimination and can be corrected through
    /// <c>UiAutomation:ExtraUiSettings</c> if a future build numbers them differently.
    /// </summary>
    private static int MapUpscaling(UpscalingMode mode) => mode switch
    {
        UpscalingMode.Off => 0,
        UpscalingMode.Fsr => 1,
        UpscalingMode.Dlss => 2,
        UpscalingMode.Tsr => 3,
        UpscalingMode.XeSs => 4,
        _ => 0,
    };
}
