#include <Arduino.h>
#include <time.h>
#include <ESP8266WiFi.h>
#include "display/LocalClock.h"
#include "display/ClockPolicy.h"
#include "display/DisplayManager.h"

// Compile-time boundary checks run in every firmware build, including millis rollover.
static_assert(!ClockPolicy::remoteExpired(14999, 0), "remote remains active before timeout");
static_assert(ClockPolicy::remoteExpired(15000, 0), "remote expires at timeout");
static_assert(!ClockPolicy::remoteExpired(20, UINT32_MAX - 100), "rollover does not expire early");
static_assert(ClockPolicy::remoteExpired(14900, UINT32_MAX - 100), "rollover still expires");
static_assert(!ClockPolicy::timeKnown(0), "cold start has unknown time");
static_assert(ClockPolicy::timeKnown(1704067200LL), "accept synchronized modern time");

namespace {
bool remote = false;
bool localScreen = false;
bool initialized = false;
uint32_t lastFrame = 0;
uint32_t lastDraw = 0;
String lastNetwork;
}

void LocalClock::begin(const char* timezone, const char* ntpServer) {
    configTime(timezone, ntpServer, "time.cloudflare.com");
    initialized = true;
    remote = false;
    localScreen = false;
}

void LocalClock::remoteFrameAccepted() {
    DisplayManager::stopGif(false);
    lastFrame = millis();
    remote = true;
    localScreen = false;
}

void LocalClock::update() {
    if (!initialized || !DisplayManager::isReady()) { return; }
    const uint32_t now = millis();
    if (remote && !ClockPolicy::remoteExpired(now, lastFrame)) { return; }
    remote = false;
    const bool firstDraw = !localScreen;
    if (!firstDraw && static_cast<uint32_t>(now - lastDraw) < 1000) { return; }
    lastDraw = now;
    if (firstDraw) {
        DisplayManager::stopGif();
        DisplayManager::drawTextWrapped(12, 12, "LOCAL CLOCK", 2, LCD_WHITE, LCD_BLACK, false);
        DisplayManager::drawTextWrapped(12, 200, "PC OFFLINE", 2, LCD_WHITE, LCD_BLACK, false);
        localScreen = true;
    }
    const bool accessPoint = (WiFi.getMode() & WIFI_AP) != 0;
    const String address = accessPoint ? WiFi.softAPIP().toString() :
        WiFi.status() == WL_CONNECTED ? WiFi.localIP().toString() : "NO CONNECTION";
    const String network = String(accessPoint ? "AP " : "IP ") + address;
    if (firstDraw || network != lastNetwork) {
        DisplayManager::fillRect(0, 166, DisplayManager::screenWidth(), 24, LCD_BLACK);
        DisplayManager::drawTextWrapped(6, 170, network, 2, LCD_WHITE, LCD_BLACK, false);
        lastNetwork = network;
    }
    // Only refresh the changing region, not the entire screen.
    DisplayManager::fillRect(0, 60, DisplayManager::screenWidth(), 100, LCD_BLACK);
    const time_t epoch = time(nullptr);
    if (!ClockPolicy::timeKnown(epoch)) {
        DisplayManager::drawTextWrapped(12, 80, "TIME NOT SYNCED", 2, LCD_WHITE, LCD_BLACK, false);
        return;
    }
    struct tm local;
    localtime_r(&epoch, &local);
    char clockText[9];
    char dateText[11];
    strftime(clockText, sizeof(clockText), "%H:%M:%S", &local);
    strftime(dateText, sizeof(dateText), "%d.%m.%Y", &local);
    DisplayManager::drawTextWrapped(12, 70, clockText, 4, LCD_WHITE, LCD_BLACK, false);
    DisplayManager::drawTextWrapped(12, 125, dateText, 2, LCD_WHITE, LCD_BLACK, false);
}
