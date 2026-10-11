using System.Globalization;
using WeatherApp.Domain.Models;

namespace WeatherApp.Presentation.Composition;

/// <summary>
/// Вывод отчёта в консоль в формате из задания:
/// <code>
/// Tokyo, Japan +18 °C
/// Japan - 3 cities, avg: +19 °C, min: +16 °C, max: +22 °C
/// </code>
/// </summary>
public sealed class ConsoleWeatherReportPrinter : IWeatherReportPrinter
{
    private readonly TextWriter _output;

    public ConsoleWeatherReportPrinter(TextWriter output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public void PrintCities(IReadOnlyList<WeatherData> cities)
    {
        ArgumentNullException.ThrowIfNull(cities);

        _output.WriteLine("Weather by city:");
        _output.WriteLine();

        foreach (WeatherData city in cities)
        {
            _output.WriteLine($"{city.City}, {city.Country} {FormatTemperature(city.TemperatureCelsius)}");
        }
    }

    public void PrintCountryStatistics(IReadOnlyList<CountryWeatherStatistic> statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        _output.WriteLine();
        _output.WriteLine("Weather by country:");
        _output.WriteLine();

        foreach (CountryWeatherStatistic country in statistics)
        {
            _output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0} - {1} {2}, avg: {3}, min: {4}, max: {5}",
                country.Country,
                country.CityCount,
                country.CityCount == 1 ? "city" : "cities",
                FormatTemperature(country.AverageTemperatureCelsius),
                FormatTemperature(country.MinTemperatureCelsius),
                FormatTemperature(country.MaxTemperatureCelsius)));
        }
    }

    public void PrintFailures(IReadOnlyList<WeatherFetchFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        if (failures.Count == 0)
        {
            return;
        }

        _output.WriteLine();
        _output.WriteLine("Could not get weather for:");
        _output.WriteLine();

        foreach (WeatherFetchFailure failure in failures)
        {
            _output.WriteLine($"{failure.City} - {failure.Reason}");
        }
    }

    /// <summary>
    /// Средние значения округляются до целого градуса, как в примере вывода,
    /// и всегда печатаются со знаком (+18 °C, -4 °C, 0 °C).
    /// </summary>
    private static string FormatTemperature(double temperatureCelsius)
    {
        double rounded = Math.Round(temperatureCelsius, MidpointRounding.AwayFromZero);
        return rounded.ToString("+0;-0;0", CultureInfo.InvariantCulture) + " °C";
    }
}
