namespace WukongBench.Presentation.Cli;

/// <summary>
/// Command line switches of the tool. Parsing is deliberately manual: the tool
/// has a small, documented surface and no dependency on a CLI framework.
/// </summary>
public sealed class CliArguments
{
    public bool ShowHelp { get; private init; }

    public bool CpuOnly { get; private init; }

    public bool GpuOnly { get; private init; }

    public bool HardwareOnly { get; private init; }

    public bool Diagnostics { get; private init; }

    public bool KeepSettings { get; private init; }

    public bool NoUiAutomation { get; private init; }

    public bool DirectLaunch { get; private init; }

    public bool Verbose { get; private init; }

    public string? ConfigPath { get; private init; }

    public string? OutputDirectory { get; private init; }

    public string? ExecutablePath { get; private init; }

    public string? SteamRoot { get; private init; }

    public static CliArguments Parse(string[] args)
    {
        var result = new CliArgumentsBuilder();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "-h":
                case "--help":
                case "/?":
                    result.ShowHelp = true;
                    break;
                case "--cpu-only":
                    result.CpuOnly = true;
                    break;
                case "--gpu-only":
                    result.GpuOnly = true;
                    break;
                case "--hardware-only":
                    result.HardwareOnly = true;
                    break;
                case "--diagnostics":
                    result.Diagnostics = true;
                    break;
                case "--keep-settings":
                    result.KeepSettings = true;
                    break;
                case "--no-ui":
                    result.NoUiAutomation = true;
                    break;
                case "--direct-launch":
                    result.DirectLaunch = true;
                    break;
                case "--verbose":
                    result.Verbose = true;
                    break;
                case "--config":
                    result.ConfigPath = Next(args, ref i);
                    break;
                case "--output":
                    result.OutputDirectory = Next(args, ref i);
                    break;
                case "--executable":
                    result.ExecutablePath = Next(args, ref i);
                    break;
                case "--steam-root":
                    result.SteamRoot = Next(args, ref i);
                    break;
                default:
                    Console.Error.WriteLine($"Unknown argument '{arg}'. Use --help to list the supported switches.");
                    break;
            }
        }

        return result.Build();
    }

    private static string? Next(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            return null;
        }

        index++;
        return args[index];
    }

    public static string HelpText => """
        wukongbench - automated Black Myth: Wukong Benchmark Tool runs

        Usage:
          wukongbench [options]

        The default run performs the complete scenario:
          1. applies the CPU-bound settings and runs the benchmark;
          2. applies the GPU-bound settings and runs the benchmark again;
          3. reads both results from the tool's history files and prints a report.

        Options:
          --cpu-only            run only the CPU-bound pass
          --gpu-only            run only the GPU-bound pass
          --hardware-only       print the hardware profile and exit (no benchmark)
          --diagnostics         print the located installation, the current settings keys
                                and the result folders, then exit
          --keep-settings       do not restore the original GameUserSettings.ini at the end
          --no-ui               do not send keyboard/mouse input (start the benchmark by hand,
                                the tool only waits for the result file)
          --direct-launch       start b1_benchmark.exe directly instead of through Steam
          --config <path>       use another appsettings.json
          --output <dir>        write the report and the raw results into this folder
          --executable <path>   explicit path of the benchmark executable
          --steam-root <path>   explicit Steam root folder
          --verbose             verbose logging
          -h, --help            show this help

        Exit codes:
          0  success (a report was produced)
          1  the run failed
          2  invalid arguments
        """;

    private sealed class CliArgumentsBuilder
    {
        public bool ShowHelp { get; set; }
        public bool CpuOnly { get; set; }
        public bool GpuOnly { get; set; }
        public bool HardwareOnly { get; set; }
        public bool Diagnostics { get; set; }
        public bool KeepSettings { get; set; }
        public bool NoUiAutomation { get; set; }
        public bool DirectLaunch { get; set; }
        public bool Verbose { get; set; }
            public string? ConfigPath { get; set; }
        public string? OutputDirectory { get; set; }
        public string? ExecutablePath { get; set; }
        public string? SteamRoot { get; set; }

        public CliArguments Build() => new()
        {
            ShowHelp = ShowHelp,
            CpuOnly = CpuOnly,
            GpuOnly = GpuOnly,
            HardwareOnly = HardwareOnly,
            Diagnostics = Diagnostics,
            KeepSettings = KeepSettings,
            NoUiAutomation = NoUiAutomation,
            DirectLaunch = DirectLaunch,
            Verbose = Verbose,
            ConfigPath = ConfigPath,
            OutputDirectory = OutputDirectory,
            ExecutablePath = ExecutablePath,
            SteamRoot = SteamRoot,
        };
    }
}
