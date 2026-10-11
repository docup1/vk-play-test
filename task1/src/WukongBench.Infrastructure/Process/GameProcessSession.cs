namespace WukongBench.Infrastructure.Process;

using System.Text;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Configuration;
using WukongBench.Infrastructure.Interop;

/// <summary>
/// One run of the benchmark tool.
///
/// The tool is normally started through the Steam client, so the process we
/// spawn (steam.exe) is not the process that shows the window. The session
/// therefore tracks the real game processes by name and start time and exposes
/// them through <see cref="IBenchmarkSession"/>.
/// </summary>
public sealed class GameProcessSession : IBenchmarkSession
{
    private static readonly string[] CandidateProcessNames =
    {
        "b1_benchmark",
        "b1-Win64-Shipping",
        "b1_benchmark-Win64-Shipping",
    };

    private readonly SteamInstallation _installation;
    private readonly ResultReaderOptions _resultOptions;
    private readonly IToolLogger _logger;
    private readonly DateTime _notBefore;
    private readonly List<System.Diagnostics.Process> _owned = new();

    private System.Diagnostics.Process? _gameProcess;
    private IntPtr _windowHandle = IntPtr.Zero;
    private string _windowTitle = string.Empty;

    public GameProcessSession(
        SteamInstallation installation,
        ResultReaderOptions resultOptions,
        IToolLogger logger,
        string commandLine,
        System.Diagnostics.Process? launchedProcess)
    {
        _installation = installation;
        _resultOptions = resultOptions;
        _logger = logger;
        CommandLine = commandLine;
        _notBefore = DateTime.Now.AddSeconds(-5);

        if (launchedProcess is not null)
        {
            _owned.Add(launchedProcess);
        }
    }

    public int ProcessId => _gameProcess?.Id ?? 0;

    public IntPtr MainWindowHandle => _windowHandle;

    public string WindowTitle => _windowTitle;

    public string CommandLine { get; }

    public bool IsRunning
    {
        get
        {
            try
            {
                return _gameProcess is not null && !_gameProcess.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    public int? ExitCode
    {
        get
        {
            try
            {
                if (_gameProcess is null || !_gameProcess.HasExited)
                {
                    return null;
                }

                return _gameProcess.ExitCode;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    public async Task<bool> WaitForWindowAsync(int timeoutSeconds, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = FindGameProcess();
            if (candidate is not null)
            {
                if (_gameProcess is not null && !ReferenceEquals(candidate, _gameProcess))
                {
                    _gameProcess.Dispose();
                }

                _gameProcess = candidate;

                try
                {
                    candidate.Refresh();
                    if (candidate.MainWindowHandle != IntPtr.Zero)
                    {
                        _windowHandle = candidate.MainWindowHandle;
                        _windowTitle = candidate.MainWindowTitle;
                        return true;
                    }
                }
                catch (InvalidOperationException)
                {
                    // The process disappeared between discovery and inspection.
                }
            }

            await Task.Delay(1000, cancellationToken);
        }

        return false;
    }

    public bool TryActivateWindow()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            if (NativeMethods.IsIconic(_windowHandle))
            {
                NativeMethods.ShowWindow(_windowHandle, NativeMethods.SwRestore);
            }

            NativeMethods.ShowWindow(_windowHandle, NativeMethods.SwShow);

            // Attaching to the foreground thread's input queue is the only
            // reliable way to steal focus from another application on Windows.
            var foreground = NativeMethods.GetForegroundWindow();
            var targetThread = NativeMethods.GetWindowThreadProcessId(_windowHandle, out _);
            var foregroundThread = foreground == IntPtr.Zero
                ? 0
                : NativeMethods.GetWindowThreadProcessId(foreground, out _);

            var attached = false;
            if (foregroundThread != 0 && foregroundThread != targetThread)
            {
                attached = NativeMethods.AttachThreadInput(foregroundThread, targetThread, true);
            }

            var activated = NativeMethods.SetForegroundWindow(_windowHandle);

            if (attached)
            {
                NativeMethods.AttachThreadInput(foregroundThread, targetThread, false);
            }

            return activated;
        }
        catch (Exception exception)
        {
            _logger.Warning($"Could not activate the benchmark window: {exception.Message}");
            return false;
        }
    }

    public async Task<bool> WaitForExitAsync(int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (_gameProcess is null)
        {
            return true;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            await _gameProcess.WaitForExitAsync(timeout.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return _gameProcess.HasExited;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    public void RequestClose()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            NativeMethods.PostMessage(_windowHandle, NativeMethods.WmClose, IntPtr.Zero, IntPtr.Zero);
        }

        try
        {
            _gameProcess?.CloseMainWindow();
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }
    }

    public async Task KillAsync(int graceSeconds, CancellationToken cancellationToken)
    {
        if (_gameProcess is null)
        {
            return;
        }

        try
        {
            if (!_gameProcess.HasExited)
            {
                _gameProcess.Kill(entireProcessTree: true);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(graceSeconds, 5)), cancellationToken);
            }
        }
        catch (Exception exception)
        {
            _logger.Warning($"Could not terminate the benchmark process: {exception.Message}");
        }
    }

    public async Task<string> ReadApplicationLogTailAsync(int maxLines, CancellationToken cancellationToken)
    {
        var candidates = _resultOptions.LogFileCandidates
            .Select(relative => Path.Combine(_installation.InstallDirectory, relative))
            .Where(File.Exists)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .ToArray();

        if (candidates.Length == 0)
        {
            return string.Empty;
        }

        try
        {
            var content = await File.ReadAllTextAsync(candidates[0].FullName, cancellationToken);
            var lines = content.Replace("\r\n", "\n").Split('\n');
            var tail = lines.Length <= maxLines ? lines : lines[^maxLines..];

            var builder = new StringBuilder();
            builder.AppendLine($"# source: {candidates[0].FullName}");
            foreach (var line in tail)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }
        catch (Exception exception)
        {
            _logger.Debug($"Could not read the log file: {exception.Message}");
            return string.Empty;
        }
    }

    public ValueTask DisposeAsync()
    {
        foreach (var process in _owned.Concat(_gameProcess is null ? Array.Empty<System.Diagnostics.Process>() : new[] { _gameProcess }))
        {
            try
            {
                process.Dispose();
            }
            catch (Exception)
            {
                // Ignored: disposing must never mask a benchmark failure.
            }
        }

        return ValueTask.CompletedTask;
    }

    private System.Diagnostics.Process? FindGameProcess()
    {
        var candidates = new List<System.Diagnostics.Process>();

        foreach (var name in CandidateProcessNames)
        {
            System.Diagnostics.Process[] processes;
            try
            {
                processes = System.Diagnostics.Process.GetProcessesByName(name);
            }
            catch (Exception exception)
            {
                _logger.Debug($"Process enumeration for '{name}' failed: {exception.Message}");
                continue;
            }

            foreach (var process in processes)
            {
                try
                {
                    if (process.StartTime < _notBefore)
                    {
                        process.Dispose();
                        continue;
                    }

                    process.Refresh();
                    candidates.Add(process);
                }
                catch (Exception)
                {
                    // Access denied or already exited - skip it.
                    process.Dispose();
                }
            }
        }

        System.Diagnostics.Process? chosen = null;
        System.Diagnostics.Process? fallback = null;

        foreach (var candidate in candidates)
        {
            if (candidate.MainWindowHandle != IntPtr.Zero && chosen is null)
            {
                chosen = candidate;
            }
            else
            {
                fallback ??= candidate;
            }
        }

        chosen ??= fallback;

        // Release the handles of everything that was not chosen.
        foreach (var candidate in candidates)
        {
            if (!ReferenceEquals(candidate, chosen))
            {
                candidate.Dispose();
            }
        }

        return chosen;
    }
}
