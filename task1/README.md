# wukongbench — автоматизация Black Myth: Wukong Benchmark Tool

Инструмент на C# (.NET 9), который **полностью автоматически** прогоняет официальный
Black Myth: Wukong Benchmark Tool из Steam два раза — сначала в CPU-режиме, затем в
GPU-режиме — сам применяет настройки графики перед каждым прогоном, сам запускает
бенчмарк, сам дожидается окончания и сам разбирает результаты, после чего печатает и
сохраняет отчёт с характеристиками ПК, результатами обоих тестов и настройками,
которыми эти результаты получены.

```
$ wukongbench
...
============================================================================================
 BLACK MYTH: WUKONG BENCHMARK TOOL - AUTOMATED CPU / GPU REPORT
============================================================================================
...
============================================================================================
                                 RESULT TABLE (report.md / report.json)
```

---

## 1. Быстрый старт

Требуется Windows 10/11, x64, установленный Steam и установленный
[Black Myth: Wukong Benchmark Tool](https://store.steampowered.com/app/3132990/) (AppID **3132990**,
бесплатный). Нужен .NET 9 SDK (для сборки) либо .NET 9 Runtime (для запуска готовой сборки).

```powershell
# 1. сборка
dotnet build WukongBench.sln -c Release

# 2. (рекомендуется) сборка готового exe
dotnet publish src/WukongBench.Presentation -c Release -r win-x64 --self-contained false -o publish

# 3. полный сценарий: CPU-тест + GPU-тест + отчёт
.\publish\wukongbench.exe

# либо из исходников
dotnet run --project src/WukongBench.Presentation -c Release
```

> **Запускайте от имени администратора.** Файл настроек бенчмарка лежит внутри папки
> Steam (`...\steamapps\common\Black Myth Wukong Benchmark Tool\b1\Saved\Config\Windows\GameUserSettings.ini`),
> то есть обычно под `Program Files`. Без прав администратора его нельзя ни изменить, ни
> забэкапить.
>
> Перед первым реальным прогоном полезно один раз выполнить `wukongbench --diagnostics` —
> он ничего не запускает, а показывает найденную установку, характеристики ПК,
> **реальные имена ключей** в вашем файле настроек и папки, где будут искаться результаты.

### Результаты прогона

Всё складывается в `artifacts\` рядом с исполняемым файлом:

```
artifacts/
├─ wukongbench-20250812-180501.log          # полный лог прогона
├─ 20250812-180501-suite/                   # прогон целиком
│  ├─ GameUserSettings.original.ini         # бэкап исходных настроек
│  ├─ 180502-cpu-cpu-bound/
│  │  ├─ settings-applied.txt               # что именно было записано в ini
│  │  ├─ GameUserSettings.snapshot.ini      # снимок ini после применения
│  │  └─ result-<unixtime>.json             # сырой JSON результатов от самого бенчмарка
│  ├─ 180905-gpu-gpu-bound/
│  │  └─ ...
│  ├─ report.md                             # читаемый отчёт (Markdown)
│  └─ report.json                           # тот же отчёт в JSON
└─ diagnostics-.../diagnostics.json         # вывод режима --diagnostics
```

`report.md` — это и есть «удобный для чтения» результат: таблица характеристик ПК,
сводная таблица метрик CPU/GPU, и для каждого прогона — полный список применённых
настроек с пояснением, зачем каждая.

---

## 2. Что именно делает инструмент

1. **Собирает характеристики ПК** через WMI (+ реестр для объёма видеопамяти, т.к. WMI
   обрезает его на 4 ГБ): CPU (ядра/потоки/частоты), GPU (модель, драйвер, дата драйвера,
   VRAM), RAM (объём, модули, частота), ОС и build, материнская плата, BIOS, накопитель,
   активная схема питания, признак виртуальной машины.
2. **Находит установку** бенчмарка: реестр Steam → `libraryfolders.vdf` всех библиотек →
   `appmanifest_3132990.acf` → папка установки → `b1_benchmark.exe`
   (можно задать путь вручную: `--executable` / `--steam-root`).
3. **Применяет настройки CPU-прогона** в `GameUserSettings.ini`, предварительно сделав
   бэкап. Опционально переключает схему питания на «High performance».
4. **Запускает бенчмарк** через Steam (`steam.exe -applaunch 3132990`, по умолчанию) или
   напрямую `b1_benchmark.exe` (`--direct-launch`), дожидается появления окна.
5. **Прогоняет бенчмарк**: окно выводится в фокус и по сконфигурированному скрипту
   отправляются mouse/keyboard-события (меню бенчмарка не реагирует на клавиатуру, пока
   по нему не проведёт мышь — поэтому первый шаг скрипта это hover+click).
6. **Дожидается окончания** не по таймеру, а по факту: опрашивает каталог истории
   результатов и ждёт появления нового файла с ненулевым `FPSAvg`.
7. **Парсит результат**, копирует сырой JSON и снимок ini в папку прогона.
8. Повторяет шаги 3–7 для **GPU-прогона**.
9. **Возвращает исходные настройки** бенчмарка и прежнюю схему питания
   (если не указан `--keep-settings`).
10. **Печатает отчёт** в консоль и пишет `report.md` + `report.json`.

Прерывание по `Ctrl+C` корректно закрывает процесс бенчмарка и тоже возвращает настройки.

---

## 3. Подобранные настройки и почему именно такие

Настройки пишутся в **два канала** сразу (см. `BenchmarkScenarioFactory`):

* `UISettingData` — собственная «кортежная» настройка бенчмарка
  (`[/Script/GSGameSettings.GSGameUserSettings]`), её читает внутриигровое меню графики;
* стандартные ключи Unreal Engine 5 — `[/Script/Engine.GameUserSettings]` и
  `[ScalabilityGroups]` (`sg.*`), которые движок применяет при старте.

Внутриигровой пресет качества — 1…5, уровень `sg.*` — 0…4, то есть `sg = preset − 1`;
инструмент пишет согласованную пару значений.

### 3.1 CPU-тест — нагрузка на процессор, влияние GPU минимально

| Параметр | Значение | Зачем |
| --- | --- | --- |
| Разрешение | 1280×720, **оконный** режим | меньше пикселей — GPU простаивает |
| `sg.ResolutionQuality` / `ImageQuality` | **25 %** (рендер 320×180) | ещё в 4 раза меньше работы для GPU |
| `QualityLevel` / все `sg.*` | **1 (Low / 0)** | выключены тени, GI, отражения, эффекты |
| Full Ray Tracing (`Rtx`) | **Off** | RT — самая дорогая часть для видеокарты |
| Апскейл (`SuperResolutionSampling`) | **Off** | апскейл сам стоит GPU-времени |
| Frame Generation (`InsertFrame`) | **Off** | генерация кадров грузит GPU |
| VSync / FrameRateLimit | **Off / 0** | иначе FPS упирается в монитор, а не в CPU |
| Dynamic Resolution | **Off** | иначе движок сам меняет разрешение |
| Схема питания | High performance | CPU не сбрасывает частоты |

При таком наборе кадр для GPU «дешёвый», а симуляция, анимация, подготовка draw-call'ов и
работа драйвера остаются полноценными — итоговый FPS определяется процессором.
Для проверки, что упор действительно в CPU, в отчёте видны `CPUAvg` / `GPUAvg`
из самого бенчмарка: у корректного CPU-прогона GPU-загрузка заметно ниже 100 %.

### 3.2 GPU-тест — максимальная нагрузка на видеокарту

| Параметр | Значение | Зачем |
| --- | --- | --- |
| Разрешение | **нативное разрешение монитора** (определяется автоматически, с `UseNativeResolution: true`) | максимум пикселей |
| `sg.ResolutionQuality` / `ImageQuality` | **100 %** | рендер без понижения |
| `QualityLevel` / все `sg.*` | **5 (Cinematic / 4)** | все эффекты на максимуме |
| Full Ray Tracing (`Rtx`) | **Ultra** | максимальная цена кадра |
| Апскейл (`SuperResolutionSampling`) | **3 = TSR, качество DLAA (100 %)** | работает на любом GPU, не даёт «схитрить» понижением рендера |
| Frame Generation (`InsertFrame`) | **Off** (настраивается) | иначе метрика описывает синтетические кадры, а не видеокарту |
| VSync / FrameRateLimit | **Off / 0** | карта не простаивает в ожидании монитора |
| `Dx12` | **1** | RT требует DX12 |

Итог — видеокарта является узким местом каждого кадра; в отчёте это подтверждается
`GPUAvg` близким к 100 % и большим `GPUFrameTime`.

Все значения переопределяются в `appsettings.json`
(`WukongBench:CpuScenario`, `WukongBench:GpuScenario`), включая дополнительные ключи
(`ExtraSettings`, `ExtraUiSettings`) — на случай, если в вашей сборке бенчмарка имена
ключей отличаются.

---

## 4. Откуда берутся результаты

Black Myth: Wukong Benchmark Tool после каждого завершённого прогона кладёт файл
**без расширения**, имя которого — Unix-время запуска, в каталог

```
%TEMP%\b1\BenchMarkHistory\Tool\<unixtime>
```

Это JSON. Инструмент читает самые свежие файлы, отбирает появившиеся после старта прогона
и разбирает:

| Поле файла | Что это | Куда попадает в отчёте |
| --- | --- | --- |
| `FPSAvg` / `FPSMin` / `FPSMax` | средний / минимальный / максимальный FPS | Average / Minimum / Maximum FPS |
| `FPS95` | 95-й процентиль FPS (низкий процентиль) | FPS 95th percentile |
| `Records[].FrameRate` | пофреймовые данные | **1% low** (считается инструментом) |
| `Records[].CPUFrameTime` / `GPUFrameTime` | время кадра CPU/GPU | Average CPU/GPU frame time |
| `VideoMem` (в ГБ) / `VideoMemSize` | использование и объём VRAM | Peak VRAM / VRAM |
| `CPUAvg` / `GPUAvg` / `GPULeak` | загрузка CPU, GPU | CPU/GPU utilisation |
| `ScreenResolution`, `QualityLevel`, `ImageQuality`, `Rtx`, `Dlss`, `InsertFrame`, `Dx12`, … | настройки, с которыми реально прошёл прогон | **сверка с тем, что запросили** |
| `CPUModel`, `GPUModel`, `GpuDriverVer`, `SysMem`, `SysVer`, `GameVer`, `BenckMarkVersion` | данные о системе от самого бенчмарка | перекрёстная проверка характеристик |

Инструмент не доверяет себе: он сравнивает настройки, **записанные самим бенчмарком**,
с теми, что были запрошены, и при расхождении добавляет предупреждение в отчёт
(«The tool recorded Rtx='0' while '4' was requested…»). Сырой JSON целиком копируется в
папку прогона, так что метрики всегда можно перепроверить.

Дополнительно (и только как вспомогательная информация) копируются лог-файлы движка,
если они есть; **никакой зависимости от логов нет** — метрики берутся из JSON истории.

---

## 5. Архитектура

Классический DDD-набор из **четырёх отдельных проектов-слоёв**:

```
WukongBench.sln
├─ src/WukongBench.Domain          (net9.0)          — интерфейсы + модели с конструктором
├─ src/WukongBench.Application     (net9.0)          — use cases, политика настроек   → Domain
├─ src/WukongBench.Infrastructure  (net9.0-windows)  — реализации портов Domain       → Domain
├─ src/WukongBench.Presentation    (net9.0-windows)  — консоль, отчёты, DI             → Domain+Application+Infrastructure
└─ tests/WukongBench.Tests         (net9.0-windows)  — 33 теста, включая тесты архитектуры
```

Правила зависимостей (проверяются автотестами `ArchitectureTests`, они падают, если
кто-то добавит ссылку в неверную сторону):

* **Domain** — не зависит ни от чего вообще, даже от NuGet-пакетов. Содержит только
  модели (неизменяемые классы с конструктором и read-only свойствами), перечисления и
  интерфейсы-порты. Никакой логики: статических хелперов, сервисов и методов в моделях нет
  (это тоже проверяется тестом `Domain_ModelsAreDataOnly`).
* **Application** — зависит **только** от Domain. Здесь сценарии использования
  (`RunBenchmarkSuiteUseCase`, `RunDiagnosticsUseCase`), политика выбора настроек
  (`BenchmarkScenarioFactory`), знание о том, как выглядит меню бенчмарка
  (`UiScriptCatalog`), конфигурация и маппинг в доменные модели.
* **Infrastructure** — зависит **только** от Domain. Здесь всё «железо»: Steam-локатор и
  парсер VDF, работа с ini (включая `UISettingData`), запуск процесса и его поиск,
  Win32 `SendInput`/`SetCursorPos`, чтение и парсинг JSON-истории, WMI, `powercfg`,
  файловая система, логгер.
* **Application ⟂ Infrastructure** — эти два слоя **не знают друг о друге**: Application
  вызывает интерфейсы Domain, Infrastructure их реализует.
* **Presentation** — единственное место, где собирается граф объектов (composition root):
  `Program.cs` регистрирует реализации Infrastructure на порты Domain и use case'ы
  Application, биндит конфигурацию и рендерит отчёт.

Соответствие «порт → адаптер»:

| Порт (Domain) | Реализация (Infrastructure) |
| --- | --- |
| `ISteamInstallationLocator` | `SteamInstallationLocator` |
| `IGraphicsSettingsStoreFactory` / `IGraphicsSettingsStore` | `UeGameUserSettingsStoreFactory` / `UeGameUserSettingsStore` |
| `IBenchmarkProcessLauncher` / `IBenchmarkSession` | `BenchmarkProcessLauncher` / `GameProcessSession` |
| `IKeyboardUiAutomation` | `Win32KeyboardUiAutomation` |
| `IBenchmarkReadinessProbe` | `TimedReadinessProbe` |
| `IBenchmarkResultReader` | `BenchmarkHistoryResultReader` |
| `IArtifactCollector` | `BenchmarkArtifactCollector` |
| `IHardwareInfoProvider` | `WmiHardwareInfoProvider` |
| `IPowerSchemeManager` | `PowerCfgSchemeManager` |
| `IBenchmarkWorkspace` | `FileSystemBenchmarkWorkspace` |
| `IClock` | `SystemClock` |
| `IToolLogger` | `ConsoleToolLogger` |

Presentation собрана как консольный проект (`OutputType=Exe`) — это единственный слой,
который должен запускаться как процесс; структурно это обычная библиотека с точкой входа,
и она не содержит ни логики сценария, ни доступа к системе.

---

## 6. Командная строка

```
wukongbench [options]

  --cpu-only            только CPU-прогон
  --gpu-only            только GPU-прогон
  --hardware-only       только характеристики ПК, без бенчмарка
  --diagnostics         показать установку, текущие ключи настроек и папки результатов
  --keep-settings       не возвращать исходный GameUserSettings.ini
  --no-ui               не отправлять ввод (запустить бенчмарк руками; инструмент
                        только дождётся файла результатов)
  --direct-launch       запускать b1_benchmark.exe напрямую, а не через Steam
  --config <path>       другой appsettings.json
  --output <dir>        каталог для отчёта и артефактов
  --executable <path>   явный путь к b1_benchmark.exe
  --steam-root <path>   явный путь к корню Steam
  --verbose             подробный лог
  -h, --help            справка

Коды возврата: 0 — успех, 1 — ошибка прогона, 2 — неверные аргументы.
```

## 7. Конфигурация

`src/WukongBench.Presentation/appsettings.json` (копируется в каталог сборки). Все ключи
секции `WukongBench` можно переопределить переменными окружения вида
`WUKONGBENCH_CpuScenario__ResolutionWidth=1920`.

Ключевые группы: `Timings` (таймауты), `CpuScenario`, `GpuScenario`,
`UiAutomation` (скрипты ввода), `LaunchThroughSteam`, `UseHighPerformancePowerScheme`,
`ArtifactsDirectory`.

Скрипт ввода (`UiAutomation:StartScript`) — данные, а не код: список шагов
`KeyPress` / `MouseMoveAndClick` (координаты в долях клиентской области, 0…1) / `Wait`.
Если меню в вашей версии бенчмарка расположено иначе, достаточно поправить
`NormalizedX`/`NormalizedY` в JSON, пересборка не нужна.

---

## 8. Тесты

```powershell
dotnet test
```

33 теста, все проходят. Что покрыто:

* `BenchmarkScenarioFactoryTests` — CPU-прогон действительно убирает всю GPU-нагрузку
  (720p, render scale 25 %, Low, RT off, FG off, VSync off, пресет 1), GPU-прогон
  действительно максимальный (нативное разрешение, Cinematic, RT Ultra, 100 % scale),
  а также переопределения и выключение сценариев.
* `UeGameUserSettingsStoreTests` — запись стандартных ключей и `UISettingData` с
  сохранением чужих ключей, комментариев и порядка; создание файла с нуля; бэкап/восстановление.
* `BenchmarkHistoryResultReaderTests` — разбор реального формата JSON (включая перевод
  `VideoMem` из ГБ в МБ, 1% low и средние frame time из `Records`, записанные настройки),
  игнорирование старых и «недописанных» файлов, выбор самого свежего.
* `SteamInstallationLocatorTests` — полный путь реестр→`libraryfolders.vdf`→
  `appmanifest_*.acf`→папка установки→`b1_benchmark.exe` на поддельной библиотеке Steam.
* `RunBenchmarkSuiteUseCaseTests` — сквозной сценарий на поддельных портах: оба прогона,
  порядок применения настроек, возврат исходных значений, схема питания, отсутствие
  результата → корректная ошибка, отсутствие установки → понятное исключение.
* `ArchitectureTests` — правила слоёв (см. раздел 5).

---

## 9. Ограничения и что стоит проверить на живой машине

Инструмент написан по проверяемым фактам о бенчмарке (пути, формат JSON, `UISettingData`,
`b1_benchmark.exe`, `-saferender`/`-dx11/-dx12` и т.д., см. `docs/RESEARCH.md`), но часть
деталей невозможно подтвердить без запуска на Windows с установленным бенчмарком:

1. **Координаты пунктов меню** в `UiAutomation:StartScript` — значения по умолчанию
   (центр экрана + Enter). Если меню другое, поправьте координаты в `appsettings.json`
   (быстрый путь — `--no-ui`: запустить бенчмарк руками и убедиться, что остальная часть
   пайплайна работает).
2. **Полный набор ключей качества** в `UISettingData` для вашей версии бенчмарка.
   Проверяется через `--diagnostics` (показывает реальные ключи файла) и через
   `ExtraSettings`/`ExtraUiSettings`. В отчёте автоматически появляется предупреждение,
   если записанные бенчмарком значения не совпали с запрошенными.
3. **Права администратора** обязательны, если Steam установлен в `Program Files`.
4. **Denuvo/Steam-контекст**: запуск идёт через Steam, что исключает диалоги о
   переустановке; `--direct-launch` предусмотрен, но менее надёжен.
5. **Первый запуск бенчмарка** показывает экран выбора языка/яркости/сбора данных.
   Пройдите его один раз вручную — дальше инструмент справляется сам.
6. Значения `VideoMem` считаются гигабайтами (так их отдаёт бенчмарк), `VideoMemoryUsage`
   в `Records` — в мегабайтах; сырые значения всегда сохраняются в `report.json`.
7. Инструмент меняет и затем восстанавливает настройки бенчмарка. Если что-то пойдёт не
   так, исходный файл всегда лежит в `artifacts\<suite>\GameUserSettings.original.ini`.

## 10. Источники ключевых фактов

Ссылки и подробности — в [`docs/RESEARCH.md`](docs/RESEARCH.md). Кратко: страница приложения
в Steam (`api.steamcmd.net/v1/info/3132990`), обсуждения и руководства Steam Community,
разборы сообщества (в том числе четыре независимых открытых реализации автоматизации
бенчмарка), обзор GIGAZINE, PCGamingWiki.
