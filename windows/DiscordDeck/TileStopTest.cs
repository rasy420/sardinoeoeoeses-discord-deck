using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 public static void RunTileStopTest(){
  Directory.CreateDirectory(Settings.Folder);var wav=Path.Combine(Settings.Folder,"tile-stop.wav");
  using(var w=new WaveFileWriter(wav,WaveFormat.CreateIeeeFloatWaveFormat(48000,1))){var s=new float[48000*12];for(int i=0;i<s.Length;i++)s[i]=.001f*(float)Math.Sin(i*2*Math.PI*440/48000);w.WriteSamples(s,0,s.Length);}
  var output=AudioEngine.Devices().First(d=>d.Direction=="Render"&&AudioEngine.IsVirtualOutput(d.Name));
  var cfg=new Settings{TourDone=true,AudioEnabled=false,OutputId=output.Id,MixMicrophone=false,MonitorEnabled=false};
  for(int i=0;i<100;i++)cfg.Slots[i]=new SoundSlot{Name=$"Stop sound {i+1}",File=wav,TrimEnd=i==3?.6:10};cfg.Save();AutoTour=false;
  using var f=new MainForm();void Prepare(Control c){_=c.Handle;foreach(Control child in c.Controls)Prepare(child);}Prepare(f);f.PerformLayout();
  var checks=new List<string>();void Check(bool b,string why){if(!b)throw new Exception(why);checks.Add(why);}
  Bitmap Paint(string? name=null){var image=new Bitmap(f.deck.Width,f.deck.Height);f.deck.DrawToBitmap(image,f.deck.ClientRectangle);if(name!=null)image.Save(Path.Combine(AppContext.BaseDirectory,name+".png"));return image;}
  Color At(Bitmap b,float x,float y)=>b.GetPixel((int)(x*f.deck.Width/1536),(int)(y*f.deck.Height/1024));
  void Click(float x,float y){Paint().Dispose();f.deck.GetType().GetMethod("OnMouseDown",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(f.deck,new object[]{new MouseEventArgs(MouseButtons.Left,1,(int)(x*f.deck.Width/1536),(int)(y*f.deck.Height/1024),0)});Paint().Dispose();}
  void Pump(int ms){var end=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(5);}}
  static bool Gold(Color c)=>c.R>200&&c.G>160&&c.B<170&&c.R-c.B>50;
  static bool Red(Color c)=>c.R>200&&c.G<130&&c.B<120;
  static bool Grey(Color c)=>c.R<80&&c.G<80&&c.B<90;
  bool RedOn(Bitmap b,int index){var r=ReferenceDeck.StopBounds(f.deck.TileBounds(index));return Red(At(b,r.X+6,r.Y+r.Height/2))&&Red(At(b,r.Right-6,r.Y+r.Height/2));}
  PointF StopCenter(int index){var r=ReferenceDeck.StopBounds(f.deck.TileBounds(index));return new(r.X+r.Width/2,r.Y+r.Height/2);}
  PointF NameSpot(int index){var r=f.deck.TileBounds(index);return new(r.X+30,r.Y+95);}
  var start=new RectangleF(1302,938,209,48);
  foreach(var size in new[]{new Size(1536,1024),new Size(1080,720)}){
   f.ClientSize=size;f.SetLanguage(size.Width<1200);f.PerformLayout();f.CloseSoundSettings();f.audio.StopSounds();if(f.audio.Running)f.ToggleAudio();
   using(var b=Paint("audio-off-"+size.Width))Check(Gold(At(b,start.X+8,start.Y+8))&&Gold(At(b,start.Right-8,start.Bottom-8)),"Start audio button is gold while audio is off "+size.Width);
   using(var b=Paint())Check(!Enumerable.Range(0,20).Any(i=>RedOn(b,i)),"No stop surface while nothing plays "+size.Width);
   var p0=NameSpot(0);Click(p0.X,p0.Y);var p1=NameSpot(1);Click(p1.X,p1.Y);
   Check(f.audio.Running&&f.audio.IsPlaying(f.settings.Slots[0])&&f.audio.IsPlaying(f.settings.Slots[1])&&f.audio.Playing==2,"Two tiles play in parallel "+size.Width);
   using(var b=Paint("playing-"+size.Width)){Check(Grey(At(b,start.X+8,start.Y+8))&&!Gold(At(b,start.X+start.Width/2,start.Y+8)),"Stop audio button turns grey while audio runs "+size.Width);Check(RedOn(b,0)&&RedOn(b,1),"Both playing tiles show a red stop surface "+size.Width);Check(!RedOn(b,2),"Idle tile shows no stop surface "+size.Width);}
   var a=f.deck.AccessibilityObject;var names=Enumerable.Range(0,a.GetChildCount()).Select(i=>a.GetChild(i)?.Name??"").ToList();Check(names.Contains((Lang.En?"Stop: ":"Stoppen: ")+"Stop sound 1")&&!names.Contains((Lang.En?"Stop: ":"Stoppen: ")+"Stop sound 3"),"Stop surfaces are accessible actions only for playing tiles "+size.Width);
   int selected=f.selectedSound;var c0=StopCenter(0);Click(c0.X,c0.Y);
   Check(!f.audio.IsPlaying(f.settings.Slots[0])&&f.audio.IsPlaying(f.settings.Slots[1])&&f.audio.Playing==1,"Stop surface stops only its own sound "+size.Width);
   Check(!f.soundPanelOpen&&f.selectedSound==selected&&f.audio.Running,"Stop click neither opens settings, changes selection nor stops audio "+size.Width);
   using(var b=Paint("stopped-one-"+size.Width))Check(!RedOn(b,0)&&RedOn(b,1),"Stop surface disappears after stopping, remains on the other tile "+size.Width);
   var c1=StopCenter(1);Click(c1.X,c1.Y);Check(f.audio.Playing==0,"Second stop surface stops the remaining sound "+size.Width);
   var p3=NameSpot(3);Click(p3.X,p3.Y);Check(f.audio.IsPlaying(f.settings.Slots[3]),"Short sound starts "+size.Width);Pump(1500);
   Check(!f.audio.IsPlaying(f.settings.Slots[3]),"Short sound ends naturally "+size.Width);using(var b=Paint())Check(!RedOn(b,3),"Stop surface vanishes after natural end "+size.Width);
   Click(p0.X,p0.Y);f.OpenSoundSettings(5);Check(f.soundPanelOpen,"Settings overlay open while a sound plays "+size.Width);Click(c0.X,c0.Y);
   Check(f.audio.Playing==0&&f.soundPanelOpen&&f.selectedSound==5,"Stop surface works with settings overlay open and leaves it untouched "+size.Width);f.CloseSoundSettings();
   Click(p0.X,p0.Y);f.ToggleSorting();using(var b=Paint())Check(!RedOn(b,0),"Edit mode hides stop surfaces "+size.Width);f.ToggleSorting();using(var b=Paint())Check(RedOn(b,0),"Stop surface returns after edit mode "+size.Width);f.audio.StopSounds();
   f.ToggleAudio();
   using(var b=Paint())Check(!f.audio.Running&&Gold(At(b,start.X+8,start.Y+8)),"Button returns to gold after Stop audio "+size.Width);
  }
  f.SetLanguage(false);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"tile-stop-test.json"),JsonSerializer.Serialize(new{ok=true,checks},new JsonSerializerOptions{WriteIndented=true}));
 }
}
