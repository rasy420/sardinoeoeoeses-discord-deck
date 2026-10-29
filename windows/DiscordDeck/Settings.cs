using System.Security.Cryptography;
using System.Text.Json;
namespace DiscordDeck;
public sealed class SoundSlot { public string Id {get;set;}=Guid.NewGuid().ToString(); public string SourceUrl {get;set;}="";public string Name {get;set;}=""; public bool CustomName {get;set;} public string File {get;set;}="";public float Volume {get;set;}=.65f;
 // Abspielbereich in Sekunden; TrimEnd 0 = bis zum Ende. Die Datei selbst bleibt unverändert.
 public double TrimStart {get;set;} public double TrimEnd {get;set;}
 // Neu importierte Sounds in freien Plätzen starten mit 150 %; belegte Plätze und eigene Pegel bleiben unverändert.
 public const float DefaultVolume=.65f,ImportVolume=1.5f;
 public static void ApplyImportVolume(SoundSlot slot){if(string.IsNullOrWhiteSpace(slot.File)&&Math.Abs(slot.Volume-DefaultVolume)<1e-4f)slot.Volume=ImportVolume;} }
public sealed class SoundBank { public string Id {get;set;}=Guid.NewGuid().ToString();public string Name {get;set;}=""; }
public sealed class Settings {
 public List<SoundBank> Banks {get;set;}=Enumerable.Range(0,5).Select(_=>new SoundBank()).ToList();
 public string ServerId {get;set;}=Guid.NewGuid().ToString();
 public string MobileToken {get;set;}=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
 public bool MonitorEnabled {get;set;}=true;
 public string MonitorId {get;set;}="";
 public bool AudioEnabled {get;set;}=false;
 public string Language {get;set;}="de"; public bool TourDone {get;set;} public bool PhoneConnected {get;set;}
 public string OutputId {get;set;}="";public string MicrophoneId {get;set;}="";public bool MixMicrophone {get;set;}=true;
 public float MicrophoneVolume {get;set;}=1;public float MasterVolume {get;set;}=.7f;
 public string Token {get;set;}=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
 public List<SoundSlot> Slots {get;set;}=Enumerable.Range(1,100).Select(i=>new SoundSlot{Name=$"Sound {i}"}).ToList();
 public static string Folder=>Environment.GetEnvironmentVariable("DISCORDDECK_HOME") is {Length:>0} home?home:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DiscordDeck");
 public static string Config=>Path.Combine(Folder,"settings.json");
 public static Settings Load(){Directory.CreateDirectory(Folder);if(!File.Exists(Config))return new();var s=JsonSerializer.Deserialize<Settings>(File.ReadAllText(Config))??new();s.MicrophoneVolume=float.IsFinite(s.MicrophoneVolume)?Math.Clamp(s.MicrophoneVolume,0,AudioEngine.MaxMicrophone):1;if(s.Slots.Count>100)throw new InvalidDataException("Maximal 100 Soundplaetze.");while(s.Slots.Count<100)s.Slots.Add(new SoundSlot{Name=$"Sound {s.Slots.Count+1}"});if(s.Banks==null)s.Banks=new();while(s.Banks.Count<5)s.Banks.Add(new());if(s.Banks.Count>5)s.Banks=s.Banks.Take(5).ToList();var bankIds=new HashSet<string>();foreach(var b in s.Banks)if(string.IsNullOrWhiteSpace(b.Id)||!bankIds.Add(b.Id)){b.Id=Guid.NewGuid().ToString();bankIds.Add(b.Id);}
var ids=new HashSet<string>();foreach(var slot in s.Slots){if(string.IsNullOrWhiteSpace(slot.Id)||!ids.Add(slot.Id)){slot.Id=Guid.NewGuid().ToString();ids.Add(slot.Id);}if(string.IsNullOrWhiteSpace(slot.Name))slot.Name=string.IsNullOrEmpty(slot.File)?$"Sound {s.Slots.IndexOf(slot)+1}":Path.GetFileNameWithoutExtension(slot.File);}return s;}
 public void Save(){Directory.CreateDirectory(Folder);var tmp=Config+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(tmp,Config,true);}
}

