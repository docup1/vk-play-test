using WeatherApp.Domain.Interfaces;
using WeatherApp.Domain.Models;

namespace WeatherApp.Application.Services;

/// <summary>
/// Реализация сценария построения отчёта.
/// Слой Application работает только через интерфейсы домена и ничего не знает
/// о том, откуда берутся данные (файл, HTTP) и куда они выводятся.
/// </summary>
public sealed class WeatherReportService : IWeatherReportService
{
    private readonly ICityListReader _cityListReader;
    private readonly IWeatherApiClient _weatherApiClient;

    public WeatherReportService(ICityListReader cityListReader, IWeatherApiClient weatherApiClient)
    {
        _cityListReader = cityListReader ?? throw new ArgumentNullException(nameof(cityListReader));
        _weatherApiClient = weatherApiClient ?? throw new ArgumentNullException(nameof(weatherApiClient));
    }

    public async Task<WeatherReport> BuildReportAsync(
        string citiesFilePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(citiesFilePath))
        {
            throw new ArgumentException("Путь к файлу со списком городов не задан.", nameof(citiesFilePath));
        }

        // 1. Загружаем список городов из файла в память.
        IReadOnlyList<string> rawCities = await _cityListReader
            .ReadAsync(citiesFilePath, cancellationToken)
            .ConfigureAwait(false);

        // 2. Оставляем только уникальные города, сохраняя исходный порядок.
        IReadOnlyList<string> uniqueCities = GetUniqueCities(rawCities);

        // 3. Для каждого уникального города получаем данные через API.
        var weatherByCity = new List<WeatherData>(uniqueCities.Count);
        var failures = new List<WeatherFetchFailure>();

        foreach (string city in uniqueCities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                WeatherData weather = await _weatherApiClient
                    .GetWeatherAsync(city, cancellationToken)
                    .ConfigureAwait(false);

                weatherByCity.Add(weather);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Один недоступный город не должен ломать весь отчёт.
                failures.Add(new WeatherFetchFailure(city, exception.Message));
            }
        }

        // 4. Группируем города по странам и считаем статистику.
        IReadOnlyList<CountryWeatherStatistic> countryStatistics = BuildCountryStatistics(weatherByCity);

        return new WeatherReport(weatherByCity, countryStatistics, failures);
    }

    /// <summary>
    /// Убирает дубликаты названий городов без учёта регистра и лишних пробелов,
    /// сохраняя порядок первого появления.
    /// </summary>
    private static IReadOnlyList<string> GetUniqueCities(IReadOnlyList<string> cities)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unique = new List<string>(cities.Count);

        foreach (string city in cities)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                continue;
            }

            string normalized = city.Trim();

            if (seen.Add(normalized))
            {
                unique.Add(normalized);
            }
        }

        return unique;
    }

    /// <summary>
    /// Считает по каждой стране количество городов, среднюю, минимальную
    /// и максимальную текущую температуру. Страны отсортированы по названию.
    /// </summary>
    private static IReadOnlyList<CountryWeatherStatistic> BuildCountryStatistics(
        IReadOnlyList<WeatherData> weatherByCity)
    {
        var result = new List<CountryWeatherStatistic>();

        foreach (IGrouping<string, WeatherData> group in weatherByCity
                     .GroupBy(city => city.Country, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            List<WeatherData> cities = group.ToList();
            var temperatures = cities.Select(city => city.TemperatureCelsius).ToList();

            double average = temperatures.Average();
            double min = temperatures.Min();
            double max = temperatures.Max();

            result.Add(new CountryWeatherStatistic(
                country: group.Key,
                cityCount: cities.Count,
                averageTemperatureCelsius: average,
                minTemperatureCelsius: min,
                maxTemperatureCelsius: max));
        }

        return result;
    }
}
