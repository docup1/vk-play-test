namespace WeatherApp.Domain.Interfaces;

/// <summary>
/// Абстракция источника списка городов.
/// Реализация живёт в слое Infrastructure.
/// </summary>
public interface ICityListReader
{
    /// <summary>
    /// Загружает список городов из источника в память.
    /// </summary>
    /// <param name="source">Путь к файлу со списком городов.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Названия городов в порядке их следования в источнике.</returns>
    Task<IReadOnlyList<string>> ReadAsync(string source, CancellationToken cancellationToken = default);
}
