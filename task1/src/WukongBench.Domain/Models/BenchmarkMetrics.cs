namespace WukongBench.Domain.Models;

/// <summary>
/// Machine data as the Benchmark Tool itself reports it inside its result file.
/// Used to cross-check what WMI returned and to show the operator that the tool
/// really measured the machine we think it did.
/// </summary>
public sealed class ReportedSystemInfo
{
    public ReportedSystemInfo(
        string cpuModel,
        string gpuModel,
        string gpuDriverVersion,
        string systemMemory,
        string osVersion,
        string gameVersion,
        string benchmarkToolVersion)
    {
        CpuModel = cpuModel;
        GpuModel = gpuModel;
        GpuDriverVersion = gpuDriverVersion;
        SystemMemory = systemMemory;
        OsVersion = osVersion;
        GameVersion = gameVersion;
        BenchmarkToolVersion = benchmarkToolVersion;
    }

    /// <summary>Value of <c>CPUModel</c>.</summary>
    public string CpuModel { get; }

    /// <summary>Value of <c>GPUModel</c>.</summary>
    public string GpuModel { get; }

    /// <summary>Value of <c>GpuDriverVer</c>.</summary>
    public string GpuDriverVersion { get; }

    /// <summary>Value of <c>SysMem</c>.</summary>
    public string SystemMemory { get; }

    /// <summary>Value of <c>SysVer</c>.</summary>
    public string OsVersion { get; }

    /// <summary>Value of <c>GameVer</c>.</summary>
    public string GameVersion { get; }

    /// <summary>Value of <c>BenckMarkVersion</c> (spelling as in the tool's file).</summary>
    public string BenchmarkToolVersion { get; }
}

/// <summary>
/// Metrics parsed out of a benchmark run.
/// Every value is nullable on purpose: the tool always reports the FPS block,
/// but VRAM / frame time may be absent depending on the tool build.
/// </summary>
public sealed class BenchmarkMetrics
{
    public BenchmarkMetrics(
        double? averageFps,
        double? minimumFps,
        double? maximumFps,
        double? percentile95Fps,
        double? percentile1LowFps,
        double? averageFrameTimeMs,
        double? averageCpuFrameTimeMs,
        double? averageGpuFrameTimeMs,
        double? maximumVramUsageMb,
        double? averageVramUsageMb,
        string vramSizeLabel,
        double? cpuUtilizationPercent,
        double? gpuUtilizationPercent,
        double? gpuLeakPercent,
        string resolution,
        string sourcePath,
        ReportedSystemInfo reportedSystem,
        IReadOnlyDictionary<string, string> recordedSettings,
        IReadOnlyDictionary<string, string> rawValues)
    {
        AverageFps = averageFps;
        MinimumFps = minimumFps;
        MaximumFps = maximumFps;
        Percentile95Fps = percentile95Fps;
        Percentile1LowFps = percentile1LowFps;
        AverageFrameTimeMs = averageFrameTimeMs;
        AverageCpuFrameTimeMs = averageCpuFrameTimeMs;
        AverageGpuFrameTimeMs = averageGpuFrameTimeMs;
        MaximumVramUsageMb = maximumVramUsageMb;
        AverageVramUsageMb = averageVramUsageMb;
        VramSizeLabel = vramSizeLabel;
        CpuUtilizationPercent = cpuUtilizationPercent;
        GpuUtilizationPercent = gpuUtilizationPercent;
        GpuLeakPercent = gpuLeakPercent;
        Resolution = resolution;
        SourcePath = sourcePath;
        ReportedSystem = reportedSystem;
        RecordedSettings = recordedSettings;
        RawValues = rawValues;
    }

    /// <summary>Average frame rate (<c>FPSAvg</c>).</summary>
    public double? AverageFps { get; }

    /// <summary>Minimum frame rate (<c>FPSMin</c>).</summary>
    public double? MinimumFps { get; }

    /// <summary>Maximum frame rate (<c>FPSMax</c>).</summary>
    public double? MaximumFps { get; }

    /// <summary>95th percentile frame rate (<c>FPS95</c>) — the low-FPS percentile shown by the tool.</summary>
    public double? Percentile95Fps { get; }

    /// <summary>1% low frame rate, computed from the per-frame records when present.</summary>
    public double? Percentile1LowFps { get; }

    /// <summary>Average frame time in milliseconds (derived when not stored directly).</summary>
    public double? AverageFrameTimeMs { get; }

    /// <summary>Average CPU frame time in milliseconds (from <c>Records[].CPUFrameTime</c>).</summary>
    public double? AverageCpuFrameTimeMs { get; }

    /// <summary>Average GPU frame time in milliseconds (from <c>Records[].GPUFrameTime</c>).</summary>
    public double? AverageGpuFrameTimeMs { get; }

    /// <summary>Peak video memory usage in megabytes (<c>VideoMem</c> is reported in GB).</summary>
    public double? MaximumVramUsageMb { get; }

    /// <summary>Average video memory usage in megabytes, computed from the per-frame records.</summary>
    public double? AverageVramUsageMb { get; }

    /// <summary>Video memory label reported by the tool, e.g. <c>12GB</c>.</summary>
    public string VramSizeLabel { get; }

    /// <summary>Average CPU utilisation in percent (<c>CPUAvg</c>).</summary>
    public double? CpuUtilizationPercent { get; }

    /// <summary>Average GPU utilisation in percent (<c>GPUAvg</c>) — the key indicator for the GPU pass.</summary>
    public double? GpuUtilizationPercent { get; }

    /// <summary>Value of <c>GPULeak</c> as reported by the tool.</summary>
    public double? GpuLeakPercent { get; }

    /// <summary>Render resolution recorded by the tool, e.g. <c>1280 × 720</c>.</summary>
    public string Resolution { get; }

    /// <summary>File the metrics were parsed from (evidence for the report).</summary>
    public string SourcePath { get; }

    /// <summary>System data echoed by the tool.</summary>
    public ReportedSystemInfo ReportedSystem { get; }

    /// <summary>Graphics settings the tool recorded for this run — proof of what was actually applied.</summary>
    public IReadOnlyDictionary<string, string> RecordedSettings { get; }

    /// <summary>Every key/value that was found in the source, kept verbatim.</summary>
    public IReadOnlyDictionary<string, string> RawValues { get; }
}
