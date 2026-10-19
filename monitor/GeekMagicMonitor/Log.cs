namespace GeekMagicMonitor;

internal static class Log
{
    public static void Error(string area,Exception error)
    {
        try
        {
            Directory.CreateDirectory(Settings.Folder);
            string file=Path.Combine(Settings.Folder,"monitor.log");
            if(File.Exists(file) && new FileInfo(file).Length>512000)File.Move(file,file+".previous",true);
            // Exception messages may contain paths or server replies: log only the type.
            File.AppendAllText(file,$"{DateTimeOffset.Now:o} {area}: {error.GetType().Name}{Environment.NewLine}");
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){ }
    }
}
