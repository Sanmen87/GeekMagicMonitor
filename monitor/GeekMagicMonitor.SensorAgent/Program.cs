using GeekMagicMonitor;
using GeekMagicMonitor.Core;
using System.Security.Principal;

// No network, UI, IPC requests or configuration input. Only a temperature snapshot.
bool probe=args.Length==2 && args[0]=="--probe";
if(args.Length!=0 && !probe){Environment.ExitCode=2;return;}
if(!probe && !Sensors.IsElevated){Environment.ExitCode=5;return;}
string sid=WindowsIdentity.GetCurrent().User!.Value;
using var mutex=new Mutex(true,"Local\\GeekMagicMonitor.SensorAgent."+sid,out bool first);
if(!first){Environment.ExitCode=3;return;}
string output=probe?Path.GetFullPath(args[1]):Path.Combine(AppContext.BaseDirectory,SensorAgentLocation.SnapshotName);
using var sensors=new Sensors(useAgent:false,cpuOnly:true);
try
{
    sensors.Read("");
    if(probe)
    {
        Thread.Sleep(1200);
        var reading=sensors.Read("");
        new SensorSnapshot(DateTimeOffset.UtcNow,reading.CpuTemp).Write(output);
        return;
    }
    bool writeFailed=false;
    while(true)
    {
        var reading=sensors.Read("");
        try
        {
            new SensorSnapshot(DateTimeOffset.UtcNow,reading.CpuTemp).Write(output);
            writeFailed=false;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
        {
            // Antivirus or a reader without delete sharing may briefly hold a file.
            // Keep polling and retry; the UI rejects the old snapshot after 10 s.
            if(!writeFailed)
            {
                try {File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"last-write-error.log"),DateTimeOffset.UtcNow+Environment.NewLine+ex);}
                catch(Exception logError) when(logError is IOException or UnauthorizedAccessException) { }
            }
            writeFailed=true;
        }
        Thread.Sleep(2000);
    }
}
catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
{
    // One-shot diagnostic failure.
    Environment.ExitCode=1;
}
