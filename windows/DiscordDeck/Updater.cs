using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using static DiscordDeck.Lang;
namespace DiscordDeck;

internal static class Updater {
 internal const string Repository="rasy420/sardinoeoeoeses-soundboard";
 internal static Version Current=>Assembly.GetExecutingAssembly().GetName().Version??new Version(1,0,0);
 internal sealed record Release(Version Version,string Url,string Digest);
 internal static Release? Parse(string json){
  using var doc=JsonDocument.Parse(json);var r=doc.RootElement;
  if(r.GetProperty("draft").GetBoolean()||r.GetProperty("prerelease").GetBoolean())return null;
  if(!Version.TryParse(r.GetProperty("tag_name").GetString()?.TrimStart('v'),out var version))throw new InvalidDataException("Invalid release version");
  foreach(var a in r.GetProperty("assets").EnumerateArray()){
   if(a.GetProperty("name").GetString()!="SSB-Setup.exe")continue;
   var url=a.GetProperty("browser_download_url").GetString()??"";
   if(!Uri.TryCreate(url,UriKind.Absolute,out var u)||u.Scheme!="https"||u.Host!="github.com"||!u.AbsolutePath.StartsWith("/"+Repository+"/releases/download/",StringComparison.Ordinal))throw new InvalidDataException("Invalid update destination");
   var digest=a.TryGetProperty("digest",out var d)?d.GetString()??"":"";
   if(!digest.StartsWith("sha256:")||digest.Length!=71||!digest[7..].All(Uri.IsHexDigit))throw new InvalidDataException(T("Prüfsumme fehlt im GitHub-Release.","GitHub release checksum is missing."));
   return new(version,url,digest[7..]);
  }
  throw new InvalidDataException(T("Kein Windows-Installer im Release gefunden.","No Windows installer found in the release."));
 }
 internal static HttpClient Client(){var c=new HttpClient{Timeout=TimeSpan.FromMinutes(15)};c.DefaultRequestHeaders.UserAgent.ParseAdd("SSB-Updater/1.2");return c;}
 internal static bool Matches(string path,string expected)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).Equals(expected,StringComparison.OrdinalIgnoreCase);
}
public sealed partial class MainForm {
 bool updating,checkingUpdate;Updater.Release? availableUpdate;string updateError="";
 readonly System.Windows.Forms.Timer updateTimer=new(){Interval=600000};
 void InitAutomaticUpdates(){if(Program.TestMode)return;updateTimer.Tick+=async(_,_)=>await RefreshUpdate();updateTimer.Start();Shown+=async(_,_)=>{await Task.Delay(2000);if(!IsDisposed)await RefreshUpdate();};FormClosed+=(_,_)=>updateTimer.Dispose();}
 async Task RefreshUpdate(){
  if(checkingUpdate||updating||IsDisposed)return;checkingUpdate=true;
  try{using var c=Updater.Client();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));var json=await c.GetStringAsync("https://api.github.com/repos/"+Updater.Repository+"/releases/latest",timeout.Token);var r=Updater.Parse(json);if(IsDisposed)return;availableUpdate=r!=null&&r.Version>Updater.Current?r:null;updateError="";}
  catch(Exception e){updateError=e.Message;}
  finally{checkingUpdate=false;if(!IsDisposed)deck.Invalidate();}
 }
 async void CheckForUpdates(){
  if(updating)return;if(availableUpdate==null){await RefreshUpdate();if(availableUpdate==null){status.Text="Update: "+(string.IsNullOrEmpty(updateError)?T("Auf dem neuesten Stand","Up to date"):T("Prüfung fehlgeschlagen: ","Check failed: ")+updateError);return;}}
  var release=availableUpdate;if(release==null)return;updating=true;
  try{
   if(DarkChoice(T("Update installieren?","Install update?"),T($"Version {release.Version} herunterladen und installieren? Sounds und Einstellungen bleiben erhalten.",$"Download and install version {release.Version}? Sounds and settings are kept."),T("Installieren","Install"),null,T("Abbrechen","Cancel"))!=DialogResult.Yes)return;
   status.Text="Update: "+T("Update wird heruntergeladen …","Downloading update …");deck.Invalidate();
   using var client=Updater.Client();var folder=Path.Combine(Path.GetTempPath(),"SSB-Update",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);var path=Path.Combine(folder,"SSB-Setup.exe");
   using(var response=await client.GetAsync(release.Url,HttpCompletionOption.ResponseHeadersRead)){response.EnsureSuccessStatusCode();await using var file=File.Create(path);await response.Content.CopyToAsync(file);}
   if(!Updater.Matches(path,release.Digest)){File.Delete(path);throw new InvalidDataException(T("Prüfsumme stimmt nicht. Update verworfen.","Checksum mismatch. Update discarded."));}
   Process.Start(new ProcessStartInfo(path){UseShellExecute=true});status.Text="Update: "+T("Update-Installer geöffnet","Update installer opened");
  }catch(Exception e){status.Text="Update: "+T("Update fehlgeschlagen: ","Update failed: ")+e.Message;}
  finally{updating=false;if(!IsDisposed)deck.Invalidate();}
 }
 sealed partial class ReferenceDeck {
  void UpdateButton(Graphics g){var r=new RectangleF(19,831,211,42);bool ready=owner.availableUpdate!=null;string label=owner.updating?T("Update wird geladen …","Downloading update …"):owner.checkingUpdate?T("Updates werden geprüft …","Checking updates …"):ready?T("↓   Updates installieren","↓   Install updates"):string.IsNullOrEmpty(owner.updateError)?T("✓   Auf dem neuesten Stand","✓   Up to date"):T("Update-Prüfung wiederholen","Retry update check");
   if(!ready){Box(g,r,C(28,36,42),C(22,29,35),8,C(64,77,87));using var font=new Font("Segoe UI",12,FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(ink);using var fmt=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};g.DrawString(label,font,brush,RectangleF.Inflate(r,-10,-4),fmt);Hit(r,owner.CheckForUpdates,label);return;}
   float pulse=(float)(.5+.5*Math.Sin(Environment.TickCount64/400d));Box(g,r,C(73+(int)(pulse*20),61+(int)(pulse*15),34),C(39,34,24),8,green);TextAt(g,label,r.X+18,r.Y+12,14,green,width:r.Width-28,height:23);Hit(r,owner.CheckForUpdates);
  }
 }
}
