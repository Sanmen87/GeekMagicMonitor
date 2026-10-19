namespace GeekMagicMonitor.Core;

public static class Wind
{
    public static double? Speed(double? value)=>value is double v && double.IsFinite(v) && v>=0 && v<=150?v:null;
    public static double? Direction(double? value)=>value is double v && double.IsFinite(v) && v>=0 && v<=360?v%360:null;
    // Meteorological direction: where the wind comes FROM, clockwise from north.
    public static string Compass(double? degrees,string language="en")
    {
        if(Direction(degrees) is not double angle)return "N/A";
        string[] names=Localization.Normalize(language)=="ru"
            ?["С","ССВ","СВ","ВСВ","В","ВЮВ","ЮВ","ЮЮВ","Ю","ЮЮЗ","ЮЗ","ЗЮЗ","З","ЗСЗ","СЗ","ССЗ"]
            :["N","NNE","NE","ENE","E","ESE","SE","SSE","S","SSW","SW","WSW","W","WNW","NW","NNW"];
        return names[(int)Math.Floor((angle+11.25)/22.5)%16];
    }
}
