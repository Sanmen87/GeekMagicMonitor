using System.Globalization;
using System.Text.Json;

namespace GeekMagicMonitor.Core;

public static class WeatherPayload
{
    public static Weather ParseWttr(string json,DateTimeOffset receivedAt)
    {
        using var document=JsonDocument.Parse(json);
        var current=document.RootElement.GetProperty("current_condition")[0];
        double ReadNumber(string field)
        {
            var value=current.GetProperty(field);
            return value.ValueKind==JsonValueKind.Number?value.GetDouble():double.Parse(value.GetString()!,CultureInfo.InvariantCulture);
        }
        double temperature=ReadNumber("temp_C"),humidity=ReadNumber("humidity");
        if(!double.IsFinite(temperature)||!double.IsFinite(humidity)||humidity<0||humidity>100)throw new JsonException("Invalid weather values");
        int code=(int)ReadNumber("weatherCode");
        string summary=code switch
        {
            113=>"Clear",116 or 119 or 122=>"Cloudy",143 or 248 or 260=>"Fog",
            200 or 386 or 389 or 392 or 395=>"Thunderstorm",
            179 or 182 or 185 or 227 or 230 or 281 or 284 or 311 or 314 or 317 or 320 or 323 or 326 or 329 or 332 or 335 or 338 or 350 or 362 or 365 or 368 or 371 or 374 or 377=>"Snow / ice",
            _=>"Rain / precipitation"
        };
        // j1 often supplies only UTC time-of-day, not a dated observation.
        // Infer the most recent occurrence and explicitly mark this estimate.
        var observed=receivedAt.ToUniversalTime();
        if(current.TryGetProperty("observation_time",out var time) &&
            DateTime.TryParseExact(time.GetString(),"hh:mm tt",CultureInfo.InvariantCulture,DateTimeStyles.None,out var clock))
        {
            observed=new DateTimeOffset(receivedAt.UtcDateTime.Date.Add(clock.TimeOfDay),TimeSpan.Zero);
            if(observed>receivedAt.AddMinutes(5))observed=observed.AddDays(-1);
        }
        double? OptionalNumber(string field)=>current.TryGetProperty(field,out var value) &&
            double.TryParse(value.ToString(),NumberStyles.Float,CultureInfo.InvariantCulture,out var number)?number:null;
        return new(temperature,humidity,summary,observed,"wttr.in",true,
            Wind.Speed(OptionalNumber("windspeedKmph")/3.6),Wind.Direction(OptionalNumber("winddirDegree")));
    }
    public static Weather Parse(string json)
    {
        using var document=JsonDocument.Parse(json);
        var current=document.RootElement.GetProperty("current");
        double temperature=current.GetProperty("temperature_2m").GetDouble(),humidity=current.GetProperty("relative_humidity_2m").GetDouble();
        if(!double.IsFinite(temperature)||!double.IsFinite(humidity)||humidity<0||humidity>100)throw new JsonException("Invalid weather values");
        int code=current.GetProperty("weather_code").GetInt32();
        string summary=code switch{0=>"Clear",1 or 2 or 3=>"Cloudy",45 or 48=>"Fog",>=71 and <=77 or 85 or 86=>"Snow",>=95=>"Thunderstorm",_=>"Rain / precipitation"};
        var measured=DateTimeOffset.Parse(current.GetProperty("time").GetString()!,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal);
        double? speed=current.TryGetProperty("wind_speed_10m",out var speedValue) && speedValue.ValueKind==JsonValueKind.Number && speedValue.TryGetDouble(out double s)?s:null;
        double? direction=current.TryGetProperty("wind_direction_10m",out var directionValue) && directionValue.ValueKind==JsonValueKind.Number && directionValue.TryGetDouble(out double d)?d:null;
        string unit="m/s"; // Request explicitly asks for m/s.
        if(document.RootElement.TryGetProperty("current_units",out var units) && units.TryGetProperty("wind_speed_10m",out var speedUnit))unit=speedUnit.GetString()??"";
        speed=unit switch{"m/s"=>speed,"km/h"=>speed/3.6,_=>null};
        return new(temperature,humidity,summary,measured,WindSpeedMps:Wind.Speed(speed),WindDirectionDegrees:Wind.Direction(direction));
    }
}
