using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
namespace DiscordDeck;
public sealed partial class MainForm {
 // Mikrofonlautstärke im Mix: Signalpegel, Begrenzung, Speichern und die beiden Regler unten in der Leiste.
 public static void RunMicLevelTest(){
  Directory.CreateDirectory(Settings.Folder);var checks=new List<string>();Exception? failure=null;void Check(bool b,string name){if(!b)throw new Exception(name);checks.Add(name);}
  try{
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   float[] Run(float amplitude,float gain){var src=new SignalGenerator(48000,2){Type=SignalGeneratorType.Square,Frequency=100,Gain=amplitude};var level=new AudioEngine.MicLevel(src){Gain=gain};var b=new float[960];level.Read(b,0,b.Length);return b;}
   Check(Math.Abs(Run(.1f,1).Max()-.1f)<1e-4,"100 % leaves the microphone unchanged");
   Check(Math.Abs(Run(.1f,3).Max()-.3f)<1e-4&&Math.Abs(Run(.1f,3).Min()+.3f)<1e-4,"300 % makes the microphone three times louder");
   Check(Math.Abs(Run(.1f,4).Max()-.4f)<1e-4,"400 % makes the microphone four times louder");
   Check(Run(.5f,4).Max()==1f&&Run(.5f,4).Min()==-1f,"Loud peaks are limited to full scale instead of overflowing");
   Check(Run(.3f,0).All(x=>x==0),"0 % mutes the microphone in the mix");
   // Echter Mikrofonweg wie in der Engine: 16-bit-Mono 44,1 kHz -> Umrechnung auf 48 kHz Stereo -> Pegel.
   var mono=new WaveFormat(44100,16,1);var buffer=new BufferedWaveProvider(mono){BufferDuration=TimeSpan.FromSeconds(1),ReadFully=true};var pcm=new byte[44100];for(int i=0;i<pcm.Length/2;i++)BitConverter.GetBytes((short)(Math.Sin(i*2*Math.PI*440/44100)*3277)).CopyTo(pcm,i*2);buffer.AddSamples(pcm,0,pcm.Length);
   var chain=new AudioEngine.MicLevel(AudioEngine.Convert(buffer.ToSampleProvider())){Gain=2.5f};var outBuf=new float[48000];chain.Read(outBuf,0,outBuf.Length);var peak=outBuf.Skip(4800).Take(19200).Max(Math.Abs);
   Check(chain.WaveFormat.SampleRate==48000&&chain.WaveFormat.Channels==2&&Math.Abs(peak-.25f)<.02f,"Real microphone format is boosted to 250 % in the mix ("+peak.ToString("0.000")+")");
   using(var engine=new AudioEngine()){var field=typeof(AudioEngine).GetField("micGain",flags)!;Check(engine.MicrophoneGain==-1,"Without running audio there is no microphone gain");field.SetValue(engine,new AudioEngine.MicLevel(new SignalGenerator(48000,2)));
    engine.SetVolume(.7f,2.2f);Check(Math.Abs(engine.MicrophoneGain-2.2f)<1e-4,"Running engine takes 220 % live");engine.SetVolume(.7f,9);Check(engine.MicrophoneGain==4,"Engine caps at 400 %");engine.SetVolume(.7f,-1);Check(engine.MicrophoneGain==0,"Engine floors at 0 %");}
   new Settings{MicrophoneVolume=9}.Save();Check(Settings.Load().MicrophoneVolume==4,"Too high saved value is capped at 400 %");new Settings{MicrophoneVolume=-3}.Save();Check(Settings.Load().MicrophoneVolume==0,"Negative saved value becomes 0 %");
   new Settings{MicrophoneVolume=1.5f}.Save();Check(Settings.Load().MicrophoneVolume==1.5f,"Saved 150 % survives a restart");
   // Regler unten in der Leiste.
   var cfg=new Settings{TourDone=true,MixMicrophone=true,MonitorEnabled=false,MicrophoneVolume=1,MasterVolume=.7f};cfg.Save();AutoTour=false;
   using var form=new MainForm();form.CreateControl();form.deck.CreateControl();form.ClientSize=new Size(1536,1024);form.PerformLayout();var deck=form.deck;
   var live=new AudioEngine.MicLevel(new SignalGenerator(48000,2));typeof(AudioEngine).GetField("micGain",flags)!.SetValue(form.audio,live);
   void Paint(){using var b=new Bitmap(deck.Width,deck.Height);deck.DrawToBitmap(b,deck.ClientRectangle);}
   void Mouse(string name,int x,int y)=>typeof(ReferenceDeck).GetMethod(name,flags)!.Invoke(deck,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});
   void Click(int x,int y){Mouse("OnMouseDown",x,y);Mouse("OnMouseUp",x,y);Paint();}
   Check(deck.Width==1536&&deck.Height==1024,"Deck at reference size");Paint();
   Click(1212,979);Check(form.settings.MicrophoneVolume==4&&live.Gain==4&&Math.Abs(form.settings.MasterVolume-.7f)<1e-4,"Mic slider right end sets 400 % live without touching app volume");
   Check(Settings.Load().MicrophoneVolume==4,"Mic level is saved immediately");
   Click(911,979);Check(Math.Abs(form.settings.MicrophoneVolume-1)<.02f&&Math.Abs(live.Gain-form.settings.MicrophoneVolume)<1e-4,"Tick position is 100 %");
   Click(811,979);Check(form.settings.MicrophoneVolume==0&&live.Gain==0,"Mic slider left end mutes the microphone");
   Mouse("OnMouseDown",811,979);Mouse("OnMouseMove",1011,981);Check(Math.Abs(form.settings.MicrophoneVolume-2)<.02f,"Dragging the mic slider sets 200 %");Mouse("OnMouseMove",1400,985);Check(form.settings.MicrophoneVolume==4,"Dragging past the end stays at 400 %");Mouse("OnMouseUp",1400,985);
   Mouse("OnMouseMove",811,985);Check(form.settings.MicrophoneVolume==4,"Moving after release changes nothing");
   Click(1212,946);Check(form.settings.MasterVolume==1&&form.settings.MicrophoneVolume==4,"App volume slider still works and leaves the mic alone");Click(811,946);Check(form.settings.MasterVolume==0,"App volume can go to zero");
   Mouse("OnMouseDown",811,946);Mouse("OnMouseMove",1011,950);Mouse("OnMouseUp",1011,950);Check(Math.Abs(form.settings.MasterVolume-.5f)<.02f&&form.settings.MicrophoneVolume==4,"Dragging app volume keeps mic level");
   Click(1011,979);using(var shot=new Bitmap(deck.Width,deck.Height)){deck.DrawToBitmap(shot,deck.ClientRectangle);shot.Save(Path.Combine(AppContext.BaseDirectory,"mic-level-deck.png"));var knob=shot.GetPixel(1011,979);Check(knob.R>200&&knob.G>200&&knob.B>200,"Mic knob is drawn at 200 %");var fill=shot.GetPixel(950,979);Check(fill.R>200&&fill.G>120&&fill.G<180&&fill.B<110,"Above 100 % the mic bar turns orange as boost warning");var app=shot.GetPixel(900,946);Check(app.R>200&&app.G>170&&app.B>100,"App volume bar keeps the gold accent");}
   form.mic.Checked=false;form.settings.MixMicrophone=false;using(var shot=new Bitmap(deck.Width,deck.Height)){deck.DrawToBitmap(shot,deck.ClientRectangle);shot.Save(Path.Combine(AppContext.BaseDirectory,"mic-level-off.png"));var fill=shot.GetPixel(950,979);Check(Math.Abs(fill.R-fill.B)<35&&fill.R<140,"Mic bar is grey while the microphone is not mixed in");}
   form.SetLanguage(true);using(var shot=new Bitmap(deck.Width,deck.Height)){deck.DrawToBitmap(shot,deck.ClientRectangle);shot.Save(Path.Combine(AppContext.BaseDirectory,"mic-level-en.png"));}form.SetLanguage(false);
  }catch(Exception e){failure=e;}
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"mic-level-test.json"),JsonSerializer.Serialize(new{ok=failure==null,checks,error=failure?.ToString()},new JsonSerializerOptions{WriteIndented=true}));if(failure!=null)Environment.ExitCode=1;
 }
}
