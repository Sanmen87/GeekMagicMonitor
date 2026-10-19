using System.Text.Json;

namespace GeekMagicMonitor.Core;

public sealed class WeatherUnavailableException(IReadOnlyList<string> failures) : Exception(string.Join("; ",failures));

public static class WeatherClient
{
    public static async Task<Weather> FetchAsync(HttpClient client,double latitude,double longitude,string provider,CancellationToken token,string language="ru")
    {
        string[] sources=provider switch {"wttr.in"=>["wttr.in"],"Open-Meteo"=>["Open-Meteo"],_=>["wttr.in","Open-Meteo"]};
        var failures=new List<string>();
        foreach(var source in sources)
        {
            string url=source=="wttr.in"
                ?FormattableString.Invariant($"https://wttr.in/{latitude:F5},{longitude:F5}?format=j1&lang=en")
                :FormattableString.Invariant($"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m,wind_direction_10m&wind_speed_unit=ms&timezone=UTC");
            try
            {
                using var request=new HttpRequestMessage(HttpMethod.Get,url);
                request.Headers.UserAgent.ParseAdd("GeekMagicMonitor/0.1");
                using var response=await client.SendAsync(request,token);
                response.EnsureSuccessStatusCode();
                var json=await response.Content.ReadAsStringAsync(token);
                return source=="wttr.in"?WeatherPayload.ParseWttr(json,DateTimeOffset.UtcNow):WeatherPayload.Parse(json);
            }
            catch(Exception ex) when(!token.IsCancellationRequested && ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException or IndexOutOfRangeException)
            {
                failures.Add(source+": "+(ex switch
                {OperationCanceledException=>Localization.Text("тайм-аут",language),HttpRequestException h when h.StatusCode.HasValue=>"HTTP "+(int)h.StatusCode.Value,HttpRequestException=>Localization.Text("нет соединения",language),_=>Localization.Text("неверный ответ",language)}));
            }
        }
        throw new WeatherUnavailableException(failures);
    }
}
