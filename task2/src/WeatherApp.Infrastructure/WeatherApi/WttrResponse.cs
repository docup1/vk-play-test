using System.Text.Json.Serialization;

namespace WeatherApp.Infrastructure.WeatherApi;

/// <summary>
/// DTO-ответа https://wttr.in/{City}?format=j1 (нужны только используемые поля).
/// </summary>
internal sealed class WttrResponse
{
    [JsonPropertyName("current_condition")]
    public WttrCurrentCondition[]? CurrentCondition { get; set; }

    [JsonPropertyName("nearest_area")]
    public WttrNearestArea[]? NearestArea { get; set; }
}

internal sealed class WttrCurrentCondition
{
    [JsonPropertyName("temp_C")]
    public string? TemperatureCelsius { get; set; }

    [JsonPropertyName("weatherDesc")]
    public WttrValue[]? WeatherDescription { get; set; }
}

internal sealed class WttrNearestArea
{
    [JsonPropertyName("areaName")]
    public WttrValue[]? AreaName { get; set; }

    [JsonPropertyName("country")]
    public WttrValue[]? Country { get; set; }
}

internal sealed class WttrValue
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
