using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using LibreHardwareMonitor.Hardware;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Color = System.Windows.Media.Color;

namespace XpuStatLine;
public class Settings { public string Color {get;set;}="#FFFFFFFF"; public double Size {get;set;}=18; public double Opacity {get;set;}=1; public double? Left {get;set;} public double? Top {get;set;} public string Hotkey {get;set;}="Ctrl+Alt+H"; public bool AlwaysOnTop {get;set;}=true; }
public partial class MainWindow : Window {
 const int HotkeyId=12341, WM_HOTKEY=0x0312; static readonly string appData=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); readonly string settingsPath=Path.Combine(appData,"XpuStatLine","settings.json");
 // Settings location used before the rename to xPU StatLine; copied over once on first run.
 readonly string legacySettingsPath=Path.Combine(appData,"HardwareOverlay","settings.json"); Settings cfg=new(); Computer? computer; readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(1)}; Forms.NotifyIcon? tray; HwndSource? source; bool registered; bool updating; bool settingsOpen; readonly object sensorLock=new();
 [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint vk);
 [DllImport("user32.dll",SetLastError=true)] static extern bool SetWindowPos(IntPtr h,IntPtr insertAfter,int x,int y,int width,int height,uint flags);
 [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h,int id);
 public MainWindow(){ InitializeComponent(); LoadSettings(); ApplyAppearance(); if(cfg.Left is double x && cfg.Top is double y && x>=SystemParameters.VirtualScreenLeft-100 && y>=SystemParameters.VirtualScreenTop-50 && x<SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth-30 && y<SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight-20){WindowStartupLocation=WindowStartupLocation.Manual;Left=x;Top=y;} else {WindowStartupLocation=WindowStartupLocation.Manual;Left=30;Top=30;}
 var menu=new System.Windows.Controls.ContextMenu(); menu.Items.Add(Item("设置", (_,_)=>OpenSettings())); menu.Items.Add(Item("隐藏", (_,_)=>Hide())); menu.Items.Add(Item("退出", (_,_)=>System.Windows.Application.Current.Shutdown())); ContextMenu=menu;
 Loaded+=OnLoaded; Closing+=(_,_)=>{timer.Stop(); if(source!=null){UnregisterHotKey(source.Handle,HotkeyId);source.RemoveHook(Hook);} tray?.Dispose(); lock(sensorLock){try{computer?.Close();}catch{} computer=null; AsusFan.Close();} Save();}; }
 MenuItem Item(string title,RoutedEventHandler act){var i=new MenuItem{Header=title};i.Click+=act;return i;}
 void OnLoaded(object sender,RoutedEventArgs e){ source=HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);source.AddHook(Hook); if(!SetHotkey(cfg.Hotkey)) System.Windows.MessageBox.Show("快捷键注册失败，请在设置中更换。","xPU StatLine");
 tray=new Forms.NotifyIcon{Text="xPU StatLine",Icon=System.Drawing.SystemIcons.Information,Visible=true};var m=new Forms.ContextMenuStrip();m.Items.Add("显示 / 隐藏",null,(_,_)=>Dispatcher.Invoke(Toggle));m.Items.Add("设置",null,(_,_)=>Dispatcher.Invoke(OpenSettings));m.Items.Add("退出",null,(_,_)=>Dispatcher.Invoke(()=>System.Windows.Application.Current.Shutdown()));tray.ContextMenuStrip=m;tray.DoubleClick+=(_,_)=>Dispatcher.Invoke(Toggle);
 var pc=new Computer{IsCpuEnabled=true,IsGpuEnabled=true};try{pc.Open();computer=pc;}catch(Exception ex){try{pc.Close();}catch{} System.Windows.MessageBox.Show("传感器初始化失败："+ex.Message);} timer.Tick+=async (_,_)=>{ MaintainTopmost(); await RefreshAsync(); };timer.Start(); _=RefreshAsync(); }
 async Task RefreshAsync(){if(updating)return;updating=true; try{var values=await Task.Run(ReadSensors);MetricsText.Text=$"CPU {Fmt(values.cpuLoad,3,"%")} {Fmt(values.cpuTemp,3,"°C")} {Fmt(values.cpuFan,5," RPM")} | GPU {Fmt(values.gpuLoad,3,"%")} {Fmt(values.gpuTemp,3,"°C")} {Fmt(values.gpuFan,5," RPM")}";}catch{MetricsText.Text="CPU / GPU 数据读取失败";}finally{updating=false;}}
 // Fixed-width fields keep the monospace overlay from resizing when digit counts change.
 static string Fmt(float? n,int width,string unit)=>(n.HasValue?n.Value.ToString("0"):"--").PadLeft(width)+unit;
 (float? cpuLoad,float? cpuTemp,float? cpuFan,float? gpuLoad,float? gpuTemp,float? gpuFan) ReadSensors(){lock(sensorLock){float? cl=null,ct=null,gl=null,gt=null; if(computer!=null)foreach(var h in computer.Hardware){if(h.HardwareType!=HardwareType.Cpu && h.HardwareType!=HardwareType.GpuNvidia)continue;h.Update();foreach(var s in h.Sensors){if(h.HardwareType==HardwareType.Cpu){if(s.SensorType==SensorType.Load && s.Name=="CPU Total")cl=s.Value; if(s.SensorType==SensorType.Temperature && s.Name=="CPU Package")ct=s.Value;}else{if(s.SensorType==SensorType.Load && s.Name=="GPU Core")gl=s.Value;if(s.SensorType==SensorType.Temperature && s.Name=="GPU Core")gt=s.Value;}}}return(cl,ct ?? AsusFan.Temperature(0x00120094),AsusFan.Read(0x00110013),gl,gt ?? AsusFan.Temperature(0x00120097),AsusFan.Read(0x00110014));}}
 void DragWindow(object sender,MouseButtonEventArgs e){if(e.ChangedButton==MouseButton.Left){DragMove();cfg.Left=Left;cfg.Top=Top;Save();}}
 void MaintainTopmost(){if(cfg.AlwaysOnTop && IsVisible && !settingsOpen && source!=null && ContextMenu?.IsOpen!=true && tray?.ContextMenuStrip?.Visible!=true) SetWindowPos(source.Handle,new IntPtr(-1),0,0,0,0,0x0013);}
 void Toggle(){if(IsVisible)Hide();else{Show();Activate();}}
 IntPtr Hook(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==WM_HOTKEY && w.ToInt32()==HotkeyId){Toggle();handled=true;}return IntPtr.Zero;}
 static bool TryParseKey(string p,out Key key){key=Key.None;if(p.Length==1&&p[0]>='0'&&p[0]<='9'){key=Key.D0+(p[0]-'0');return true;}if(p.All(char.IsDigit))return false;return Enum.TryParse(p,true,out key)&&Enum.IsDefined(key);}
 bool SetHotkey(string? text){if(source==null||string.IsNullOrWhiteSpace(text))return false;var parts=text.Split('+',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);uint mod=0;Key key=Key.None;foreach(var p in parts){if(p.Equals("Ctrl",StringComparison.OrdinalIgnoreCase)||p.Equals("Control",StringComparison.OrdinalIgnoreCase))mod|=2;else if(p.Equals("Alt",StringComparison.OrdinalIgnoreCase))mod|=1;else if(p.Equals("Shift",StringComparison.OrdinalIgnoreCase))mod|=4;else if(p.Equals("Win",StringComparison.OrdinalIgnoreCase))mod|=8;else if(!TryParseKey(p,out key))return false;}if(mod==0||key==Key.None||KeyInterop.VirtualKeyFromKey(key)==0)return false;string old=cfg.Hotkey;if(registered)UnregisterHotKey(source.Handle,HotkeyId);if(RegisterHotKey(source.Handle,HotkeyId,mod|0x4000,(uint)KeyInterop.VirtualKeyFromKey(key))){registered=true;return true;}registered=false;if(old!=text)SetHotkey(old);return false;}
 void ApplyAppearance(){try{var brush=(SolidColorBrush)new BrushConverter().ConvertFromString(cfg.Color)!;MetricsText.Foreground=brush;}catch{} MetricsText.FontSize=Math.Clamp(cfg.Size,10,48);Opacity=Math.Clamp(cfg.Opacity,.2,1);Topmost=cfg.AlwaysOnTop;}
 void LoadSettings(){try{if(!File.Exists(settingsPath)&&File.Exists(legacySettingsPath)){Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);File.Copy(legacySettingsPath,settingsPath);}}catch{} try{if(File.Exists(settingsPath))cfg=JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath))??new();}catch{cfg=new();} var defaults=new Settings(); if(string.IsNullOrWhiteSpace(cfg.Hotkey))cfg.Hotkey=defaults.Hotkey; if(!IsValidColor(cfg.Color))cfg.Color=defaults.Color; if(!double.IsFinite(cfg.Size))cfg.Size=defaults.Size; if(!double.IsFinite(cfg.Opacity))cfg.Opacity=defaults.Opacity;}
 static bool IsValidColor(string? s){if(string.IsNullOrWhiteSpace(s))return false;try{return System.Windows.Media.ColorConverter.ConvertFromString(s) is Color;}catch{return false;}}
 void Save(){try{Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);File.WriteAllText(settingsPath,JsonSerializer.Serialize(cfg,new JsonSerializerOptions{WriteIndented=true}));}catch{}}
 void OpenSettings(){var w=new Window{Title="xPU StatLine 设置",Width=360,Height=340,WindowStartupLocation=WindowStartupLocation.CenterScreen,ResizeMode=ResizeMode.NoResize,Topmost=true};var p=new StackPanel{Margin=new Thickness(18)};w.Content=p; p.Children.Add(new TextBlock{Text="文字颜色"});var color=new System.Windows.Controls.Button{Content="选择颜色",Margin=new Thickness(0,3,0,8)};p.Children.Add(color);string selected=cfg.Color;color.Click+=(_,_)=>{using var dlg=new Forms.ColorDialog();if(IsValidColor(selected)){var c=(Color)System.Windows.Media.ColorConverter.ConvertFromString(selected);dlg.Color=System.Drawing.Color.FromArgb(c.R,c.G,c.B);}if(dlg.ShowDialog()==Forms.DialogResult.OK)selected=$"#FF{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}";};p.Children.Add(new TextBlock{Text="文字大小（10–48）"});var size=new System.Windows.Controls.TextBox{Text=cfg.Size.ToString(),Margin=new Thickness(0,3,0,8)};p.Children.Add(size);p.Children.Add(new TextBlock{Text="窗口透明度（20–100%）"});var opacity=new System.Windows.Controls.TextBox{Text=(cfg.Opacity*100).ToString("0"),Margin=new Thickness(0,3,0,8)};p.Children.Add(opacity);p.Children.Add(new TextBlock{Text="显示 / 隐藏快捷键，如 Ctrl+Alt+H"});var hotkey=new System.Windows.Controls.TextBox{Text=cfg.Hotkey,Margin=new Thickness(0,3,0,10)};p.Children.Add(hotkey);var always=new System.Windows.Controls.CheckBox{Content="始终置顶（适用于窗口化和无边框游戏）",IsChecked=cfg.AlwaysOnTop,Margin=new Thickness(0,0,0,10)};p.Children.Add(always);var save=new System.Windows.Controls.Button{Content="保存",Height=30};p.Children.Add(save);save.Click+=(_,_)=>{if(!double.TryParse(size.Text,out var sz)||sz<10||sz>48||!double.TryParse(opacity.Text,out var op)||op<20||op>100){System.Windows.MessageBox.Show(w,"请输入范围内的大小和透明度。");return;}string hk=hotkey.Text.Trim();if(!SetHotkey(hk)){System.Windows.MessageBox.Show(w,"快捷键无效或已被占用，请更换。");return;}cfg.Color=selected;cfg.Size=sz;cfg.Opacity=op/100;cfg.Hotkey=hk;cfg.AlwaysOnTop=always.IsChecked==true;ApplyAppearance();Save();w.Close();};settingsOpen=true;try{w.ShowDialog();}finally{settingsOpen=false;MaintainTopmost();}}
}
static class AsusFan {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafeFileHandle CreateFile(string name,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool DeviceIoControl(SafeFileHandle handle,uint code,byte[] input,uint inputSize,byte[] output,uint outputSize,out uint returned,IntPtr overlapped);
 static readonly object gate=new(); static SafeFileHandle? handle;
 // 0 means the sensor is absent or unsupported, so show "--" instead of 0°C.
 public static float? Temperature(uint id) { var v=Read(id); return v is float t && t>0 ? t/100 : null; }
 public static float? Read(uint id){lock(gate){if(handle==null||handle.IsInvalid||handle.IsClosed){handle?.Dispose();handle=CreateFile(@"\\.\ATKACPI",0xC0000000,3,IntPtr.Zero,3,0x80,IntPtr.Zero);if(handle.IsInvalid){handle.Dispose();handle=null;return null;}}byte[] input=new byte[16],output=new byte[16];BitConverter.GetBytes((uint)0x53545344).CopyTo(input,0);BitConverter.GetBytes((uint)8).CopyTo(input,4);BitConverter.GetBytes(id).CopyTo(input,8);if(!DeviceIoControl(handle,0x0022240C,input,16,output,16,out uint n,IntPtr.Zero)||n<4){handle.Dispose();handle=null;return null;}int v=BitConverter.ToInt32(output,0)-65536;return v>=0&&(v&65535)<=120?(v&65535)*100:null;}}
 public static void Close(){lock(gate){handle?.Dispose();handle=null;}}
}





