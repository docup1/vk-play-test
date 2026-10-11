namespace WukongBench.Tests.Fakes;

using WukongBench.Application.Configuration;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;

public sealed class FakeInstallationLocator : ISteamInstallationLocator
{
    public FakeInstallationLocator(SteamInstallation? installation) => Installation = installation;

    public SteamInstallation? Installation { get; }

    public SteamInstallation? Locate(int steamAppId, out string diagnostics)
    {
        diagnostics = "fake";
        return Installation;
    }
}

public sealed class FakeSettingsStoreFactory : IGraphicsSettingsStoreFactory
{
    public List<GraphicsSettingsProfile> Applied { get; } = new();

    public int BackupCount { get; private set; }

    public int RestoreCount { get; private set; }

    public IGraphicsSettingsStore Create(SteamInstallation installation) => new Store(this);

    private sealed class Store : IGraphicsSettingsStore
    {
        private readonly FakeSettingsStoreFactory _owner;

        public Store(FakeSettingsStoreFactory owner) => _owner = owner;

        public string SettingsFilePath => @"C:\fake\GameUserSettings.ini";

        public bool Exists => true;

        public IReadOnlyList<GraphicsSettingValue> ReadAll() =>
            _owner.Applied.SelectMany(profile => profile.Values).ToArray();

        public void Apply(GraphicsSettingsProfile profile) => _owner.Applied.Add(profile);

        public bool TryBackup(string backupFilePath, out string message)
        {
            _owner.BackupCount++;
            message = "backup";
            return true;
        }

        public bool TryRestore(string backupFilePath, out string message)
        {
            _owner.RestoreCount++;
            message = "restore";
            return true;
        }
    }
}

public sealed class FakeBenchmarkSession : IBenchmarkSession
{
    public bool IsRunning { get; set; } = true;

    public int ProcessId => 4242;

    public IntPtr MainWindowHandle => new(1);

    public string WindowTitle => "Black Myth: Wukong Benchmark Tool";

    public string CommandLine => "fake";

    public int? ExitCode => 0;

    public List<int> WaitedForWindow { get; } = new();

    public bool CloseRequested { get; private set; }

    public Task<bool> WaitForWindowAsync(int timeoutSeconds, CancellationToken cancellationToken)
    {
        WaitedForWindow.Add(timeoutSeconds);
        return Task.FromResult(true);
    }

    public bool TryActivateWindow() => true;

    public Task<bool> WaitForExitAsync(int timeoutSeconds, CancellationToken cancellationToken)
    {
        IsRunning = false;
        return Task.FromResult(true);
    }

    public void RequestClose() => CloseRequested = true;

    public Task KillAsync(int graceSeconds, CancellationToken cancellationToken)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task<string> ReadApplicationLogTailAsync(int maxLines, CancellationToken cancellationToken) =>
        Task.FromResult(string.Empty);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class FakeProcessLauncher : IBenchmarkProcessLauncher
{
    public FakeBenchmarkSession Session { get; } = new();

    public List<IReadOnlyList<string>> Arguments { get; } = new();

    public Task<IBenchmarkSession> StartAsync(
        SteamInstallation installation,
        IReadOnlyList<string> arguments,
        bool launchThroughSteam,
        CancellationToken cancellationToken)
    {
        Arguments.Add(arguments);
        return Task.FromResult<IBenchmarkSession>(Session);
    }
}

public sealed class FakeReadinessProbe : IBenchmarkReadinessProbe
{
    public Task<string> WaitUntilReadyAsync(IBenchmarkSession session, BenchmarkTimings timings, CancellationToken cancellationToken) =>
        Task.FromResult("ready");
}

public sealed class FakeUiAutomation : IKeyboardUiAutomation
{
    public List<string> Scripts { get; } = new();

    public Task<bool> RunAsync(IBenchmarkSession session, UiScript script, CancellationToken cancellationToken)
    {
        Scripts.Add(script.Name);
        return Task.FromResult(true);
    }
}

public sealed class FakeResultReader : IBenchmarkResultReader
{
    private readonly Func<int, BenchmarkMetrics?> _factory;
    private int _calls;

    /// <summary>Creates a reader that decides its answer from the call index.</summary>
    public FakeResultReader(Func<int, BenchmarkMetrics?> factory) => _factory = factory;

    public int ReadCount => _calls;

    public Task<BenchmarkMetrics?> TryReadLatestAsync(SteamInstallation installation, DateTimeOffset notOlderThan, CancellationToken cancellationToken)
    {
        var result = _factory(_calls);
        _calls++;
        return Task.FromResult(result);
    }

    public IReadOnlyList<string> GetWatchedLocations(SteamInstallation installation) => new[] { @"C:\Temp\b1\BenchMarkHistory\Tool" };

    public IReadOnlyList<string> FindExistingResultFiles(SteamInstallation installation, DateTimeOffset notOlderThan) => Array.Empty<string>();

    public IReadOnlyList<string> FindExistingLogFiles(SteamInstallation installation) => Array.Empty<string>();
}

public sealed class FakeArtifactCollector : IArtifactCollector
{
    public Task<IReadOnlyList<string>> CollectAsync(
        SteamInstallation installation,
        string destinationDirectory,
        DateTimeOffset notOlderThan,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
}

public sealed class FakeHardwareInfoProvider : IHardwareInfoProvider
{
    public HardwareProfile Profile { get; set; } = new(
        new SystemInfo("PC", "Windows 11", "1", "1", "64-bit", "board", "vendor", "model", "bios", "disk", "High performance", "no"),
        new CpuInfo("CPU", "vendor", 8, 16, "4000 MHz", "4000 MHz", "64-bit"),
        new GpuInfo("GPU", "vendor", "1.0", "2024-01-01", "12 GB", "vp", "2560 x 1440 x 144 hertz", "144"),
        new MemoryInfo("32 GB", "2 x 16 GB", "3600", "4096 MB"));

    public Task<HardwareProfile> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Profile);
}

public sealed class FakePowerSchemeManager : IPowerSchemeManager
{
    public List<string> Activated { get; } = new();

    public string Current { get; private set; } = PowerSchemes.Balanced;

    public Task<string> GetActiveSchemeNameAsync(CancellationToken cancellationToken) => Task.FromResult("Balanced");

    public Task<string> GetActiveSchemeGuidAsync(CancellationToken cancellationToken) => Task.FromResult(Current);

    public Task<bool> TryActivateAsync(string schemeGuid, CancellationToken cancellationToken)
    {
        Current = schemeGuid;
        Activated.Add(schemeGuid);
        return Task.FromResult(true);
    }
}

public sealed class TempWorkspace : IBenchmarkWorkspace, IDisposable
{
    public TempWorkspace()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "wukongbench-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    private int _counter;

    public string CreateRunDirectory(string runName)
    {
        var path = Path.Combine(RootPath, $"{++_counter:00}-{runName}");
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetPath(params string[] segments) => Path.Combine(new[] { RootPath }.Concat(segments).ToArray());

    public Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken) =>
        File.WriteAllTextAsync(path, content, cancellationToken);

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken) => File.ReadAllTextAsync(path, cancellationToken);

    public bool FileExists(string path) => File.Exists(path);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort.
        }
    }
}
