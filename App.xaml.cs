using System.IO;
using System.Windows;
using LibreHardwareMonitor.Hardware;
namespace XpuStatLine;
public partial class App : System.Windows.Application {
 protected override void OnStartup(StartupEventArgs e){base.OnStartup(e); if(e.Args.Length==2 && e.Args[0]=="--diagnose") { try { var c=new Computer{IsCpuEnabled=true,IsGpuEnabled=true};c.Open(); var lines=new List<string>();foreach(var h in c.Hardware){h.Update();foreach(var s in h.Sensors)if((h.HardwareType==HardwareType.Cpu&&(s.Name=="CPU Total"||s.Name=="CPU Package"))||(h.HardwareType==HardwareType.GpuNvidia&&s.Name=="GPU Core"))lines.Add($"{h.HardwareType} {s.SensorType} {s.Name}: {s.Value}");}lines.Add($"CPU temperature fallback: {AsusFan.Temperature(0x00120094)}");lines.Add($"CPU fan: {AsusFan.Read(0x00110013)}");lines.Add($"GPU fan: {AsusFan.Read(0x00110014)}");c.Close();File.WriteAllLines(e.Args[1],lines);}catch(Exception ex){File.WriteAllText(e.Args[1],ex.ToString());}Shutdown();return;}
 instanceMutex=new Mutex(true,@"Local\XpuStatLine.SingleInstance",out bool isFirst); if(!isFirst){Shutdown();return;}
 new MainWindow().Show(); }
 static Mutex? instanceMutex;
}


