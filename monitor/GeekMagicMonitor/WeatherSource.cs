using System.Globalization;
using System.Text.Json;
using GeekMagicMonitor.Core;

namespace GeekMagicMonitor;

public sealed class WeatherSource(HttpClient client)
{
    private record Cache(double Latitude,double Longitude,Weather Value);
    public string Language {get;set;}="ru";
    private string T(string text)=>Localization.Text(text,Language);
    private DateTimeOffset nextTry;
    private string key="";
    public Weather? Current {get;private set;}
    private static string CacheKey(Settings settings)=>FormattableString.Invariant($"{settings.Latitude},{settings.Longitude},{settings.WeatherProvider}");
    public Weather? For(Settings settings) => settings.WeatherEnabled && key==CacheKey(settings) ? Current : null;
    public string Detail {get;private set;} = "Погода выключена";
    public void RetryNow() {nextTry=default;}
    public async Task UpdateAsync(Settings settings, CancellationToken token)
    {
        Language=settings.Language;
        if(!settings.WeatherEnabled) { Current=null; Detail=T("Погода выключена"); key=""; return; }
        if(string.IsNullOrWhiteSpace(settings.Location)){Current=null;Detail=T("Погода: выберите город и примените настройки");return;}
        var newKey=CacheKey(settings);
        string file=Path.Combine(Settings.Folder,"weather.json");
        if(key!=newKey)
        {
            key=newKey; nextTry=default; Current=null;
            try
            {
                var cache=JsonSerializer.Deserialize<Cache>(File.ReadAllText(file));
                if(cache is not null && cache.Latitude==settings.Latitude && cache.Longitude==settings.Longitude &&
                    (settings.WeatherProvider=="Auto" || cache.Value.Provider==settings.WeatherProvider)) Current=cache.Value;
            }
            catch(Exception ex) when(ex is IOException or JsonException or UnauthorizedAccessException) { }
        }
        if(DateTimeOffset.UtcNow<nextTry) return;
        Detail=T("Погода: запрос для ")+settings.Location+"…";
        try
        {
            Current=await WeatherClient.FetchAsync(client,settings.Latitude,settings.Longitude,settings.WeatherProvider,token,Language);
            Directory.CreateDirectory(Settings.Folder);
            File.WriteAllText(file,JsonSerializer.Serialize(new Cache(settings.Latitude,settings.Longitude,Current)));
            nextTry=DateTimeOffset.UtcNow.AddMinutes(15);
            Detail=T("Погода: ")+settings.Location+"; "+Current.Provider;
        }
        catch(Exception ex) when(!token.IsCancellationRequested && ex is WeatherUnavailableException or HttpRequestException or TaskCanceledException or JsonException or IOException or InvalidOperationException or KeyNotFoundException or FormatException or UnauthorizedAccessException)
        {
            nextTry=DateTimeOffset.UtcNow.AddMinutes(1);
            string reason=ex switch
            {
                WeatherUnavailableException=>ex.Message,
                OperationCanceledException=>T("сервер Open-Meteo не ответил за 15 с; проверьте доступ/VPN к api.open-meteo.com"),
                HttpRequestException h when h.StatusCode.HasValue=>T("сервер вернул HTTP ")+(int)h.StatusCode.Value,
                HttpRequestException=>T("нет соединения с api.open-meteo.com; проверьте сеть/VPN"),
                IOException or UnauthorizedAccessException=>T("не удалось сохранить кэш"),
                _=>T("некорректный ответ сервера")
            };
            Detail=T("Погода: ")+reason+(Current is null?T("; кэша нет"):T("; показан последний результат"));
            Log.Error("weather",ex);
        }
    }
}
