using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NAudio.Wave;
namespace DiscordDeck;
public static class MyInstants {
 public record Imported(string Name,string File,string Source);
 static readonly HttpClient http=new(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(30)};
 public static Uri Validate(string url){if(!Uri.TryCreate(url,UriKind.Absolute,out var u)||u.Scheme!="https"||!u.IsDefaultPort||u.UserInfo!=""||(u.Host!="www.myinstants.com"&&u.Host!="myinstants.com"))throw new InvalidDataException("Bitte einen HTTPS-Link von myinstants.com eingeben.");return u;}
 static async Task<byte[]> Get(Uri uri,int max){for(int redirect=0;redirect<4;redirect++){using var response=await http.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead);if((int)response.StatusCode is >=300 and <400){uri=Validate(new Uri(uri,response.Headers.Location??throw new IOException("Weiterleitung fehlt")).AbsoluteUri);continue;}response.EnsureSuccessStatusCode();if(response.Content.Headers.ContentLength>max)throw new IOException("Sound/Seite ist zu gross.");using var input=await response.Content.ReadAsStreamAsync();using var bytes=new MemoryStream();var buffer=new byte[8192];int n;while((n=await input.ReadAsync(buffer))>0){if(bytes.Length+n>max)throw new IOException("Sound/Seite ist zu gross.");bytes.Write(buffer,0,n);}return bytes.ToArray();}throw new IOException("Zu viele Weiterleitungen.");}
 public static (Uri Audio,string Name) Parse(Uri page,string html){
  var audio=Regex.Match(html,"<meta\\s+property=[\"']og:audio[\"']\\s+content=[\"']([^\"']+)[\"']",RegexOptions.IgnoreCase);
  if(!audio.Success)throw new IOException("Kein Sound gefunden. Bitte den Link eines einzelnen MyInstants-Sounds verwenden.");
  var uri=Validate(new Uri(page,WebUtility.HtmlDecode(audio.Groups[1].Value)).AbsoluteUri);
  if(!uri.AbsolutePath.StartsWith("/media/sounds/",StringComparison.Ordinal))throw new IOException("Unerwarteter Audio-Link.");
  var heading=Regex.Match(html,"<h1[^>]*>(.*?)</h1>",RegexOptions.IgnoreCase|RegexOptions.Singleline);
  string name=WebUtility.HtmlDecode(Regex.Replace(heading.Groups[1].Value,"<[^>]*>","")).Trim();if(name.Length==0)name=Path.GetFileNameWithoutExtension(uri.AbsolutePath);if(name.Length>100)name=name[..100];
  return(uri,name);
 }
 public static async Task<Imported> Import(string link){var page=Validate(link);var html=Encoding.UTF8.GetString(await Get(page,2_000_000));var parsed=Parse(page,html);var data=await Get(parsed.Audio,20_000_000);var folder=Path.Combine(Settings.Folder,"Sounds");Directory.CreateDirectory(folder);var path=Path.Combine(folder,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(parsed.Audio.AbsoluteUri)))+".mp3");var temp=path+"."+Guid.NewGuid()+".mp3";try{await System.IO.File.WriteAllBytesAsync(temp,data);using(var probe=new AudioFileReader(temp)){if(probe.TotalTime<=TimeSpan.Zero)throw new IOException("Audiodatei ist leer.");}System.IO.File.Move(temp,path,true);return new(parsed.Name,path,page.AbsoluteUri);}finally{if(System.IO.File.Exists(temp))System.IO.File.Delete(temp);}}
}
