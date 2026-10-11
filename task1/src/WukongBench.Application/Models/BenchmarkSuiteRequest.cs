namespace WukongBench.Application.Models;

/// <summary>
/// What the operator asked the tool to do in this invocation.
/// </summary>
public sealed class BenchmarkSuiteRequest
{
    public BenchmarkSuiteRequest(
        bool runCpuPass,
        bool runGpuPass,
        bool keepModifiedSettings,
        bool launchThroughSteam,
        bool useHighPerformancePowerScheme,
        bool uiAutomationEnabled)
    {
        RunCpuPass = runCpuPass;
        RunGpuPass = runGpuPass;
        KeepModifiedSettings = keepModifiedSettings;
        LaunchThroughSteam = launchThroughSteam;
        UseHighPerformancePowerScheme = useHighPerformancePowerScheme;
        UiAutomationEnabled = uiAutomationEnabled;
    }

    /// <summary>Execute the CPU-bound pass.</summary>
    public bool RunCpuPass { get; }

    /// <summary>Execute the GPU-bound pass.</summary>
    public bool RunGpuPass { get; }

    /// <summary>Do not roll the benchmark tool settings back after the suite.</summary>
    public bool KeepModifiedSettings { get; }

    /// <summary>Start the benchmark tool through the Steam client.</summary>
    public bool LaunchThroughSteam { get; }

    /// <summary>Try to switch to the high performance power scheme.</summary>
    public bool UseHighPerformancePowerScheme { get; }

    /// <summary>Send the configured key scripts instead of waiting for a human.</summary>
    public bool UiAutomationEnabled { get; }
}
