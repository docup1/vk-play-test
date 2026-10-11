namespace WukongBench.Domain.Abstractions;

/// <summary>
/// Abstraction over the system clock, so use cases stay testable and every
/// timestamp in the report comes from one place (UTC).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
