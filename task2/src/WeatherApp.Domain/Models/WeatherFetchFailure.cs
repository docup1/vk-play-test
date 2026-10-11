namespace WeatherApp.Domain.Models;

/// <summary>
/// Город, для которого не удалось получить данные о погоде.
/// Модель домена: только состояние и конструктор.
/// </summary>
public sealed class WeatherFetchFailure
{
    public WeatherFetchFailure(string city, string reason)
    {
        City = string.IsNullOrWhiteSpace(city)
            ? throw new ArgumentException("Название города не может быть пустым.", nameof(city))
            : city.Trim();

        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    /// <summary>Название города, для которого не удалось получить данные.</summary>
    public string City { get; }

    /// <summary>Причина, по которой данные получить не удалось.</summary>
    public string Reason { get; }
}
