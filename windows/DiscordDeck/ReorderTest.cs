using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 public static void RunReorderTest(){
  var checks=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
  Directory.CreateDirectory(Settings.Folder);var wave=Path.Combine(Settings.Folder,"Reorder.wav");
  using(var writer=new WaveFileWriter(wave,WaveFormat.CreateIeeeFloatWaveFormat(44100,1))){var samples=new float[44100];for(int i=0;i<samples.Length;i++)samples[i]=.02f*(float)Math.Sin(i*2*Math.PI*440/44100);writer.WriteSamples(samples,0,samples.Length);}
  var initial=new Settings{TourDone=true,AudioEnabled=false,MixMicrophone=false,MonitorEnabled=false};
  for(int i=0;i<100;i++)initial.Slots[i]=new SoundSlot{Name=$"Sample {i+1}",File=wave,SourceUrl=$"https://example.test/{i+1}",Volume=(i+1)/100f,TrimStart=.1,TrimEnd=.7};initial.Save();
  AutoTour=false;using var form=new MainForm();
  void Prepare(Control c){_=c.Handle;foreach(Control child in c.Controls)Prepare(child);}Prepare(form);form.PerformLayout();
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  void Paint(string? file=null){using var b=new Bitmap(form.deck.Width,form.deck.Height);form.deck.DrawToBitmap(b,form.deck.ClientRectangle);if(file!=null)b.Save(Path.Combine(AppContext.BaseDirectory,file));}
  void Event(string method,int x,int y,MouseButtons button=MouseButtons.Left)=>form.deck.GetType().GetMethod(method,flags)!.Invoke(form.deck,new object[]{new MouseEventArgs(button,1,x,y,0)});
  void Drag(int x,int y,int tx,int ty){Event("OnMouseDown",x,y);Event("OnMouseMove",tx,ty);Paint();Event("OnMouseUp",tx,ty);Paint();}
  var original=form.settings.Slots.ToArray();Paint();Event("OnMouseDown",1000,90);Event("OnMouseUp",1000,90);Check(form.sorting,"Top reorder button enters mode");Paint("reorder-mode.png");
  Drag(400,275,700,410);Check(ReferenceEquals(form.settings.Slots[4],original[0]),"Mouse drag moves slot 1 to slot 5");Check(ReferenceEquals(form.settings.Slots[0],original[1]),"Other sounds shift without overwriting");Check(form.selectedSound==4,"Selection follows the moved sound");Check(form.settings.Slots[4].TrimStart==.1&&form.settings.Slots[4].Volume==.01f&&form.settings.Slots[4].SourceUrl.EndsWith("/1"),"File, gain, source and trim move together");
  Check(form.MoveSound(4,0)&&form.settings.Slots.SequenceEqual(original),"Moving backwards restores original order");
  Check(!form.MoveSound(-1,0)&&!form.MoveSound(0,100)&&!form.MoveSound(0,0),"Invalid and same-position moves ignored");
  var persisted=File.ReadAllBytes(Settings.Config);Directory.CreateDirectory(Settings.Config+".tmp");bool rejected=false;try{form.MoveSound(0,4);}catch(IOException){rejected=true;}catch(UnauthorizedAccessException){rejected=true;}finally{Directory.Delete(Settings.Config+".tmp");}Check(rejected&&form.settings.Slots.SequenceEqual(original)&&File.ReadAllBytes(Settings.Config).SequenceEqual(persisted),"Failed save restores order and keeps the existing config");
  Drag(400,275,1200,100);Check(form.settings.Slots.SequenceEqual(original),"Dropping outside grid cancels without data loss");
  Event("OnMouseDown",400,275);Event("OnMouseMove",100,270);form.deck.ReorderTick(DateTime.UtcNow.AddSeconds(1));Check(form.bank==2,"Holding over a bank switches bank during drag");Event("OnMouseMove",400,275);Event("OnMouseUp",400,275);Check(ReferenceEquals(form.settings.Slots[40],original[0]),"Drag across banks preserves destination position");
  form.bank=4;form.scrollRow=0;Paint();Event("OnMouseDown",400,275);Event("OnMouseMove",700,870);form.deck.ReorderTick(DateTime.UtcNow.AddSeconds(1));Check(form.scrollRow>0,"Drag auto-scroll reaches hidden rows");form.deck.CancelReorder();
  Check(form.MoveSound(40,99)&&ReferenceEquals(form.settings.Slots[99],original[0]),"Last slot 100 reachable across all banks");
  var loaded=Settings.Load();Check(loaded.Slots[99].Name==original[0].Name&&loaded.Slots.Count==100,"New order survives settings reload");Check(loaded.Slots.Select(s=>s.SourceUrl).Distinct().Count()==100,"All 100 sounds retained exactly once");
  var payload=JsonSerializer.SerializeToElement(form.Dispatch("status",0));Check(payload.GetProperty("slots")[99].GetProperty("name").GetString()==original[0].Name,"Existing mobile status protocol sends the new order");form.Dispatch("gain",99*401+237);Check(original[0].Volume==2.37f,"Existing phone gain command targets the new position");
  using(var server=new LanServer(form.settings,form.Dispatch,47846,47847)){
   using var http=new HttpClient{BaseAddress=new Uri("http://127.0.0.1:47846")};http.DefaultRequestHeaders.Add("X-DiscordDeck-Key",form.settings.MobileToken);
   T Await<T>(Task<T> task){while(!task.IsCompleted){Application.DoEvents();Thread.Sleep(5);}return task.GetAwaiter().GetResult();}
   var old=JsonDocument.Parse(Await(http.GetStringAsync("/status")));Check(old.RootElement.GetProperty("slots")[99].GetProperty("name").GetString()==original[0].Name,"Real LAN status exposes moved sound at slot 100");
   form.MoveSound(99,98);var fresh=JsonDocument.Parse(Await(http.GetStringAsync("/status")));Check(fresh.RootElement.GetProperty("slots")[98].GetProperty("name").GetString()==original[0].Name,"Next existing Android poll receives new order without reconnecting");
   var gain=Await(http.PostAsync("/gain/99/237",null));Check(gain.IsSuccessStatusCode&&original[0].Volume==2.37f,"Existing Android HTTP gain route targets reordered sound");form.MoveSound(98,99);
  }
  form.Dispatch("play",99);Check(form.audio.Running&&form.audio.Playing>0,"Existing play command plays the sound at its new position");form.audio.Stop();
  form.bank=0;form.scrollRow=0;form.SelectSound(0);Paint();Event("OnMouseDown",400,275);Event("OnMouseMove",700,410);form.deck.GetType().GetMethod("OnKeyDown",flags)!.Invoke(form.deck,new object[]{new KeyEventArgs(Keys.Escape)});var before=form.settings.Slots.ToArray();Event("OnMouseUp",700,410);Check(form.settings.Slots.SequenceEqual(before)&&form.sorting,"Escape cancels an active drag");
  form.deck.GetType().GetMethod("OnKeyDown",flags)!.Invoke(form.deck,new object[]{new KeyEventArgs(Keys.Escape)});Check(!form.sorting,"Escape exits reorder mode when idle");
  form.ToggleSorting();Paint();Event("OnMouseDown",1000,90);Event("OnMouseUp",1000,90);Check(!form.sorting,"Done button exits mode");
  form.StartTour();Check(form.tourStep==0,"Tour still works");form.EndTour();
  form.ClientSize=new Size(1080,720);form.sorting=true;form.PerformLayout();Paint("reorder-small.png");
  Event("OnMouseDown",(int)(400*1080f/1536),(int)(275*720f/1024));Event("OnMouseMove",(int)(700*1080f/1536),(int)(410*720f/1024));Event("OnMouseUp",(int)(700*1080f/1536),(int)(410*720f/1024));Check(form.selectedSound==4,"Drag coordinates scale in the small window");
  var preserved=form.settings.Slots.Select(s=>s.SourceUrl).ToArray();form.Dispose();using var reopened=new MainForm();Check(reopened.settings.Slots.Select(s=>s.SourceUrl).SequenceEqual(preserved),"New MainForm instance restores the same order");
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"reorder-test.json"),JsonSerializer.Serialize(new{ok=true,checks},new JsonSerializerOptions{WriteIndented=true}));
 }
}
