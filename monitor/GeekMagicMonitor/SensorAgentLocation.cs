using System.Security.Principal;

namespace GeekMagicMonitor;

internal static class SensorAgentLocation
{
    public static string SnapshotName => "snapshot-"+WindowsIdentity.GetCurrent().User!.Value+".json";
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"GeekMagicMonitor","SensorAgent");
    public static string SnapshotPath => Path.Combine(DirectoryPath,SnapshotName);
}
