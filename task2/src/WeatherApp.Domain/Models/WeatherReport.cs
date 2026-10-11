namespace WeatherApp.Domain.Models;

/// <summary>
/// Готовый отчёт: погода по каждому городу и агрегированная статистика по странам.
/// Модель домена: только состояние и конструктор.
/// </summary>
public sealed class WeatherReport
{
    public WeatherReport(
        IReadOnlyList<WeatherData> cities,
        IReadOnlyList<CountryWeatherStatistic> countries,
        IReadOnlyList<WeatherFetchFailure> failures)
    {
        Cities = cities ?? throw new ArgumentNullException(nameof(cities));
        Countries = countries ?? throw new ArgumentNullException(nameof(countries));
        Failures = failures ?? throw new ArgumentNullException(nameof(failures));
    }

    /// <summary>Погода по каждому городу (в порядке следования в исходном файле).</summary>
    public IReadOnlyList<WeatherData> Cities { get; }

    /// <summary>Статистика по странам, отсортированная по названию страны.</summary>
    public IReadOnlyList<CountryWeatherStatistic> Countries { get; }

    /// <summary>Города, данные по которым получить не удалось.</summary>
    public IReadOnlyList<WeatherFetchFailure> Failures { get; }
}
