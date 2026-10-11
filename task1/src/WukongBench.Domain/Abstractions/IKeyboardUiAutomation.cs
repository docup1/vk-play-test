namespace WukongBench.Domain.Abstractions;

/// <summary>
/// Drives the benchmark tool window with keyboard input. The script itself
/// (which keys, in which order, with which delays) is business knowledge and
/// lives in the Application layer; this port only knows how to press keys.
/// </summary>
public interface IKeyboardUiAutomation
{
    /// <summary>
    /// Executes the script against the window of <paramref name="session"/>.
    /// Returns <c>false</c> when the window could not be focused or the session died.
    /// </summary>
    Task<bool> RunAsync(IBenchmarkSession session, UiScript script, CancellationToken cancellationToken);
}

/// <summary>
/// Waits for the benchmark tool to become idle/ready. Implementations may use
/// the tool's own log file, window title changes or a plain delay.
/// </summary>
public interface IBenchmarkReadinessProbe
{
    /// <summary>
    /// Waits until the tool is expected to accept input, or until the timeout
    /// expires. Returns the reason of the returned state.
    /// </summary>
    Task<string> WaitUntilReadyAsync(IBenchmarkSession session, BenchmarkTimings timings, CancellationToken cancellationToken);
}
