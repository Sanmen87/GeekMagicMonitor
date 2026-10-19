# Прошивка GeekMagicMonitor

Рабочая доработка HoloClawd для HelloCubic Lite, ESP8266, Flash 4 МиБ,
ST7789 240×240. Происхождение: [уведомления](../THIRD_PARTY_NOTICES.md).

- [Основной README](../README.md)
- [Сборка и установка](../docs/FIRMWARE.md)
- [Разработка](../docs/DEVELOPMENT.md)

Сборка из корня: `powershell -File .\build_firmware.ps1`.
Выход: `artifacts/firmware/firmware.bin` и `littlefs.bin`, без загрузки в куб.
При отсутствии config.json копируется пример с пустыми Wi-Fi credentials.
Свой config.json не добавляйте в Git.
