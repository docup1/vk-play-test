namespace WukongBench.Domain.Models;

/// <summary>
/// Final document produced by the tool: machine characteristics, the CPU pass,
/// the GPU pass and the settings used for each of them.
/// </summary>
public sealed class BenchmarkReport
{
    public BenchmarkReport(
        string toolName,
        string toolVersion,
        HardwareProfile hardware,
        BenchmarkRun? cpuRun,
        BenchmarkRun? gpuRun,
        DateTimeOffset generatedAt,
        string installationSummary,
        string settingsFilePath,
        string toolLogTail,
        IReadOnlyList<string> artifactPaths,
        IReadOnlyList<string> notes)
    {
        ToolName = toolName;
        ToolVersion = toolVersion;
        Hardware = hardware;
        CpuRun = cpuRun;
        GpuRun = gpuRun;
        GeneratedAt = generatedAt;
        InstallationSummary = installationSummary;
        SettingsFilePath = settingsFilePath;
        ToolLogTail = toolLogTail;
        ArtifactPaths = artifactPaths;
        Notes = notes;
    }

    public string ToolName { get; }
    public string ToolVersion { get; }

    /// <summary>CPU / GPU / RAM / OS characteristics of the tested machine.</summary>
    public HardwareProfile Hardware { get; }

    /// <summary>Result of the CPU-bound pass (<c>null</c> when it was skipped).</summary>
    public BenchmarkRun? CpuRun { get; }

    /// <summary>Result of the GPU-bound pass (<c>null</c> when it was skipped).</summary>
    public BenchmarkRun? GpuRun { get; }

    /// <summary>Report generation timestamp (UTC).</summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>Short description of the located Steam installation.</summary>
    public string InstallationSummary { get; }

    /// <summary>User settings file that was modified by the tool.</summary>
    public string SettingsFilePath { get; }

    /// <summary>Tail of the benchmark tool log captured after the last pass.</summary>
    public string ToolLogTail { get; }

    /// <summary>Every file the tool produced (report, raw results, logs, screenshots).</summary>
    public IReadOnlyList<string> ArtifactPaths { get; }

    /// <summary>Warnings, assumptions and hints for the operator.</summary>
    public IReadOnlyList<string> Notes { get; }
}
