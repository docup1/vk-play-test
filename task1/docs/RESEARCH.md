# Исследование: как устроен Black Myth: Wukong Benchmark Tool

Документ фиксирует факты, на которых построена автоматизация, и их источники.
Всё, что не удалось подтвердить независимо, помечено как **не подтверждено**.

## 1. Приложение

| Параметр | Значение | Источник |
| --- | --- | --- |
| Steam AppID | 3132990 (родитель 2358720, тип «Tool», бесплатный) | [Steam Store](https://store.steampowered.com/app/3132990/), `api.steamcmd.net/v1/info/3132990` |
| Depot | 3132991, buildid 15365379, ~7,8 ГиБ | `api.steamcmd.net/v1/info/3132990` |
| Каталог установки | `<SteamLibrary>\steamapps\common\Black Myth Wukong Benchmark Tool` | там же |
| Точка входа | `b1_benchmark.exe` в корне установки (373 КБ — загрузчик) | steamcmd appinfo + скриншот содержимого папки на [gamemad](https://gamemad.com/guide/339869) |
| Второй launch-option | `b1_benchmark.exe -saferender` («Compatibility Mode») | steamcmd appinfo |
| Внутренний UE5-бинарь | `b1\Binaries\Win64\b1-Win64-Shipping.exe` — **не подтверждено** (в одном сообщении Steam приводится дамп Reliability Monitor с этим путём, другой инструмент ищет `b1.exe`) | [Steam Community](https://steamcommunity.com/app/2358720/discussions/0/805721252880856031/) |
| Запуск | `steam.exe -applaunch 3132990` работает; прямой запуск `b1_benchmark.exe` тоже (пользователи так делают), но требует запущенного Steam и переменных окружения `SteamAppId`/`SteamGameId`/`SteamOverlayGameId`, иначе приложение само перезапускается через Steam с диалогом подтверждения | Steam Community, обсуждения репаков |

## 2. Файл настроек

**Важно:** для *бенчмарка* файл настроек лежит **внутри папки установки**, а не в
`%LOCALAPPDATA%` (последнее — путь *самой игры* 2358720, он же часто встречается в гайдах
и легко вводит в заблуждение).

```
<SteamLibrary>\steamapps\common\Black Myth Wukong Benchmark Tool\b1\Saved\Config\Windows\GameUserSettings.ini
```

Источник: [Steam Community, «тест принудительного включения HDR»](https://steamcommunity.com/app/2358720/discussions/0/6149188575101226005/?l=english)
(там же предупреждение, что инструмент перезаписывает файл и его предлагают делать read-only — отсюда
опция `LockFileDuringRun` в `SettingsStoreOptions`).

Реальный пример содержимого (фикстура из открытого проекта автоматизации —
[EgorZarame, TestData/GameUserSettings.ini](https://raw.githubusercontent.com/EgorZarame/vk-tech-task-1-wukong-benchmark/main/tests/WukongBenchRunner.Tests/TestData/GameUserSettings.ini)):

```ini
[/Script/GSGameSettings.GSGameUserSettings]
UISettingData=(("ScreenMode", "1"),("ScreenResolution", "0"),("ImageQuality", "720"),("WindowFullImageQuality", "0"),("SuperResolutionSampling", "3"),("Vsync", "1"),("LockFrameRate", "1"),("MotionBlur", "1"),("InsertFrame", "1"),("Rtx", "0"),("QualityLevel", "3"),("ViewDistance", "3"),...,("GlobalIllumination", "3"),("ReflectionQuality", "3"),("Brightness", "50"))
bUseVSync=True
ResolutionSizeX=1920
ResolutionSizeY=1080
LastUserConfirmedResolutionSizeX=...
LastUserConfirmedResolutionSizeY=...
FullscreenMode=1
FrameRateLimit=60.000000
Version=5

[ScalabilityGroups]
sg.ResolutionQuality=67
sg.ViewDistanceQuality=2
...
```

Наблюдаемые соответствия (по согласованным наблюдениям двух независимых авторов):

* внутриигровой пресет качества **1…5** ↔ `sg.*` = **preset − 1** (0…4);
* `SuperResolutionSampling`: **2 = DLSS**, **3 = TSR**; остальные значения не подтверждены
  (в коде они упорядочены «по остаточному принципу» и переопределяются конфигом);
* `ImageQuality` — внутренняя высота рендера в пикселях (при `sg.ResolutionQuality=67`
  и 1080p там стоит `720`, то есть 67 % от 1080 ≈ 720) — **вывод сделан сопоставлением
  значений фикстуры, не подтверждён первоисточником**;
* `FullscreenMode`: 0 = эксклюзивный полный экран, 1 = borderless, 2 = окно
  (совпадает с `EWindowMode` в UE).

Реальные ключи, встреченные в публичных обсуждениях: `bUseHDRDisplayOutput`,
`ResolutionSizeX/Y`, `LastUserConfirmedResolutionSizeX/Y`, `LastConfirmedFullscreenMode`,
`FullscreenMode` — [PCGamingWiki](https://www.pcgamingwiki.com/wiki/Black_Myth:_Wukong),
[Steam Community](https://steamcommunity.com/app/2358720/discussions/0/6149188575100375814/?l=english).

## 3. Результаты

После каждого завершённого прогона бенчмарк кладёт **файл без расширения**, имя которого —
Unix-время запуска, в

```
%TEMP%\b1\BenchMarkHistory\Tool\<unixtime>
```

Формат — JSON. Это подтверждают четыре независимые реализации автоматизации
([welldanov](https://github.com/welldanov/wukong-benchmark-automation),
[meumeu21](https://github.com/meumeu21/WukongBenchmarkTool),
[EgorZarame](https://github.com/EgorZarame/vk-tech-task-1-wukong-benchmark),
[t1box](https://github.com/t1box/WukongBench)), причём у meumeu21 в репозитории лежит
**реальный файл вывода** (`example/cpu-history.json`), по которому и установлены имена полей.

Структура (сокращённо):

```json
{
  "TimeStamp": 1755000000,
  "BenckMarkVersion": "1.0.3.14649",
  "GameVer": "1.0.3.14649",
  "SysVer": "Windows 11 Pro ...",
  "CPUModel": "...", "GPUModel": "...", "GpuDriverVer": "...", "SysMem": "32GB",
  "FPSAvg": 60, "FPSMin": 2, "FPSMax": 120, "FPS95": 17,
  "CPUAvg": 74.5, "GPUAvg": 98.2, "GPULeak": 0.3,
  "VideoMem": 1.70000004768372, "VideoMemSize": "12GB",
  "ScreenMode": 1, "ScreenResolution": "1280 × 720", "QualityLevel": 1, "ImageQuality": 180,
  "ViewDistance": 1, "AntiAliasing": 1, "PostProcessing": 1, "ShadowQuality": 1,
  "TextureQuality": 1, "MaterialQuality": 1, "VegetationQuality": 1, "MotionBlur": 0,
  "Rtx": 0, "Dlss": 0, "InsertFrame": 0, "Dx12": 1,
  "Records": [ { "TimeStamp": 1, "FrameRate": 60, "CPUUsage": 70, "GPUUsage": 80,
                 "CPUFrameTime": 16.6, "GPUFrameTime": 14.0, "VideoMemoryUsage": 1024 } ]
}
```

Замечания:

* опечатка `BenckMarkVersion` — так в самом файле, инструмент читает именно это имя;
* поле «Score» в файле **отсутствует** (утверждение одного из репозиториев не подтверждается);
* семантика `FPS95` (95-й процентиль FPS / 5-й процентиль) в источниках описывается
  по-разному — в отчёте поле показывается как «FPS 95th percentile» без домыслов о формуле;
* `VideoMem` — гигабайты (в отчёте переводятся в МБ), `Records[].VideoMemoryUsage` —
  мегабайты (не подтверждено первоисточником, поэтому сырые значения сохраняются целиком).

## 4. Альтернативные источники результатов (почему не используются)

* **Лог UE5** — ни один из ~10 открытых проектов не читает метрики из `.log`; shipping-сборки
  UE5 по умолчанию логи не пишут ([Epic KB](https://dev.epicgames.com/community/learning/knowledge-base/vzvZ/unreal-engine-enabling-logging-in-shipping-builds)).
  Инструмент прикладывает лог, если он существует, но не зависит от него.
* **Скриншоты + OCR** — применяется в нескольких проектах (Windows.Media.Ocr, Tesseract),
  но это распознавание пикселей: язык интерфейса локализуется (JP/CN/RU), точность ниже, чем
  у JSON. Оставлено как возможное расширение, в текущей версии не используется.
* **UE5 CSV Profiler** (`-csvCaptureFrames`) — подход одного из проектов, автор сам помечает
  его как неподтверждённый.

## 5. Управление меню

Пункты меню бенчмарка не реагируют на клавиатуру, пока по ним не проведёт курсор
(замечание из практики автоматизации, [welldanov](https://github.com/welldanov/wukong-benchmark-automation)).
Поэтому скрипт ввода начинается с перемещения мыши в центр окна и клика, и только затем
отправляет `Enter` для подтверждения выбранного пункта и диалога запуска.

Координаты в конфиге заданы долями клиентской области (0…1), что не зависит от разрешения.
Один из проектов использует жёстко прописанные «разрешение-независимые» координаты кликов —
тот же принцип.

Отдельно: у бенчмарка есть цикличный режим («loop mode») и экран истории результатов по
клавише `T` ([GIGAZINE](https://gigazine.net/news/20240826-black-myth-wukong-benchmark-tool/)) —
ни то, ни другое для автоматизации не требуется, так как результаты читаются из файлов.
