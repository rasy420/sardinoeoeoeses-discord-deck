using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 public static void RunMobileStopTest(bool fixture){
  Directory.CreateDirectory(Settings.Folder);
  var wave=Path.Combine(Settings.Folder,"mobile-stop.wav");
  using(var w=new WaveFileWriter(wave,WaveFormat.CreateIeeeFloatWaveFormat(48000,1))){var samples=new float[48000*12];for(int i=0;i<samples.Length;i++)samples[i]=.001f*(float)Math.Sin(i*2*Math.PI*440/48000);w.WriteSamples(samples,0,samples.Length);}
  var output=AudioEngine.Devices().First(d=>d.Direction=="Render"&&AudioEngine.IsVirtualOutput(d.Name));
  var cfg=new Settings{TourDone=true,AudioEnabled=false,OutputId=output.Id,MixMicrophone=false,MonitorEnabled=false};
  for(int i=0;i<3;i++)cfg.Slots[i]=new SoundSlot{Name="USB Test "+(i+1),File=wave,TrimEnd=i==2?.65:10};cfg.Save();AutoTour=false;
  using var form=new MainForm();_=form.Handle;form.PerformLayout();form.audio.Start(cfg);
  using var server=new LanServer(form.settings,form.Dispatch,47846,47847);
  if(fixture){File.WriteAllText(Path.Combine(Settings.Folder,"fixture.json"),JsonSerializer.Serialize(new{pin=server.BeginPairing(),id=cfg.ServerId}));var end=DateTime.UtcNow.AddMinutes(15);while(DateTime.UtcNow<end&&!File.Exists(Path.Combine(Settings.Folder,"done"))){Application.DoEvents();Thread.Sleep(5);}return;}
  var checks=new List<string>();void Check(bool condition,string name){if(!condition)throw new Exception(name);checks.Add(name);}
  T Await<T>(Task<T> t){while(!t.IsCompleted){Application.DoEvents();Thread.Sleep(5);}return t.GetAwaiter().GetResult();}
  using var http=new HttpClient{BaseAddress=new Uri("http://127.0.0.1:47846")};
  Check(Await(http.PostAsync("/stop-sound/"+cfg.Slots[0].Id,null)).StatusCode==System.Net.HttpStatusCode.Forbidden,"Unauthenticated targeted stop rejected");
  http.DefaultRequestHeaders.Add("X-DiscordDeck-Key",cfg.MobileToken);
  JsonElement Post(string path)=>JsonSerializer.Deserialize<JsonElement>(Await(Await(http.PostAsync(path,null)).Content.ReadAsStringAsync()));
  var first=form.settings.Slots[0];var second=form.settings.Slots[1];
  Check(Post("/play-sound/"+first.Id).GetProperty("playing").GetBoolean(),"Stable ID starts actual audio");
  Post("/play-sound/"+first.Id);Post("/play-sound/"+second.Id);Check(form.audio.Playing==3,"Parallel and repeated playback supported");
  form.MoveSound(0,10);Check(Post("/stop-sound/"+first.Id).GetProperty("ok").GetBoolean(),"Stable ID survives reorder");
  Check(!form.audio.IsPlaying(first)&&form.audio.IsPlaying(second)&&form.audio.Playing==1,"Targeted stop ends all target instances and leaves another slot sharing the same file playing");
  Check(Post("/stop-sound/"+first.Id).GetProperty("ok").GetBoolean(),"Repeated stop is harmless");
  Post("/stop-sound/"+second.Id);var shortSound=form.settings.Slots.First(s=>s.Name=="USB Test 3");Post("/play-sound/"+shortSound.Id);
  var until=DateTime.UtcNow.AddSeconds(2);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}Check(!form.audio.IsPlaying(shortSound),"Natural completion clears status");
  var status=JsonSerializer.Deserialize<JsonElement>(Await(http.GetStringAsync("/status")));Check(status.GetProperty("perSoundStop").GetBoolean()&&status.GetProperty("slots").EnumerateArray().All(s=>!s.GetProperty("playing").GetBoolean()),"Status exposes confirmed per-sound state");
  Check(!Post("/play-sound/"+Guid.NewGuid()).GetProperty("ok").GetBoolean(),"Deleted ID rejected without playing another sound");
  Check(Await(http.PostAsync("/stop-sound/invalid",null)).StatusCode==System.Net.HttpStatusCode.BadRequest,"Invalid ID rejected");
  Post("/play/1");Check(form.audio.Playing==1,"Older Android playback route retained");Post("/stop");Check(form.audio.Playing==0,"Older Android stop route retained");
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"mobile-stop-test.json"),JsonSerializer.Serialize(new{ok=true,checks},new JsonSerializerOptions{WriteIndented=true}));
 }
}
