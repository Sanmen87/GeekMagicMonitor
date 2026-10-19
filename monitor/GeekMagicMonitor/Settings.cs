using GeekMagicMonitor.Core;
using System.Text.Json;

namespace GeekMagicMonitor;

public record Settings
{
    public string Host {get;init;} = "192.168.4.1";
    public int Interval {get;init;} = 2;
    public int ScreenSeconds {get;init;} = 15;
    public string Screen {get;init;} = "Auto";
    public string Language {get;init;} = "ru";
    public string DisplayLanguage {get;init;} = "en";
    public string Gpu {get;init;} = "";
    public bool Demo {get;init;} = true;
    public bool SendToCube {get;init;}
    public bool WeatherEnabled {get;init;}
    public string WeatherProvider {get;init;}="Auto";
    public double Latitude {get;init;} = 43.94508;
    public double Longitude {get;init;} = 42.51652;
    public string Location {get;init;} = "Учкекен, Карачаево-Черкессия, Россия";
    public string TimeZone {get;init;} = "Russian Standard Time";
    public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GeekMagicMonitor");
    public Settings Normalize()=>this with {Screen=ScreenMode.Normalize(Screen),Language=Localization.Normalize(Language),DisplayLanguage=Localization.Normalize(DisplayLanguage)};
    public static Settings Load(string? folder=null)
    {
        try
        {
            var loaded=JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine(folder??Folder,"settings.json")))??new();
            loaded=loaded.Normalize();
            return string.IsNullOrWhiteSpace(loaded.Location)&&loaded.Latitude==0&&loaded.Longitude==0
                ?loaded with{Location="Учкекен, Карачаево-Черкессия, Россия",Latitude=43.94508,Longitude=42.51652}:loaded;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void Save(string? folder=null)
    {
        folder??=Folder;
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder,"settings.json");
        File.WriteAllText(file+".tmp",JsonSerializer.Serialize(this,new JsonSerializerOptions {WriteIndented=true}));
        File.Move(file+".tmp",file,true);
    }
}
