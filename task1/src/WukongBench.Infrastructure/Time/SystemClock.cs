namespace WukongBench.Infrastructure.Time;

using WukongBench.Domain.Abstractions;

/// <summary>System clock implementation.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
