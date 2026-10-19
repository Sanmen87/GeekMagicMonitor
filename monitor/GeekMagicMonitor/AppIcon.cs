namespace GeekMagicMonitor;

internal static class AppIcon
{
    public static Icon Value {get;}=Load();
    private static Icon Load()
    {
        using var stream=typeof(AppIcon).Assembly.GetManifestResourceStream("GeekMagicMonitor.AppIcon.ico")
            ??throw new InvalidOperationException("Application icon resource missing");
        using var icon=new Icon(stream);
        return (Icon)icon.Clone();
    }
}
