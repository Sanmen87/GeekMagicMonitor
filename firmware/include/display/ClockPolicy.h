#pragma once
#include <stdint.h>

namespace ClockPolicy {
constexpr uint32_t RemoteTimeoutMs = 15000;
constexpr bool remoteExpired(uint32_t now, uint32_t lastFrame) {
    return static_cast<uint32_t>(now - lastFrame) >= RemoteTimeoutMs;
}
constexpr bool timeKnown(int64_t epoch) { return epoch >= 1704067200LL; }
}
