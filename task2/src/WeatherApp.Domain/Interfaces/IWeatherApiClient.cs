using WeatherApp.Domain.Models;

namespace WeatherApp.Domain.Interfaces;

/// <summary>
/// Абстракция поставщика данных о погоде.
/// Реализация живёт в слое Infrastructure и не знает об Application.
/// </summary>
public interface IWeatherApiClient
{
    /// <summary>
    /// Получает текущие данные о погоде для указанного города.
    /// </summary>
    /// <param name="city">Название города.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные о погоде в городе.</returns>
    Task<WeatherData> GetWeatherAsync(string city, CancellationToken cancellationToken = default);
}
