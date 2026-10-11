# Weather by city (wttr.in)

Консольное приложение на C#, которое загружает список городов из файла,
получает по каждому уникальному городу текущую погоду через
`https://wttr.in/{City}?format=j1` и выводит отчёт: погоду по городам и
агрегированную статистику по странам.

## Запуск

```bash
dotnet run --project src/WeatherApp.Presentation/WeatherApp.Presentation.csproj
```

Путь к файлу со списком городов можно передать аргументом:

```bash
dotnet run --project src/WeatherApp.Presentation/WeatherApp.Presentation.csproj -- ./Cities.txt
```

Если аргумент не передан, файл `Cities.txt` ищется рядом с исполняемым файлом,
а затем в текущем каталоге.

## Пример вывода

```
Weather by city:

Moscow, Russia +12 °C
Khabarovsk, Russia +7 °C
Saint-Petersburg, Russia +9 °C
Vienna, Austria +16 °C
Izhevsk, Russia +8 °C
Perm, Russia +9 °C
NhaTrang, Vietnam +26 °C
Villach, Austria +13 °C

Weather by country:

Austria - 2 cities, avg: +15 °C, min: +13 °C, max: +16 °C
Russia - 5 cities, avg: +9 °C, min: +7 °C, max: +12 °C
Vietnam - 1 city, avg: +26 °C, min: +26 °C, max: +26 °C
```

## Архитектура: 4 слоя

```
WeatherApp.Domain          модели + интерфейсы (без логики и без зависимостей)
      ^            ^
      |            |
WeatherApp.     WeatherApp.
Application     Infrastructure
      ^            ^
       \          /
     WeatherApp.Presentation   (DI / composition root + вывод в консоль)
```

| Слой | Проект | Содержимое |
|---|---|---|
| Domain | `src/WeatherApp.Domain` | `WeatherData`, `CountryWeatherStatistic`, `WeatherFetchFailure`, `WeatherReport`, `ICityListReader`, `IWeatherApiClient` |
| Application | `src/WeatherApp.Application` | `IWeatherReportService`, `WeatherReportService` |
| Infrastructure | `src/WeatherApp.Infrastructure` | `FileCityListReader`, `WttrWeatherApiClient` (+ DTO для System.Text.Json) |
| Presentation | `src/WeatherApp.Presentation` | `Program`, `CompositionRoot`, `ConsoleWeatherReportPrinter`, `CityListPathResolver` |

Правила зависимостей:

* **Domain** не ссылается ни на что. Здесь только модели (состояние + конструктор)
  и интерфейсы — никакой логики вычислений и никаких деталей реализации.
* **Application** ссылается только на **Domain** и работает исключительно через его
  интерфейсы (`ICityListReader`, `IWeatherApiClient`). Вся логика (дедупликация,
  группировка, расчёт средней/минимальной/максимальной температуры) — здесь.
* **Infrastructure** ссылается только на **Domain** и реализует его интерфейсы
  (`FileCityListReader : ICityListReader`, `WttrWeatherApiClient : IWeatherApiClient`).
* **Application** и **Infrastructure** не ссылаются друг на друга — ни в одну сторону.
* **Presentation** ссылается на все слои и выполняет DI: в `CompositionRoot`
  выбираются конкретные реализации и подставляются в сервис приложения.

## Ограничения задания

* JSON — только `System.Text.Json` (из BCL). `Newtonsoft.Json` не понадобился.
* Сторонние NuGet-пакеты не используются вообще: DI выполняется вручную
  в `CompositionRoot` (`Microsoft.Extensions.DependencyInjection` — это внешний пакет).
* Все значения (количество городов, средняя/минимальная/максимальная температура)
  считаются кодом, а не берутся из ответа API.

## Поведение

* Список городов загружается в память целиком; пустые строки и лишние пробелы
  отбрасываются.
* Дубликаты городов убираются без учёта регистра, порядок первого появления
  сохраняется (проверено: `Moscow` + `moscow` → один город).
* Запросы выполняются последовательно, с 3 попытками и паузой между ними —
  wttr.in иногда отвечает не с первого раза.
* Если по городу данные получить не удалось, город пропускается, а причина
  выводится отдельным блоком; остальной отчёт строится.
* Средняя температура округляется до целого градуса при выводе (как в примере
  задания), значения печатаются со знаком: `+18 °C`, `-4 °C`, `0 °C`.
