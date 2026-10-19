namespace GeekMagicMonitor.Core;

public static class Localization
{
    public static string Normalize(string? language) => language == "en" ? "en" : "ru";
    public static string Text(string text,string? language) => Normalize(language)=="en" && English.TryGetValue(text,out var value)?value:text;
    public static IReadOnlyDictionary<string,string> English {get;} = new Dictionary<string,string>
    {
        ["GeekMagicMonitor — предпросмотр"]="GeekMagicMonitor — preview",
        ["Язык приложения"]="App language",
        ["Язык куба"]="Cube language",
        ["Адрес куба"]="Cube address",
        ["Опрос датчиков, с"]="Sensor interval, s",
        ["Переключение, с"]="Screen interval, s",
        ["Экран"]="Screen",
        ["Авто"]="Auto",
        ["Часы и погода"]="Clock and weather",
        ["Монитор ПК"]="PC monitor",
        ["Часовой пояс Windows"]="Windows time zone",
        ["Город / поиск"]="City / search",
        ["Найти город"]="Find city",
        ["Выберите результат"]="Select result",
        ["Широта"]="Latitude",
        ["Долгота"]="Longitude",
        ["Сервер погоды"]="Weather provider",
        ["Авто (wttr → Open-Meteo)"]="Auto (wttr → Open-Meteo)",
        ["Демо-данные"]="Demo data",
        ["Отправлять на куб (HoloClawd)"]="Send to cube (HoloClawd)",
        ["Показывать погоду"]="Show weather",
        ["Автозапуск в трее"]="Start in tray at sign-in",
        ["Зеркальный вид LCD (предпросмотр)"]="Mirror LCD preview",
        ["Применить и сохранить"]="Apply and save",
        ["Обновить погоду сейчас"]="Refresh weather now",
        ["Перезапустить с правами администратора"]="Restart as administrator",
        ["Предпросмотр 240×240: шрифт схематичный. Состояние отправки сохраняется."]="240×240 preview: approximate font. Sending preference is saved.",
        ["Свернуть в трей"]="Minimize to tray",
        ["Выход"]="Exit",
        ["Открыть"]="Open",
        ["Выберите населённый пункт и нажмите «Применить»"]="Select a place and click Apply and save",
        ["Город не найден. Попробуйте другое написание или введите координаты вручную."]="City not found. Try another spelling or enter coordinates manually.",
        ["Поиск города недоступен. Учкекен доступен в списке без сети; можно ввести координаты вручную."]="City search unavailable. Uchkeken is available offline; you can also enter coordinates manually.",
        ["Повышение прав отменено; приложение продолжает работать без температуры CPU."]="Elevation cancelled; the application continues running. CPU temperature requires the collector or administrator access.",
        ["Проверьте настройки: "]="Check settings: ",
        ["Укажите город и его координаты"]="Enter a city and its coordinates",
        ["Настройки сохранены"]="Settings saved",
        ["Настройки"]="Settings",
        ["ДЕМО: числа условные. Для реальных показаний снимите флажок и примените."]="DEMO: sample values. Clear Demo data and apply to see live readings.",
        ["Предпросмотр — отправка на куб выключена"]="Preview — sending to cube is disabled",
        ["Отправка приостановлена: проверьте адрес куба и часовой пояс."]="Sending paused: check the cube address and time zone.",
        ["Ошибка: "]="Error: ",
        ["GeekMagicMonitor уже запущен. Откройте значок в трее."]="GeekMagicMonitor is already running. Open its tray icon.",
        ["Погода выключена"]="Weather disabled",
        ["Погода: выберите город и примените настройки"]="Weather: select a city and apply settings",
        ["Погода: запрос для "]="Weather: requesting ",
        ["Погода: "]="Weather: ",
        ["сервер Open-Meteo не ответил за 15 с; проверьте доступ/VPN к api.open-meteo.com"]="Weather request timed out after 15 s; check your network/VPN",
        ["сервер вернул HTTP "]="Server returned HTTP ",
        ["нет соединения с api.open-meteo.com; проверьте сеть/VPN"]="Cannot connect to the weather provider; check your network/VPN",
        ["не удалось сохранить кэш"]="Could not save cache",
        ["некорректный ответ сервера"]="Invalid server response",
        ["; кэша нет"]="; no cached data",
        ["; показан последний результат"]="; showing last result",
        ["тайм-аут"]="timeout",
        ["нет соединения"]="connection failed",
        ["неверный ответ"]="invalid response",
        ["Датчики ещё не опрошены"]="Sensors have not been polled yet",
        ["температура: фоновый сборщик"]="temperature: background collector",
        ["температура: "]="temperature: ",
        ["температура недоступна: PawnIO не установлен"]="temperature unavailable: PawnIO is not installed",
        ["температура недоступна: попробуйте запуск от администратора"]="temperature unavailable: try running as administrator",
        ["температура недоступна: проверьте драйвер PawnIO"]="temperature unavailable: check the PawnIO driver",
        ["не найден"]="not found",
        ["Ошибка датчиков: "]="Sensor error: ",
        [". Попробуйте внешний LibreHardwareMonitor; повышение прав автоматически не выполняется."]=". Try an external LibreHardwareMonitor; the app does not elevate automatically.",
        ["Повтор через "]="Retry in ",
        [" с"]=" s",
        ["Куб: экран актуален"]="Cube: screen up to date",
        ["Неполное подтверждение кадра"]="Incomplete frame acknowledgement",
        ["Настройки изменены; ожидается новый кадр"]="Settings changed; awaiting a new frame",
        ["Куб: кадр принят"]="Cube: frame accepted",
        ["Куб недоступен: "]="Cube unavailable: ",
        ["Для русского экрана обновите firmware.bin: текущая прошивка не поддерживает кириллицу."]="Update firmware.bin to use Russian screens: the current firmware does not support Cyrillic.",
        ["Введите IP или имя куба без http:// и пути"]="Enter the cube IP or hostname without http:// or a path",
        ["Введите хотя бы две буквы города"]="Enter at least two letters of the city name"
    };
}

// Settings use stable identifiers regardless of the displayed UI language.
public static class ScreenMode
{
    public static string Normalize(string? value) => value switch
    {
        "Clock" or "Часы и погода" or "Clock and weather" => "Clock",
        "Monitor" or "PC MONITOR" or "Монитор ПК" or "PC monitor" => "Monitor",
        _ => "Auto"
    };
}
