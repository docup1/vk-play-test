using System.Text;
using WeatherApp.Domain.Models;
using WeatherApp.Presentation.Composition;

namespace WeatherApp.Presentation;

/// <summary>
/// Точка входа консольного приложения.
/// Здесь же выполняется внедрение зависимостей (см. <see cref="CompositionRoot"/>).
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string citiesFilePath = CityListPathResolver.Resolve(args);

        using CancellationTokenSource cancellationTokenSource = CreateCancellationSource();

        try
        {
            using ApplicationContainer container = CompositionRoot.Compose(Console.Out);

            WeatherReport report = await container.WeatherReportService
                .BuildReportAsync(citiesFilePath, cancellationTokenSource.Token)
                .ConfigureAwait(false);

            container.Printer.PrintCities(report.Cities);
            container.Printer.PrintCountryStatistics(report.Countries);
            container.Printer.PrintFailures(report.Failures);

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Операция отменена.");
            return 130;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Ошибка: {exception.Message}");
            return 1;
        }
    }

    private static CancellationTokenSource CreateCancellationSource()
    {
        var source = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            source.Cancel();
        };

        return source;
    }
}
