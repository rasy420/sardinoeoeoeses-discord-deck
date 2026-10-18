using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Win32;

// Virtuelles Audiokabel: vorhandenes erkennen, sonst das unveränderte Originalpaket von vb-audio.com laden und den
// offiziellen VB-CABLE-Installer starten. Die Lizenz erlaubt kein stilles Einbetten in fremde Installer; der Nutzer
// bestätigt die Installation deshalb selbst im VB-Audio-Fenster.
static class Cable {
 public const string PackUrl="https://download.vb-audio.com/Download_CABLE/VBCABLE_Driver_Pack45.zip",Signer="BUREL VINCENT";
 const string Name="{a45c254e-df1c-4efd-8020-67d146a850e0},2",Interface="{b3f8fa53-0004-438e-9003-51a46e139bfc},6";

 // Liefert den Namen eines aktiven virtuellen Wiedergabegeräts (CABLE oder Steam Streaming Microphone) oder null.
 public static string? Find(){
  if(Environment.GetEnvironmentVariable("SSB_TEST_NO_CABLE")=="1")return null;
  try{
   using var render=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render");
   if(render==null)return null;
   foreach(var id in render.GetSubKeyNames()){
    using var device=render.OpenSubKey(id);if(device?.GetValue("DeviceState") is not int state||state!=1)continue;
    using var props=device.OpenSubKey("Properties");var name=props?.GetValue(Name) as string??"";var iface=props?.GetValue(Interface) as string??"";
    if(iface.Contains("CABLE",StringComparison.OrdinalIgnoreCase)||name.Contains("CABLE",StringComparison.OrdinalIgnoreCase)||iface.Contains("Steam Streaming Microphone",StringComparison.OrdinalIgnoreCase))return iface.Length>0?$"{name} ({iface})":name;
   }
  }catch{}
  return null;
 }

 public static string Download(Action<string> report){
  var folder=Path.Combine(Path.GetTempPath(),"SSB-VBCABLE-"+Guid.NewGuid().ToString("N")[..8]);Directory.CreateDirectory(folder);
  var zip=Path.Combine(folder,"VBCABLE_Driver_Pack45.zip");
  using(var http=new HttpClient{Timeout=TimeSpan.FromSeconds(90)}){
   http.DefaultRequestHeaders.UserAgent.ParseAdd("SSB-Setup/1.1");
   using var response=http.GetAsync(PackUrl,HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();response.EnsureSuccessStatusCode();
   if(response.Content.Headers.ContentType?.MediaType is {} type&&!type.Contains("zip"))throw new IOException(Program.L("vb-audio.com lieferte kein ZIP-Paket.","vb-audio.com did not return a ZIP package."));
   using var file=File.Create(zip);response.Content.CopyToAsync(file).GetAwaiter().GetResult();
  }
  report(Program.L("VB-CABLE wird entpackt und geprüft …","Unpacking and verifying VB-CABLE …"));
  var target=Path.Combine(folder,"pack");ZipFile.ExtractToDirectory(zip,target);
  var setup=Path.Combine(target,Environment.Is64BitOperatingSystem?"VBCABLE_Setup_x64.exe":"VBCABLE_Setup.exe");
  if(!File.Exists(setup))throw new IOException(Program.L("VB-CABLE-Installer fehlt im Paket.","VB-CABLE installer missing from the package."));
  if(!SignedByVbAudio(setup))throw new IOException(Program.L("Die Signatur des VB-CABLE-Installers ist ungültig. Abgebrochen.","The VB-CABLE installer signature is invalid. Aborted."));
  return setup;
 }

 // Authenticode-Prüfung über Windows selbst: gültige Signatur und Herausgeber VB-Audio (Vincent Burel).
 public static bool SignedByVbAudio(string file){
  try{
   var psi=new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -Command \"$s=Get-AuthenticodeSignature -LiteralPath '"+file.Replace("'","''")+"';if($s.Status -eq 'Valid' -and $s.SignerCertificate.Subject -like '*"+Signer+"*'){exit 0}else{exit 1}\""){CreateNoWindow=true,UseShellExecute=false};
   using var p=Process.Start(psi)!;return p.WaitForExit(30000)&&p.ExitCode==0;
  }catch{return false;}
 }

 // Startet den offiziellen Installer mit Administratorrechten und stellt danach die vorherigen Standardgeräte wieder her,
 // weil Windows das neue Kabel sonst gern zum Standard-Lautsprecher macht.
 public static (bool installed,string? device) Install(string setup,Action<string> report){
  var before=Defaults.Read();
  report(Program.L("Bitte im VB-Audio-Fenster auf „Install Driver“ klicken …","Please click “Install Driver” in the VB-Audio window …"));
  using(var p=Process.Start(new ProcessStartInfo(setup){UseShellExecute=true,Verb="runas",WorkingDirectory=Path.GetDirectoryName(setup)!}))p?.WaitForExit((int)TimeSpan.FromMinutes(15).TotalMilliseconds);
  report(Program.L("Warte auf das neue Audiogerät …","Waiting for the new audio device …"));
  string? found=null;for(int i=0;i<40&&(found=Find())==null;i++)Thread.Sleep(500);
  Defaults.Restore(before);
  return (found!=null,found);
 }
}

// Standard-Audiogeräte lesen und setzen (IMMDeviceEnumerator / IPolicyConfig, wie in den Windows-Soundeinstellungen).
static class Defaults {
 public sealed record Snapshot(Dictionary<(int flow,int role),string> Ids);
 public static Snapshot Read(){
  var map=new Dictionary<(int,int),string>();
  try{var e=(IMMDeviceEnumerator)new MMDeviceEnumerator();foreach(var flow in new[]{0,1})foreach(var role in new[]{0,2})if(e.GetDefaultAudioEndpoint(flow,role,out var d)==0&&d.GetId(out var id)==0)map[(flow,role)]=id;}catch{}
  return new(map);
 }
 public static int Restore(Snapshot before){
  int changed=0;var now=Read();
  try{var policy=(IPolicyConfig)new PolicyConfigClient();
   foreach(var (key,id) in before.Ids){if(now.Ids.TryGetValue(key,out var current)&&current==id)continue;
    // Rolle 0 (Konsole) gilt in Windows zusammen mit Multimedia (1).
    foreach(var role in key.role==0?new[]{0,1}:new[]{key.role})policy.SetDefaultEndpoint(id,role);changed++;}
  }catch{}
  return changed;
 }
 public static bool Set(string id,int role){try{var policy=(IPolicyConfig)new PolicyConfigClient();return policy.SetDefaultEndpoint(id,role)==0;}catch{return false;}}

 [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDeviceEnumerator{}
 [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IMMDeviceEnumerator{[PreserveSig]int EnumAudioEndpoints(int flow,int mask,out IntPtr devices);[PreserveSig]int GetDefaultAudioEndpoint(int flow,int role,out IMMDevice device);}
 [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IMMDevice{[PreserveSig]int Activate(ref Guid iid,int ctx,IntPtr p,out IntPtr o);[PreserveSig]int OpenPropertyStore(int access,out IntPtr store);[PreserveSig]int GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);}
 [ComImport,Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")] class PolicyConfigClient{}
 [ComImport,Guid("f8679f50-850a-41cf-9c72-430f290290c8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IPolicyConfig{
  [PreserveSig]int GetMixFormat(IntPtr a,IntPtr b);[PreserveSig]int GetDeviceFormat(IntPtr a,int b,IntPtr c);[PreserveSig]int ResetDeviceFormat(IntPtr a);[PreserveSig]int SetDeviceFormat(IntPtr a,IntPtr b,IntPtr c);
  [PreserveSig]int GetProcessingPeriod(IntPtr a,int b,IntPtr c,IntPtr d);[PreserveSig]int SetProcessingPeriod(IntPtr a,IntPtr b);[PreserveSig]int GetShareMode(IntPtr a,IntPtr b);[PreserveSig]int SetShareMode(IntPtr a,IntPtr b);
  [PreserveSig]int GetPropertyValue(IntPtr a,IntPtr b,IntPtr c);[PreserveSig]int SetPropertyValue(IntPtr a,IntPtr b,IntPtr c);
  [PreserveSig]int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)]string id,int role);[PreserveSig]int SetEndpointVisibility(IntPtr a,int b);
 }
}
