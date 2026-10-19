#pragma once
#include <stddef.h>
#include <stdint.h>

// Fixed 5x7 uppercase Cyrillic, with the same 6x8 cell as the built-in ASCII font.
// Lowercase Cyrillic deliberately uses uppercase glyphs on the small LCD.
namespace CyrillicFont {
constexpr uint32_t next(const char* text, size_t length, size_t& offset) {
    if (offset >= length) return 0;
    const auto first = static_cast<uint8_t>(text[offset++]);
    if (first < 0x80) return first;
    unsigned remaining = 0;
    uint32_t value = 0, minimum = 0;
    if (first >= 0xC2 && first <= 0xDF) { remaining = 1; value = first & 0x1F; minimum = 0x80; }
    else if (first >= 0xE0 && first <= 0xEF) { remaining = 2; value = first & 0x0F; minimum = 0x800; }
    else if (first >= 0xF0 && first <= 0xF4) { remaining = 3; value = first & 7; minimum = 0x10000; }
    else return '?';
    if (offset + remaining > length) return '?';
    for (unsigned i = 0; i < remaining; ++i) {
        const auto byte = static_cast<uint8_t>(text[offset + i]);
        if ((byte & 0xC0) != 0x80) return '?';
        value = (value << 6) | (byte & 0x3F);
    }
    offset += remaining;
    return value < minimum || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF) ? '?' : value;
}

constexpr size_t length(const char* text, size_t bytes) {
    size_t offset = 0, count = 0;
    while (offset < bytes) { next(text, bytes, offset); ++count; }
    return count;
}

static constexpr uint8_t rows[][7] = {
        {14,17,17,31,17,17,17}, // А
        {31,16,16,30,17,17,30}, // Б
        {30,17,17,30,17,17,30}, // В
        {31,16,16,16,16,16,16}, // Г
        {14,10,10,10,10,31,17}, // Д
        {31,16,16,30,16,16,31}, // Е
        {21,21,14,4,14,21,21},  // Ж
        {14,17,1,6,1,17,14},   // З
        {17,17,19,21,25,17,17},// И
        {10,4,17,19,21,25,17}, // Й
        {17,18,20,24,20,18,17},// К
        {7,9,9,9,9,9,17},     // Л
        {17,27,21,21,17,17,17},// М
        {17,17,17,31,17,17,17},// Н
        {14,17,17,17,17,17,14},// О
        {31,17,17,17,17,17,17},// П
        {30,17,17,30,16,16,16},// Р
        {14,17,16,16,16,17,14},// С
        {31,4,4,4,4,4,4},     // Т
        {17,17,17,15,1,17,14}, // У
        {4,14,21,21,21,14,4}, // Ф
        {17,17,10,4,10,17,17},// Х
        {18,18,18,18,18,31,1},// Ц
        {17,17,17,15,1,1,1},  // Ч
        {21,21,21,21,21,21,31},// Ш
        {21,21,21,21,21,31,1},// Щ
        {24,8,8,14,9,9,14},   // Ъ
        {17,17,17,25,21,21,25},// Ы
        {16,16,16,30,17,17,30},// Ь
        {14,17,1,7,1,17,14},  // Э
        {18,21,21,29,21,21,18},// Ю
        {15,17,17,15,5,9,17}, // Я
        {10,0,31,16,30,16,31} // Ё
};
constexpr const uint8_t* glyph(uint32_t codepoint) {
    if (codepoint >= 0x430 && codepoint <= 0x44F) codepoint -= 0x20;
    if (codepoint == 0x451) codepoint = 0x401;
    if (codepoint == 0x401) return rows[32];
    return codepoint >= 0x410 && codepoint <= 0x42F ? rows[codepoint - 0x410] : nullptr;
}
}
