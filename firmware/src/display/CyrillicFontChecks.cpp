#include "display/CyrillicFont.h"

namespace {
constexpr bool decoderChecks() {
    size_t offset = 0;
    constexpr char mixed[] = "\xD0\x90\xD1\x91Z"; // АёZ
    if (CyrillicFont::next(mixed, 5, offset) != 0x410 || offset != 2) return false;
    if (CyrillicFont::next(mixed, 5, offset) != 0x451 || offset != 4) return false;
    if (CyrillicFont::next(mixed, 5, offset) != 'Z' || offset != 5) return false;
    offset = 0;
    if (CyrillicFont::next("\xD0", 1, offset) != '?' || offset != 1) return false;
    offset = 0;
    if (CyrillicFont::next("\xD0Z", 2, offset) != '?' || offset != 1) return false;
    if (CyrillicFont::next("\xD0Z", 2, offset) != 'Z') return false;
    offset = 0;
    if (CyrillicFont::next("\xED\xA0\x80", 3, offset) != '?') return false; // surrogate
    offset = 0;
    if (CyrillicFont::next("\xF0\x9F\x8C\xA1", 4, offset) != 0x1F321) return false;
    return CyrillicFont::length(mixed, 5) == 3;
}
constexpr bool glyphChecks() {
    for (uint32_t c = 0x410; c <= 0x42F; ++c) {
        if (CyrillicFont::glyph(c) == nullptr || CyrillicFont::glyph(c) != CyrillicFont::glyph(c + 32)) return false;
        for (int row = 0; row < 7; ++row) if (CyrillicFont::glyph(c)[row] > 31) return false;
    }
    return CyrillicFont::glyph(0x401) == CyrillicFont::glyph(0x451) &&
        CyrillicFont::glyph(0x410)[0] == 14 && CyrillicFont::glyph('A') == nullptr;
}
static_assert(decoderChecks(), "UTF-8 decoding must preserve character boundaries and reject malformed input");
static_assert(glyphChecks(), "All Russian letters need bounded 5x7 glyphs");
}
