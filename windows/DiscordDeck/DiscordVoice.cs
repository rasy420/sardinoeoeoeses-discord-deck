#nullable enable
using System.Text;
using System.Text.Json;
namespace DiscordDeck;
// Liest Discords gespeichertes Eingabegerät aus dessen Local-Storage-LevelDB (nur lesen, Discord darf dabei laufen).
public static class DiscordVoice {
 public sealed record Result(bool Installed,string? InputDeviceId,string? InputDeviceName,string Detail);

 // Live: Discord öffnet das Mikrofon im Sprachkanal und in den Spracheinstellungen – diese aktive Sitzung ist die sicherste Quelle.
 public static (int pid,string deviceId)? LiveInput(){
  try{using var en=new NAudio.CoreAudioApi.MMDeviceEnumerator();
   foreach(var d in en.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture,NAudio.CoreAudioApi.DeviceState.Active)){using(d){var sessions=d.AudioSessionManager.Sessions;
    for(int i=0;i<sessions.Count;i++){var x=sessions[i];if(x.State!=NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateActive)continue;int pid=(int)x.GetProcessID;if(IsDiscord(pid))return (pid,d.ID);}}}
  }catch{}
  return null;
 }
 public static bool IsDiscord(int pid){try{using var p=System.Diagnostics.Process.GetProcessById(pid);return p.Id!=Environment.ProcessId&&p.ProcessName.StartsWith("Discord",StringComparison.OrdinalIgnoreCase)&&!p.ProcessName.StartsWith("DiscordDeck",StringComparison.OrdinalIgnoreCase);}catch{return false;}}
 static readonly byte[] Key=Encoding.ASCII.GetBytes("MediaEngineStore");

 public static IEnumerable<string> Folders(){var roaming=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);foreach(var app in new[]{"discord","discordptb","discordcanary"}){var dir=Path.Combine(roaming,app,"Local Storage","leveldb");if(Directory.Exists(dir))yield return dir;}}

 public static Result Read(){
  var folders=Folders().ToList();if(folders.Count==0)return new(false,null,null,"Discord ist nicht installiert.");
  (ulong seq,byte[] value)? best=null;
  foreach(var dir in folders)foreach(var file in Directory.GetFiles(dir)){
   try{
    var data=ReadShared(file);
    if(file.EndsWith(".ldb",StringComparison.OrdinalIgnoreCase))foreach(var hit in Table(data))Keep(ref best,hit);
    else if(file.EndsWith(".log",StringComparison.OrdinalIgnoreCase))foreach(var hit in Log(data))Keep(ref best,hit);
   }catch{}
  }
  if(best==null)return new(true,null,null,"Discord hat noch keine Spracheinstellungen gespeichert.");
  var json=Decode(best.Value.value);
  try{
   using var doc=JsonDocument.Parse(json);
   foreach(var context in doc.RootElement.EnumerateObject()){
    if(context.Value.ValueKind!=JsonValueKind.Object||!context.Value.TryGetProperty("inputDeviceId",out var id))continue;
    return new(true,id.GetString(),null,"ok");
   }
  }catch(JsonException){}
  return new(true,"default",null,"Kein Eingabegerät gespeichert (Standard).");
 }

 static void Keep(ref (ulong seq,byte[] value)? best,(ulong seq,byte[] value) hit){if(best==null||hit.seq>best.Value.seq)best=hit;}
 static byte[] ReadShared(string path){using var s=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();}
 // Chromium speichert Werte mit Präfix: 0 = UTF-16LE, 1 = Latin-1.
 static string Decode(byte[] v)=>v.Length==0?"":v[0]==0?Encoding.Unicode.GetString(v,1,v.Length-1):Encoding.Latin1.GetString(v,1,v.Length-1);
 static bool EndsWithKey(ReadOnlySpan<byte> userKey)=>userKey.EndsWith(Key);

 static ulong Varint(ReadOnlySpan<byte> d,ref int p){ulong r=0;int shift=0;while(true){byte b=d[p++];r|=(ulong)(b&0x7f)<<shift;if(b<0x80)return r;shift+=7;}}

 // SSTable: Footer -> Index-Block -> Datenblöcke (optional Snappy-komprimiert).
 static IEnumerable<(ulong,byte[])> Table(byte[] f){
  if(f.Length<48)yield break;int p=f.Length-48;Varint(f,ref p);Varint(f,ref p);var idxOff=(int)Varint(f,ref p);var idxSize=(int)Varint(f,ref p);
  var index=Block(f,idxOff,idxSize);
  foreach(var (_,handle) in Entries(index)){int q=0;var off=(int)Varint(handle,ref q);var size=(int)Varint(handle,ref q);
   var block=Block(f,off,size);
   foreach(var (key,value) in Entries(block)){if(key.Length<8)continue;var user=key.AsSpan(0,key.Length-8);if(!EndsWithKey(user))continue;var tag=BitConverter.ToUInt64(key,key.Length-8);if((tag&0xff)!=1)continue;yield return (tag>>8,value);}
  }
 }
 static byte[] Block(byte[] f,int off,int size){var raw=f.AsSpan(off,size);byte type=f[off+size];return type==1?Snappy(raw):raw.ToArray();}
 static IEnumerable<(byte[] key,byte[] value)> Entries(byte[] b){
  if(b.Length<4)yield break;int restarts=BitConverter.ToInt32(b,b.Length-4);int end=b.Length-4-restarts*4;int p=0;byte[] last=Array.Empty<byte>();
  while(p<end){int shared=(int)Varint(b,ref p),own=(int)Varint(b,ref p),vlen=(int)Varint(b,ref p);var key=new byte[shared+own];Array.Copy(last,key,shared);Array.Copy(b,p,key,shared,own);p+=own;var value=b.AsSpan(p,vlen).ToArray();p+=vlen;last=key;yield return (key,value);}
 }
 static byte[] Snappy(ReadOnlySpan<byte> s){int p=0;int len=(int)Varint(s,ref p);var o=new byte[len];int w=0;
  while(p<s.Length){byte tag=s[p++];int kind=tag&3;
   if(kind==0){int l=tag>>2;if(l>=60){int n=l-59;l=0;for(int i=0;i<n;i++)l|=s[p++]<<(8*i);}l++;s.Slice(p,l).CopyTo(o.AsSpan(w));p+=l;w+=l;continue;}
   int length,offset;
   if(kind==1){length=4+((tag>>2)&7);offset=((tag>>5)<<8)|s[p++];}
   else if(kind==2){length=1+(tag>>2);offset=s[p]|(s[p+1]<<8);p+=2;}
   else{length=1+(tag>>2);offset=s[p]|(s[p+1]<<8)|(s[p+2]<<16)|(s[p+3]<<24);p+=4;}
   for(int i=0;i<length;i++,w++)o[w]=o[w-offset];
  }return o;}

 // Log-Datei: 32-KB-Blöcke mit Records (FULL/FIRST/MIDDLE/LAST), die WriteBatches enthalten.
 static IEnumerable<(ulong,byte[])> Log(byte[] f){
  var batch=new List<byte>();int p=0;
  while(p+7<=f.Length){int inBlock=32768-p%32768;if(inBlock<7){p+=inBlock;continue;}
   int len=f[p+4]|(f[p+5]<<8);byte type=f[p+6];if(type==0&&len==0){p+=inBlock;continue;}if(p+7+len>f.Length)break;
   var payload=f.AsSpan(p+7,len);p+=7+len;
   if(type==1){batch.Clear();batch.AddRange(payload.ToArray());}
   else if(type==2){batch.Clear();batch.AddRange(payload.ToArray());continue;}
   else if(type==3){batch.AddRange(payload.ToArray());continue;}
   else if(type==4)batch.AddRange(payload.ToArray());
   else continue;
   foreach(var hit in Batch(batch.ToArray()))yield return hit;
  }
 }
 static IEnumerable<(ulong,byte[])> Batch(byte[] b){
  if(b.Length<12)yield break;ulong seq=BitConverter.ToUInt64(b,0);int count=BitConverter.ToInt32(b,8);int p=12;var hits=new List<(ulong,byte[])>();
  for(int i=0;i<count&&p<b.Length;i++){byte kind=b[p++];int kl=(int)Varint(b,ref p);var key=b.AsSpan(p,kl).ToArray();p+=kl;
   if(kind==1){int vl=(int)Varint(b,ref p);var value=b.AsSpan(p,vl).ToArray();p+=vl;if(EndsWithKey(key))hits.Add((seq+(ulong)i,value));}
  }
  foreach(var h in hits)yield return h;
 }
}
