using System.Security.Cryptography;
using System.Text.Json;
namespace DiscordDeck;
public sealed class SoundSlot { public string SourceUrl {get;set;}="";public string Name {get;set;}=""; public string File {get;set;}="";public float Volume {get;set;}=.65f; }
public sealed class Settings {
 public string ServerId {get;set;}=Guid.NewGuid().ToString();
 public string MobileToken {get;set;}=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
 public bool MonitorEnabled {get;set;}=true;
 public string MonitorId {get;set;}="";
 public bool AudioEnabled {get;set;}=false;
 public string OutputId {get;set;}="";public string MicrophoneId {get;set;}="";public bool MixMicrophone {get;set;}=true;
 public float MicrophoneVolume {get;set;}=1;public float MasterVolume {get;set;}=.7f;
 public string Token {get;set;}=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
 public List<SoundSlot> Slots {get;set;}=Enumerable.Range(1,100).Select(i=>new SoundSlot{Name=$"Sound {i}"}).ToList();
 public static string Folder=>Environment.GetEnvironmentVariable("DISCORDDECK_HOME") is {Length:>0} home?home:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DiscordDeck");
 public static string Config=>Path.Combine(Folder,"settings.json");
 public static Settings Load(){Directory.CreateDirectory(Folder);if(!File.Exists(Config))return new();var s=JsonSerializer.Deserialize<Settings>(File.ReadAllText(Config))??new();if(s.Slots.Count>100)throw new InvalidDataException("Maximal 100 Soundplaetze.");while(s.Slots.Count<100)s.Slots.Add(new SoundSlot{Name=$"Sound {s.Slots.Count+1}"});foreach(var slot in s.Slots)if(!string.IsNullOrWhiteSpace(slot.File)&&string.IsNullOrWhiteSpace(slot.SourceUrl))slot.Name=Path.GetFileNameWithoutExtension(slot.File);return s;}
 public void Save(){Directory.CreateDirectory(Folder);var tmp=Config+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(tmp,Config,true);}
}

