#include <Arduino.h>
#include <LittleFS.h>
#include <Arduino_GFX_Library.h>
#include <SPI.h>
#include <ESP8266HTTPUpdateServer.h>
#include <ArduinoJson.h>
#include <flash_hal.h>

#include <Logger.h>
#include "project_version.h"
#include "config/ConfigManager.h"
#include "wireless/WiFiManager.h"
#include "display/DisplayManager.h"
#include "display/LocalClock.h"
#include "web/Webserver.h"
#include "web/Api.h"

ConfigManager configManager;
const char* AP_SSID = "GeekMagic";
const char* AP_PASSWORD = "$str0ngPa$$w0rd";
WiFiManager* wifiManager = nullptr;

static constexpr uint32_t SERIAL_BAUD_RATE = 115200;
static constexpr uint32_t BOOT_DELAY_MS = 200;
static constexpr int LOADING_BAR_TEXT_X = 50;
static constexpr int LOADING_BAR_TEXT_Y = 80;
static constexpr int LOADING_BAR_Y = 110;
static constexpr int LOADING_DELAY_MS = 1000;

Webserver* webserver = nullptr;
static ESP8266HTTPUpdateServer recoveryUpdater;
static bool recoveryMode = false;

static void registerDeviceStatus() {
    webserver->raw().on("/api/v1/device", HTTP_GET, []() {
        JsonDocument status;
        status["application"] = "GeekMagicMonitor";
        status["mode"] = recoveryMode ? "recovery" : "normal";
        status["ip"] = wifiManager->getIP().toString();
        status["accessPoint"] = wifiManager->isApMode();
        status["flashRealBytes"] = ESP.getFlashChipRealSize();
        status["flashConfiguredBytes"] = ESP.getFlashChipSize();
        status["filesystemPartitionBytes"] = static_cast<size_t>(FS_end - FS_start);
        status["displayReady"] = DisplayManager::isReady();
        status["cyrillicText"] = true;
        String body;
        serializeJson(status, body);
        webserver->raw().send(200, "application/json", body);
    });
}

/**
 * @brief Initializes the system
 *
 */
void setup() {
    Serial.begin(SERIAL_BAUD_RATE);
    delay(BOOT_DELAY_MS);
    Serial.println("");
    Logger::info(("GeekMagic Open Firmware " + String(PROJECT_VER_STR)).c_str());

    constexpr int TOTAL_STEPS = 5;
    int step = 0;

    // Never format an existing device filesystem implicitly during first boot.
    LittleFS.setConfig(LittleFSConfig(false));
    const bool mounted = LittleFS.begin();
    // Never apply an unrelated stock config to unknown LCD GPIO on first boot.
    const bool configured = mounted && LittleFS.exists("/geekmagic-monitor.profile") && configManager.load();
    if (!configured) {
        recoveryMode = true;
        Logger::error("Filesystem/config unavailable; starting recovery AP without LCD GPIO access");
        wifiManager = new WiFiManager("", "", AP_SSID, AP_PASSWORD);
        wifiManager->startAccessPointMode();
        webserver = new Webserver();
        webserver->begin();
        webserver->raw().on("/", HTTP_GET, []() {
            String body = "<!doctype html><meta name='viewport' content='width=device-width'><title>GeekMagic recovery</title>"
                "<h1>GeekMagic recovery</h1><p>Our filesystem/config is unavailable. LCD GPIO disabled. No automatic format.</p>"
                "<p>IP: " + wifiManager->getIP().toString() + "</p>"
                "<p><a href='/api/v1/device'>Device / flash information</a></p>"
                "<p><a href='/legacyupdate'>Firmware / filesystem updater</a></p>"
                "<p>Check flash capacity before uploading LittleFS. Choose Filesystem for littlefs.bin; Firmware for firmware.bin.</p>";
            webserver->raw().send(200, "text/html", body);
        });
        recoveryUpdater.setup(&webserver->raw(), "/legacyupdate");
        registerDeviceStatus();
        return;
    }

    step++;

    Logger::info("Configuration loaded successfully");
    step++;

    DisplayManager::begin();
    if (DisplayManager::isReady()) {
        DisplayManager::drawTextWrapped(LOADING_BAR_TEXT_X, LOADING_BAR_TEXT_Y, "Starting...", 2, LCD_WHITE, LCD_BLACK,
                                        true);
        DisplayManager::drawLoadingBar((float)step / TOTAL_STEPS, LOADING_BAR_Y);
    }
    step++;

    wifiManager = new WiFiManager(configManager.getSSID(), configManager.getPassword(), AP_SSID, AP_PASSWORD);
    wifiManager->begin();
    if (DisplayManager::isReady()) {
        DisplayManager::drawLoadingBar((float)step / TOTAL_STEPS, LOADING_BAR_Y);
    }
    step++;

    webserver = new Webserver();
    webserver->begin();
    if (DisplayManager::isReady()) {
        DisplayManager::drawLoadingBar((float)step / TOTAL_STEPS, LOADING_BAR_Y);
    }
    step++;

    registerApiEndpoints(webserver);
    registerDeviceStatus();

    webserver->serveStatic("/", "/web/index.html", "text/html");
    webserver->serveStatic("/header.html", "/web/header.html", "text/html");
    webserver->serveStatic("/footer.html", "/web/footer.html", "text/html");
    webserver->serveStatic("/index.html", "/web/index.html", "text/html");
    webserver->serveStatic("/update.html", "/web/update.html", "text/html");
    webserver->serveStatic("/gif_upload.html", "/web/gif_upload.html", "text/html");
    webserver->serveStatic("/wifi.html", "/web/wifi.html", "text/html");

    webserver->serveStatic("/css/pico.min.css", "/web/css/pico.min.css", "text/css");
    webserver->serveStatic("/css/style.css", "/web/css/style.css", "text/css");
    webserver->serveStatic("/js/alpinejs.min.js", "/web/js/alpinejs.min.js", "application/javascript");
    webserver->serveStatic("/js/main.js", "/web/js/main.js", "application/javascript");

    if (DisplayManager::isReady()) {
        DisplayManager::drawLoadingBar(1.0F, LOADING_BAR_Y);
    }

    delay(LOADING_DELAY_MS);

    DisplayManager::drawStartup(wifiManager->getIP().toString());
    LocalClock::begin(configManager.clock_timezone.c_str(), configManager.clock_ntp_server.c_str());
}

void loop() {
    if (webserver != nullptr) {
        webserver->handleClient();
    }
    if (!recoveryMode) {
        DisplayManager::update();
        LocalClock::update();
    }
}
