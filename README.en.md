# GeekMagicMonitor

[Русская инструкция](README.md)

Clock, weather and CPU/GPU/RAM monitoring for the **GeekMagic HelloCubic Lite**.
A Windows application reads local sensors, fetches weather and draws colourful
240×240 screens over a local HTTP API. The ESP8266 firmware provides Wi-Fi,
display commands, OTA updates and an NTP clock when the computer is offline.

Tested on one ESP8266 / 4 MiB flash / ST7789 HelloCubic Lite. Other hardware
revisions need their own compatibility checks. This replaces the stock application.

## Windows application

Use Windows 11 x64 and [.NET Desktop Runtime 10 x64](https://dotnet.microsoft.com/download/dotnet/10.0).
To build from source, install the .NET 10 SDK instead. The first build requires NuGet access.
From the project directory, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build_monitor.ps1
```

Start `artifacts/monitor/GeekMagicMonitor.exe`. Keep the entire output folder,
including DLLs and runtime configuration files. Exit the old copy through its
tray menu before replacing application files.

1. Set **Язык приложения / App language** to **English**. The interface changes immediately.
2. Enter the cube IP without `http://` or a path. The default `192.168.4.1` is for setup;
   use the home-network IP after configuring Wi-Fi.
3. Clear **Demo data**. Choose the GPU, sensor interval and screen mode.
4. Enable **Show weather**, search for your city and select a result. Alternatively,
   enter its coordinates. Time zone uses Windows IDs, e.g. `Russian Standard Time`.
5. Click **Apply and save**, then enable **Send to cube (HoloClawd)**.
6. **Cube: frame accepted** confirms delivery.

The sending checkbox is saved immediately when enabled or disabled. Sending resumes
on application restart or Windows sign-in if it was enabled. Fresh installations and
older settings without this preference start with sending disabled. The close button hides the window;
**Exit** in the tray menu ends the process. **Start in tray at sign-in** is a separate setting.
Settings, weather cache and logs are in `%LOCALAPPDATA%\GeekMagicMonitor`.

**App language** and **Cube language** independently select Russian or English.
Language changes apply immediately and persist without changing other pending fields.
Existing settings retain their screen, GPU, city and weather provider. Initial defaults
remain Russian for the app and English for the cube.

Russian cube screens require the current `firmware.bin`: update **Firmware** through
`/legacyupdate`. Language support alone does not require a FileSystem update.
The client detects older firmware and explains the required update instead of sending
unreadable Cyrillic. The standalone clock and firmware web pages currently use English.

## Firmware build and first installation

The detailed [firmware guide](docs/FIRMWARE.md) is in Russian. The procedure is:

1. Verify ESP8266, 4 MiB flash and the LCD wiring/profile. A normal USB cable did not
   expose a COM port on the tested cube; backup and hardware recovery require a 3.3 V UART adapter.
2. Install Python 3.12 and create a local PlatformIO environment:

   ```powershell
   py -3.12 -m venv .venv
   .\.venv\Scripts\python.exe -m pip install platformio==6.2.0
   powershell -NoProfile -ExecutionPolicy Bypass -File .\build_firmware.ps1
   ```

3. The build creates `artifacts/firmware/firmware.bin` and `littlefs.bin`.
   A missing `firmware/data/config.json` is generated from the blank example;
   never commit personal Wi-Fi credentials.
4. On the stock cube, open `http://CURRENT_IP/update` and upload our `firmware.bin`
   using the **Firmware** field. Keep power connected until it reboots.
5. Join Wi-Fi **GeekMagic**, password **`$str0ngPa$$w0rd`**. Open `http://192.168.4.1/api/v1/device`.
   Expect application `GeekMagicMonitor`, mode `recovery`, flash sizes `4194304`,
   filesystem partition `2072576` and `displayReady=false`. Stop if the sizes differ.
6. Open `http://192.168.4.1/legacyupdate` and upload `littlefs.bin` under **FileSystem**.
7. Rejoin GeekMagic after reboot, open `/wifi.html` and configure your 2.4 GHz network.
8. Find the new IP on the cube or router. `/api/v1/device` should report mode `normal`
   and `displayReady=true`. Use this IP in the Windows application.

For subsequent firmware-only updates, use `/legacyupdate` → **Firmware**.
Uploading FileSystem again can replace saved Wi-Fi settings. The tested optical profile
uses `lcd_rotation=4`; mirror preview affects only the Windows preview.

After 15 seconds without PC frames, the cube shows its local NTP clock. Weather is
fetched by the PC, with wttr.in/Open-Meteo fallback and cached readings.

## CPU temperature without elevating the main application

GPU/RAM and CPU load generally work as a regular user. CPU temperature may need the
official [PawnIO driver](https://pawnio.eu/) and an elevated collector.
Review [the collector guide](docs/SENSOR_AGENT.md) and `tools/setup_sensor_agent.ps1` first.

Build the application, install PawnIO, then run PowerShell as administrator **under
the same Windows account**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\setup_sensor_agent.ps1 -Install
```

This installs the collector under Program Files and creates an elevated logon task.
The regular application reads a protected snapshot; data older than 10 seconds is rejected.
Use `-Remove` instead of `-Install` to stop the collector and remove its task.

## Development and licensing

See [development instructions](docs/DEVELOPMENT.md) for tests, offline previews and Git export.
The build checks 304 bilingual frame layouts, payload limits, incremental updates,
weather/sensor handling, settings migration and HTTP delivery. Firmware compilation
also checks UTF-8 decoding and all 33 Russian glyphs.

Own code is under [MIT](LICENSE). Preserve [third-party notices](THIRD_PARTY_NOTICES.md)
and `licenses/` when redistributing. Local downloads, experiments, credentials and
generated binaries are excluded from Git.

Device API and OTA currently have no authentication; use a trusted LAN without public port forwarding.
