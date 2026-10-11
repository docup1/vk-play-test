namespace WukongBench.Presentation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WukongBench.Application.Automation;
using WukongBench.Application.Configuration;
using WukongBench.Application.Models;
using WukongBench.Application.Scenarios;
using WukongBench.Application.UseCases;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Automation;
using WukongBench.Infrastructure.Configuration;
using WukongBench.Infrastructure.Hardware;
using WukongBench.Infrastructure.Input;
using WukongBench.Infrastructure.Logging;
using WukongBench.Infrastructure.Power;
using WukongBench.Infrastructure.Process;
using WukongBench.Infrastructure.Results;
using WukongBench.Infrastructure.Settings;
using WukongBench.Infrastructure.Steam;
using WukongBench.Infrastructure.Time;
using WukongBench.Infrastructure.Workspace;
using WukongBench.Presentation.Cli;
using WukongBench.Presentation.Rendering;

/// <summary>
/// Composition root. This is the only project that knows both the Application
/// and the Infrastructure layer: it binds the configuration, registers the
/// Infrastructure adapters under the Domain ports and executes the use cases.
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var cli = CliArguments.Parse(args);

        if (cli.ShowHelp)
        {
            Console.WriteLine(CliArguments.HelpText);
            return 0;
        }

        if (cli.CpuOnly && cli.GpuOnly)
        {
            Console.Error.WriteLine("--cpu-only and --gpu-only are mutually exclusive.");
            return 2;
        }

        var options = LoadOptions(cli);
        var artifactsRoot = Path.GetFullPath(options.ArtifactsDirectory);
        Directory.CreateDirectory(artifactsRoot);

        var logPath = Path.Combine(artifactsRoot, $"wukongbench-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        var logger = new ConsoleToolLogger(logPath, cli.Verbose);

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            logger.Warning("Cancellation requested - finishing the current step ...");
            cancellation.Cancel();
        };

        var provider = BuildServiceProvider(options, cli, logger, artifactsRoot);
        var console = new ConsoleReportRenderer(Console.Out);

        try
        {
            if (cli.HardwareOnly)
            {
                var hardware = await provider.GetRequiredService<IHardwareInfoProvider>().ReadAsync(cancellation.Token);
                console.RenderHardware(hardware);
                return 0;
            }

            if (cli.Diagnostics)
            {
                var diagnostics = await provider.GetRequiredService<RunDiagnosticsUseCase>().ExecuteAsync(cancellation.Token);
                console.RenderDiagnostics(diagnostics);
                WriteDiagnosticsArtifacts(artifactsRoot, diagnostics);
                return 0;
            }

            var request = new BenchmarkSuiteRequest(
                runCpuPass: !cli.GpuOnly,
                runGpuPass: !cli.CpuOnly,
                keepModifiedSettings: cli.KeepSettings,
                launchThroughSteam: !cli.DirectLaunch,
                useHighPerformancePowerScheme: options.UseHighPerformancePowerScheme,
                uiAutomationEnabled: !cli.NoUiAutomation);

            var report = await provider.GetRequiredService<RunBenchmarkSuiteUseCase>()
                .ExecuteAsync(request, cancellation.Token);

            console.Render(report);
            var reportDirectory = WriteReport(report, artifactsRoot);

            Console.WriteLine($"Report files written to: {reportDirectory}");
            return report.CpuRun?.Succeeded == false || report.GpuRun?.Succeeded == false ? 1 : 0;
        }
        catch (OperationCanceledException)
        {
            logger.Warning("Cancelled.");
            return 1;
        }
        catch (Exception exception)
        {
            logger.Error($"The run failed: {exception.Message}", exception);
            return 1;
        }
        finally
        {
            provider.Dispose();
            Console.WriteLine($"Log file: {logPath}");
        }
    }

    private static WukongBenchOptions LoadOptions(CliArguments cli)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var configPath = cli.ConfigPath ?? Path.Combine(baseDirectory, "appsettings.json");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(baseDirectory)
            .AddJsonFile(configPath, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("WUKONGBENCH_")
            .Build();

        var options = configuration.GetSection(WukongBenchOptions.SectionName).Get<WukongBenchOptions>()
                      ?? new WukongBenchOptions();

        if (!string.IsNullOrWhiteSpace(cli.OutputDirectory))
        {
            options.ArtifactsDirectory = cli.OutputDirectory;
        }

        if (!string.IsNullOrWhiteSpace(cli.ExecutablePath))
        {
            options.ExecutablePathOverride = cli.ExecutablePath;
        }

        if (!string.IsNullOrWhiteSpace(cli.SteamRoot))
        {
            options.SteamRootOverride = cli.SteamRoot;
        }

        // A relative artifacts folder is resolved next to the executable, so the
        // output location does not depend on the current working directory.
        if (!Path.IsPathRooted(options.ArtifactsDirectory))
        {
            options.ArtifactsDirectory = Path.Combine(baseDirectory, options.ArtifactsDirectory);
        }

        return options;
    }

    /// <summary>Registers the object graph: Application use cases over Infrastructure adapters.</summary>
    private static ServiceProvider BuildServiceProvider(
        WukongBenchOptions options,
        CliArguments cli,
        ConsoleToolLogger logger,
        string artifactsRoot)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IToolLogger>(logger);
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(Options.Create(new SteamLocatorOptions
        {
            SteamRootOverride = options.SteamRootOverride,
            ExecutablePathOverride = options.ExecutablePathOverride,
        }));
        services.AddSingleton(Options.Create(new SettingsStoreOptions()));
        services.AddSingleton(Options.Create(new ResultReaderOptions()));

        // Infrastructure adapters bound to the Domain ports.
        services.AddSingleton<ISteamInstallationLocator, SteamInstallationLocator>();
        services.AddSingleton<IGraphicsSettingsStoreFactory, UeGameUserSettingsStoreFactory>();
        services.AddSingleton<IBenchmarkProcessLauncher, BenchmarkProcessLauncher>();
        services.AddSingleton<IBenchmarkReadinessProbe, TimedReadinessProbe>();
        services.AddSingleton<IKeyboardUiAutomation, Win32KeyboardUiAutomation>();
        services.AddSingleton<IBenchmarkResultReader, BenchmarkHistoryResultReader>();
        services.AddSingleton<IArtifactCollector, BenchmarkArtifactCollector>();
        services.AddSingleton<IHardwareInfoProvider, WmiHardwareInfoProvider>();
        services.AddSingleton<IPowerSchemeManager, PowerCfgSchemeManager>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IBenchmarkWorkspace>(_ => new FileSystemBenchmarkWorkspace(artifactsRoot));

        // Application services.
        services.AddSingleton<BenchmarkScenarioFactory>();
        services.AddSingleton<UiScriptCatalog>();
        services.AddSingleton<RunBenchmarkSuiteUseCase>();
        services.AddSingleton<RunDiagnosticsUseCase>();

        // Presentation services.
        services.AddSingleton<MarkdownReportRenderer>();
        services.AddSingleton<JsonReportRenderer>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private static string WriteReport(BenchmarkReport report, string artifactsRoot)
    {
        var suiteDirectory = report.ArtifactPaths
            .FirstOrDefault(path => Directory.Exists(path) && Path.GetFileName(path).EndsWith("-suite", StringComparison.Ordinal))
            ?? artifactsRoot;

        var markdown = new MarkdownReportRenderer().Render(report);
        File.WriteAllText(Path.Combine(suiteDirectory, "report.md"), markdown);
        File.WriteAllText(Path.Combine(suiteDirectory, "report.json"), new JsonReportRenderer().Render(report));

        return suiteDirectory;
    }

    private static void WriteDiagnosticsArtifacts(string artifactsRoot, DiagnosticsSnapshot snapshot)
    {
        var directory = Path.Combine(artifactsRoot, $"diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "diagnostics.json"), new JsonReportRenderer().RenderDiagnostics(snapshot));
        Console.WriteLine($"Diagnostics written to: {directory}");
    }
}
