namespace WukongBench.Domain.Models;

/// <summary>
/// One benchmark pass: which kind of load we create plus the exact settings
/// that must be applied before the tool is started.
/// </summary>
public sealed class BenchmarkScenario
{
    public BenchmarkScenario(
        BenchmarkScenarioKind kind,
        string displayName,
        string rationale,
        GraphicsSettingsProfile settings,
        IReadOnlyList<string> launchArguments,
        int order)
    {
        Kind = kind;
        DisplayName = displayName;
        Rationale = rationale;
        Settings = settings;
        LaunchArguments = launchArguments;
        Order = order;
    }

    /// <summary>CPU or GPU pass.</summary>
    public BenchmarkScenarioKind Kind { get; }

    /// <summary>Name shown in the console and in the report.</summary>
    public string DisplayName { get; }

    /// <summary>Why the pass is expected to stress the requested component.</summary>
    public string Rationale { get; }

    /// <summary>Settings written to the benchmark tool before the pass.</summary>
    public GraphicsSettingsProfile Settings { get; }

    /// <summary>Extra command line arguments passed to the tool executable.</summary>
    public IReadOnlyList<string> LaunchArguments { get; }

    /// <summary>Execution order (1 = first).</summary>
    public int Order { get; }
}
