using GeekMagicMonitor.Core;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.PawnIo;
using System.Security.Principal;

namespace GeekMagicMonitor;

public sealed class Sensors(bool useAgent=true,bool cpuOnly=false) : IDisposable
{
    public string Language {get;set;}="ru";
    private string T(string text)=>Localization.Text(text,Language);
    private Computer? computer;
    public string Detail {get;private set;} = "Датчики ещё не опрошены";
    public string[] Gpus {get;private set;} = [];
    public TemperatureReading[] CpuTemperatures {get;private set;}=[];
    public bool AgentUsed {get;private set;}
    public bool PawnInstalled => PawnIo.IsInstalled;
    public static bool IsElevated
    {
        get {using var identity=WindowsIdentity.GetCurrent();return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);}
    }
    public object Diagnostic(Metrics selected) => new {CapturedAt=DateTimeOffset.Now,Elevated=IsElevated,PawnInstalled,
        PawnVersion=PawnIo.Version?.ToString(),CpuTemperatures,AgentUsed,AgentSnapshotPath=SensorAgentLocation.SnapshotPath,Selected=selected,Detail};
    public Metrics Read(string gpuName)
    {
        try
        {
            if (computer is null)
            {
                computer = new Computer {IsCpuEnabled=true,IsGpuEnabled=!cpuOnly,IsMemoryEnabled=!cpuOnly};
                computer.Open();
            }
            var all = new List<IHardware>();
            void Update(IHardware item) { item.Update(); all.Add(item); foreach(var child in item.SubHardware) Update(child); }
            foreach(var hardware in computer.Hardware) Update(hardware);
            var cpu = all.FirstOrDefault(x=>x.HardwareType==HardwareType.Cpu);
            var gpus = all.Where(x=>x.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel).ToArray();
            Gpus = gpus.Select(x=>x.Name).ToArray();
            var gpu = gpus.FirstOrDefault(x=>x.Name==gpuName) ?? gpus.FirstOrDefault();
            var memory = all.FirstOrDefault(x=>x.HardwareType==HardwareType.Memory);
            static double? Pick(IHardware? h, SensorType kind, params string[] names)
            {
                var values = h?.Sensors.Where(x=>x.SensorType==kind && x.Value.HasValue && float.IsFinite(x.Value.Value)).ToArray() ?? [];
                foreach(var name in names)
                {
                    var sensor=values.FirstOrDefault(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase));
                    if(sensor is not null) return sensor.Value;
                }
                return null;
            }
            CpuTemperatures=cpu?.Sensors.Where(x=>x.SensorType==SensorType.Temperature).Select(x=>new TemperatureReading(x.Name,x.Value)).ToArray()??[];
            var selectedTemperature=TemperatureSelection.Cpu(CpuTemperatures);
            double? cpuTemp=selectedTemperature?.Value;
            var snapshot=useAgent && cpuTemp is null?SensorSnapshot.Read(SensorAgentLocation.SnapshotPath,DateTimeOffset.UtcNow):null;
            AgentUsed=snapshot is not null;
            if(AgentUsed)cpuTemp=snapshot!.CpuTemperature;
            var used=Pick(memory,SensorType.Data,"Memory Used");
            var available=Pick(memory,SensorType.Data,"Memory Available");
            string temperatureStatus=AgentUsed?T("температура: фоновый сборщик"):selectedTemperature is not null?T("температура: ")+selectedTemperature.Name:
                !PawnInstalled?T("температура недоступна: PawnIO не установлен"):
                !IsElevated?T("температура недоступна: попробуйте запуск от администратора"):T("температура недоступна: проверьте драйвер PawnIO");
            Detail=$"CPU: {cpu?.Name ?? T("не найден")} ({temperatureStatus}); GPU: {gpu?.Name ?? T("не найден")}.";
            return new(cpuTemp,Pick(cpu,SensorType.Load,"CPU Total"),Pick(gpu,SensorType.Temperature,"GPU Core"),
                Pick(gpu,SensorType.Load,"GPU Core","D3D 3D"),used,used+available);
        }
        catch(Exception ex) { Detail=T("Ошибка датчиков: ")+ex.GetType().Name+T(". Попробуйте внешний LibreHardwareMonitor; повышение прав автоматически не выполняется."); return new(); }
    }
    public void Dispose() { computer?.Close(); computer=null; }
}
