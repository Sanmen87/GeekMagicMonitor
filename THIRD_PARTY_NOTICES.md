# Сторонние компоненты и происхождение

Собственный код GeekMagicMonitor и наши дополнения распространяются по MIT
из корневого LICENSE. Условия сторонних файлов сохраняются за их авторами.
Программы и библиотеки устанавливаются/восстанавливаются штатными инструментами;
скачанные установщики и заводские образы не включены в исходный репозиторий.

## Основа прошивки

- [andrewjiang/HoloClawd-Open-Firmware](https://github.com/andrewjiang/HoloClawd-Open-Firmware),
  snapshot `ed60271ed32c61d816c5b36d92af3fcb9ff4c526`.
- Основан на [Times-Z/GeekMagic-Open-Firmware](https://github.com/Times-Z/GeekMagic-Open-Firmware).
- LICENSE именно использованного snapshot — MIT, Copyright (c) 2026 Times-Z.
  Сохранён без изменения в [firmware/LICENSE](firmware/LICENSE) и
  [licenses/HoloClawd-MIT.txt](licenses/HoloClawd-MIT.txt).
- Проверенный источник лицензии:
  [LICENSE зафиксированной версии](https://github.com/andrewjiang/HoloClawd-Open-Firmware/blob/ed60271ed32c61d816c5b36d92af3fcb9ff4c526/LICENSE).
  В более позднем upstream Times-Z LICENSE изменён на GPL-3.0; при переносе
  нового кода из другой версии нужно повторно проверить её условия.

Наши изменения: recovery без LittleFS/настроек, метка собственного FS,
сохранение полей config, автономные NTP-часы и remote timeout, IP и информация
о Flash, ограничения batch API и исправление OTA FS. Windows-приложение,
его экранные композиции и сборщик CPU написаны для этого проекта.

## Веб-ресурсы, включённые в дерево

| Компонент | Версия | Лицензия / источник |
|---|---|---|
| Pico CSS | 2.1.1 | [MIT](licenses/Pico-CSS-MIT.txt), [upstream](https://github.com/picocss/pico/tree/v2.1.1) |
| Alpine.js | 3.15.3 | [MIT](licenses/AlpineJS-MIT.txt), [upstream](https://github.com/alpinejs/alpine/tree/v3.15.3) |

Исходные уведомления Pico CSS в минифицированном файле сохранены.
Сторонние наборы пиктограмм, примерные GIF и приложения upstream удалены
из публикуемого дерева. SVG/ICO куба в monitor/GeekMagicMonitor/Assets созданы
для GeekMagicMonitor, генератор — tools/make_icon.ps1.

## Зависимости Windows

Точные версии находятся в packages.lock.json каждого проекта, лицензии пакетов
также доступны в их NuGet metadata. Заимствованные библиотеки не модифицированы.

| Пакеты | Лицензия / исходники |
|---|---|
| LibreHardwareMonitorLib 0.9.6 | [MPL-2.0](licenses/MPL-2.0.txt), [исходники](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/v0.9.6) |
| BlackSharp.Core, DiskInfoToolkit, RAMSPDToolkit-NDD | MPL-2.0; [Blacktempel](https://github.com/Blacktempel) |
| HidSharp 2.6.4 | [оригинальный LICENSE](licenses/HidSharp.txt), [проект](https://software.seekye.com/hidsharp) |
| Mono.Posix.NETStandard 1.0.0 | [Условия Microsoft/Mono](licenses/Mono-LICENSE.txt), [NuGet](https://www.nuget.org/packages/Mono.Posix.NETStandard/1.0.0) |
| Microsoft System.IO.Ports / System.Management / System.IO.FileSystem.AccessControl | [MIT](licenses/DotNet-MIT.txt), [dotnet/runtime](https://github.com/dotnet/runtime), [уведомления](licenses/DotNet-THIRD-PARTY-NOTICES.txt) |

PawnIO — отдельный системный компонент от [namazso](https://pawnio.eu/).
Его установщик/драйвер не включён в репозиторий или сборку приложения.

## Зависимости сборки прошивки

| Компонент | Версия | Источник / условия |
|---|---|---|
| ESP8266 Arduino Core | 3.1.2 | [LGPL-2.1](licenses/ESP8266-LGPL-2.1.txt), [исходники](https://github.com/esp8266/Arduino/tree/3.1.2) |
| ArduinoJson | 7.4.3 | [MIT](licenses/ArduinoJson-MIT.txt), [исходники](https://github.com/bblanchon/ArduinoJson/tree/v7.4.3) |
| Arduino_GFX | 1.6.9 | [исходники и уведомления файлов](https://github.com/moononournation/Arduino_GFX/tree/v1.6.9); содержит производные Adafruit/LovyanGFX |
| AnimatedGIF | 2.2.3 | [Apache-2.0](licenses/AnimatedGIF-Apache-2.0.txt), [исходники](https://github.com/bitbank2/AnimatedGIF) |

Сборочные скрипты копируют этот файл и licenses в выходные папки. При
распространении бинарных сборок сохраняйте уведомления и доступ к соответствующим
исходникам согласно условиям библиотек; корневая MIT их не заменяет.
