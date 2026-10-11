namespace WukongBench.Infrastructure.Workspace;

using WukongBench.Domain.Abstractions;

/// <summary>
/// File system implementation of the tool workspace: <c>&lt;root&gt;/&lt;timestamp&gt;-suite/...</c>.
/// </summary>
public sealed class FileSystemBenchmarkWorkspace : IBenchmarkWorkspace
{
    public FileSystemBenchmarkWorkspace(string rootPath)
    {
        RootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public string CreateRunDirectory(string runName)
    {
        var safeName = string.Join('_', runName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var path = Path.Combine(RootPath, safeName);
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetPath(params string[] segments)
    {
        var parts = new List<string> { RootPath };
        parts.AddRange(segments);
        return Path.Combine(parts.ToArray());
    }

    public async Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken) =>
        File.ReadAllTextAsync(path, cancellationToken);

    public bool FileExists(string path) => File.Exists(path);
}
