using System.Text.Json;

namespace GeekMagicMonitor.Core;

// The elevated collector publishes measurements only. No commands flow back to it.
public sealed record SensorSnapshot(DateTimeOffset CapturedAt,double? CpuTemperature)
{
    public const int MaximumBytes=4096;
    public static SensorSnapshot? Parse(string json,DateTimeOffset now)
    {
        try
        {
            var value=JsonSerializer.Deserialize<SensorSnapshot>(json);
            if(value is null || now-value.CapturedAt<TimeSpan.FromSeconds(-2) || now-value.CapturedAt>TimeSpan.FromSeconds(10))return null;
            if(value.CpuTemperature is not double temperature || !double.IsFinite(temperature) || temperature<=0 || temperature>125)return null;
            return value;
        }
        catch(JsonException){return null;}
    }
    public static SensorSnapshot? Read(string path,DateTimeOffset now)
    {
        try
        {
            using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
            if(file.Length>MaximumBytes)return null;
            using var reader=new StreamReader(file);
            return Parse(reader.ReadToEnd(),now);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return null;}
    }
    public void Write(string path)
    {
        string temporary=path+".tmp";
        File.WriteAllText(temporary,JsonSerializer.Serialize(this));
        File.Move(temporary,path,true);
    }
}
