namespace WukongBench.Infrastructure.Power;

using System.Diagnostics;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;

/// <summary>
/// Windows power scheme control through <c>powercfg</c>.
/// </summary>
public sealed class PowerCfgSchemeManager : IPowerSchemeManager
{
    private readonly IToolLogger _logger;

    public PowerCfgSchemeManager(IToolLogger logger)
    {
        _logger = logger;
    }

    public async Task<string> GetActiveSchemeNameAsync(CancellationToken cancellationToken)
    {
        var output = await RunAsync("/getactivescheme", cancellationToken);
        return ParseName(output);
    }

    public async Task<string> GetActiveSchemeGuidAsync(CancellationToken cancellationToken)
    {
        var output = await RunAsync("/getactivescheme", cancellationToken);
        return ParseGuid(output);
    }

    public async Task<bool> TryActivateAsync(string schemeGuid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(schemeGuid))
        {
            return false;
        }

        var output = await RunAsync($"/setactive {schemeGuid}", cancellationToken);
        if (output is null)
        {
            return false;
        }

        var active = await GetActiveSchemeGuidAsync(cancellationToken);
        var success = string.Equals(active, schemeGuid, StringComparison.OrdinalIgnoreCase);
        if (!success)
        {
            _logger.Warning($"Could not activate power scheme {schemeGuid}.");
        }

        return success;
    }

    private async Task<string?> RunAsync(string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo("powercfg.exe", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return output;
        }
        catch (Exception exception)
        {
            _logger.Debug($"powercfg {arguments} failed: {exception.Message}");
            return null;
        }
    }

    private static string ParseGuid(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return string.Empty;
        }

        // "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)"
        var start = output.IndexOf(':');
        if (start < 0)
        {
            return string.Empty;
        }

        var remainder = output[(start + 1)..].Trim();
        var end = remainder.IndexOfAny(new[] { ' ', '\r', '\n', '(' });
        var guid = end < 0 ? remainder : remainder[..end];
        return guid.Trim();
    }

    private static string ParseName(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return string.Empty;
        }

        var open = output.IndexOf('(');
        var close = output.LastIndexOf(')');
        return open >= 0 && close > open ? output[(open + 1)..close].Trim() : output.Trim();
    }
}
