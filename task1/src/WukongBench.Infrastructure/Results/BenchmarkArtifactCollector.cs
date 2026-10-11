namespace WukongBench.Infrastructure.Results;

using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Configuration;
using WukongBench.Infrastructure.Settings;

/// <summary>
/// Copies the raw evidence of a pass into the run folder: the benchmark result
/// JSON written by the tool, a snapshot of the settings file and any Unreal log
/// file that exists.
/// </summary>
public sealed class BenchmarkArtifactCollector : IArtifactCollector
{
    private const int MaxResultFiles = 3;

    private readonly IBenchmarkResultReader _resultReader;
    private readonly IGraphicsSettingsStoreFactory _settingsStoreFactory;
    private readonly ResultReaderOptions _options;
    private readonly IToolLogger _logger;

    public BenchmarkArtifactCollector(
        IBenchmarkResultReader resultReader,
        IGraphicsSettingsStoreFactory settingsStoreFactory,
        Microsoft.Extensions.Options.IOptions<ResultReaderOptions> options,
        IToolLogger logger)
    {
        _resultReader = resultReader;
        _settingsStoreFactory = settingsStoreFactory;
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<string>> CollectAsync(
        SteamInstallation installation,
        string destinationDirectory,
        DateTimeOffset notOlderThan,
        CancellationToken cancellationToken)
    {
        var copied = new List<string>();
        Directory.CreateDirectory(destinationDirectory);

        foreach (var result in _resultReader.FindExistingResultFiles(installation, notOlderThan).Take(MaxResultFiles))
        {
            var target = Path.Combine(destinationDirectory, $"result-{Path.GetFileName(result)}.json");
            if (TryCopy(result, target))
            {
                copied.Add(target);
            }
        }

        var store = _settingsStoreFactory.Create(installation);
        if (store.Exists)
        {
            var target = Path.Combine(destinationDirectory, "GameUserSettings.snapshot.ini");
            if (TryCopy(store.SettingsFilePath, target))
            {
                copied.Add(target);
            }
        }

        foreach (var log in _resultReader.FindExistingLogFiles(installation))
        {
            var target = Path.Combine(destinationDirectory, $"tool-log-{Path.GetFileName(log)}");
            if (TryCopy(log, target))
            {
                copied.Add(target);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(copied);
    }

    private bool TryCopy(string source, string destination)
    {
        try
        {
            using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.Read);
            input.CopyTo(output);
            return true;
        }
        catch (Exception exception)
        {
            _logger.Debug($"Could not copy '{source}': {exception.Message}");
            return false;
        }
    }
}
