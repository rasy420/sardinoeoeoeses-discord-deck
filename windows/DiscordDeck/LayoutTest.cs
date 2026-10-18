using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 public static void RunLayoutTest(){
  var checks=new List<string>();var errors=new List<string>();MainForm? f=null;
  void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
  try{
   Directory.CreateDirectory(Settings.Folder);var wav=Path.Combine(Settings.Folder,"layout.wav");using(var w=new WaveFileWriter(wav,WaveFormat.CreateIeeeFloatWaveFormat(44100,1)))w.WriteSamples(new float[44100*4],0,44100*4);
   var settings=new Settings{TourDone=true,MixMicrophone=false,MonitorEnabled=false};settings.Slots[0].File=wav;settings.Save();AutoTour=false;f=new MainForm();void Prepare(Control c){_=c.Handle;foreach(Control child in c.Controls)Prepare(child);}Prepare(f);f.PerformLayout();var form=f;
   Bitmap Paint(string? name=null){var b=new Bitmap(form.deck.Width,form.deck.Height);form.deck.DrawToBitmap(b,form.deck.ClientRectangle);if(name!=null)b.Save(Path.Combine(AppContext.BaseDirectory,name+".png"));return b;}
   int WavePixels(Bitmap b,RectangleF r){int count=0;float sx=b.Width/1536f,sy=b.Height/1024f;for(int x=(int)((r.X+61)*sx);x<(r.X+177)*sx;x++)for(int y=(int)((r.Y+16)*sy);y<(r.Y+62)*sy;y++){var c=b.GetPixel(x,y);if(c.R>45&&c.B-c.R>25&&c.G-c.R>10)count++;}return count;}
   using(var b=Paint("layout-closed")){Check(WavePixels(b,f.deck.TileBounds(0))>50,"Assigned tile retains its waveform");Check(WavePixels(b,f.deck.TileBounds(1))==0,"Empty tile has no invented waveform");}
   for(int bank=1;bank<5;bank++){f.bank=bank;using var b=Paint();for(int j=0;j<20;j++)Check(WavePixels(b,f.deck.TileBounds(bank*20+j))==0,$"Empty slot {bank*20+j+1} has no waveform");}
   f.bank=0;f.OpenSoundSettings(1);using(var b=Paint("layout-empty-panel")){int colored=0;for(int x=1195;x<1510;x++)for(int y=187;y<243;y++){var c=b.GetPixel(x,y);if(c.R>45&&c.B-c.R>25&&c.G-c.R>10)colored++;}Check(colored==0,"Empty settings panel has no invented waveform");}
   f.CloseSoundSettings();using(var b=Paint()){}var actions=f.deck.AccessibilityObject;int Find(string text)=>Enumerable.Range(0,actions.GetChildCount()).First(i=>actions.GetChild(i)?.Name?.Contains(text)==true);
   var reorder=actions.GetChild(Find("Umsortieren"))!;var local=f.deck.RectangleToClient(reorder.Bounds);Check(local.Top>=120&&local.Top<170&&local.Left>680&&local.Right<form.deck.GridRight,"Reorder pencil sits beside the bank numbers above the grid");reorder.DoDefaultAction();Check(f.sorting,"Moved reorder button activates mode");f.ToggleSorting();f.Play(0);using(var b=Paint()){}actions=f.deck.AccessibilityObject;var stop=actions.GetChild(Find("Alle Sounds stoppen"))!;local=f.deck.RectangleToClient(stop.Bounds);Check(local.Top>426&&local.Right<247,"Stop sits below the five banks");stop.DoDefaultAction();Check(f.audio.Playing==0,"Moved stop button stops playback");
   f.OpenSoundSettings(42);f.panelBeforeTour=false;f.bankBeforeTour=0;f.scrollBeforeTour=0;
   for(int step=0;step<TourSteps().Length;step++){f.tourStep=step;f.PrepareTourStep();foreach(var r in f.deck.TourSpots(step))Check(r.Left>=0&&r.Top>=62&&r.Right<=1536&&r.Bottom<=1024||r.Top<62&&r.Bottom<=62,$"Tour step {step+1} highlight is within its UI area");using var b=Paint("layout-tour-"+(step+1));if(step==2)Check(f.deck.TourSpots(step)[0].Contains(f.deck.TileBounds(42)),"Tour follows the edited tile in its actual bank");if(step==5)Check(f.deck.TourSpots(step)[0].Contains(new RectangleF(f.deck.GridRight-211,870,205,31)),"Audio tutorial follows the actual settings button");}
   f.EndTour();Check(!f.soundPanelOpen&&f.bank==0,"Tour restores prior panel and bank");f.ClientSize=new(1080,720);f.SetLanguage(true);using(var b=Paint("layout-small-en")){}Check(f.deck.AccessibilityObject.GetChildCount()>20,"Small English window keeps controls accessible");f.SetLanguage(false);
  }catch(Exception e){errors.Add(e.ToString());}
  finally{if(f!=null){f.exitRequested=true;f.Close();f.Dispose();}File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"layout-test.json"),JsonSerializer.Serialize(new{ok=errors.Count==0,checks,errors},new JsonSerializerOptions{WriteIndented=true}));if(errors.Count>0)Environment.ExitCode=1;}
 }
}
