using System.Globalization;
using System.Text.Json;

namespace GeekMagicMonitor.Core;

public static class Art
{
    public static Dictionary<string,object> Rect(int x,int y,int w,int h,string color,int radius=0) =>
        new(){{"type",radius>0?"roundrect":"rect"},{"x",x},{"y",y},{"w",w},{"h",h},{"r",radius},{"color",color},{"fill",true}};
    public static Dictionary<string,object> Circle(int x,int y,int radius,string color,bool fill=true) =>
        new(){{"type","circle"},{"x",x},{"y",y},{"r",radius},{"color",color},{"fill",fill}};
    public static Dictionary<string,object> Line(int x,int y,int x1,int y1,string color) =>
        new(){{"type","line"},{"x0",x},{"y0",y},{"x1",x1},{"y1",y1},{"color",color}};
    public static Dictionary<string,object> Text(ScreenLine line,string background="#091321") =>
        new(){{"type","text"},{"x",line.X},{"y",line.Y},{"size",line.Size},{"text",line.Text},{"color",line.Color},{"bg",background}};
}

public static class Screens
{
    private const string Ink="#edf5ff",Muted="#91a9c3",Cyan="#31d7ed",Purple="#b798ff",Green="#66e2a9",Gold="#ffcf66";
    private static string Number(double? value,string format="0") => value is double v && double.IsFinite(v)
        ? v.ToString(format,CultureInfo.InvariantCulture) : "N/A";
    private static double? Percent(double? value) => value is double v && double.IsFinite(v)?Math.Clamp(v,0,100):null;
    private static string Temperature(double? value)=>value is double v && double.IsFinite(v)?Number(v)+"C":"N/A";
    private static string Heat(double? temperature,string normal) => temperature>=85?"#ff6b79":temperature>=70?Gold:normal;
    public static ScreenFrame Monitor(Metrics data,bool demo,bool stale=false,string? deviceAddress=null,string language="en")
    {
        string L(string ru,string en)=>Localization.Normalize(language)=="ru"?ru:en;
        List<ScreenLine> lines=[];
        List<ScreenRegion> regions=[];
        ScreenLine Text(string id,int x,int y,int size,string value,string color=Ink)
        {var line=new ScreenLine(id,x,y,size,value,color);lines.Add(line);return line;}
        regions.Add(new("header",0,0,240,28,[
            Art.Text(Text("title",12,7,2,L("МОНИТОР","PC MONITOR"),Cyan)),
            Art.Rect(184,10,44,15,"#263348",5),Art.Text(Text("badge",190,14,1,demo?L("ДЕМО","DEMO"):L("ОНЛ","LIVE"),demo?Gold:Green),"#263348")]));
        void Card(string id,int y,string label,double? temp,double? load,string accent,string background,bool gpu)
        {
            var drawing=new List<Dictionary<string,object>> {
                Art.Rect(12,y,216,60,background,9),Art.Rect(12,y+12,3,36,accent)};
            if(gpu)
            {
                // Full-length add-in card, two fans, PCIe edge and slot bracket.
                drawing.Add(Art.Rect(20,y+20,33,18,accent,2));
                drawing.Add(Art.Rect(22,y+22,29,14,background,1));
                drawing.Add(Art.Circle(29,y+29,5,accent));drawing.Add(Art.Circle(44,y+29,5,accent));
                drawing.Add(Art.Circle(29,y+29,2,background));drawing.Add(Art.Circle(44,y+29,2,background));
                drawing.Add(Art.Rect(25,y+39,20,3,Gold));drawing.Add(Art.Line(18,y+16,18,y+41,accent));
            }
            else
            {
                drawing.Add(Art.Rect(25,y+17,22,22,accent,2));drawing.Add(Art.Rect(28,y+20,16,16,background,1));
                foreach(int offset in new[]{0,7,14})
                {
                    drawing.Add(Art.Line(29+offset,y+12,29+offset,y+16,accent));
                    drawing.Add(Art.Line(29+offset,y+40,29+offset,y+44,accent));
                    drawing.Add(Art.Line(20,y+21+offset,24,y+21+offset,accent));
                    drawing.Add(Art.Line(48,y+21+offset,52,y+21+offset,accent));
                }
                drawing.Add(Art.Rect(32,y+24,8,8,accent,1));
            }
            drawing.Add(Art.Text(Text(id+"label",62,y+6,2,label,accent),background));
            drawing.Add(Art.Text(Text(id+"temp",62,y+28,3,Temperature(temp),Heat(temp,Ink)),background));
            drawing.Add(Art.Text(Text(id+"loadlabel",160,y+10,1,L("НАГР","LOAD"),Muted),background));
            drawing.Add(Art.Text(Text(id+"load",148,y+27,3,Number(load)+"%",accent),background));
            drawing.Add(Art.Rect(148,y+53,66,3,"#30445b",1));
            if(Percent(load) is double percent && percent>0)drawing.Add(Art.Rect(148,y+53,Math.Max(1,(int)(66*percent/100)),3,accent,1));
            regions.Add(new(id,0,y,240,60,drawing));
        }
        Card("cpu",30,"CPU",data.CpuTemp,data.CpuLoad,Cyan,"#102c3d",false);
        Card("gpu",96,"GPU",data.GpuTemp,data.GpuLoad,Purple,"#27213e",true);
        string unit=L("ГИБ","GiB");
        string ramText=$"{Number(data.RamUsed,"0.0")}/{Number(data.RamTotal,"0.0")} {unit}";
        if(ramText.Length>17)ramText=$"{Number(data.RamUsed)}/{Number(data.RamTotal)} {unit}";
        double? ramUsage=data.RamTotal>0?Percent(100*data.RamUsed/data.RamTotal):null;
        var ram=new List<Dictionary<string,object>> {Art.Rect(12,162,216,52,"#17332f",9),
            Art.Text(Text("ramlabel",24,168,2,"RAM",Green),"#17332f"),
            Art.Text(Text("rampercent",166,168,2,Number(ramUsage)+"%",Green),"#17332f"),
            Art.Text(Text("ram",(240-ramText.Length*12)/2,188,2,ramText),"#17332f"),
            Art.Rect(24,207,192,3,"#304a46",1)};
        if(ramUsage is double usage && usage>0)
            ram.Add(Art.Rect(24,207,Math.Max(1,(int)(192*usage/100)),3,Green,1));
        regions.Add(new("ram",0,162,240,52,ram));
        var footer=new List<Dictionary<string,object>> {Art.Circle(16,deviceAddress is null?227:222,3,stale?Gold:Green),
            Art.Text(Text("footer",26,deviceAddress is null?224:219,1,stale?L("ДАТЧИКИ УСТАРЕЛИ","SENSORS STALE"):demo?L("ДЕМО / ПРЕДПРОСМОТР","DEMO DATA / PREVIEW"):L("ДАТЧИКИ / ОНЛАЙН","LOCAL SENSORS / ONLINE"),Muted))};
        if(deviceAddress is not null)footer.Add(Art.Text(Text("address",26,231,1,AddressLabel(deviceAddress),Muted)));
        regions.Add(new("footer",0,217,240,23,footer));
        return new("monitor",lines,regions);
    }
    private static string AddressLabel(string address)
    {
        string text="IP "+address;
        return text[..Math.Min(text.Length,34)];
    }
    public static ScreenFrame Clock(DateTimeOffset instant,TimeZoneInfo zone,Weather? weather,bool demo=false,string? deviceAddress=null,string language="en")
    {
        var time=TimeZoneInfo.ConvertTime(instant,zone);
        var age=weather is null?TimeSpan.MaxValue:instant-weather.CapturedAt;
        bool usable=age>=TimeSpan.Zero && age<TimeSpan.FromHours(6);
        string L(string ru,string en)=>Localization.Normalize(language)=="ru"?ru:en;
        List<ScreenLine> lines=[];
        ScreenLine Text(string id,int x,int y,int size,string value,string color=Ink)
        {var line=new ScreenLine(id,x,y,size,value,color);lines.Add(line);return line;}
        var weatherArt=new List<Dictionary<string,object>> {Art.Rect(12,112,216,100,"#162d49",10),
            Art.Text(Text("temperature",104,122,usable && (Number(weather!.Temperature)+"C").Length>4?2:3,usable?Number(weather!.Temperature)+"C":"N/A",Gold),"#162d49"),
            Art.Text(Text("humidity",104,160,2,usable?L("ВЛ ","RH ")+Number(weather!.Humidity)+"%":L("ВЛ Н/Д","RH N/A"),Cyan),"#162d49"),
            Art.Text(Text("wind",20,188,2,L("В ","W ")+(usable && Wind.Speed(weather!.WindSpeedMps) is double windSpeed?Number(windSpeed,"0.0")+L("м/с","m/s"):"N/A"),Green),"#162d49"),
            Art.Text(Text("windDirection",156,188,2,usable && Wind.Speed(weather!.WindSpeedMps)==0?L("ШТИЛЬ","CALM"):usable?Wind.Compass(weather!.WindDirectionDegrees,language):"N/A",Muted),"#162d49")};
        if(usable && Wind.Speed(weather!.WindSpeedMps)>0 && Wind.Direction(weather.WindDirectionDegrees) is double bearing)
        {
            double angle=bearing*Math.PI/180;
            const int centerX=205,centerY=140;
            weatherArt.Add(Art.Circle(centerX,centerY,17,"#42627c",false));
            weatherArt.Add(Art.Text(Text("compassNorth",202,116,1,L("С","N"),Muted),"#162d49"));
            int tipX=centerX+(int)Math.Round(14*Math.Sin(angle)),tipY=centerY-(int)Math.Round(14*Math.Cos(angle));
            int tailX=centerX-(int)Math.Round(10*Math.Sin(angle)),tailY=centerY+(int)Math.Round(10*Math.Cos(angle));
            weatherArt.Add(Art.Line(tailX,tailY,tipX,tipY,Green));
            weatherArt.Add(Art.Line(tailX+1,tailY,tipX+1,tipY,Green));
            weatherArt.Add(Art.Line(tipX,tipY,tipX-(int)Math.Round(7*Math.Sin(angle+0.6)),tipY+(int)Math.Round(7*Math.Cos(angle+0.6)),Green));
            weatherArt.Add(Art.Line(tipX,tipY,tipX-(int)Math.Round(7*Math.Sin(angle-0.6)),tipY+(int)Math.Round(7*Math.Cos(angle-0.6)),Green));
        }
        DrawWeatherIcon(weatherArt,usable?weather!.Summary:"unavailable");
        var footer=new List<Dictionary<string,object>> {Art.Circle(16,deviceAddress is null?227:222,3,usable?age>=TimeSpan.FromMinutes(30)?Gold:Green:Muted),
            Art.Text(Text("footer",26,deviceAddress is null?224:219,1,usable?age>=TimeSpan.FromMinutes(30)?L("ПОГОДА УСТАРЕЛА","WEATHER STALE"):weather!.Provider.ToUpperInvariant()+(weather.TimeEstimated?L(" / ВРЕМЯ ~"," / EST. TIME"):L(" / ПОГОДА"," / WEATHER")):L("ПОГОДЫ НЕТ","WEATHER N/A"),Muted))};
        if(deviceAddress is not null)footer.Add(Art.Text(Text("address",26,231,1,AddressLabel(deviceAddress),Muted)));
        return new("clock",lines,[
            new("header",0,0,240,30,[Art.Text(Text("title",12,10,2,L("ЧАСЫ","CLOCK"),Cyan)),
                Art.Rect(184,10,44,15,"#263348",5),Art.Text(Text("badge",190,14,1,demo?L("ДЕМО","DEMO"):L("ОНЛ","LIVE"),demo?Gold:Green),"#263348")]),
            new("time",0,34,240,48,[Art.Text(Text("time",14,39,5,time.ToString("HH:mm",CultureInfo.InvariantCulture))),
                Art.Text(Text("seconds",184,55,2,time.ToString("ss",CultureInfo.InvariantCulture),Cyan))]),
            new("date",0,84,240,24,[Art.Text(Text("date",16,88,2,time.ToString("dd.MM.yyyy",CultureInfo.InvariantCulture))),
                Art.Text(Text("weekday",168,88,2,Localization.Normalize(language)=="ru"?new[]{"ВС","ПН","ВТ","СР","ЧТ","ПТ","СБ"}[(int)time.DayOfWeek]:time.ToString("ddd",CultureInfo.InvariantCulture).ToUpperInvariant(),Cyan))]),
            new("weather",0,112,240,100,weatherArt),
            new("footer",0,214,240,26,footer)]);
    }
    private static void DrawWeatherIcon(List<Dictionary<string,object>> art,string summary)
    {
        var icon=new List<Dictionary<string,object>>();
        DrawWeatherShapes(icon,summary);
        // Centre the complete symbol, including rays, cloud and precipitation,
        // above the wind row in the left half of the weather card.
        var bounds=icon.Select(shape=>shape["type"].ToString() switch
        {
            "circle"=>((int)shape["x"]-(int)shape["r"],(int)shape["y"]-(int)shape["r"],(int)shape["x"]+(int)shape["r"],(int)shape["y"]+(int)shape["r"]),
            "line"=>(Math.Min((int)shape["x0"],(int)shape["x1"]),Math.Min((int)shape["y0"],(int)shape["y1"]),Math.Max((int)shape["x0"],(int)shape["x1"]),Math.Max((int)shape["y0"],(int)shape["y1"])),
            _=>((int)shape["x"],(int)shape["y"],(int)shape["x"]+(int)shape["w"]-1,(int)shape["y"]+(int)shape["h"]-1)
        }).ToArray();
        int dx=(int)Math.Round(55-(bounds.Min(b=>b.Item1)+bounds.Max(b=>b.Item3))/2.0);
        int dy=(int)Math.Round(151-(bounds.Min(b=>b.Item2)+bounds.Max(b=>b.Item4))/2.0);
        foreach(var shape in icon)
        {
            foreach(string key in new[]{"x","x0","x1"})if(shape.TryGetValue(key,out var x))shape[key]=(int)x+dx;
            foreach(string key in new[]{"y","y0","y1"})if(shape.TryGetValue(key,out var y))shape[key]=(int)y+dy;
            art.Add(shape);
        }
    }
    private static void DrawWeatherShapes(List<Dictionary<string,object>> art,string summary)
    {
        string value=summary.ToLowerInvariant();
        bool absent=value=="unavailable",rain=value.Contains("rain")||value.Contains("precipitation")||value.Contains("thunder"),snow=value.Contains("snow"),fog=value.Contains("fog");
        if(absent){art.Add(Art.Circle(56,153,19,Muted,false));art.Add(Art.Line(43,166,69,140,Muted));return;}
        if(!rain && !snow && !fog)
        {
            art.Add(Art.Circle(48,144,15,Gold));
            foreach(var (x,y,xx,yy) in new[]{(48,120,48,124),(48,164,48,168),(24,144,28,144),(68,144,72,144),
                (31,127,34,130),(62,158,65,161),(31,161,34,158),(62,130,65,127)})art.Add(Art.Line(x,y,xx,yy,Gold));
        }
        if(value.Contains("clear"))return;
        art.Add(Art.Circle(49,155,12,"#dae9fa"));art.Add(Art.Circle(65,147,16,"#dae9fa"));
        art.Add(Art.Circle(79,158,10,"#dae9fa"));art.Add(Art.Rect(40,155,43,14,"#dae9fa",3));
        if(rain||snow)foreach(int x in new[]{46,61,76})
        {if(snow)art.Add(Art.Circle(x,180,2,Ink));else art.Add(Art.Line(x+3,175,x,183,Cyan));}
        if(fog){art.Add(Art.Rect(34,177,48,2,Muted));art.Add(Art.Rect(42,184,36,2,Muted));}
    }
}

internal static class ColorScreens
{
    public static List<Dictionary<string,object>> Commands(ScreenFrame next,ScreenFrame? previous)
    {
        bool full=previous is null || previous.Kind!=next.Kind || previous.Background!=next.Background;
        var commands=new List<Dictionary<string,object>>();
        if(full)commands.Add(new(){{"type","clear"},{"color",next.Background}});
        foreach(var region in next.Regions!)
        {
            var old=previous?.Regions?.FirstOrDefault(x=>x.Id==region.Id);
            if(!full && old is not null && old.X==region.X && old.Y==region.Y && old.Width==region.Width && old.Height==region.Height &&
                JsonSerializer.Serialize(old.Drawing)==JsonSerializer.Serialize(region.Drawing))continue;
            if(!full)commands.Add(Art.Rect(region.X,region.Y,region.Width,region.Height,next.Background));
            commands.AddRange(region.Drawing);
        }
        return commands;
    }
}
