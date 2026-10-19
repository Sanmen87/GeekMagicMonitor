using System.Diagnostics;
using System.Text.Json;
using GeekMagicMonitor.Core;

namespace GeekMagicMonitor;

internal static class CubeCheck
{
    public static async Task RunAsync(string output)
    {
        var settings=Settings.Load();
        var address=CubeSender.Address(settings.Host);
        using var handler=new RecordingHandler();
        using var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(8)};
        using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var device=JsonDocument.Parse(await client.GetStringAsync(new Uri(address,"api/v1/device"),stop.Token));
        var status=device.RootElement;
        if(status.GetProperty("application").GetString()!="GeekMagicMonitor" || status.GetProperty("mode").GetString()!="normal" || !status.GetProperty("displayReady").GetBoolean())
            throw new InvalidOperationException("Expected our running firmware with an initialized display");
        using var sensors=new Sensors();
        sensors.Read(settings.Gpu);
        await Task.Delay(1200,stop.Token);
        var metrics=sensors.Read(settings.Gpu);
        using var weatherHttp=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
        var weather=await WeatherClient.FetchAsync(weatherHttp,settings.Latitude,settings.Longitude,settings.WeatherProvider,stop.Token);
        var zone=TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone);
        var sender=new CubeSender(client);
        async Task Send(ScreenFrame frame)
        {
            string result=await sender.SendAsync(address,frame,DateTimeOffset.UtcNow,stop.Token);
            if(result!="Куб: кадр принят" && result!="Куб: экран актуален")throw new IOException(result);
        }
        try
        {
            await Send(Screens.Clock(DateTimeOffset.UtcNow,zone,weather,deviceAddress:settings.Host));
            await Task.Delay(3000,stop.Token);
            await Send(Screens.Clock(DateTimeOffset.UtcNow,zone,weather,deviceAddress:settings.Host));
            await Send(Screens.Monitor(metrics,false,deviceAddress:settings.Host));
            await Task.Delay(2500,stop.Token);
            metrics=sensors.Read(settings.Gpu);
            await Send(Screens.Monitor(metrics,false,deviceAddress:settings.Host));
            await Task.Delay(3000,stop.Token);
            await Send(Screens.Clock(DateTimeOffset.UtcNow,zone,weather,deviceAddress:settings.Host));
            File.WriteAllText(output,JsonSerializer.Serialize(new{Status="PASS",CapturedAt=DateTimeOffset.Now,Host=settings.Host,
                Elevated=Sensors.IsElevated,sensors.AgentUsed,Metrics=metrics,Weather=weather,Requests=handler.Requests},new JsonSerializerOptions{WriteIndented=true}));
        }
        catch(Exception ex)
        {
            File.WriteAllText(output,JsonSerializer.Serialize(new{Status="FAIL",Error=ex.Message,Requests=handler.Requests},new JsonSerializerOptions{WriteIndented=true}));
            throw;
        }
    }
    private sealed class RecordingHandler : DelegatingHandler
    {
        public List<object> Requests {get;}=[];
        public RecordingHandler() : base(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}) { }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            int commands=0,bytes=0;bool full=false;
            long? lengthBeforeSend=request.Content?.Headers.ContentLength;
            var watch=Stopwatch.StartNew();
            var response=await base.SendAsync(request,token);
            string body=await response.Content.ReadAsStringAsync(token);
            // Inspect the payload only AFTER sending. Buffering JsonContent before
            // dispatch would change framing and conceal the ESP8266 chunked-body bug.
            if(request.Content is not null)
            {
                byte[] payload=await request.Content.ReadAsByteArrayAsync(token);bytes=payload.Length;
                using var json=JsonDocument.Parse(payload);
                var batch=json.RootElement.GetProperty("commands");commands=batch.GetArrayLength();
                full=batch.EnumerateArray().Any(item=>item.GetProperty("type").GetString()=="clear");
            }
            if(request.Method==HttpMethod.Post)Requests.Add(new{Path=request.RequestUri!.AbsolutePath,Commands=commands,Bytes=bytes,Full=full,
                LengthBeforeSend=lengthBeforeSend,HttpStatus=(int)response.StatusCode,ElapsedMs=watch.ElapsedMilliseconds,Response=body});
            return response;
        }
    }
}
