namespace GeekMagicMonitor.Core;

public record TemperatureReading(string Name,double? Value);
public static class TemperatureSelection
{
    // Zero is returned by some inaccessible driver paths. Never present it as a
    // measured CPU temperature. Subzero cooling would need an explicit profile.
    public static TemperatureReading? Cpu(IEnumerable<TemperatureReading> readings)
    {
        var valid=readings.Where(x=>x.Value is double v && double.IsFinite(v) && v>0 && v<=125).ToArray();
        foreach(var name in new[]{"CPU Package","Core (Tctl/Tdie)","CPU (Tctl/Tdie)","Core (Tdie)","Core Average","CCD1 (Tdie)"})
        {
            var preferred=valid.FirstOrDefault(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase));
            if(preferred is not null)return preferred;
        }
        return valid.OrderByDescending(x=>x.Value).FirstOrDefault();
    }
}
