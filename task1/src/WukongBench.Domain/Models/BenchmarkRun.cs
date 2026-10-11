namespace WukongBench.Domain.Models;

/// <summary>
/// The outcome of a single benchmark pass: the scenario that was applied,
/// the parsed metrics (when available) and the bookkeeping around it.
/// </summary>
public sealed class BenchmarkRun
{
    public BenchmarkRun(
        BenchmarkScenario scenario,
        BenchmarkMetrics? metrics,
        bool succeeded,
        string statusMessage,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        IReadOnlyList<string> artifactPaths,
        bool processStarted,
        bool processExitedCleanly,
        string applicationLogTail)
    {
        Scenario = scenario;
        Metrics = metrics;
        Succeeded = succeeded;
        StatusMessage = statusMessage;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        ArtifactPaths = artifactPaths;
        ProcessStarted = processStarted;
        ProcessExitedCleanly = processExitedCleanly;
        ApplicationLogTail = applicationLogTail;
    }

    /// <summary>Pass that was executed.</summary>
    public BenchmarkScenario Scenario { get; }

    /// <summary>Parsed metrics, or <c>null</c> when nothing could be read.</summary>
    public BenchmarkMetrics? Metrics { get; }

    /// <summary><c>true</c> when the pass ran and its metrics were parsed.</summary>
    public bool Succeeded { get; }

    /// <summary>Human readable status (success or the reason of the failure).</summary>
    public string StatusMessage { get; }

    /// <summary>Pass start (UTC).</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>Pass end (UTC).</summary>
    public DateTimeOffset CompletedAt { get; }

    /// <summary>Files copied into the run folder (logs, raw results, ini snapshots).</summary>
    public IReadOnlyList<string> ArtifactPaths { get; }

    /// <summary>Whether the benchmark process was actually started.</summary>
    public bool ProcessStarted { get; }

    /// <summary>Whether the benchmark process exited on its own.</summary>
    public bool ProcessExitedCleanly { get; }

    /// <summary>Tail of the benchmark tool's own log, useful when parsing fails.</summary>
    public string ApplicationLogTail { get; }
}
