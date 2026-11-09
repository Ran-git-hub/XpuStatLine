using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace XpuStatLine;

// All readings come from user-mode APIs (no kernel driver, no admin rights):
//  - CPU load: GetSystemTimes deltas
//  - GPU load/temperature: NVIDIA NVML (nvml.dll ships with the NVIDIA driver)
//  - CPU/GPU temperature fallback and fan speeds: ASUS ATKACPI interface
static class Sensors {
 const uint CpuTempId=0x00120094, GpuTempId=0x00120097, CpuFanId=0x00110013, GpuFanId=0x00110014;

 public static (float? cpuLoad,float? cpuTemp,float? cpuFan,float? gpuLoad,float? gpuTemp,float? gpuFan) Read(){
  var (gl,gt)=Nvml.Read();
  return (CpuLoad.Read(),AsusFan.Temperature(CpuTempId),AsusFan.Read(CpuFanId),gl,gt ?? AsusFan.Temperature(GpuTempId),AsusFan.Read(GpuFanId));
 }

 public static IEnumerable<string> Diagnose(){
  Read(); Thread.Sleep(1000); var v=Read();
  yield return $"CPU load: {v.cpuLoad}";
  yield return $"CPU temperature (ASUS ACPI): {v.cpuTemp}";
  yield return $"CPU fan (ASUS ACPI): {v.cpuFan}";
  yield return $"NVML: {Nvml.Status}";
  yield return $"GPU load (NVML): {v.gpuLoad}";
  yield return $"GPU temperature (NVML, else ASUS ACPI): {v.gpuTemp}";
  yield return $"GPU fan (ASUS ACPI): {v.gpuFan}";
 }

 public static void Close(){Nvml.Close();AsusFan.Close();}
}

static class CpuLoad {
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetSystemTimes(out long idle,out long kernel,out long user);
 static long lastIdle,lastTotal; static bool primed;
 // Kernel time includes idle time, so busy = total - idle. The first call only primes the counters.
 public static float? Read(){
  if(!GetSystemTimes(out long idle,out long kernel,out long user))return null;
  long total=kernel+user; long dIdle=idle-lastIdle, dTotal=total-lastTotal; bool had=primed;
  lastIdle=idle; lastTotal=total; primed=true;
  if(!had||dTotal<=0)return null;
  return Math.Clamp(100f*(dTotal-dIdle)/dTotal,0,100);
 }
}

static class Nvml {
 [StructLayout(LayoutKind.Sequential)] struct Utilization { public uint Gpu, Memory; }
 [DllImport("nvml.dll",EntryPoint="nvmlInit_v2")] static extern int Init();
 [DllImport("nvml.dll",EntryPoint="nvmlShutdown")] static extern int Shutdown();
 [DllImport("nvml.dll",EntryPoint="nvmlDeviceGetHandleByIndex_v2")] static extern int GetHandle(uint index,out IntPtr device);
 [DllImport("nvml.dll",EntryPoint="nvmlDeviceGetName")] static extern int GetName(IntPtr device,byte[] name,uint length);
 [DllImport("nvml.dll",EntryPoint="nvmlDeviceGetUtilizationRates")] static extern int GetUtilization(IntPtr device,out Utilization u);
 [DllImport("nvml.dll",EntryPoint="nvmlDeviceGetTemperature")] static extern int GetTemperature(IntPtr device,int sensor,out uint temp);

 static IntPtr device; static bool ready; static DateTime nextAttempt;
 public static string Status {get;private set;}="not initialized";

 // Retries initialization at most every 10 s, e.g. after a driver update or if no NVIDIA GPU is present.
 static bool Ensure(){
  if(ready)return true; if(DateTime.UtcNow<nextAttempt)return false; nextAttempt=DateTime.UtcNow.AddSeconds(10);
  try{
   int r=Init(); if(r!=0){Status=$"nvmlInit failed ({r})";return false;}
   r=GetHandle(0,out device); if(r!=0){Shutdown();Status=$"no NVIDIA GPU ({r})";return false;}
   var name=new byte[96]; Status=GetName(device,name,(uint)name.Length)==0?Encoding.ASCII.GetString(name).TrimEnd('\0'):"NVIDIA GPU";
   ready=true; return true;
  }catch(Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException){Status="nvml.dll not found";return false;}
 }

 public static (float? load,float? temp) Read(){
  if(!Ensure())return(null,null);
  float? load=GetUtilization(device,out var u)==0?u.Gpu:null;
  float? temp=GetTemperature(device,0,out uint t)==0&&t>0?t:null;
  return(load,temp);
 }

 public static void Close(){if(ready){try{Shutdown();}catch{} ready=false;}}
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
