namespace WukongBench.Application.Configuration;

using WukongBench.Domain.Models;

/// <summary>
/// Maps the option model onto the immutable domain model.
/// </summary>
public static class BenchmarkTimingsMapper
{
    public static BenchmarkTimings ToDomain(BenchmarkTimingsOptions options) =>
        new(
            options.LaunchTimeoutSeconds,
            options.MainMenuSettleSeconds,
            options.BenchmarkCompletionTimeoutSeconds,
            options.ResultPollIntervalSeconds,
            options.ResultSettleSeconds,
            options.ProcessExitTimeoutSeconds,
            options.ProcessKillGraceSeconds);
}
