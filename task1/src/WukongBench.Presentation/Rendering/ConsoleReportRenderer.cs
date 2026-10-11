namespace WukongBench.Presentation.Rendering;

using WukongBench.Domain.Models;

/// <summary>
/// Renders the final report to the console. Everything the task asks for is in
/// this output: machine characteristics, both results and the settings that were
/// used for each pass.
/// </summary>
public sealed class ConsoleReportRenderer
{
    private const int Width = 92;

    private readonly TextWriter _writer;

    public ConsoleReportRenderer(TextWriter writer)
    {
        _writer = writer;
    }

    public void Render(BenchmarkReport report)
    {
        Rule('=');
        _writer.WriteLine(" BLACK MYTH: WUKONG BENCHMARK TOOL - AUTOMATED CPU / GPU REPORT");
        _writer.WriteLine($" {report.ToolName} {report.ToolVersion}, generated {Format.Timestamp(report.GeneratedAt)}");
        Rule('=');
        _writer.WriteLine($" Installation : {report.InstallationSummary}");
        _writer.WriteLine($" Settings file: {report.SettingsFilePath}");
        _writer.WriteLine();

        RenderHardware(report.Hardware);
        RenderRun(report.CpuRun, "CPU-BOUND PASS");
        RenderRun(report.GpuRun, "GPU-BOUND PASS");
        RenderNotes(report);
    }

    public void RenderHardware(HardwareProfile hardware)
    {
        Section("MACHINE");
        _writer.WriteLine($"  {"Machine",-14}: {Format.Text(hardware.System.MachineName)}");
        _writer.WriteLine($"  {"CPU",-14}: {Format.Text(hardware.Cpu.Name)}");
        _writer.WriteLine($"  {"CPU cores",-14}: {hardware.Cpu.PhysicalCores} physical / {hardware.Cpu.LogicalProcessors} logical, " +
                          $"{Format.Text(hardware.Cpu.MaxClockMhz)} max, {Format.Text(hardware.Cpu.Architecture)}");
        _writer.WriteLine($"  {"GPU",-14}: {Format.Text(hardware.Gpu.Name)}");
        _writer.WriteLine($"  {"GPU driver",-14}: {Format.Text(hardware.Gpu.DriverVersion)} ({Format.Text(hardware.Gpu.DriverDate)})");
        _writer.WriteLine($"  {"VRAM",-14}: {Format.Text(hardware.Gpu.AdapterRamBytes)}");
        _writer.WriteLine($"  {"Display",-14}: {Format.Text(hardware.Gpu.CurrentResolution)}" +
                          (string.IsNullOrWhiteSpace(hardware.Gpu.CurrentRefreshRateHz) ? string.Empty : $" @ {hardware.Gpu.CurrentRefreshRateHz} Hz"));
        _writer.WriteLine($"  {"RAM",-14}: {Format.Text(hardware.Memory.TotalBytes)}" +
                          (string.IsNullOrWhiteSpace(hardware.Memory.SpeedMhz) ? string.Empty : $" @ {hardware.Memory.SpeedMhz} MHz"));
        _writer.WriteLine($"  {"RAM modules",-14}: {Format.Text(hardware.Memory.ModulesSummary)}");
        _writer.WriteLine($"  {"Page file",-14}: {Format.Text(hardware.Memory.PageFileSummary)}");
        _writer.WriteLine($"  {"OS",-14}: {Format.Text(hardware.System.OperatingSystem)} ({Format.Text(hardware.System.OsVersion)}, build {Format.Text(hardware.System.OsBuild)}, {Format.Text(hardware.System.OsArchitecture)})");
        _writer.WriteLine($"  {"Motherboard",-14}: {Format.Text(hardware.System.Motherboard)}");
        _writer.WriteLine($"  {"BIOS",-14}: {Format.Text(hardware.System.BiosVersion)}");
        _writer.WriteLine($"  {"Storage",-14}: {Format.Text(hardware.System.PrimaryStorage)}");
        _writer.WriteLine($"  {"Power scheme",-14}: {Format.Text(hardware.System.ActivePowerScheme)}");
        _writer.WriteLine($"  {"Virtual machine",-14}: {Format.Text(hardware.System.IsVirtualMachine)}");
        _writer.WriteLine();
    }

    public void RenderRun(BenchmarkRun? run, string title)
    {
        if (run is null)
        {
            Section(title);
            _writer.WriteLine("  Not executed in this run.");
            _writer.WriteLine();
            return;
        }

        Section(title);
        _writer.WriteLine($"  Scenario     : {run.Scenario.DisplayName} ({run.Scenario.Settings.Name})");
        _writer.WriteLine($"  Status       : {(run.Succeeded ? "OK" : "FAILED")} - {run.StatusMessage}");
        _writer.WriteLine($"  Started      : {Format.Timestamp(run.StartedAt)}");
        _writer.WriteLine($"  Duration     : {Format.Duration(run.CompletedAt - run.StartedAt)}");
        _writer.WriteLine($"  Why it tests {(run.Scenario.Kind == BenchmarkScenarioKind.Cpu ? "the CPU" : "the GPU")}: {run.Scenario.Rationale}");
        _writer.WriteLine();

        var metrics = run.Metrics;
        _writer.WriteLine("  Results (as reported by the Benchmark Tool):");
        _writer.WriteLine($"    {"Average FPS",-16}: {Format.Fps(metrics?.AverageFps)}");
        _writer.WriteLine($"    {"Minimum FPS",-16}: {Format.Fps(metrics?.MinimumFps)}");
        _writer.WriteLine($"    {"Maximum FPS",-16}: {Format.Fps(metrics?.MaximumFps)}");
        _writer.WriteLine($"    {"FPS 95th pct",-16}: {Format.Fps(metrics?.Percentile95Fps)}");
        _writer.WriteLine($"    {"1% low (frames)",-16}: {Format.Fps(metrics?.Percentile1LowFps)}");
        _writer.WriteLine($"    {"Avg frame time",-16}: {Format.Millis(metrics?.AverageFrameTimeMs)}");
        _writer.WriteLine($"    {"CPU frame time",-16}: {Format.Millis(metrics?.AverageCpuFrameTimeMs)}");
        _writer.WriteLine($"    {"GPU frame time",-16}: {Format.Millis(metrics?.AverageGpuFrameTimeMs)}");
        _writer.WriteLine($"    {"VRAM peak",-16}: {Format.MegaBytes(metrics?.MaximumVramUsageMb)}" +
                          (string.IsNullOrWhiteSpace(metrics?.VramSizeLabel) ? string.Empty : $" (of {metrics!.VramSizeLabel})"));
        _writer.WriteLine($"    {"VRAM average",-16}: {Format.MegaBytes(metrics?.AverageVramUsageMb)}");
        _writer.WriteLine($"    {"CPU utilisation",-16}: {Format.Percent(metrics?.CpuUtilizationPercent)}");
        _writer.WriteLine($"    {"GPU utilisation",-16}: {Format.Percent(metrics?.GpuUtilizationPercent)}");
        _writer.WriteLine($"    {"Resolution",-16}: {Format.Text(metrics?.Resolution ?? string.Empty)}");
        _writer.WriteLine($"    {"GPU leak",-16}: {Format.Percent(metrics?.GpuLeakPercent)}");

        if (metrics is not null)
        {
            _writer.WriteLine($"    {"Raw result",-16}: {metrics.SourcePath}");
            _writer.WriteLine($"    {"Tool version",-16}: {Format.Text(metrics.ReportedSystem.BenchmarkToolVersion)} (game {Format.Text(metrics.ReportedSystem.GameVersion)})");
        }

        _writer.WriteLine();
        _writer.WriteLine("  Settings applied for this pass:");
        foreach (var setting in run.Scenario.Settings.Values)
        {
            _writer.WriteLine($"    {setting.Section} {setting.Key} = {setting.Value}");
        }

        foreach (var setting in run.Scenario.Settings.UiSettings)
        {
            _writer.WriteLine($"    UISettingData.{setting.Key} = {setting.Value}");
        }

        _writer.WriteLine();
    }

    public void RenderNotes(BenchmarkReport report)
    {
        Section("ARTEFACTS");
        foreach (var path in report.ArtifactPaths)
        {
            _writer.WriteLine($"  - {path}");
        }

        _writer.WriteLine();
        Section("NOTES");
        if (report.Notes.Count == 0)
        {
            _writer.WriteLine("  - none");
        }
        else
        {
            foreach (var note in report.Notes)
            {
                _writer.WriteLine($"  - {note}");
            }
        }

        _writer.WriteLine();
    }

    public void RenderDiagnostics(DiagnosticsSnapshot snapshot)
    {
        Rule('=');
        _writer.WriteLine(" DIAGNOSTICS");
        Rule('=');

        if (snapshot.Installation is null)
        {
            _writer.WriteLine("  The benchmark tool was not located.");
        }
        else
        {
            _writer.WriteLine($"  Install dir : {snapshot.Installation.InstallDirectory}");
            _writer.WriteLine($"  Executable  : {snapshot.Installation.ExecutablePath}");
            _writer.WriteLine($"  Manifest    : {Format.Text(snapshot.Installation.ManifestPath)}");
            _writer.WriteLine($"  Build id    : {Format.Text(snapshot.Installation.BuildId)}");
        }

        _writer.WriteLine($"  Settings    : {snapshot.SettingsFilePath} ({(snapshot.SettingsFileExists ? "found" : "missing")})");
        _writer.WriteLine();

        RenderHardware(snapshot.Hardware);

        Section("CURRENT SETTINGS KEYS");
        if (snapshot.CurrentSettings.Count == 0)
        {
            _writer.WriteLine("  (none)");
        }
        else
        {
            foreach (var group in snapshot.CurrentSettings.GroupBy(entry => entry.Section))
            {
                _writer.WriteLine($"  {group.Key}");
                foreach (var entry in group)
                {
                    _writer.WriteLine($"    {entry.Key} = {entry.Value}");
                }
            }
        }

        _writer.WriteLine();
        Section("RESULT LOCATIONS");
        foreach (var location in snapshot.WatchedResultLocations)
        {
            _writer.WriteLine($"  - {location}");
        }

        _writer.WriteLine();
        _writer.WriteLine($"  Existing result files: {snapshot.ExistingResultFiles.Count}");
        foreach (var file in snapshot.ExistingResultFiles.Take(10))
        {
            _writer.WriteLine($"    {file}");
        }

        _writer.WriteLine($"  Existing log files   : {snapshot.BenchmarkLogFiles.Count}");
        foreach (var file in snapshot.BenchmarkLogFiles)
        {
            _writer.WriteLine($"    {file}");
        }

        _writer.WriteLine();
        Section("NOTES");
        foreach (var note in snapshot.Notes)
        {
            _writer.WriteLine($"  - {note}");
        }
    }

    private void Section(string title)
    {
        _writer.WriteLine(title);
        _writer.WriteLine(new string('-', Width));
    }

    private void Rule(char character) => _writer.WriteLine(new string(character, Width));
}
