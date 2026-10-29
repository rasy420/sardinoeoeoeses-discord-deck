using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
// Automatischer Test (--trim-test, mit DISCORDDECK_HOME auf einen Testordner): Zuschneiden über Dialog, Speicherung und Wiedergabe.
public sealed partial class MainForm {
 public static void RunTrimTest(){
  var log=new List<string>();var errors=new List<string>();
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(_,e)=>errors.Add(e.Exception.ToString());
  void Check(bool ok,string what){log.Add((ok?"OK   ":"FEHL ")+what);if(!ok)errors.Add(what);}
  void Pump(int ms){var until=DateTime.Now.AddMilliseconds(ms);while(DateTime.Now<until){Application.DoEvents();Thread.Sleep(10);}}
  // 4 s Testsignal: 1–2 s Ton, sonst Stille.
  Directory.CreateDirectory(Settings.Folder);var wav=Path.Combine(Settings.Folder,"trim-test.wav");
  using(var w=new WaveFileWriter(wav,WaveFormat.CreateIeeeFloatWaveFormat(44100,1))){var data=new float[44100*4];for(int i=44100;i<88200;i++)data[i]=.5f*(float)Math.Sin(i*2*Math.PI*440/44100);w.WriteSamples(data,0,data.Length);}
  (int total,int loud) Measure(double from,double to){using var clip=new AudioEngine.Clip(wav,1,null,from,to);var buf=new float[9600];int total=0,loud=0,n;while((n=clip.Read(buf,0,buf.Length))>0){total+=n;for(int i=0;i<n;i++)if(Math.Abs(buf[i])>.01f)loud++;}return (total,loud);}
  var full=Measure(0,0);Check(Math.Abs(full.total-4*48000*2)<2000,$"ganzer Sound: {full.total} Samples (erwartet ~{4*48000*2})");
  var tone=Measure(1,2);Check(Math.Abs(tone.total-96000)<1500&&tone.loud>tone.total*.95,$"1–2 s: {tone.total} Samples, {tone.loud} mit Ton");
  var quiet=Measure(2.2,3.8);Check(quiet.loud<200&&Math.Abs(quiet.total-153600)<1500,$"2,2–3,8 s: {quiet.total} Samples, {quiet.loud} mit Ton (erwartet Stille)");
  Check(AudioEngine.Clip.Range(3,1,4)==(0,4),"Ende vor Start → ganzer Sound");Check(AudioEngine.Clip.Range(-1,9,4)==(0,4),"außerhalb → ganzer Sound");Check(AudioEngine.Clip.Range(1,0,4)==(1,4),"nur Start → bis Ende");
  foreach(var mp3 in Directory.EnumerateFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DiscordDeck","Sounds"),"*.mp3",SearchOption.AllDirectories).Take(1)){
   using var r=new AudioFileReader(mp3);var len=r.TotalTime.TotalSeconds;if(len<1)continue;using var clip=new AudioEngine.Clip(mp3,1,null,len/2,0);var buf=new float[9600];long n=0;int k;while((k=clip.Read(buf,0,buf.Length))>0)n+=k;var secs=n/96000.0;Check(Math.Abs(secs-len/2)<.15,$"MP3 ab Hälfte: {secs:0.00} s von {len:0.00} s ({Path.GetFileName(mp3)})");}
  AutoTour=false;using var form=new MainForm();form.Show();Pump(1500);
  var slot=form.settings.Slots[0];slot.File=wav;slot.Name="Trim-Test";slot.TrimStart=slot.TrimEnd=0;form.SelectSound(0);
  Check(form.TrimLabel(slot)=="Ganz · 4,00 s","Anzeige vorher: "+form.TrimLabel(slot));
  int step=0;var driver=new System.Windows.Forms.Timer{Interval=300};
  driver.Tick+=(_,_)=>{var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Text.StartsWith("Länge zuschneiden"));if(dialog==null)return;var nums=dialog.Controls.OfType<NumericUpDown>().ToList();var buttons=dialog.Controls.OfType<Button>().ToList();
   switch(step++){
    case 0:nums[0].Value=1.25m;nums[1].Value=2.5m;dialog.Refresh();using(var shot=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(shot,new Rectangle(0,0,dialog.Width,dialog.Height));shot.Save(Path.Combine(AppContext.BaseDirectory,"trim-dialog.png"));}break;
    case 1:buttons.First(b=>b.Text.Contains("Vorhören")).PerformClick();break;
    case 2:Check(buttons.First(b=>b.Text.Contains("Stopp")||b.Text.Contains("Vorhören")).Text.Contains("Stopp"),"Vorhören läuft");break;
    case 6:Check(buttons[0].Text.Contains("Vorhören"),"Vorhören endet nach Bereichsende von selbst");nums[1].Value=1.2m;break;
    case 7:Check(!buttons.First(b=>b.Text=="Übernehmen").Enabled,"Ende vor Start: Übernehmen gesperrt");nums[1].Value=2.5m;break;
    case 8:buttons.First(b=>b.Text=="Übernehmen").PerformClick();driver.Stop();break;
   }};
  driver.Start();form.EditTrim();driver.Dispose();
  Check(slot.TrimStart==1.25&&slot.TrimEnd==2.5,$"gespeichert: {slot.TrimStart}–{slot.TrimEnd}");Check(form.TrimLabel(slot)=="1,25 – 2,50 s","Anzeige nachher: "+form.TrimLabel(slot));
  var saved=Settings.Load().Slots[0];Check(saved.TrimStart==1.25&&saved.TrimEnd==2.5,"aus settings.json neu geladen");
  // Direkt in der Vorschau rechts: Start-Marker mit der Maus ziehen, anhören, speichern.
  IntPtr L(float x,float y)=>(IntPtr)(((int)(y*form.deck.Height/1024)<<16)|(int)(x*form.deck.Width/1536));
  Pump(800);float X(double s)=>1193+319f*(float)s/4;
  Native.SendMessage(form.deck.Handle,0x201,(IntPtr)1,L(X(1.25),215));Pump(50);Native.SendMessage(form.deck.Handle,0x200,(IntPtr)1,L(X(0.8),215));Pump(50);Native.SendMessage(form.deck.Handle,0x200,(IntPtr)1,L(X(0.5),215));Pump(50);Native.SendMessage(form.deck.Handle,0x202,IntPtr.Zero,L(X(0.5),215));Pump(200);
  var panel=form.PanelTrim();Check(panel is {} pt&&Math.Abs(pt.from-.5)<.03&&Math.Abs(pt.to-2.5)<.01,$"Marker gezogen: {panel}");Check(form.TrimDirty,"ungespeichert markiert");Check(slot.TrimStart==1.25,"vor Speichern unverändert");
  Native.SendMessage(form.deck.Handle,0x201,(IntPtr)1,L(X(2.5),215));Native.SendMessage(form.deck.Handle,0x200,(IntPtr)1,L(X(3.9),215));Native.SendMessage(form.deck.Handle,0x202,IntPtr.Zero,L(X(3.9),215));Pump(200);
  Check(form.PanelTrim() is {} p2&&Math.Abs(p2.to-3.9)<.03,$"End-Marker gezogen: {form.PanelTrim()}");
  using(var shot=new Bitmap(form.deck.Width,form.deck.Height)){form.deck.DrawToBitmap(shot,form.deck.ClientRectangle);shot.Save(Path.Combine(AppContext.BaseDirectory,"trim-panel-dirty.png"));}
  form.TogglePanelPreview();Pump(400);Check(form.PanelPreviewing,"Vorhören im Panel läuft");using(var shot=new Bitmap(form.deck.Width,form.deck.Height)){form.deck.DrawToBitmap(shot,form.deck.ClientRectangle);shot.Save(Path.Combine(AppContext.BaseDirectory,"trim-panel-playing.png"));}
  Pump(3600);Check(!form.PanelPreviewing,"Vorhören im Panel endet am End-Marker");
  form.SaveTrim();Check(Math.Abs(slot.TrimStart-.5)<.03&&Math.Abs(slot.TrimEnd-3.9)<.03&&!form.TrimDirty,$"Panel gespeichert: {slot.TrimStart}–{slot.TrimEnd}");
  form.SelectSound(1);form.SelectSound(0);Check(form.PanelTrim() is {} p3&&Math.Abs(p3.from-.5)<.03,"nach Platzwechsel aus Speicher geladen");
  var savedFrom=slot.TrimStart;var savedTo=slot.TrimEnd;form.DragTrim(true,.8);form.ResetTrim();Check(!form.TrimDirty,"⟲ gespeicherten Zustand wiederhergestellt");form.SaveTrim();Check(slot.TrimStart==savedFrom&&slot.TrimEnd==savedTo,"Zurücksetzen behält gespeicherten Ausschnitt");
  slot.TrimStart=1.25;slot.TrimEnd=2.5;form.ForgetTrim();form.SelectSound(1);form.SelectSound(0);
  // Echte Wiedergabe über die Audio-Pipeline: Clip muss nach ~1,25 s von selbst enden.
  try{form.Try(form.StartAudio);Pump(500);if(form.audio.Running){form.Play(0);Pump(200);Check(form.audio.Playing==1,"Pipeline spielt");Pump(1600);Check(form.audio.Playing==0,"Pipeline: Clip nach Bereichsende beendet");for(int i=0;i<20;i++)form.Play(0);Check(form.audio.Playing==16,"Spammen: ab 16 Sounds ersetzt der neue den ältesten ohne Fehler");form.audio.StopSounds();Check(form.audio.Playing==0,"Alle Sounds stoppen beendet alle");form.ToggleAudio();}else log.Add("Pipeline übersprungen: "+form.status.Text);}catch(Exception e){errors.Add("Pipeline: "+e.Message);}
  // Neue Datei setzt den Bereich zurück (wie bei Datei/MyInstants/YouTube).
  form.deck.Invalidate();Pump(300);using(var shot=new Bitmap(form.deck.Width,form.deck.Height)){form.deck.DrawToBitmap(shot,form.deck.ClientRectangle);shot.Save(Path.Combine(AppContext.BaseDirectory,"trim-deck.png"));}
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"trim-test.json"),JsonSerializer.Serialize(new{ok=errors.Count==0,errors,log},new JsonSerializerOptions{WriteIndented=true}));
  form.exitRequested=true;form.Close();
 }
}
