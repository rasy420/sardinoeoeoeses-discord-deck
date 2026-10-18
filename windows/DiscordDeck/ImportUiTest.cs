using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 public static void RunImportUiTest(){
  Directory.CreateDirectory(Settings.Folder);var source=Path.Combine(Settings.Folder,"Import sample.wav");using(var w=new WaveFileWriter(source,WaveFormat.CreateIeeeFloatWaveFormat(44100,1))){var samples=new float[44100*6];for(int i=0;i<samples.Length;i++)samples[i]=.003f*(float)Math.Sin(i*2*Math.PI*440/44100);w.WriteSamples(samples,0,samples.Length);}
  var cfg=new Settings{TourDone=true,MixMicrophone=false,MonitorEnabled=false};cfg.Slots[0].Name="Keep custom name";cfg.Slots[0].CustomName=true;cfg.Save();AutoTour=false;
  var checks=new List<string>();Exception? failure=null;void Check(bool b,string name){if(!b)throw new Exception(name);checks.Add(name);}
  using var host=new MainForm();_=host.Handle;using var driver=new Form();driver.Shown+=async(_,_)=>{driver.Hide();try{
   int changed=0;using var dialog=new AudioImportDialog(host,host.settings,()=>changed++);void Prepare(Control c){_=c.Handle;c.PerformLayout();foreach(Control child in c.Controls)Prepare(child);}Prepare(dialog);dialog.PerformLayout();
   Check(!dialog.Save.Enabled&&!dialog.Preview.Enabled,"Cannot save or preview without loaded audio");await dialog.LoadSource(source,"https://www.youtube.com/watch?v=test");
   Check(dialog.Wave.Peaks?.Length==240&&dialog.Wave.Peaks.Max()>.1,"Real waveform extracted from audio");Check(dialog.Save.Enabled&&dialog.From.Enabled,"Loading enables selection and save");
   void Event(string name,int x)=>typeof(ImportWave).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(dialog.Wave,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,50,0)});
   void Drag(bool start,double target){Event("OnMouseDown",(int)dialog.Wave.X(start?dialog.Wave.From:dialog.Wave.To));Event("OnMouseMove",(int)dialog.Wave.X(target));Event("OnMouseUp",(int)dialog.Wave.X(target));}
   Drag(true,1);Drag(false,3.5);Check(Math.Abs((double)dialog.From.Value-1)<.03&&Math.Abs((double)dialog.To.Value-3.5)<.03,"Mouse dragging changes both trim markers");
   dialog.TogglePreview();Check(dialog.Previewing&&!host.audio.Running,"Preview plays independently of virtual microphone output");await Task.Delay(120);dialog.From.Value=1.2m;Check(!dialog.Previewing,"Changing range stops the previous preview");
   var end=dialog.To.Value;Drag(true,6);Check(dialog.From.Value<dialog.To.Value,"Start marker cannot cross end marker");dialog.From.Value=1m;dialog.To.Value=1.3m;dialog.TogglePreview();await Task.Delay(1000);Check(!dialog.Previewing,"Preview stops and resets automatically at selected end");
   dialog.From.Value=4m;dialog.To.Value=2m;Check(!dialog.Save.Enabled&&!dialog.Preview.Enabled,"Invalid manual range blocks save and preview");dialog.From.Value=1m;dialog.To.Value=3.5m;
   using(var b=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(b,dialog.ClientRectangle);b.Save(Path.Combine(AppContext.BaseDirectory,"import-wave-de.png"));}
   Check(dialog.Save.Height<=80&&dialog.RectangleToClient(dialog.Save.RectangleToScreen(dialog.Save.ClientRectangle)).Bottom<dialog.ClientSize.Height,"Footer buttons have compact height and remain inside the dialog");
   host.SetLanguage(true);using(var english=new AudioImportDialog(host,host.settings,()=>{})){Prepare(english);await english.LoadSource(source);Check(english.Save.Text=="Save clip"&&english.Text.Contains("Import from"),"English import labels localized");using var b=new Bitmap(english.Width,english.Height);english.DrawToBitmap(b,english.ClientRectangle);b.Save(Path.Combine(AppContext.BaseDirectory,"import-wave-en.png"));}
   host.SetLanguage(false);
   await dialog.SaveAudio();var saved=Settings.Load().Slots[0];Check(changed==1&&File.Exists(saved.File),"Save assigns encoded MP3 and notifies the deck");using(var r=new AudioFileReader(saved.File))Check(Math.Abs(r.TotalTime.TotalSeconds-2.5)<.12,"Saved MP3 contains precisely the selected interval");
   Check(saved.Name=="Keep custom name"&&saved.CustomName&&saved.SourceUrl=="https://www.youtube.com/watch?v=test","Custom display name and loaded source URL preserved");Check(saved.TrimStart==0&&saved.TrimEnd==0&&File.Exists(source),"Encoded selection resets old trim and preserves original");
   host.SetLanguage(false);host.settings.Banks[0].Name="A very long custom soundbank name";host.OpenSoundSettings(0);using(var image=new Bitmap(host.deck.Width,host.deck.Height)){host.deck.DrawToBitmap(image,host.deck.ClientRectangle);image.Save(Path.Combine(AppContext.BaseDirectory,"heading-long-bank.png"));}Check(host.deck.HeadingBankBounds.Right+20<=host.deck.HeadingRangeBounds.Left&&host.deck.HeadingRangeBounds.Right<1169,"Long bank name and range stay separated and visible beside the title");
  }catch(Exception e){failure=e;}finally{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"import-ui-test.json"),JsonSerializer.Serialize(new{ok=failure==null,checks,error=failure?.ToString()},new JsonSerializerOptions{WriteIndented=true}));driver.Close();}};Application.Run(driver);if(failure!=null)Environment.ExitCode=1;
 }
}
