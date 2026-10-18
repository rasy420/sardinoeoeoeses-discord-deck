using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 public static void RunImportTimelineTest(){
  Directory.CreateDirectory(Settings.Folder);var source=Path.Combine(Settings.Folder,"Langes Video.wav");using(var w=new WaveFileWriter(source,WaveFormat.CreateIeeeFloatWaveFormat(22050,1))){var samples=new float[22050*90];for(int i=0;i<samples.Length;i++)samples[i]=.003f*(float)Math.Sin(i*2*Math.PI*330/22050);w.WriteSamples(samples,0,samples.Length);}
  var cfg=new Settings{TourDone=true,MixMicrophone=false,MonitorEnabled=false};cfg.Slots[2].Name="Eigener Name";cfg.Slots[2].CustomName=true;cfg.Save();AutoTour=false;
  var checks=new List<string>();Exception? failure=null;void Check(bool b,string name){if(!b)throw new Exception(name);checks.Add(name);}
  using var host=new MainForm();_=host.Handle;using var driver=new Form();driver.Shown+=async(_,_)=>{driver.Hide();try{
   foreach(var en in new[]{false,true}){host.SetLanguage(en);
    Check(AudioImportDialog.Clock(61,0)=="1:01"&&AudioImportDialog.Clock(45.3,2)==(en?"45.30":"45,30")&&AudioImportDialog.Clock(125.5,3)==(en?"2:05.500":"2:05,500")&&AudioImportDialog.Clock(59.999,2)==(en?"1:00.00":"1:00,00")&&AudioImportDialog.Clock(3725,0)=="62:05","Times from 60 s show as minutes, below as seconds "+(en?"en":"de"));}
   host.SetLanguage(false);
   decimal v;Check(AudioImportDialog.TryParseClock("1:01,5",out v)&&v==61.5m&&AudioImportDialog.TryParseClock("61,5",out v)&&v==61.5m&&AudioImportDialog.TryParseClock("1:01.5",out v)&&v==61.5m&&AudioImportDialog.TryParseClock("2:05,500 min",out v)&&v==125.5m,"Typed times accepted as m:ss or seconds");
   Check(!AudioImportDialog.TryParseClock("abc",out _)&&!AudioImportDialog.TryParseClock("1,5:00",out _)&&!AudioImportDialog.TryParseClock("",out _),"Invalid typed times rejected");
   int changed=0;using var dialog=new AudioImportDialog(host,host.settings,()=>changed++);void Prepare(Control c){_=c.Handle;c.PerformLayout();foreach(Control child in c.Controls)Prepare(child);}Prepare(dialog);dialog.PerformLayout();
   Check(dialog.SoundName.Text==host.settings.Slots[0].Name,"Name field shows the slot name before loading");
   await dialog.LoadSource(source,"https://www.youtube.com/watch?v=long");
   Check(dialog.To.Text=="1:30,000"&&dialog.From.Text=="0,000","Start/end fields show 1:30 instead of 90 seconds");
   Check(dialog.Controls.OfType<Control>().SelectMany(Flatten).OfType<Label>().Any(l=>l.Text=="Dauer: 1:30,00 min"),"Length label shows 1:30,00 min");
   dialog.From.Text="1:05,25";Check(dialog.From.Value==65.25m&&dialog.From.Text=="1:05,250","Typing 1:05,25 sets start to 65.25 s");dialog.From.Text="20";Check(dialog.From.Value==20m&&dialog.From.Text=="20,000","Typing plain seconds still works");
   dialog.From.Text="quatsch";Check(dialog.From.Value==20m&&dialog.From.Text=="20,000","Invalid input reverts to previous value");dialog.From.Value=0;
   dialog.To.UpButton();Check(dialog.To.Value==dialog.To.Maximum,"Arrow keys stay within audio length");dialog.To.DownButton();Check(dialog.To.Value==dialog.To.Maximum-.05m&&dialog.To.Text=="1:29,950","Arrow step shows minute format");dialog.To.Value=dialog.To.Maximum;
   void Event(string name,int x)=>typeof(ImportWave).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(dialog.Wave,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,50,0)});
   int X(double s)=>(int)Math.Round(dialog.Wave.X(s));
   Event("OnMouseMove",X(45));Check(Math.Abs(dialog.Wave.Hover-45)<.5,"Hover shows the time under the mouse");
   using(var b=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(b,dialog.ClientRectangle);b.Save(Path.Combine(AppContext.BaseDirectory,"import-timeline-hover.png"));}
   Event("OnMouseDown",X(30));Event("OnMouseUp",X(30));
   Check(dialog.Previewing&&Math.Abs(dialog.PreviewFrom-30)<.5&&!host.audio.Running,"Click in timeline plays from that point on the monitor only");
   Check(dialog.From.Value==0&&dialog.To.Value==dialog.To.Maximum,"Timeline click does not move the markers");
   await Task.Delay(600);Check(dialog.Previewing&&dialog.Wave.Position>30.3&&dialog.Wave.Position<31.5,"Playhead advances from the clicked point");
   using(var b=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(b,dialog.ClientRectangle);b.Save(Path.Combine(AppContext.BaseDirectory,"import-timeline-playing.png"));}
   Event("OnMouseDown",X(10));Check(!dialog.Previewing&&Math.Abs(dialog.Wave.Position-10)<.5,"Pressing in the timeline stops playback and shows the playhead");Event("OnMouseMove",X(50));Check(!dialog.Previewing&&Math.Abs(dialog.Wave.Position-50)<.5,"Dragging scrubs the playhead");Event("OnMouseUp",X(50));
   Check(dialog.Previewing&&Math.Abs(dialog.PreviewFrom-50)<.5,"Releasing after scrubbing plays from the new point");
   dialog.TogglePreview();Check(!dialog.Previewing&&dialog.Preview.Text.Contains("Vorhören"),"Preview button stops timeline playback");
   dialog.From.Value=10;dialog.To.Value=40;Event("OnMouseDown",X(70));Event("OnMouseUp",X(70));Check(dialog.Previewing&&Math.Abs(dialog.PreviewFrom-70)<.5&&dialog.From.Value==10&&dialog.To.Value==40,"Click behind the selection plays from there without changing it");
   Event("OnMouseDown",X(39.5));Event("OnMouseUp",X(39.5));Check(!dialog.Previewing||dialog.PreviewFrom<0,"Click near a marker grabs the marker instead of playing");Check(Math.Abs((double)dialog.To.Value-39.5)<.4,"Marker grab moves the end marker");
   Event("OnMouseDown",X(10));Event("OnMouseMove",X(12));Event("OnMouseUp",X(12));Check(Math.Abs((double)dialog.From.Value-12)<.4&&!dialog.Previewing,"Start marker can still be dragged");
   Event("OnMouseDown",X(20));Event("OnMouseUp",X(20));await Task.Delay(300);Check(dialog.Previewing,"Click inside selection plays");dialog.From.Value=11;Check(!dialog.Previewing,"Changing the range stops timeline playback");
   Event("OnMouseDown",X(90));Event("OnMouseUp",X(90));Check(!dialog.Previewing,"Click at the very end does not start an empty clip");
   Event("OnMouseLeave",0);Check(dialog.Wave.Hover<0,"Hover marker disappears when the mouse leaves");
   // Name direkt beim Speichern vergeben.
   Check(dialog.SoundName.Text=="Langes Video","Name field suggests the video title after loading");
   dialog.Slots.SelectedIndex=2;Check(dialog.SoundName.Text=="Eigener Name","Name field shows an existing custom name of the chosen slot");
   dialog.Slots.SelectedIndex=1;dialog.SoundName.Text="  Mein Knopf  ";dialog.Slots.SelectedIndex=4;Check(dialog.SoundName.Text=="  Mein Knopf  ","Typed name survives switching the slot");dialog.Slots.SelectedIndex=1;
   dialog.From.Value=5;dialog.To.Value=8;await dialog.SaveAudio();var saved=Settings.Load().Slots[1];
   Check(saved.Name=="Mein Knopf"&&saved.CustomName&&host.settings.Slots[1].Name=="Mein Knopf"&&File.Exists(saved.File)&&changed==1,"Typed name renames the button when saving");
   Check(dialog.Slots.Items[1]!.ToString()=="02 · Mein Knopf","Slot list shows the new name right away");
   using(var r=new AudioFileReader(saved.File))Check(Math.Abs(r.TotalTime.TotalSeconds-3)<.15,"Saved clip has the selected length");
   host.bank=0;using(var image=new Bitmap(host.deck.Width,host.deck.Height)){host.deck.DrawToBitmap(image,host.deck.ClientRectangle);image.Save(Path.Combine(AppContext.BaseDirectory,"import-name-deck.png"));}
   var a=host.deck.AccessibilityObject;Check(Enumerable.Range(0,a.GetChildCount()).Any(i=>(a.GetChild(i)?.Name??"").Contains("Mein Knopf")),"Deck tile shows the new name");
   dialog.Slots.SelectedIndex=3;Check(dialog.SoundName.Text=="Langes Video","Next slot gets the title suggestion again");await dialog.SaveAudio();saved=Settings.Load().Slots[3];Check(saved.Name=="Langes Video"&&!saved.CustomName,"Untouched name field keeps automatic title naming");
   dialog.Slots.SelectedIndex=2;await dialog.SaveAudio();saved=Settings.Load().Slots[2];Check(saved.Name=="Eigener Name"&&saved.CustomName,"Untouched field keeps an existing custom name");
   dialog.Slots.SelectedIndex=5;dialog.SoundName.Text="   ";await dialog.SaveAudio();saved=Settings.Load().Slots[5];Check(saved.Name=="Langes Video"&&!saved.CustomName,"Empty name falls back to the video title");
   dialog.Slots.SelectedIndex=6;dialog.SoundName.Text="Tab\tName";var before=Settings.Load().Slots[6].File;await dialog.SaveAudio();Check(Settings.Load().Slots[6].File==before,"Name with control characters is refused without saving");
   dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(40,40);dialog.Show();dialog.Activate();await Task.Delay(400);Event("OnMouseMove",X(45));Event("OnMouseDown",X(30));Event("OnMouseUp",X(30));await Task.Delay(500);dialog.Refresh();using(var shot=new Bitmap(dialog.Width,dialog.Height)){using(var g=Graphics.FromImage(shot))g.CopyFromScreen(dialog.Location,Point.Empty,dialog.Size);shot.Save(Path.Combine(AppContext.BaseDirectory,"import-timeline-screen.png"));}dialog.TogglePreview();dialog.Hide();
   host.SetLanguage(true);using(var english=new AudioImportDialog(host,host.settings,()=>{})){Prepare(english);await english.LoadSource(source);Check(english.To.Text=="1:30.000","English minute format");using var b=new Bitmap(english.Width,english.Height);english.DrawToBitmap(b,english.ClientRectangle);b.Save(Path.Combine(AppContext.BaseDirectory,"import-timeline-en.png"));}
   host.SetLanguage(false);
  }catch(Exception e){failure=e;}finally{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"import-timeline-test.json"),JsonSerializer.Serialize(new{ok=failure==null,checks,error=failure?.ToString()},new JsonSerializerOptions{WriteIndented=true}));driver.Close();}};Application.Run(driver);if(failure!=null)Environment.ExitCode=1;
 }
 static IEnumerable<Control> Flatten(Control c)=>new[]{c}.Concat(c.Controls.OfType<Control>().SelectMany(Flatten));
}
