namespace WukongBench.Domain.Abstractions;

/// <summary>
/// Parses the benchmark results that the Benchmark Tool writes after every pass
/// (result files, UE log statistics, or any other artefact left on disk).
/// </summary>
public interface IBenchmarkResultReader
{
    /// <summary>
    /// Returns the metrics of the newest result that is not older than
    /// <paramref name="notOlderThan"/>, or <c>null</c> when nothing was found yet.
    /// The method is expected to be polled by the caller.
    /// </summary>
    Task<BenchmarkMetrics?> TryReadLatestAsync(
        SteamInstallation installation,
        DateTimeOffset notOlderThan,
        CancellationToken cancellationToken);

    /// <summary>Directories that are scanned for results (shown by the diagnostics mode).</summary>
    IReadOnlyList<string> GetWatchedLocations(SteamInstallation installation);

    /// <summary>Result files that already exist, newest first.</summary>
    IReadOnlyList<string> FindExistingResultFiles(SteamInstallation installation, DateTimeOffset notOlderThan);

    /// <summary>Unreal Engine log files of the tool, newest first.</summary>
    IReadOnlyList<string> FindExistingLogFiles(SteamInstallation installation);
}

/// <summary>
/// Copies the raw evidence of a pass (result files, logs, screenshots) into the
/// run folder created by the tool, so the report is self-contained.
/// </summary>
public interface IArtifactCollector
{
    Task<IReadOnlyList<string>> CollectAsync(
        SteamInstallation installation,
        string destinationDirectory,
        DateTimeOffset notOlderThan,
        CancellationToken cancellationToken);
}
