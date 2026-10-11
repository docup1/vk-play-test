using WeatherApp.Domain.Models;

namespace WeatherApp.Presentation.Composition;

/// <summary>
/// Абстракция вывода отчёта. Это забота слоя Presentation,
/// поэтому и интерфейс, и реализация живут здесь.
/// </summary>
public interface IWeatherReportPrinter
{
    /// <summary>Выводит погоду по каждому городу одним сообщением на город.</summary>
    void PrintCities(IReadOnlyList<WeatherData> cities);

    /// <summary>Выводит агрегированную статистику по странам.</summary>
    void PrintCountryStatistics(IReadOnlyList<CountryWeatherStatistic> statistics);

    /// <summary>Выводит предупреждения о городах, по которым не удалось получить данные.</summary>
    void PrintFailures(IReadOnlyList<WeatherFetchFailure> failures);
}
