namespace GeekMagicMonitor;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if(args.Length==2 && args[0]=="--wait-parent" && int.TryParse(args[1],out int parentId))
        {
            try {using var parent=System.Diagnostics.Process.GetProcessById(parentId);parent.WaitForExit(20000);}
            catch(ArgumentException) { }
        }
        using var mutex=new Mutex(true,"Local\\GeekMagicMonitor",out bool first);
        bool utility=args.Length==2 && args[0] is "--diagnose-sensors" or "--diagnose-weather" or "--export-preview" or "--smoke-test" or "--smoke-test-en" or "--export-preview-ru" or "--test-cube";
        if(!first && !utility) { MessageBox.Show(GeekMagicMonitor.Core.Localization.Text("GeekMagicMonitor уже запущен. Откройте значок в трее.",Settings.Load().Language)); return; }
        ApplicationConfiguration.Initialize();
        if(args.Length==2 && args[0]=="--test-cube")
        {
            try {CubeCheck.RunAsync(args[1]).GetAwaiter().GetResult();}
            catch(Exception ex)
            {
                File.WriteAllText(args[1]+".error.txt",ex.ToString());Environment.ExitCode=1;
            }
            return;
        }
        if(args.Length==2 && args[0]=="--diagnose-weather")
        {
            try
            {
                using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
                var settings=Settings.Load();
                var value=GeekMagicMonitor.Core.WeatherClient.FetchAsync(client,settings.Latitude,settings.Longitude,settings.WeatherProvider,default).GetAwaiter().GetResult();
                File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(value,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
            }
            catch(Exception ex){File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{Error=ex.Message}));Environment.ExitCode=1;}
            return;
        }
        if(args.Length==2 && args[0]=="--diagnose-sensors")
        {
            using var sensors=new Sensors();sensors.Read("");Thread.Sleep(1200);var values=sensors.Read("");
            File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(sensors.Diagnostic(values),new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
            return;
        }
        if(args.Length==2 && args[0] is "--export-preview" or "--export-preview-ru")
        {
            Directory.CreateDirectory(args[1]);
            using var preview=new Preview {Size=new(480,480)};
            var now=DateTimeOffset.Now;
            string language=args[0]=="--export-preview-ru"?"ru":"en";
            var screens=new[] {GeekMagicMonitor.Core.Screens.Clock(now,TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"),new(18,65,"Cloudy",now,WindSpeedMps:3.2,WindDirectionDegrees:45),true,"192.168.88.129",language),GeekMagicMonitor.Core.Screens.Monitor(new(54,28,63,76,12.4,32),true,deviceAddress:"192.168.88.129",language:language)};
            foreach(var screen in screens)
            {
                preview.Frame=screen;
                using var bitmap=new Bitmap(480,480);
                preview.DrawToBitmap(bitmap,new Rectangle(0,0,480,480));
                bitmap.Save(Path.Combine(args[1],screen.Kind+".png"),System.Drawing.Imaging.ImageFormat.Png);
                preview.Size=new(240,240);
                using(var actualSize=new Bitmap(240,240))
                {
                    preview.DrawToBitmap(actualSize,new Rectangle(0,0,240,240));
                    actualSize.Save(Path.Combine(args[1],screen.Kind+"-240.png"),System.Drawing.Imaging.ImageFormat.Png);
                }
                preview.Size=new(480,480);
                preview.MirrorHorizontal=true;
                preview.DrawToBitmap(bitmap,new Rectangle(0,0,480,480));
                bitmap.Save(Path.Combine(args[1],screen.Kind+"-lcd-mirror.png"),System.Drawing.Imaging.ImageFormat.Png);
                preview.MirrorHorizontal=false;
            }
            foreach(string condition in new[]{"Clear","Rain","Snow","Fog"})
            {
                preview.Frame=GeekMagicMonitor.Core.Screens.Clock(now,TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"),new(22,35,condition,now,WindSpeedMps:1.9,WindDirectionDegrees:76),deviceAddress:"192.168.88.129",language:language);
                using var bitmap=new Bitmap(480,480);
                preview.DrawToBitmap(bitmap,new Rectangle(0,0,480,480));
                bitmap.Save(Path.Combine(args[1],"clock-"+condition.ToLowerInvariant()+".png"),System.Drawing.Imaging.ImageFormat.Png);
            }
            return;
        }
        Application.Run(new MainForm(args.Contains("--tray"),args.Length==2 && args[0] is "--smoke-test" or "--smoke-test-en" ? args[1] : null,args.Contains("--smoke-test-en")?"en":"ru"));
    }
}
