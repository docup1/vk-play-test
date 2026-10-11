namespace WukongBench.Domain.Abstractions;

/// <summary>
/// File system workspace of the tool: <c>artifacts/&lt;timestamp&gt;-suite/</c> and friends.
/// </summary>
public interface IBenchmarkWorkspace
{
    /// <summary>Absolute path of the workspace root (created on demand).</summary>
    string RootPath { get; }

    /// <summary>Creates (if needed) and returns a sub folder for one run.</summary>
    string CreateRunDirectory(string runName);

    /// <summary>Builds an absolute path inside the workspace.</summary>
    string GetPath(params string[] segments);

    Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken);

    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken);

    bool FileExists(string path);
}
