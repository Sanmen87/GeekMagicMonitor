using System.Net;
using System.Text;
using System.Text.Json;
using GeekMagicMonitor.Core;

static void Check(bool condition,string message) {if(!condition)throw new Exception(message);}
var now=new DateTimeOffset(2026,10,18,10,0,0,TimeSpan.Zero);
var metrics=new Metrics(54,28,63,76,12.4,32);
string SnapshotJson(DateTimeOffset stamp,double? temperature)=>JsonSerializer.Serialize(new SensorSnapshot(stamp,temperature));
Check(SensorSnapshot.Parse(SnapshotJson(now,52.5),now)?.CpuTemperature==52.5,"Fresh collector snapshot must be usable");
Check(SensorSnapshot.Parse(SnapshotJson(now.AddSeconds(-11),52.5),now) is null,"Dead collector must not leave a frozen temperature");
Check(SensorSnapshot.Parse(SnapshotJson(now.AddSeconds(3),52.5),now) is null,"Future snapshot must be rejected");
Check(SensorSnapshot.Parse(SnapshotJson(now,0),now) is null && SensorSnapshot.Parse(SnapshotJson(now,null),now) is null,"Unavailable collector temperatures must remain N/A");
Check(SensorSnapshot.Parse("{broken",now) is null,"Incomplete snapshot must not break sensor polling");
string snapshotTest=Path.Combine(Path.GetTempPath(),"GeekMagicSnapshot-"+Guid.NewGuid()+".json");
try
{
    new SensorSnapshot(now,51).Write(snapshotTest);
    new SensorSnapshot(now,53).Write(snapshotTest);
    Check(SensorSnapshot.Read(snapshotTest,now)?.CpuTemperature==53,"Atomic snapshot replacement must expose the latest reading");
    File.WriteAllText(snapshotTest,new string(' ',SensorSnapshot.MaximumBytes+1));
    Check(SensorSnapshot.Read(snapshotTest,now) is null,"Oversized collector snapshot must be rejected");
}
finally {File.Delete(snapshotTest);}
Check(TemperatureSelection.Cpu([new("Core (Tctl/Tdie)",0)]) is null,"Inaccessible Ryzen zero reading must not be displayed as a temperature");
Check(TemperatureSelection.Cpu([new("CPU Package",0),new("Core (Tctl/Tdie)",53)])?.Value==53,"Invalid preferred reading must not hide a valid Ryzen temperature");
Check(TemperatureSelection.Cpu([new("CPU Package",double.NaN),new("Core (Tdie)",47)])?.Value==47,"Temperature fallback must reject NaN");
var cityResults=CitySearch.Parse("""{"results":[{"name":"Учкекен","admin1":"Карачаево-Черкессия","country":"Россия","latitude":43.94508,"longitude":42.51652,"timezone":"Europe/Moscow"}]}""");
Check(cityResults.Count==1 && cityResults[0].Latitude==43.94508 && cityResults[0].ToString().Contains("Учкекен"),"City search must preserve selected location and coordinates");
Check(CitySearch.Parse("{}").Count==0,"Empty city search must be handled");
var parsedWeather=WeatherPayload.Parse("""{"current":{"temperature_2m":8.3,"relative_humidity_2m":72,"weather_code":85,"time":"2026-10-18T07:00"}}""");
Check(parsedWeather.Temperature==8.3 && parsedWeather.Summary=="Snow" && parsedWeather.CapturedAt.Offset==TimeSpan.Zero,"Weather parser must preserve values and UTC timestamp");
bool badWeatherRejected=false;
try{WeatherPayload.Parse("""{"current":{"temperature_2m":8.3,"relative_humidity_2m":172,"weather_code":3,"time":"2026-10-18T07:00"}}""");}catch(JsonException){badWeatherRejected=true;}
Check(badWeatherRejected,"Invalid humidity must not become a weather measurement");
const string wttrFixture="""{"current_condition":[{"temp_C":"22","humidity":"35","weatherCode":"113","observation_time":"07:18 AM","windspeedKmph":"7.2","winddirDegree":"67.5"}]}""";
var wttrWeather=WeatherPayload.ParseWttr(wttrFixture,now);
Check(wttrWeather.Temperature==22 && wttrWeather.Humidity==35 && wttrWeather.Provider=="wttr.in" && wttrWeather.Summary=="Clear" && wttrWeather.TimeEstimated,"wttr data must retain source and time uncertainty");
Check(wttrWeather.WindSpeedMps==2 && Wind.Compass(wttrWeather.WindDirectionDegrees)=="ENE","wttr km/h must convert to m/s and meteorological compass direction");
Check(Wind.Compass(0)=="N" && Wind.Compass(360)=="N" && Wind.Compass(90)=="E" && Wind.Compass(180)=="S" && Wind.Compass(270)=="W","Compass cardinal directions must be correct");
Check(Wind.Compass(11.24)=="N" && Wind.Compass(11.25)=="NNE" && Wind.Compass(null)=="N/A","Compass boundaries and missing direction must be handled");
Check(Wind.Speed(-1) is null && Wind.Speed(0)==0 && Wind.Direction(361) is null,"Invalid wind values must not look like calm weather");
var openMeteoWind=WeatherPayload.Parse("""{"current_units":{"wind_speed_10m":"km/h"},"current":{"temperature_2m":12,"relative_humidity_2m":60,"weather_code":3,"time":"2026-10-18T07:00","wind_speed_10m":18,"wind_direction_10m":180}}""");
Check(openMeteoWind.WindSpeedMps==5 && Wind.Compass(openMeteoWind.WindDirectionDegrees)=="S","Open-Meteo units and wind direction must be normalized");
Check(parsedWeather.WindSpeedMps is null && parsedWeather.WindDirectionDegrees is null,"Old responses without wind must remain compatible");
Check(WeatherPayload.ParseWttr(wttrFixture,now).CapturedAt==new DateTimeOffset(2026,10,18,7,18,0,TimeSpan.Zero),"wttr UTC time must be interpreted correctly");
Check(WeatherPayload.ParseWttr(wttrFixture,new DateTimeOffset(2026,10,18,0,5,0,TimeSpan.Zero)).CapturedAt.Day==17,"wttr date inference must handle midnight");
using(var weatherClient=new HttpClient(new WeatherMock()))
{
    var fallbackWeather=await WeatherClient.FetchAsync(weatherClient,43.94508,42.51652,"Auto",default);
    Check(fallbackWeather.Provider=="Open-Meteo" && fallbackWeather.Temperature==12,"Failed primary weather server must fall back to Open-Meteo");
}
var frame=Screens.Monitor(metrics,false,deviceAddress:"192.168.88.129");
Check(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(new{commands=Frames.Commands(frame,null)}))<8192,"Frame exceeds firmware body limit");
Check(Frames.Commands(frame,null)[0]["type"].Equals("clear"),"Initial frame must clear screen");
Check(Frames.Commands(frame,frame).Count==0,"Unchanged frame should have no dirty regions");
var changed=Screens.Monitor(metrics with {CpuTemp=9},false,deviceAddress:"192.168.88.129");
Check(Frames.Commands(changed,frame).Count>1 && Frames.Commands(changed,frame).All(x=>!x["type"].Equals("clear")),"Changed value must redraw its card without clearing the screen");
Check(Frames.Commands(changed,frame).Where(x=>x["type"].Equals("text")).All(x=>x["text"].ToString()!="GPU"),"CPU update must not redraw GPU card");
Check(Screens.Monitor(new(),false).Lines.Any(x=>x.Text.Contains("N/A")),"Missing sensors must not become zero");
var zone=TimeZoneInfo.CreateCustomTimeZone("MSK",TimeSpan.FromHours(3),"MSK","MSK");
var clock=Screens.Clock(new DateTimeOffset(2026,10,18,21,0,0,TimeSpan.Zero),zone,null);
Check(clock.Lines.Any(x=>x.Text=="19.10.2026"),"Date should roll over in target timezone");
var weather=new Weather(20,50,"Clear",now.AddMinutes(-31));
Check(Screens.Clock(now,zone,weather).Lines.Any(x=>x.Text=="WEATHER STALE"),"Old weather should be marked stale");
Check(Screens.Clock(now,zone,weather with {CapturedAt=now.AddHours(-6)}).Lines.Any(x=>x.Text=="WEATHER N/A"),"Six-hour weather must expire");
var weatherCases=from summary in new[]{"Clear","Cloudy","Rain","Snow","Fog","Thunder"}
    from temperature in new[]{-35d,9d,22d,45d}
    from bearing in new double?[]{null,0,90,180,270,359}
    select Screens.Clock(now,zone,new Weather(temperature,100,summary,now,WindSpeedMps:bearing is null?null:3.2,WindDirectionDegrees:bearing),deviceAddress:"255.255.255.255");
Check(frame.Lines.Any(line=>line.Text=="IP 192.168.88.129"),"Monitor must retain the configured cube address");
var addressChange=Screens.Monitor(metrics,false,deviceAddress:"192.168.88.130");
Check(Frames.Commands(addressChange,frame).All(command=>!command["type"].Equals("clear")) &&
    Frames.Commands(addressChange,frame).Where(command=>command["type"].Equals("text")).All(command=>command["text"].ToString()!="CPU"),"Address change must refresh only footer");
var extremeCases=new[]{Screens.Monitor(new(125,100,100,100,255.9,1024),false),Screens.Monitor(new(125,100,100,100,4096,4096),false),
    Screens.Clock(now,zone,weather with{CapturedAt=now,WindSpeedMps=0,WindDirectionDegrees=0}),
    Screens.Clock(now,zone,weather with{CapturedAt=now,WindSpeedMps=150,WindDirectionDegrees=359})};
var russianCases=(from summary in new[]{"Clear","Cloudy","Rain","Snow","Fog","Thunder"}
    from temperature in new[]{-35d,9d,22d,45d}
    from bearing in new double?[]{null,0,90,180,270,359}
    select Screens.Clock(now,zone,new Weather(temperature,100,summary,now,WindSpeedMps:bearing is null?null:3.2,WindDirectionDegrees:bearing),deviceAddress:"255.255.255.255",language:"ru"))
    .Concat(new[]{Screens.Monitor(metrics,false,language:"ru"),Screens.Monitor(new(),true,true,"255.255.255.255","ru"),
        Screens.Clock(now,zone,null,language:"ru"),Screens.Clock(now,zone,weather,language:"ru"),
        Screens.Clock(now,zone,weather with{CapturedAt=now,WindSpeedMps=0},language:"ru")});
Check(Screens.Clock(now,zone,wttrWeather,language:"ru").Lines.Any(x=>x.Id=="weekday" && x.Text=="ВС"),"Russian weekdays must not depend on the OS culture");
Check(Screens.Clock(now,zone,wttrWeather,language:"en").Lines.Any(x=>x.Id=="weekday" && x.Text=="SUN"),"English weekdays must not depend on the OS culture");
Check(Wind.Compass(67.5,"ru")=="ВСВ" && Wind.Compass(270,"ru")=="З","Russian wind direction must be localized");
var englishClock=Screens.Clock(now,zone,wttrWeather,language:"en");
var russianClock=Screens.Clock(now,zone,wttrWeather,language:"ru");
Check(Frames.Commands(russianClock,englishClock).Any(x=>x["type"].Equals("text") && x["text"].Equals("ЧАСЫ")),"Language switch must invalidate translated regions");
Check(englishClock.Lines.First(x=>x.Id=="temperature").Text==russianClock.Lines.First(x=>x.Id=="temperature").Text,"Language must not change measurements");
foreach(var legacy in new[]{"Авто","Часы и погода","PC MONITOR","Auto","Clock","Monitor"})
{
    var loaded=JsonSerializer.Deserialize<GeekMagicMonitor.Settings>(JsonSerializer.Serialize(new{Screen=legacy}))!.Normalize();
    Check(loaded.Screen==ScreenMode.Normalize(legacy) && loaded.Language=="ru" && loaded.DisplayLanguage=="en","Existing settings must migrate without changing the original languages");
    var selected=loaded with{Language="en",DisplayLanguage="ru",Gpu="Test GPU"};
    Check(JsonSerializer.Deserialize<GeekMagicMonitor.Settings>(JsonSerializer.Serialize(selected))==selected,"Languages and stable selections must survive saving and reloading");
}
Check(new GeekMagicMonitor.Settings{Language="unsupported",DisplayLanguage="unsupported",Screen="unsupported"}.Normalize() is {Language:"ru",DisplayLanguage:"ru",Screen:"Auto"},"Unknown language/mode values need a deterministic fallback");
string settingsTestFolder=Path.Combine(Path.GetTempPath(),"GeekMagicSettings-"+Guid.NewGuid());
try
{
    Check(!GeekMagicMonitor.Settings.Load(settingsTestFolder).SendToCube,"A first launch must not enable sending without a saved preference");
    Directory.CreateDirectory(settingsTestFolder);
    File.WriteAllText(Path.Combine(settingsTestFolder,"settings.json"),"""{"Host":"192.168.88.129","Screen":"Часы и погода","Demo":false,"Language":"en"}""");
    var restored=GeekMagicMonitor.Settings.Load(settingsTestFolder);
    Check(!restored.SendToCube && restored.Screen=="Clock","Legacy settings must load without unexpectedly enabling sending");
    foreach(bool enabled in new[]{true,false,true,false})
    {
        restored=restored with{SendToCube=enabled};
        restored.Save(settingsTestFolder);
        var restarted=GeekMagicMonitor.Settings.Load(settingsTestFolder);
        Check(restarted.SendToCube==enabled && restarted==restored,"Sending on/off must survive a real file save and fresh load without losing other settings");
        var translated=restarted with{Language=restarted.Language=="en"?"ru":"en"};
        translated.Save(settingsTestFolder);
        restored=GeekMagicMonitor.Settings.Load(settingsTestFolder);
        Check(restored.SendToCube==enabled,"Saving another preference must not reset sending on/off");
    }
}
finally
{
    File.Delete(Path.Combine(settingsTestFolder,"settings.json"));
    File.Delete(Path.Combine(settingsTestFolder,"settings.json.tmp"));
    if(Directory.Exists(settingsTestFolder))Directory.Delete(settingsTestFolder);
}
Console.WriteLine("PASS: sending on/off survives file save and restart; legacy defaults and other preferences preserved.");
int checkedFrames=0;
foreach(var screen in new[]{frame,changed,Screens.Monitor(new(),true),clock,Screens.Clock(now,zone,weather),Screens.Clock(now,zone,wttrWeather),Screens.Clock(now,zone,weather with{Summary="Rain"})}.Concat(weatherCases).Concat(extremeCases).Concat(russianCases))
{
    checkedFrames++;
    var commands=Frames.Commands(screen,null);
    Check(commands.Count<=64,"Color screen exceeds 64-command firmware limit");
    Check(Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(new{commands}))<=8192,"Color screen exceeds firmware body limit");
    foreach(var line in screen.Lines)Check(line.X+line.Text.Length*6*line.Size<=240 && line.Y+8*line.Size<=240,"Text does not fit 240x240: "+line.Id);
    for(int i=0;i<screen.Lines.Count;i++)for(int j=i+1;j<screen.Lines.Count;j++)
    {
        var a=screen.Lines[i];var b=screen.Lines[j];
        bool intersects=a.X<b.X+b.Text.Length*6*b.Size && b.X<a.X+a.Text.Length*6*a.Size && a.Y<b.Y+8*b.Size && b.Y<a.Y+8*a.Size;
        Check(!intersects,$"Text overlaps: {a.Id} / {b.Id}");
    }
    foreach(var region in screen.Regions!)foreach(var text in region.Drawing.Where(command=>command["type"].Equals("text")))
    {
        int x=(int)text["x"],y=(int)text["y"],size=(int)text["size"];
        Check(x>=region.X && y>=region.Y && x+text["text"].ToString()!.Length*6*size<=region.X+region.Width && y+8*size<=region.Y+region.Height,
            "Text escapes its redraw region: "+region.Id);
    }
    foreach(var command in commands)
    {
        foreach(string key in new[]{"x","x0","x1","y","y0","y1"})
            if(command.TryGetValue(key,out var coordinate))Check((int)coordinate>=0 && (int)coordinate<240,"Drawing coordinate outside LCD: "+key);
        if(command["type"].Equals("circle"))
        {
            int x=(int)command["x"],y=(int)command["y"],r=(int)command["r"];
            Check(x-r>=0 && y-r>=0 && x+r<240 && y+r<240,"Circle outside LCD");
        }
        if(command.ContainsKey("w"))Check((int)command["x"]+(int)command["w"]<=240 && (int)command["y"]+(int)command["h"]<=240,"Rectangle outside LCD");
    }
}
Console.WriteLine($"PASS: {checkedFrames} frames checked against LCD geometry and batch size/command limits.");
foreach(var host in new[]{"http://127.0.0.1","user@localhost","localhost/path","localhost?query=1"})
{
    bool rejected=false;try{CubeSender.Address(host);}catch(ArgumentException){rejected=true;}
    Check(rejected,"Unsafe address accepted: "+host);
}
var handler=new MockCube();using var client=new HttpClient(handler);var sender=new CubeSender(client);var address=CubeSender.Address("127.0.0.1:8081");
await sender.SendAsync(address,frame,now,default);
Check(handler.Calls==1 && handler.LastHadClear,"First request must be full");
await sender.SendAsync(address,changed,now.AddSeconds(2),default);
Check(handler.Calls==2 && !handler.LastHadClear,"Incremental request expected");
handler.Fail=true;await sender.SendAsync(address,frame,now.AddSeconds(3),default);
int calls=handler.Calls;
await sender.SendAsync(address,frame,now.AddSeconds(4),default);
Check(handler.Calls==calls,"Retry must respect backoff");
handler.Fail=false;await sender.SendAsync(address,frame,now.AddSeconds(5),default);
Check(handler.LastHadClear,"Recovery must resend full screen");
await sender.SendAsync(address,frame,now.AddSeconds(16),default);
Check(handler.LastHadClear && handler.Calls==calls+2,"Idle heartbeat must refresh cube lease");
handler.BadAck=true;await sender.SendAsync(address,changed,now.AddSeconds(18),default);
handler.BadAck=false;await sender.SendAsync(address,changed,now.AddSeconds(20),default);
Check(handler.LastHadClear,"Invalid acknowledgement must invalidate delta state");
handler.OnRequest=sender.Reset;
await sender.SendAsync(address,frame,now.AddSeconds(22),default);
await sender.SendAsync(address,frame,now.AddSeconds(23),default);
Check(handler.LastHadClear,"In-flight reply from old settings must not restore previous frame state");
using(var languageHandler=new MockCube())
using(var languageClient=new HttpClient(languageHandler))
{
    var languageSender=new CubeSender(languageClient);
    string blocked=await languageSender.SendAsync(address,russianClock,now,default,"en");
    Check(blocked.Contains("Update firmware.bin") && languageHandler.Calls==0,"Legacy firmware must not receive unreadable Cyrillic");
    await languageSender.SendAsync(address,englishClock,now.AddSeconds(1),default,"en");
    Check(languageHandler.Calls==1,"English screens must still work on legacy firmware");
    languageHandler.Cyrillic=true;languageSender.Reset();
    string accepted=await languageSender.SendAsync(address,russianClock,now.AddSeconds(2),default,"en");
    Check(accepted=="Cube: frame accepted" && languageHandler.Calls==2 && languageHandler.LastHadClear,"Supported firmware must receive a full Russian frame after switching language");
}
Console.WriteLine("PASS: bilingual screen layout, language migration/persistence, firmware capability and localized delivery status.");
Console.WriteLine("PASS: frame deltas, missing data, timezone rollover, weather expiry, address validation, HTTP acknowledgement, retries and heartbeat.");

sealed class MockCube : HttpMessageHandler
{
    public int Calls; public bool Fail,BadAck,LastHadClear,Cyrillic;
    public Action? OnRequest;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        if(request.Method==HttpMethod.Get && request.RequestUri!.AbsolutePath=="/api/v1/device")
            return new(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(new{application="GeekMagicMonitor",cyrillicText=Cyrillic}))};
        Calls++;
        var callback=OnRequest;OnRequest=null;callback?.Invoke();
        CheckEndpoint(request);
        long? declaredLength=request.Content!.Headers.ContentLength;
        if(declaredLength is null || declaredLength<=0 || request.Headers.TransferEncodingChunked==true)
            throw new Exception("Cube requests require a known Content-Length before reading/serializing the content");
        using var json=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
        var commands=json.RootElement.GetProperty("commands");
        LastHadClear=commands.EnumerateArray().Any(x=>x.GetProperty("type").GetString()=="clear");
        return new(Fail?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(new{status="ok",processed=BadAck?0:commands.GetArrayLength()}),Encoding.UTF8,"application/json")};
    }
    private static void CheckEndpoint(HttpRequestMessage request)
    {if(request.Method!=HttpMethod.Post || request.RequestUri!.AbsolutePath!="/api/v1/draw/batch")throw new Exception("Wrong endpoint");}
}

sealed class WeatherMock : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        bool primary=request.RequestUri!.Host=="wttr.in";
        string body="""{"current":{"temperature_2m":12,"relative_humidity_2m":60,"weather_code":3,"time":"2026-10-18T07:00"}}""";
        return Task.FromResult(new HttpResponseMessage(primary?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){Content=new StringContent(body)});
    }
}
