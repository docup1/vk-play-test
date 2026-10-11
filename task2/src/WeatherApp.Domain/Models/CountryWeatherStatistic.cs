namespace WeatherApp.Domain.Models;

/// <summary>
/// Сводная статистика по одной стране.
/// Модель домена: только состояние и конструктор.
/// </summary>
public sealed class CountryWeatherStatistic
{
    public CountryWeatherStatistic(
        string country,
        int cityCount,
        double averageTemperatureCelsius,
        double minTemperatureCelsius,
        double maxTemperatureCelsius)
    {
        Country = string.IsNullOrWhiteSpace(country)
            ? throw new ArgumentException("Название страны не может быть пустым.", nameof(country))
            : country.Trim();

        if (cityCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cityCount), "Количество городов не может быть отрицательным.");
        }

        CityCount = cityCount;
        AverageTemperatureCelsius = averageTemperatureCelsius;
        MinTemperatureCelsius = minTemperatureCelsius;
        MaxTemperatureCelsius = maxTemperatureCelsius;
    }

    /// <summary>Название страны.</summary>
    public string Country { get; }

    /// <summary>Количество уникальных городов страны.</summary>
    public int CityCount { get; }

    /// <summary>Средняя текущая температура по городам страны.</summary>
    public double AverageTemperatureCelsius { get; }

    /// <summary>Минимальная текущая температура по городам страны.</summary>
    public double MinTemperatureCelsius { get; }

    /// <summary>Максимальная текущая температура по городам страны.</summary>
    public double MaxTemperatureCelsius { get; }
}
