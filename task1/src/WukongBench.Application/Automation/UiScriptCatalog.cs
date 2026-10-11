namespace WukongBench.Application.Automation;

using Microsoft.Extensions.Options;
using WukongBench.Application.Configuration;
using WukongBench.Domain.Models;

/// <summary>
/// Turns the configured key/mouse sequences into <see cref="UiScript"/> models.
/// This is business knowledge ("what has to be clicked to start the benchmark"),
/// so it belongs to the Application layer, while the actual input synthesis is
/// an Infrastructure concern behind <c>IKeyboardUiAutomation</c>.
/// </summary>
public sealed class UiScriptCatalog
{
    private readonly UiAutomationOptions _options;

    public UiScriptCatalog(IOptions<WukongBenchOptions> options)
    {
        _options = options.Value.UiAutomation;
    }

    /// <summary>Sequence that starts a benchmark pass from the main menu.</summary>
    public UiScript CreateStartScript() =>
        new(
            "start-benchmark",
            "Hover the main menu, confirm 'Benchmark Test' and the confirmation dialog",
            Map(_options.StartScript));

    /// <summary>Sequence that leaves the results screen before the tool is closed.</summary>
    public UiScript CreateExitScript() =>
        new(
            "leave-results",
            "Leave the results screen so the tool can be closed cleanly",
            Map(_options.ExitScript));

    private static IReadOnlyList<UiStep> Map(IEnumerable<UiStepOptions> steps) =>
        steps.Select(step => new UiStep(
            step.Action,
            step.Key,
            step.NormalizedX,
            step.NormalizedY,
            step.HoldMilliseconds,
            step.DelayAfterMilliseconds,
            string.IsNullOrWhiteSpace(step.Description) ? step.Action.ToString() : step.Description)).ToArray();
}
