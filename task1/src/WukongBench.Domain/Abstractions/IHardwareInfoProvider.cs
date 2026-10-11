namespace WukongBench.Domain.Abstractions;

/// <summary>
/// Reads the characteristics of the machine that is being benchmarked
/// (CPU, GPU, RAM, OS, motherboard, storage, active power scheme).
/// </summary>
public interface IHardwareInfoProvider
{
    Task<HardwareProfile> ReadAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Controls the Windows power scheme. The CPU pass is much more reproducible
/// when the machine is not allowed to drop into a power-saving state.
/// </summary>
public interface IPowerSchemeManager
{
    /// <summary>Returns the friendly name of the active scheme, or an empty string.</summary>
    Task<string> GetActiveSchemeNameAsync(CancellationToken cancellationToken);

    /// <summary>Returns the GUID of the active scheme, or an empty string.</summary>
    Task<string> GetActiveSchemeGuidAsync(CancellationToken cancellationToken);

    /// <summary>Activates the given scheme GUID. Returns <c>false</c> on failure.</summary>
    Task<bool> TryActivateAsync(string schemeGuid, CancellationToken cancellationToken);
}
