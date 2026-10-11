using WeatherApp.Domain.Models;

namespace WeatherApp.Application.Services;

/// <summary>
/// Сценарий использования: построить отчёт по списку городов.
/// Интерфейс объявлен в слое Application, потому что это контракт приложения,
/// а не часть модели домена.
/// </summary>
public interface IWeatherReportService
{
    /// <summary>
    /// Загружает города из файла, получает погоду по каждому уникальному городу
    /// и считает статистику по странам.
    /// </summary>
    /// <param name="citiesFilePath">Путь к файлу со списком городов.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<WeatherReport> BuildReportAsync(string citiesFilePath, CancellationToken cancellationToken = default);
}
