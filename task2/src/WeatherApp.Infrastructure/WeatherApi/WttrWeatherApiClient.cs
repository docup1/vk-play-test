using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using WeatherApp.Domain.Interfaces;
using WeatherApp.Domain.Models;

namespace WeatherApp.Infrastructure.WeatherApi;

/// <summary>
/// Клиент сервиса wttr.in, реализующий интерфейс домена <see cref="IWeatherApiClient"/>.
/// JSON разбирается средствами System.Text.Json (внешних пакетов нет).
/// </summary>
public sealed class WttrWeatherApiClient : IWeatherApiClient
{
    private const string ServiceBaseUrl = "https://wttr.in";
    private const int DefaultMaxAttempts = 3;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly int _maxAttempts;

    public WttrWeatherApiClient(HttpClient httpClient, int maxAttempts = DefaultMaxAttempts)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Количество попыток должно быть не меньше одной.");
        }

        _maxAttempts = maxAttempts;

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("WeatherApp", "1.0"));
        }

        if (!_httpClient.DefaultRequestHeaders.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json")))
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public async Task<WeatherData> GetWeatherAsync(
        string city,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("Название города не задано.", nameof(city));
        }

        string requestUri = $"{ServiceBaseUrl}/{Uri.EscapeDataString(city.Trim())}?format=j1";
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await RequestWeatherAsync(city.Trim(), requestUri, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException or TaskCanceledException)
            {
                lastException = exception;

                if (attempt < _maxAttempts)
                {
                    // Небольшая пауза перед повтором: сервис иногда отвечает не с первого раза.
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        throw new InvalidOperationException(
            $"Не удалось получить данные о погоде для города «{city}» после {_maxAttempts} попыток.",
            lastException);
    }

    private async Task<WeatherData> RequestWeatherAsync(
        string city,
        string requestUri,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient
            .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using Stream contentStream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        WttrResponse? payload = await JsonSerializer
            .DeserializeAsync<WttrResponse>(contentStream, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return MapToWeatherData(city, payload);
    }

    private static WeatherData MapToWeatherData(string requestedCity, WttrResponse? payload)
    {
        if (payload is null)
        {
            throw new InvalidOperationException("Ответ API пуст или не является корректным JSON.");
        }

        WttrCurrentCondition? currentCondition = payload.CurrentCondition?.FirstOrDefault()
            ?? throw new InvalidOperationException("В ответе API отсутствует блок current_condition.");

        string rawTemperature = currentCondition.TemperatureCelsius
            ?? throw new InvalidOperationException("В ответе API отсутствует поле current_condition.temp_C.");

        if (!double.TryParse(rawTemperature, NumberStyles.Float, CultureInfo.InvariantCulture, out double temperature))
        {
            throw new InvalidOperationException($"Не удалось разобрать температуру «{rawTemperature}».");
        }

        string? country = payload.NearestArea?.FirstOrDefault()?.Country?.FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new InvalidOperationException("В ответе API отсутствует страна (nearest_area.country).");
        }

        return new WeatherData(requestedCity, temperature, country);
    }
}
