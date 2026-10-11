namespace WukongBench.Presentation.Rendering;

using System.Globalization;
using System.Text;
using WukongBench.Domain.Models;

/// <summary>Renders the report as a Markdown document (report.md).</summary>
public sealed class MarkdownReportRenderer
{
    public string Render(BenchmarkReport report)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Black Myth: Wukong Benchmark Tool — automated CPU/GPU report");
        builder.AppendLine();
        builder.AppendLine($"* Generated: **{Format.Timestamp(report.GeneratedAt)}** by `{report.ToolName}` {report.ToolVersion}");
        builder.AppendLine($"* Installation: `{report.InstallationSummary}`");
        builder.AppendLine($"* Settings file: `{report.SettingsFilePath}`");
        builder.AppendLine();

        RenderHardware(builder, report.Hardware);

        builder.AppendLine("## Results at a glance");
        builder.AppendLine();
        builder.AppendLine("| Metric | CPU-bound pass | GPU-bound pass |");
        builder.AppendLine("| --- | ---: | ---: |");

        var cpu = report.CpuRun?.Metrics;
        var gpu = report.GpuRun?.Metrics;

        Row(builder, "Average FPS", Format.Fps(cpu?.AverageFps), Format.Fps(gpu?.AverageFps));
        Row(builder, "Minimum FPS", Format.Fps(cpu?.MinimumFps), Format.Fps(gpu?.MinimumFps));
        Row(builder, "Maximum FPS", Format.Fps(cpu?.MaximumFps), Format.Fps(gpu?.MaximumFps));
        Row(builder, "FPS 95th percentile", Format.Fps(cpu?.Percentile95Fps), Format.Fps(gpu?.Percentile95Fps));
        Row(builder, "1% low (from frame records)", Format.Fps(cpu?.Percentile1LowFps), Format.Fps(gpu?.Percentile1LowFps));
        Row(builder, "Average frame time", Format.Millis(cpu?.AverageFrameTimeMs), Format.Millis(gpu?.AverageFrameTimeMs));
        Row(builder, "Average CPU frame time", Format.Millis(cpu?.AverageCpuFrameTimeMs), Format.Millis(gpu?.AverageCpuFrameTimeMs));
        Row(builder, "Average GPU frame time", Format.Millis(cpu?.AverageGpuFrameTimeMs), Format.Millis(gpu?.AverageGpuFrameTimeMs));
        Row(builder, "Peak VRAM", Format.MegaBytes(cpu?.MaximumVramUsageMb), Format.MegaBytes(gpu?.MaximumVramUsageMb));
        Row(builder, "Average VRAM", Format.MegaBytes(cpu?.AverageVramUsageMb), Format.MegaBytes(gpu?.AverageVramUsageMb));
        Row(builder, "CPU utilisation", Format.Percent(cpu?.CpuUtilizationPercent), Format.Percent(gpu?.CpuUtilizationPercent));
        Row(builder, "GPU utilisation", Format.Percent(cpu?.GpuUtilizationPercent), Format.Percent(gpu?.GpuUtilizationPercent));
        Row(builder, "Resolution", Format.Text(cpu?.Resolution ?? string.Empty), Format.Text(gpu?.Resolution ?? string.Empty));
        builder.AppendLine();

        RenderRun(builder, report.CpuRun, "CPU-bound pass");
        RenderRun(builder, report.GpuRun, "GPU-bound pass");

        builder.AppendLine("## Notes");
        builder.AppendLine();
        if (report.Notes.Count == 0)
        {
            builder.AppendLine("_none_");
        }
        else
        {
            foreach (var note in report.Notes)
            {
                builder.AppendLine($"* {note}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Artefacts");
        builder.AppendLine();
        foreach (var path in report.ArtifactPaths)
        {
            builder.AppendLine($"* `{path}`");
        }

        return builder.ToString();
    }

    private static void RenderHardware(StringBuilder builder, HardwareProfile hardware)
    {
        builder.AppendLine("## Machine");
        builder.AppendLine();
        builder.AppendLine("| Component | Detail |");
        builder.AppendLine("| --- | --- |");
        Entry(builder, "Machine", hardware.System.MachineName);
        Entry(builder, "CPU", hardware.Cpu.Name);
        Entry(builder, "CPU cores", $"{hardware.Cpu.PhysicalCores} physical / {hardware.Cpu.LogicalProcessors} logical, {hardware.Cpu.MaxClockMhz}, {hardware.Cpu.Architecture}");
        Entry(builder, "GPU", hardware.Gpu.Name);
        Entry(builder, "GPU driver", $"{hardware.Gpu.DriverVersion} ({hardware.Gpu.DriverDate})");
        Entry(builder, "VRAM", hardware.Gpu.AdapterRamBytes);
        Entry(builder, "Display", $"{hardware.Gpu.CurrentResolution} {hardware.Gpu.CurrentRefreshRateHz}".Trim());
        Entry(builder, "RAM", hardware.Memory.TotalBytes);
        Entry(builder, "RAM modules", hardware.Memory.ModulesSummary);
        Entry(builder, "Page file", hardware.Memory.PageFileSummary);
        Entry(builder, "Operating system", $"{hardware.System.OperatingSystem} {hardware.System.OsVersion} (build {hardware.System.OsBuild}, {hardware.System.OsArchitecture})");
        Entry(builder, "Motherboard", hardware.System.Motherboard);
        Entry(builder, "BIOS", hardware.System.BiosVersion);
        Entry(builder, "Storage", hardware.System.PrimaryStorage);
        Entry(builder, "Power scheme", hardware.System.ActivePowerScheme);
        Entry(builder, "Virtual machine", hardware.System.IsVirtualMachine);
        builder.AppendLine();
    }

    private static void RenderRun(StringBuilder builder, BenchmarkRun? run, string title)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();

        if (run is null)
        {
            builder.AppendLine("_Not executed in this run._");
            builder.AppendLine();
            return;
        }

        builder.AppendLine($"* Status: **{(run.Succeeded ? "OK" : "FAILED")}** — {run.StatusMessage}");
        builder.AppendLine($"* Started: {Format.Timestamp(run.StartedAt)} ({Format.Duration(run.CompletedAt - run.StartedAt)})");
        builder.AppendLine($"* Profile: `{run.Scenario.Settings.Name}`");
        builder.AppendLine($"* Rationale: {run.Scenario.Rationale}");

        if (run.Metrics is not null)
        {
            builder.AppendLine($"* Raw result: `{run.Metrics.SourcePath}`");
            builder.AppendLine($"* Reported by the tool: {run.Metrics.ReportedSystem.CpuModel} / {run.Metrics.ReportedSystem.GpuModel} / " +
                               $"driver {run.Metrics.ReportedSystem.GpuDriverVersion} / {run.Metrics.ReportedSystem.SystemMemory} RAM / " +
                               $"{run.Metrics.ReportedSystem.OsVersion}");
        }

        builder.AppendLine();
        builder.AppendLine("### Settings used");
        builder.AppendLine();
        builder.AppendLine("| Section | Key | Value | Meaning |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var setting in run.Scenario.Settings.Values)
        {
            builder.AppendLine($"| `{setting.Section}` | `{setting.Key}` | `{setting.Value}` | {setting.Meaning} |");
        }

        foreach (var setting in run.Scenario.Settings.UiSettings)
        {
            builder.AppendLine($"| `UISettingData` | `{setting.Key}` | `{setting.Value}` | {setting.Meaning} |");
        }

        builder.AppendLine();
    }

    private static void Row(StringBuilder builder, string label, string cpu, string gpu) =>
        builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1} | {2} |", label, cpu, gpu));

    private static void Entry(StringBuilder builder, string label, string value) =>
        builder.AppendLine($"| {label} | {Format.Text(value)} |");
}
