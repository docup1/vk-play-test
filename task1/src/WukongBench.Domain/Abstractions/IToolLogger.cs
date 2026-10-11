namespace WukongBench.Domain.Abstractions;

/// <summary>
/// Minimal logging port. The Domain layer must not depend on any logging
/// package, so the infrastructure adapts this to <c>ILogger</c>/console output.
/// </summary>
public interface IToolLogger
{
    void Debug(string message);

    void Info(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);

    /// <summary>Starts a named scope used to indent the console output.</summary>
    IDisposable BeginScope(string name);
}
