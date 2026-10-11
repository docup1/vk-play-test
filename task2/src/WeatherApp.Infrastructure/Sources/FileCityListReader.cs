using WeatherApp.Domain.Interfaces;

namespace WeatherApp.Infrastructure.Sources;

/// <summary>
/// Читает список городов из текстового файла: одна строка — один город.
/// Реализует интерфейс домена <see cref="ICityListReader"/>.
/// </summary>
public sealed class FileCityListReader : ICityListReader
{
    public async Task<IReadOnlyList<string>> ReadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Путь к файлу со списком городов не задан.", nameof(source));
        }

        if (!File.Exists(source))
        {
            throw new FileNotFoundException($"Файл со списком городов не найден: {source}", source);
        }

        string[] lines = await File
            .ReadAllLinesAsync(source, cancellationToken)
            .ConfigureAwait(false);

        var cities = new List<string>(lines.Length);

        foreach (string line in lines)
        {
            string city = line.Trim();

            if (city.Length == 0)
            {
                continue;
            }

            cities.Add(city);
        }

        return cities;
    }
}
