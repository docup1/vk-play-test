namespace WukongBench.Infrastructure.Process;

using Microsoft.Extensions.Options;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Configuration;

/// <summary>
/// Starts the benchmark tool.
///
/// By default the tool is started through the Steam client
/// (<c>steam.exe -applaunch 3132990</c>), because the executable expects the
/// Steam context and otherwise re-launches itself with a confirmation dialog.
/// A direct start is still supported; in that case the Steam environment
/// variables are provided explicitly.
/// </summary>
public sealed class BenchmarkProcessLauncher : IBenchmarkProcessLauncher
{
    private readonly IToolLogger _logger;
    private readonly ResultReaderOptions _resultOptions;

    public BenchmarkProcessLauncher(IOptions<ResultReaderOptions> resultOptions, IToolLogger logger)
    {
        _logger = logger;
        _resultOptions = resultOptions.Value;
    }

    public Task<IBenchmarkSession> StartAsync(
        SteamInstallation installation,
        IReadOnlyList<string> arguments,
        bool launchThroughSteam,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var steamExe = Path.Combine(installation.SteamRootPath, "steam.exe");
        var useSteam = launchThroughSteam && !string.IsNullOrWhiteSpace(installation.SteamRootPath) && File.Exists(steamExe);

        System.Diagnostics.Process? process;
        string commandLine;

        if (useSteam)
        {
            var argumentList = new List<string> { "-applaunch", installation.AppId.ToString() };
            argumentList.AddRange(arguments);

            var startInfo = new System.Diagnostics.ProcessStartInfo(steamExe)
            {
                UseShellExecute = false,
                WorkingDirectory = installation.SteamRootPath,
            };

            foreach (var argument in argumentList)
            {
                startInfo.ArgumentList.Add(argument);
            }

            commandLine = $"{steamExe} {string.Join(' ', argumentList)}";
            _logger.Info($"Starting through Steam: {commandLine}");
            process = System.Diagnostics.Process.Start(startInfo);
        }
        else
        {
            var executable = installation.ExecutablePath;
            var startInfo = new System.Diagnostics.ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                WorkingDirectory = installation.InstallDirectory,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            // A direct start of the stub expects the Steam context to be present.
            startInfo.Environment["SteamAppId"] = installation.AppId.ToString();
            startInfo.Environment["SteamGameId"] = installation.AppId.ToString();
            startInfo.Environment["SteamOverlayGameId"] = installation.AppId.ToString();

            commandLine = $"{executable} {string.Join(' ', arguments)}";
            _logger.Info($"Starting directly: {commandLine}");
            process = System.Diagnostics.Process.Start(startInfo);
        }

        if (process is null)
        {
            throw new InvalidOperationException($"Could not start the benchmark tool ({commandLine}).");
        }

        var session = new GameProcessSession(installation, _resultOptions, _logger, commandLine, process);
        return Task.FromResult<IBenchmarkSession>(session);
    }
}
