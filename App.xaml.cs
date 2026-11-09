using System.IO;
using System.Windows;
namespace XpuStatLine;
public partial class App : System.Windows.Application {
 protected override void OnStartup(StartupEventArgs e){base.OnStartup(e); if(e.Args.Length==2 && e.Args[0]=="--diagnose") { try { File.WriteAllLines(e.Args[1],Sensors.Diagnose().ToList()); Sensors.Close();}catch(Exception ex){File.WriteAllText(e.Args[1],ex.ToString());}Shutdown();return;}
 instanceMutex=new Mutex(true,@"Local\XpuStatLine.SingleInstance",out bool isFirst); if(!isFirst){Shutdown();return;}
 new MainWindow().Show(); }
 static Mutex? instanceMutex;
}


