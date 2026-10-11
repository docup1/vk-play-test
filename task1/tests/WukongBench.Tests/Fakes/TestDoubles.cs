namespace WukongBench.Tests.Fakes;

using WukongBench.Domain.Abstractions;

/// <summary>Logger that simply records every message so tests can assert on it.</summary>
public sealed class RecordingLogger : IToolLogger
{
    public List<string> Messages { get; } = new();

    public void Debug(string message) => Messages.Add($"DEBUG: {message}");

    public void Info(string message) => Messages.Add($"INFO: {message}");

    public void Warning(string message) => Messages.Add($"WARN: {message}");

    public void Error(string message, Exception? exception = null) => Messages.Add($"ERROR: {message}");

    public IDisposable BeginScope(string name) => new NoOpScope();

    private sealed class NoOpScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

/// <summary>
/// Clock that starts at a fixed instant and then advances with real time.
/// Advancing matters: the polling loops of the use cases terminate on it.
/// </summary>
public sealed class FakeClock : IClock
{
    private readonly DateTimeOffset _start;
    private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();

    public FakeClock(DateTimeOffset start) => _start = start;

    public DateTimeOffset UtcNow => _start + _stopwatch.Elapsed;
}
