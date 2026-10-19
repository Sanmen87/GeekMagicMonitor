#pragma once

namespace LocalClock {
void begin(const char* timezone, const char* ntpServer);
void remoteFrameAccepted();
void update();
}
