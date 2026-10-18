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
 bool updating;
 async void CheckForUpdates(){
  if(updating)return;updating=true;
  try{
   status.Text=T("Suche nach Updates …","Checking for updates …");deck.Invalidate();
   using var client=Updater.Client();var json=await client.GetStringAsync("https://api.github.com/repos/"+Updater.Repository+"/releases/latest");var release=Updater.Parse(json);
   if(release==null||release.Version<=Updater.Current){status.Text=T("Du nutzt die neueste Version: ","You are using the latest version: ")+Updater.Current.ToString(3);return;}
   if(MessageBox.Show(this,T($"Version {release.Version} ist verfügbar. Jetzt herunterladen und installieren? Sounds und Einstellungen bleiben erhalten.",$"Version {release.Version} is available. Download and install now? Your sounds and settings are kept."),T("Update verfügbar","Update available"),MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes){status.Text=T("Update abgebrochen","Update cancelled");return;}
   status.Text=T("Update wird heruntergeladen …","Downloading update …");
   var folder=Path.Combine(Path.GetTempPath(),"SSB-Update",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);var path=Path.Combine(folder,"SSB-Setup.exe");
   using(var response=await client.GetAsync(release.Url,HttpCompletionOption.ResponseHeadersRead)){response.EnsureSuccessStatusCode();await using var file=File.Create(path);await response.Content.CopyToAsync(file);}
   if(!Updater.Matches(path,release.Digest)){File.Delete(path);throw new InvalidDataException(T("Prüfsumme stimmt nicht. Update verworfen.","Checksum mismatch. Update discarded."));}
   Process.Start(new ProcessStartInfo(path){UseShellExecute=true});status.Text=T("Update-Installer geöffnet","Update installer opened");
  }catch(Exception e){status.Text=T("Update fehlgeschlagen: ","Update failed: ")+e.Message;}
  finally{updating=false;if(!IsDisposed)deck.Invalidate();}
 }
}
