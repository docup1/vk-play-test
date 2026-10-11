namespace WukongBench.Domain.Abstractions;


/// <summary>
/// A running instance of the benchmark tool.
/// </summary>
public interface IBenchmarkSession : IAsyncDisposable
{
    /// <summary>Operating system process id of the running benchmark tool.</summary>
    int ProcessId { get; }

    /// <summary>Handle of the main window, or <see cref="IntPtr.Zero"/> until it exists.</summary>
    IntPtr MainWindowHandle { get; }

    /// <summary>Title of the main window once it has been found.</summary>
    string WindowTitle { get; }

    /// <summary>Full command line that was used to start the tool.</summary>
    string CommandLine { get; }

    /// <summary><c>true</c> while the process is alive.</summary>
    bool IsRunning { get; }

    /// <summary>Exit code after the process has exited.</summary>
    int? ExitCode { get; }

    /// <summary>Waits until the main window exists and is visible.</summary>
    Task<bool> WaitForWindowAsync(int timeoutSeconds, CancellationToken cancellationToken);

    /// <summary>Brings the window to the foreground (needed before sending keys).</summary>
    bool TryActivateWindow();

    /// <summary>Waits for a graceful exit; returns <c>false</c> on timeout.</summary>
    Task<bool> WaitForExitAsync(int timeoutSeconds, CancellationToken cancellationToken);

    /// <summary>Requests a graceful close of the main window.</summary>
    void RequestClose();

    /// <summary>Terminates the process tree.</summary>
    Task KillAsync(int graceSeconds, CancellationToken cancellationToken);

    /// <summary>Tail of the Unreal Engine log file produced by the tool.</summary>
    Task<string> ReadApplicationLogTailAsync(int maxLines, CancellationToken cancellationToken);
}

/// <summary>
/// Starts the benchmark tool. Implementations may start it directly
/// (<c>b1_benchmark.exe</c> inside the install folder) or through the Steam
/// client, which is the safer default because the tool expects the Steam context.
/// </summary>
public interface IBenchmarkProcessLauncher
{
    /// <summary>Starts the tool and returns a handle to the running session.</summary>
    Task<IBenchmarkSession> StartAsync(
        SteamInstallation installation,
        IReadOnlyList<string> arguments,
        bool launchThroughSteam,
        CancellationToken cancellationToken);
}
