namespace WukongBench.Application.Configuration;

/// <summary>
/// Well known Windows power scheme GUIDs (data only).
/// The CPU pass is far more reproducible when the machine is not allowed to drop
/// into a power saving state, so the tool switches to "High performance" for the
/// duration of the suite and restores the previous scheme afterwards.
/// </summary>
public static class PowerSchemes
{
    /// <summary>Balanced (the Windows default).</summary>
    public const string Balanced = "381b4222-f694-41f0-9685-ff5bb260df2e";

    /// <summary>High performance.</summary>
    public const string HighPerformance = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

    /// <summary>Ultimate performance (only present on some SKUs).</summary>
    public const string UltimatePerformance = "e9a42b02-d5df-448d-aa00-03f14749eb61";
}

/// <summary>Steam application ids involved in the automation.</summary>
public static class SteamAppIds
{
    /// <summary>Black Myth: Wukong (the full game).</summary>
    public const int BlackMythWukong = 2358720;

    /// <summary>Black Myth: Wukong Benchmark Tool (free, standalone).</summary>
    public const int BenchmarkTool = 3132990;
}
