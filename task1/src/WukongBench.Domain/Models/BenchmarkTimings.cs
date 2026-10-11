namespace WukongBench.Domain.Models;

/// <summary>
/// All waiting periods used by the automation, in one place, so the operator can
/// tune the tool for a slow machine without recompiling.
/// </summary>
public sealed class BenchmarkTimings
{
    public BenchmarkTimings(
        int launchTimeoutSeconds,
        int mainMenuSettleSeconds,
        int benchmarkCompletionTimeoutSeconds,
        int resultPollIntervalSeconds,
        int resultSettleSeconds,
        int processExitTimeoutSeconds,
        int processKillGraceSeconds)
    {
        LaunchTimeoutSeconds = launchTimeoutSeconds;
        MainMenuSettleSeconds = mainMenuSettleSeconds;
        BenchmarkCompletionTimeoutSeconds = benchmarkCompletionTimeoutSeconds;
        ResultPollIntervalSeconds = resultPollIntervalSeconds;
        ResultSettleSeconds = resultSettleSeconds;
        ProcessExitTimeoutSeconds = processExitTimeoutSeconds;
        ProcessKillGraceSeconds = processKillGraceSeconds;
    }

    /// <summary>How long to wait for the tool window after starting the process.</summary>
    public int LaunchTimeoutSeconds { get; }

    /// <summary>Extra wait after the window appeared (loading / shader compilation).</summary>
    public int MainMenuSettleSeconds { get; }

    /// <summary>Maximum duration of one benchmark pass.</summary>
    public int BenchmarkCompletionTimeoutSeconds { get; }

    /// <summary>Polling interval while waiting for the result file to appear.</summary>
    public int ResultPollIntervalSeconds { get; }

    /// <summary>Extra wait after the result file appeared before reading it.</summary>
    public int ResultSettleSeconds { get; }

    /// <summary>How long to wait for a graceful exit of the tool.</summary>
    public int ProcessExitTimeoutSeconds { get; }

    /// <summary>How long to wait after a kill request before giving up.</summary>
    public int ProcessKillGraceSeconds { get; }
}
