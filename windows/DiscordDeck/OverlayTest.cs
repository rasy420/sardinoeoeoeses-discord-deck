using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 public static void RunOverlayTest(){
  Directory.CreateDirectory(Settings.Folder);var wav=Path.Combine(Settings.Folder,"overlay.wav");using(var w=new WaveFileWriter(wav,WaveFormat.CreateIeeeFloatWaveFormat(44100,1)))w.WriteSamples(new float[44100*3],0,44100*3);
  var cfg=new Settings{TourDone=true,MixMicrophone=false,MonitorEnabled=false};for(int i=0;i<100;i++)cfg.Slots[i]=new SoundSlot{Name=$"Overlay sound {i+1}",File=wav};cfg.Save();AutoTour=false;
  using var f=new MainForm();void Prepare(Control c){_=c.Handle;foreach(Control child in c.Controls)Prepare(child);}Prepare(f);f.PerformLayout();
  var checks=new List<string>();void Check(bool b,string why){if(!b)throw new Exception(why);checks.Add(why);}
  void Paint(string? name=null){using var image=new Bitmap(f.deck.Width,f.deck.Height);f.deck.DrawToBitmap(image,f.deck.ClientRectangle);if(name!=null)image.Save(Path.Combine(AppContext.BaseDirectory,name+".png"));}
  void Click(float x,float y){Paint();f.deck.GetType().GetMethod("OnMouseDown",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(f.deck,new object[]{new MouseEventArgs(MouseButtons.Left,1,(int)(x*f.deck.Width/1536),(int)(y*f.deck.Height/1024),0)});Paint();}
  foreach(var size in new[]{new Size(1536,1024),new Size(1080,720)}){f.ClientSize=size;f.SetLanguage(size.Width<1200);f.PerformLayout();f.CloseSoundSettings();var before=Enumerable.Range(0,20).Select(f.deck.TileBounds).ToArray();Paint();f.OpenSoundSettings(0);Paint("overlay-"+size.Width);
   Check(Enumerable.Range(0,20).All(i=>f.deck.TileBounds(i)==before[i]),$"All 20 tile positions and sizes stay fixed at {size.Width}");
   Check(f.deck.TileAt(new PointF(1300,620))==-1,"Covered tiles cannot receive drag input "+size.Width);
   var a=f.deck.AccessibilityObject;Check(!Enumerable.Range(0,a.GetChildCount()).Any(i=>a.GetChild(i)?.Name=="Overlay sound 4"),"Covered tile removed from accessible actions "+size.Width);
   Click(1300,620);Check(f.soundPanelOpen&&f.audio.Playing==0&&f.selectedSound==0,"Blank overlay click cannot play or select underlying sound "+size.Width);
   Click(500,140);Check(!f.soundPanelOpen&&f.audio.Playing==0,"Title click dismisses clean overlay without playback "+size.Width);
   f.OpenSoundSettings(0);Click(580,325);Check(!f.soundPanelOpen,"Gap between tiles dismisses clean overlay "+size.Width);
   f.OpenSoundSettings(0);f.SetGain(f.draftGain*100+5);Click(500,140);Check(f.soundPanelOpen&&f.TrimDirty&&f.audio.Playing==0,"Dirty title dismissal blocked until Save "+size.Width);
   f.SaveTrim();Click(500,140);Check(!f.soundPanelOpen,"Title dismissal succeeds after explicit Save "+size.Width);
   f.OpenSoundSettings(0);Click(600,250);Check(!f.soundPanelOpen&&f.audio.Playing==1,"Visible tile still closes clean overlay and plays once "+size.Width);f.audio.StopSounds();
  }
  f.SetLanguage(false);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"overlay-test.json"),JsonSerializer.Serialize(new{ok=true,checks},new JsonSerializerOptions{WriteIndented=true}));
 }
}
