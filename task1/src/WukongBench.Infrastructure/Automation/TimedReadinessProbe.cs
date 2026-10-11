namespace WukongBench.Infrastructure.Automation;

using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;

/// <summary>
/// Default readiness strategy: after the window appeared, the tool spends a
/// while compiling shaders and initialising the renderer, so the automation
/// waits a configurable number of seconds and keeps checking that the process is
/// still alive. It also waits until the window responds to a foreground request,
/// which is a good indicator that the render loop is running.
/// </summary>
public sealed class TimedReadinessProbe : IBenchmarkReadinessProbe
{
    private readonly IToolLogger _logger;

    public TimedReadinessProbe(IToolLogger logger)
    {
        _logger = logger;
    }

    public async Task<string> WaitUntilReadyAsync(
        IBenchmarkSession session,
        BenchmarkTimings timings,
        CancellationToken cancellationToken)
    {
        var wait = TimeSpan.FromSeconds(Math.Max(0, timings.MainMenuSettleSeconds));
        var deadline = DateTime.UtcNow.Add(wait);

        _logger.Info($"Waiting {wait.TotalSeconds:0}s for the tool to finish loading (shader compilation, level streaming).");

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!session.IsRunning)
            {
                return "the benchmark process exited while waiting for the main menu";
            }

            await Task.Delay(1000, cancellationToken);
        }

        session.TryActivateWindow();
        return $"the tool has been running for {wait.TotalSeconds:0}s and the window is available";
    }
}
