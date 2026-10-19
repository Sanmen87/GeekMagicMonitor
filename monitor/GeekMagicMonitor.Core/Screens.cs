namespace GeekMagicMonitor.Core;

public record Metrics(double? CpuTemp = null, double? CpuLoad = null, double? GpuTemp = null,
    double? GpuLoad = null, double? RamUsed = null, double? RamTotal = null);
public record Weather(double Temperature,double Humidity,string Summary,DateTimeOffset CapturedAt,string Provider="Open-Meteo",bool TimeEstimated=false,
    double? WindSpeedMps=null,double? WindDirectionDegrees=null);
public record ScreenLine(string Id, int X, int Y, int Size, string Text, string Color = "#ffffff");
public record ScreenRegion(string Id,int X,int Y,int Width,int Height,IReadOnlyList<Dictionary<string,object>> Drawing);
public record ScreenFrame(string Kind,IReadOnlyList<ScreenLine> Lines,IReadOnlyList<ScreenRegion> Regions,string Background="#091321");

public static class Frames
{
    public static List<Dictionary<string,object>> Commands(ScreenFrame next,ScreenFrame? previous) => ColorScreens.Commands(next,previous);
}
