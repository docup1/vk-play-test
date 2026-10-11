namespace WeatherApp.Domain.Models;

/// <summary>
/// Данные о погоде в конкретном городе.
/// Модель домена: только состояние и конструктор, никакой логики и никаких
/// зависимостей от способа получения данных (HTTP, файл, БД).
/// </summary>
public sealed class WeatherData
{
    public WeatherData(string city, double temperatureCelsius, string country)
    {
        City = string.IsNullOrWhiteSpace(city)
            ? throw new ArgumentException("Название города не может быть пустым.", nameof(city))
            : city.Trim();

        Country = string.IsNullOrWhiteSpace(country)
            ? throw new ArgumentException("Название страны не может быть пустым.", nameof(country))
            : country.Trim();

        TemperatureCelsius = temperatureCelsius;
    }

    /// <summary>Название города.</summary>
    public string City { get; }

    /// <summary>Текущая температура в градусах Цельсия.</summary>
    public double TemperatureCelsius { get; }

    /// <summary>Страна, в которой находится город.</summary>
    public string Country { get; }
}
