namespace WukongBench.Application.Configuration;

using WukongBench.Domain.Models;

/// <summary>
/// Root configuration of the tool, bound from <c>appsettings.json</c> and from
/// command line switches by the Presentation layer.
/// </summary>
public sealed class WukongBenchOptions
{
    public const string SectionName = "WukongBench";

    /// <summary>Steam application id of the Black Myth: Wukong Benchmark Tool.</summary>
    public int SteamAppId { get; set; } = SteamAppIds.BenchmarkTool;

    /// <summary>Optional explicit path to the Steam library root that holds the tool.</summary>
    public string? SteamRootOverride { get; set; }

    /// <summary>
    /// Optional explicit path of the benchmark executable. When set, Steam
    /// discovery is skipped entirely.
    /// </summary>
    public string? ExecutablePathOverride { get; set; }

    /// <summary>
    /// Start the tool through the Steam client (<c>steam.exe -applaunch 3132990</c>).
    /// This is the default because the tool expects the Steam context; a direct
    /// start of <c>b1_benchmark.exe</c> makes it re-launch itself through Steam
    /// and show a confirmation dialog.
    /// </summary>
    public bool LaunchThroughSteam { get; set; } = true;

    /// <summary>Switch to the "High performance" power scheme during the suite.</summary>
    public bool UseHighPerformancePowerScheme { get; set; } = true;

    /// <summary>Folder that receives the report, the raw results and the logs.</summary>
    public string ArtifactsDirectory { get; set; } = "artifacts";

    /// <summary>Report title.</summary>
    public string ToolName { get; set; } = "wukongbench";

    /// <summary>Report version.</summary>
    public string ToolVersion { get; set; } = "1.0.0";

    /// <summary>Timeout and delay configuration.</summary>
    public BenchmarkTimingsOptions Timings { get; set; } = new();

    /// <summary>Settings used for the CPU-bound pass.</summary>
    public CpuScenarioOptions CpuScenario { get; set; } = new();

    /// <summary>Settings used for the GPU-bound pass.</summary>
    public GpuScenarioOptions GpuScenario { get; set; } = new();

    /// <summary>Keyboard / mouse automation configuration.</summary>
    public UiAutomationOptions UiAutomation { get; set; } = new();
}

/// <summary>Times, in seconds, that the automation waits for.</summary>
public sealed class BenchmarkTimingsOptions
{
    public int LaunchTimeoutSeconds { get; set; } = 240;
    public int MainMenuSettleSeconds { get; set; } = 40;
    public int BenchmarkCompletionTimeoutSeconds { get; set; } = 900;
    public int ResultPollIntervalSeconds { get; set; } = 5;
    public int ResultSettleSeconds { get; set; } = 8;
    public int ProcessExitTimeoutSeconds { get; set; } = 60;
    public int ProcessKillGraceSeconds { get; set; } = 15;
}

/// <summary>
/// One extra (game specific) standard ini key. Unknown keys are ignored by
/// Unreal, so this is a safe escape hatch for tool builds whose key names differ.
/// </summary>
public sealed class ExtraSettingOptions
{
    public string Section { get; set; } = "[/Script/GSGameSettings.GSGameUserSettings]";

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Meaning { get; set; } = string.Empty;
}

/// <summary>One extra entry merged into the tool's <c>UISettingData</c> tuple.</summary>
public sealed class ExtraUiSettingOptions
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Meaning { get; set; } = string.Empty;
}

/// <summary>Settings of the CPU-bound pass. Also the base class of the GPU pass.</summary>
public class CpuScenarioOptions
{
    public bool Enabled { get; set; } = true;

    public int ResolutionWidth { get; set; } = 1280;

    public int ResolutionHeight { get; set; } = 720;

    /// <summary>Internal render scale in percent (<c>sg.ResolutionQuality</c>).</summary>
    public int ResolutionQualityPercent { get; set; } = 25;

    public WindowMode WindowMode { get; set; } = WindowMode.Windowed;

    /// <summary>Quality preset. <c>Low</c> = in-game preset 1, <c>Cinematic</c> = preset 5.</summary>
    public QualityLevel Quality { get; set; } = QualityLevel.Low;

    public UpscalingMode Upscaling { get; set; } = UpscalingMode.Off;

    public UpscalingQuality UpscalingQuality { get; set; } = UpscalingQuality.UltraPerformance;

    public FrameGenerationMode FrameGeneration { get; set; } = FrameGenerationMode.Off;

    public RayTracingMode RayTracing { get; set; } = RayTracingMode.Off;

    public bool VSync { get; set; }

    public int FrameRateLimit { get; set; }

    /// <summary>Motion blur is a pure GPU cost and never measured; off by default.</summary>
    public bool MotionBlur { get; set; }

    public List<ExtraSettingOptions> ExtraSettings { get; set; } = new();

    public List<ExtraUiSettingOptions> ExtraUiSettings { get; set; } = new();

    public List<string> ExtraLaunchArguments { get; set; } = new();
}

/// <summary>Settings of the GPU-bound pass.</summary>
public sealed class GpuScenarioOptions : CpuScenarioOptions
{
    public GpuScenarioOptions()
    {
        ResolutionWidth = 3840;
        ResolutionHeight = 2160;
        ResolutionQualityPercent = 100;
        WindowMode = WindowMode.Fullscreen;
        Quality = QualityLevel.Cinematic;
        Upscaling = UpscalingMode.Tsr;
        UpscalingQuality = UpscalingQuality.Dlaa;
        FrameGeneration = FrameGenerationMode.Off;
        RayTracing = RayTracingMode.Ultra;
        VSync = false;
        FrameRateLimit = 0;
    }

    /// <summary>Use the monitor resolution detected on this machine instead of the configured one.</summary>
    public bool UseNativeResolution { get; set; } = true;

    /// <summary>
    /// Force DirectX 12 (required by ray tracing) by writing <c>Dx12=1</c>.
    /// </summary>
    public bool ForceDirectX12 { get; set; } = true;
}

/// <summary>Keyboard / mouse automation configuration.</summary>
public sealed class UiAutomationOptions
{
    /// <summary>
    /// When <c>false</c> the tool never sends input: the operator starts the
    /// benchmark manually and the tool only waits for the result file.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Steps executed once the main menu is expected to be ready.
    /// The main menu of the benchmark tool does not react to the keyboard before
    /// the mouse hovers it, therefore the default script hovers the centre of the
    /// window, then confirms the pre-selected "Benchmark Test" entry and the
    /// confirmation dialog.
    /// </summary>
    public List<UiStepOptions> StartScript { get; set; } = new()
    {
        new UiStepOptions
        {
            Action = UiActionKind.MouseMoveAndClick,
            NormalizedX = 0.5,
            NormalizedY = 0.5,
            HoldMilliseconds = 60,
            DelayAfterMilliseconds = 2500,
            Description = "Hover/click the centre of the main menu so the UI accepts input",
        },
        new UiStepOptions
        {
            Action = UiActionKind.KeyPress,
            Key = VirtualKey.Enter,
            HoldMilliseconds = 60,
            DelayAfterMilliseconds = 2500,
            Description = "Confirm the pre-selected 'Benchmark Test' entry",
        },
        new UiStepOptions
        {
            Action = UiActionKind.KeyPress,
            Key = VirtualKey.Enter,
            HoldMilliseconds = 60,
            DelayAfterMilliseconds = 2500,
            Description = "Confirm the start dialog",
        },
        new UiStepOptions
        {
            Action = UiActionKind.Wait,
            DelayAfterMilliseconds = 1500,
            Description = "Hand control back to the benchmark",
        },
    };

    /// <summary>Steps executed after the results were read, before the tool is closed.</summary>
    public List<UiStepOptions> ExitScript { get; set; } = new()
    {
        new UiStepOptions
        {
            Action = UiActionKind.KeyPress,
            Key = VirtualKey.Escape,
            HoldMilliseconds = 60,
            DelayAfterMilliseconds = 1500,
            Description = "Leave the results screen",
        },
    };
}

/// <summary>One scripted interaction, bound from configuration.</summary>
public sealed class UiStepOptions
{
    public UiActionKind Action { get; set; } = UiActionKind.Wait;

    public VirtualKey Key { get; set; } = VirtualKey.None;

    public double NormalizedX { get; set; }

    public double NormalizedY { get; set; }

    public int HoldMilliseconds { get; set; } = 60;

    public int DelayAfterMilliseconds { get; set; } = 1000;

    public string Description { get; set; } = string.Empty;
}
