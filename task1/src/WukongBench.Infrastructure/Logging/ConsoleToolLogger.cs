namespace WukongBench.Infrastructure.Logging;

using WukongBench.Domain.Abstractions;

/// <summary>
/// Console logger with indentation for scopes. It writes to the console and,
/// optionally, mirrors everything into a log file so the run can be reviewed
/// afterwards.
/// </summary>
public sealed class ConsoleToolLogger : IToolLogger
{
    private static readonly object SyncRoot = new();

    private readonly Stack<string> _scopes = new();
    private readonly TextWriter? _fileWriter;
    private readonly bool _useColour;
    private readonly bool _verbose;

    public ConsoleToolLogger(string? logFilePath = null, bool verbose = false)
    {
        _verbose = verbose;
        _useColour = !Console.IsOutputRedirected;

        if (!string.IsNullOrWhiteSpace(logFilePath))
        {
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _fileWriter = TextWriter.Synchronized(new StreamWriter(new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true,
            });
        }
    }

    public void Debug(string message)
    {
        if (_verbose)
        {
            Write("DEBUG", message, ConsoleColor.DarkGray);
        }
    }

    public void Info(string message) => Write("INFO ", message, ConsoleColor.Gray);

    public void Warning(string message) => Write("WARN ", message, ConsoleColor.Yellow);

    public void Error(string message, Exception? exception = null)
    {
        Write("ERROR", message, ConsoleColor.Red);
        if (exception is not null)
        {
            Write("ERROR", exception.ToString(), ConsoleColor.DarkRed);
        }
    }

    public IDisposable BeginScope(string name)
    {
        lock (SyncRoot)
        {
            _scopes.Push(name);
        }

        Info($"== {name} ==");
        return new Scope(this);
    }

    private void Write(string level, string message, ConsoleColor colour)
    {
        var indent = new string(' ', Math.Max(0, _scopes.Count - 1) * 2);
        var line = $"{DateTime.Now:HH:mm:ss} [{level}] {indent}{message}";

        lock (SyncRoot)
        {
            if (_useColour)
            {
                var previous = Console.ForegroundColor;
                Console.ForegroundColor = colour;
                Console.Out.WriteLine(line);
                Console.ForegroundColor = previous;
            }
            else
            {
                Console.Out.WriteLine(line);
            }

            _fileWriter?.WriteLine(line);
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly ConsoleToolLogger _owner;

        public Scope(ConsoleToolLogger owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            lock (SyncRoot)
            {
                if (_owner._scopes.Count > 0)
                {
                    _owner._scopes.Pop();
                }
            }
        }
    }
}
