namespace WukongBench.Domain.Models;

/// <summary>
/// The two benchmark passes required by the task.
/// </summary>
public enum BenchmarkScenarioKind
{
    /// <summary>CPU-bound pass: settings are chosen so the GPU is (almost) never the bottleneck.</summary>
    Cpu = 1,

    /// <summary>GPU-bound pass: settings are chosen to maximise the load on the video card.</summary>
    Gpu = 2,
}

/// <summary>
/// Unreal Engine <c>FullscreenMode</c> values (mirrors <c>EWindowMode</c>).
/// </summary>
public enum WindowMode
{
    Fullscreen = 0,
    WindowedFullscreen = 1,
    Windowed = 2,
}

/// <summary>
/// Unreal Engine scalability levels (mirrors <c>EQualityLevel</c>).
/// </summary>
public enum QualityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Epic = 3,
    Cinematic = 4,
}

/// <summary>
/// Temporal upscaler / super resolution mode used by the benchmark tool.
/// </summary>
public enum UpscalingMode
{
    /// <summary>Native rendering, no upscaling (heaviest for the GPU).</summary>
    Off = 0,

    /// <summary>AMD FidelityFX Super Resolution.</summary>
    Fsr = 1,

    /// <summary>NVIDIA Deep Learning Super Sampling.</summary>
    Dlss = 2,

    /// <summary>Intel Xe Super Sampling.</summary>
    XeSs = 3,

    /// <summary>Unreal Temporal Super Resolution (vendor neutral).</summary>
    Tsr = 4,
}

/// <summary>
/// Quality of the temporal upscaler. Higher index = better image = more GPU load.
/// </summary>
public enum UpscalingQuality
{
    UltraPerformance = 0,
    Performance = 1,
    Balanced = 2,
    Quality = 3,
    Dlaa = 4,
}

/// <summary>
/// Frame generation (DLSS 3 / FSR 3) toggle.
/// </summary>
public enum FrameGenerationMode
{
    Off = 0,
    On = 1,
}

/// <summary>
/// Full ray tracing / path tracing toggle.
/// </summary>
public enum RayTracingMode
{
    Off = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Ultra = 4,
}

/// <summary>
/// Kind of interaction performed by the UI automation.
/// </summary>
public enum UiActionKind
{
    /// <summary>Do nothing, just wait.</summary>
    Wait = 0,

    /// <summary>Press and release a key.</summary>
    KeyPress = 1,

    /// <summary>
    /// Move the cursor to a position expressed as a fraction of the client area
    /// and click the left mouse button. The benchmark tool's main menu does not
    /// react to the keyboard until the mouse hovers it, so this action is the
    /// only reliable way to get through the entry screen.
    /// </summary>
    MouseMoveAndClick = 2,
}

/// <summary>
/// Keyboard keys the tool is allowed to press while driving the benchmark UI.
/// Values are Win32 virtual-key codes so the infrastructure can use them directly.
/// </summary>
public enum VirtualKey
{
    /// <summary>No key — the step is a pure delay.</summary>
    None = 0,
    Enter = 0x0D,
    Escape = 0x1B,
    Space = 0x20,
    Tab = 0x09,
    Left = 0x25,
    Up = 0x26,
    Right = 0x27,
    Down = 0x28,
    Digit1 = 0x31,
    Digit2 = 0x32,
    Digit3 = 0x33,
    A = 0x41,
    B = 0x42,
    C = 0x43,
    D = 0x44,
    E = 0x45,
    F = 0x46,
    N = 0x4E,
    O = 0x4F,
    Q = 0x51,
    R = 0x52,
    S = 0x53,
    T = 0x54,
    W = 0x57,
    Y = 0x59,
    F1 = 0x70,
    OemMinus = 0xBD,
    OemPlus = 0xBB,
    LeftShift = 0xA0,
}
