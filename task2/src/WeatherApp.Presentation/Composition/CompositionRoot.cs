using WeatherApp.Application.Services;
using WeatherApp.Domain.Interfaces;
using WeatherApp.Infrastructure.Sources;
using WeatherApp.Infrastructure.WeatherApi;

namespace WeatherApp.Presentation.Composition;

/// <summary>
/// Готовый набор зависимостей приложения, собранный в слое Presentation.
/// </summary>
public sealed class ApplicationContainer : IDisposable
{
    public ApplicationContainer(
        IWeatherReportService weatherReportService,
        IWeatherReportPrinter printer,
        HttpClient httpClient)
    {
        WeatherReportService = weatherReportService ?? throw new ArgumentNullException(nameof(weatherReportService));
        Printer = printer ?? throw new ArgumentNullException(nameof(printer));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    private readonly HttpClient _httpClient;

    public IWeatherReportService WeatherReportService { get; }

    public IWeatherReportPrinter Printer { get; }

    public void Dispose() => _httpClient.Dispose();
}

/// <summary>
/// Композиционный корень (composition root).
/// Только этот слой знает обо всех остальных: он решает, какие реализации
/// интерфейсов домена подставить в сервисы приложения.
/// </summary>
public static class CompositionRoot
{
    /// <summary>
    /// Ручной DI: контейнер не используется, потому что сторонние NuGet-пакеты запрещены.
    /// </summary>
    public static ApplicationContainer Compose(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        // Infrastructure -> реализации интерфейсов Domain.
        ICityListReader cityListReader = new FileCityListReader();
        IWeatherApiClient weatherApiClient = new WttrWeatherApiClient(httpClient);

        // Application -> использует интерфейсы Domain, но не знает об Infrastructure.
        IWeatherReportService weatherReportService = new WeatherReportService(cityListReader, weatherApiClient);

        // Presentation -> отвечает за показ результата пользователю.
        IWeatherReportPrinter printer = new ConsoleWeatherReportPrinter(output);

        return new ApplicationContainer(weatherReportService, printer, httpClient);
    }
}
