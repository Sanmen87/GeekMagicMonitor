using System.Text.Json;

namespace GeekMagicMonitor.Core;

public record CityResult(string Name,string Region,string Country,double Latitude,double Longitude,string TimeZone)
{
    public override string ToString()=>string.Join(", ",new[]{Name,Region,Country}.Where(x=>!string.IsNullOrWhiteSpace(x)));
}
public static class CitySearch
{
    public static readonly CityResult Uchkeken=new("Учкекен","Карачаево-Черкессия","Россия",43.94508,42.51652,"Europe/Moscow");
    public static CityResult DefaultCity(string language)=>Localization.Normalize(language)=="en"
        ?Uchkeken with{Name="Uchkeken",Region="Karachay-Cherkessia",Country="Russia"}:Uchkeken;
    public static async Task<IReadOnlyList<CityResult>> FindAsync(HttpClient client,string query,CancellationToken token,string language="ru")
    {
        query=query.Trim();if(query.Length<2)throw new ArgumentException(Localization.Text("Введите хотя бы две буквы города",language));
        using var response=await client.GetAsync("https://geocoding-api.open-meteo.com/v1/search?name="+Uri.EscapeDataString(query)+"&count=10&language="+Localization.Normalize(language)+"&format=json",token);
        response.EnsureSuccessStatusCode();
        return Parse(await response.Content.ReadAsStringAsync(token));
    }
    public static IReadOnlyList<CityResult> Parse(string json)
    {
        using var document=JsonDocument.Parse(json);
        if(!document.RootElement.TryGetProperty("results",out var results))return [];
        string Field(JsonElement x,string key)=>x.TryGetProperty(key,out var value)?value.GetString()??"":"";
        return results.EnumerateArray().Select(x=>new CityResult(Field(x,"name"),Field(x,"admin1"),Field(x,"country"),x.GetProperty("latitude").GetDouble(),x.GetProperty("longitude").GetDouble(),Field(x,"timezone")))
            .Where(x=>double.IsFinite(x.Latitude)&&Math.Abs(x.Latitude)<=90&&double.IsFinite(x.Longitude)&&Math.Abs(x.Longitude)<=180).ToArray();
    }
}
