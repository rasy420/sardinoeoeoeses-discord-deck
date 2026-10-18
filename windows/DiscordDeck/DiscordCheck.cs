using System.Diagnostics;
using NAudio.CoreAudioApi;
using static DiscordDeck.Lang;
namespace DiscordDeck;
// Prüft regelmäßig, ob Discord das virtuelle Mikrofon als Eingabegerät nutzt, und meldet sonst eine Warnung.
public sealed record DiscordWarning(string Title,string Detail,bool OpensDiscord);
public sealed partial class MainForm {
 static (int pid,string deviceId)? lastLive;
 DiscordWarning? discordWarning;bool discordChecking;readonly System.Windows.Forms.Timer discordTimer=new(){Interval=4000};
 void StartDiscordCheck(){discordTimer.Tick+=(_,_)=>CheckDiscord();discordTimer.Start();Activated+=(_,_)=>CheckDiscord();CheckDiscord();}
 async void CheckDiscord(){
  if(discordChecking||!Visible||IsDisposed)return;discordChecking=true;
  try{var outputId=(output.SelectedItem as AudioDevice)?.Id??"";var warning=await Task.Run(()=>Evaluate(outputId));
   if(warning!=discordWarning){discordWarning=warning;deck.Invalidate();}}
  catch{}finally{discordChecking=false;}
 }
 public static DiscordWarning? Evaluate(string outputId){
  if(outputId=="")return new(T("Kein virtuelles Mikrofon gefunden","No virtual microphone found"),T("Installer erneut ausführen – er installiert VB-CABLE","Run the installer again – it installs VB-CABLE"),false);
  var expected=PairedCapture(outputId);if(expected==null)return null;
  // Discord speichert eine Änderung oft erst beim Beenden; solange derselbe Discord-Prozess läuft, gilt das zuletzt live gesehene Gerät.
  string? current=null;var live=DiscordVoice.LiveInput();if(live!=null)lastLive=live;else if(lastLive is {} seen&&DiscordVoice.IsDiscord(seen.pid))current=seen.deviceId;else lastLive=null;
  if(live!=null)current=live.Value.deviceId;
  if(current==null){var discord=DiscordVoice.Read();if(!discord.Installed||discord.InputDeviceId==null)return null;current=discord.InputDeviceId;if(current=="default")current=DefaultCapture();}
  return string.Equals(current,expected.Value.id,StringComparison.OrdinalIgnoreCase)?null:new(T("Discord: Audiogerät umstellen auf:","Discord: switch input device to:"),expected.Value.name,true);
 }
 // Gegenstück zum gewählten virtuellen Ausgang: Aufnahmegerät mit demselben Schnittstellennamen (z. B. „VB-Audio Virtual Cable“).
 public static (string id,string name)? PairedCapture(string renderId){
  using var en=new MMDeviceEnumerator();string iface;try{using var render=en.GetDevice(renderId);iface=render.DeviceFriendlyName;}catch{return null;}
  foreach(var d in en.EnumerateAudioEndPoints(DataFlow.Capture,DeviceState.Active)){using(d){if(string.Equals(d.DeviceFriendlyName,iface,StringComparison.OrdinalIgnoreCase))return (d.ID,d.FriendlyName);}}
  return null;
 }
 static string DefaultCapture(){try{using var en=new MMDeviceEnumerator();using var d=en.GetDefaultAudioEndpoint(DataFlow.Capture,Role.Console);return d.ID;}catch{return "";}}
 // Beim Kaltstart öffnet Discord nur das Hauptfenster; deshalb den Link nach dem Start noch einmal senden.
 void OpenDiscordVoice(){
  static void Link()=>Process.Start(new ProcessStartInfo("discord://-/settings/voice"){UseShellExecute=true});
  try{bool running=Process.GetProcesses().Any(p=>{using(p)return p.ProcessName.StartsWith("Discord",StringComparison.OrdinalIgnoreCase)&&!p.ProcessName.StartsWith("DiscordDeck",StringComparison.OrdinalIgnoreCase);});Link();
   status.Text=running?T("Discord geöffnet · unter Mikrofon das virtuelle Mikrofon wählen","Discord opened · choose the virtual microphone under Input device"):T("Discord startet · Spracheinstellungen öffnen sich gleich","Discord is starting · voice settings will open shortly");
   if(!running)foreach(var delay in new[]{12000,25000}){var t=new System.Windows.Forms.Timer{Interval=delay};t.Tick+=(_,_)=>{t.Dispose();try{Link();}catch{}};t.Start();}
  }catch(Exception e){status.Text=T("Discord konnte nicht geöffnet werden: ","Could not open Discord: ")+e.Message;}
 }
}
