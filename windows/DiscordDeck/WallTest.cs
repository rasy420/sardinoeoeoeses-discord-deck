using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 // Durchgehende Soundwand, pulsierende Hinweise, YouTube-Knopf und 150 % beim Import.
 public static void RunWallTest(){
  var checks=new List<string>();object result;
  try{
   Directory.CreateDirectory(Settings.Folder);var wav=Path.Combine(Settings.Folder,"wall.wav");
   using(var w=new WaveFileWriter(wav,WaveFormat.CreateIeeeFloatWaveFormat(48000,1))){var s=new float[48000];for(int i=0;i<s.Length;i++)s[i]=.001f*(float)Math.Sin(i*2*Math.PI*440/48000);w.WriteSamples(s,0,s.Length);}
   var output=AudioEngine.Devices().First(d=>d.Direction=="Render"&&AudioEngine.IsVirtualOutput(d.Name));
   var cfg=new Settings{TourDone=true,AudioEnabled=false,OutputId=output.Id,MixMicrophone=false,MonitorEnabled=false};
   for(int i=0;i<90;i++)cfg.Slots[i]=new SoundSlot{Name=$"Wall {i+1}",File=wav,Volume=i==5?2.2f:.65f};cfg.Save();AutoTour=false;
   using var f=new MainForm();void Prepare(Control c){_=c.Handle;foreach(Control child in c.Controls)Prepare(child);}Prepare(f);f.ClientSize=new Size(1536,1024);f.PerformLayout();
   void Check(bool b,string why){if(!b)throw new Exception(why);checks.Add(why);}
   const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
   Bitmap Paint(string? name=null){var image=new Bitmap(f.deck.Width,f.deck.Height);f.deck.DrawToBitmap(image,f.deck.ClientRectangle);if(name!=null)image.Save(Path.Combine(AppContext.BaseDirectory,name+".png"));return image;}
   Color At(Bitmap b,float x,float y)=>b.GetPixel((int)(x*f.deck.Width/1536),(int)(y*f.deck.Height/1024));
   Point Px(float x,float y)=>new((int)(x*f.deck.Width/1536),(int)(y*f.deck.Height/1024));
   void Mouse(string name,float x,float y,int delta=0){var p=Px(x,y);f.deck.GetType().GetMethod(name,flags)!.Invoke(f.deck,new object[]{new MouseEventArgs(name=="OnMouseWheel"?MouseButtons.None:MouseButtons.Left,1,p.X,p.Y,delta)});}
   void Click(float x,float y){Paint().Dispose();Mouse("OnMouseDown",x,y);Mouse("OnMouseUp",x,y);}
   void Pump(int ms){var end=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(5);}}
   void Settle(){var end=DateTime.UtcNow.AddSeconds(3);while(f.deck.Scrolling&&DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(5);}}
   bool GoldBank(Bitmap b,int i)=>At(b,200,145+i*64) is var c&&c.R>200&&c.G>160&&c.B<160;

   // Durchgehendes Scrollen mit dem Mausrad
   f.bank=0;Paint().Dispose();Check(f.scrollY==0&&f.bank==0,"Start at the top of the wall");
   for(int i=0;i<4;i++)Mouse("OnMouseWheel",700,500,-120);
   Check(f.deck.Scrolling,"Mouse wheel starts a smooth scroll animation");
   var mid=new List<float>();var until=DateTime.UtcNow.AddSeconds(3);while(f.deck.Scrolling&&DateTime.UtcNow<until){Application.DoEvents();mid.Add(f.scrollY);Thread.Sleep(5);}
   Check(Math.Abs(f.scrollY-4*ReferenceDeck.RowStep)<.01f,"Four wheel notches scroll exactly four rows");
   Check(mid.Count>3&&mid.Distinct().Count()>3,"Scrolling moves through intermediate positions instead of jumping");
   Check(f.bank==1,"Gold bank follows the scroll position into bank 2");
   using(var b=Paint("wall-mid")){Check(GoldBank(b,1)&&!GoldBank(b,0),"Bank 2 is painted gold while scrolling through it");}
   // Kacheln zweier Bänke gleichzeitig sichtbar, ohne Grenze
   f.scrollY=ReferenceDeck.BankHeight*.5f;f.deck.SnapScroll();Paint().Dispose();
   int top=f.deck.TileAt(new PointF(400,220)),bottom=f.deck.TileAt(new PointF(400,840));
   Check(top>=0&&top<20&&bottom>=20&&bottom<40,"Tiles of bank 1 and bank 2 are visible at the same time");
   var t22=f.deck.TileBounds(21);Click(t22.X+40,t22.Y+95);Pump(200);Check(f.audio.Running&&f.audio.IsPlaying(f.settings.Slots[21]),$"Click on a tile of the next bank plays exactly that sound (running={f.audio.Running} playing={f.audio.Playing} at={f.deck.TileAt(new PointF(t22.X+40,t22.Y+95))} status={f.status.Text})");f.audio.StopSounds();
   // Teilweise verdeckte Kacheln sind außerhalb des Rasters nicht anklickbar
   f.scrollY=60;f.deck.SnapScroll();Paint().Dispose();
   var hidden=f.deck.TileBounds(0);Check(hidden.Top<ReferenceDeck.ViewTop,"First row is partly above the visible grid");
   var settingsOpen=f.soundPanelOpen;Click(hidden.Right-25,hidden.Top+20);Check(f.soundPanelOpen==settingsOpen,"Hidden part of a tile above the grid does not react");
   f.scrollY=0;f.deck.SnapScroll();
   // Ende der Wand: Slot 100 erreichbar, nicht über das Ende hinaus
   for(int i=0;i<60;i++)Mouse("OnMouseWheel",700,500,-120);Settle();
   Check(f.scrollY==f.deck.MaxScroll&&f.bank==4,"Wheel stops at the end of the wall in bank 5");
   var last=f.deck.TileBounds(99);Check(last.Bottom<=f.deck.ViewBottom&&last.Top>=ReferenceDeck.ViewTop,"Slot 100 is fully visible at the end");
   for(int i=0;i<60;i++)Mouse("OnMouseWheel",700,500,120);Settle();Check(f.scrollY==0&&f.bank==0,"Wheel back up reaches slot 1 and bank 1");
   // Bank links anklicken: weich dorthin, Bank sofort gold
   Click(110,123+3*64+20);Check(f.bank==3,"Clicking bank 4 marks it immediately");using(var b=Paint())Check(GoldBank(b,3),"Clicked bank is gold during the animation");
   Check(f.deck.Scrolling,"Clicking a bank scrolls there smoothly");Settle();Check(f.scrollY==3*ReferenceDeck.BankHeight&&f.bank==3,"Bank click lands on the first row of bank 4");
   var first=f.deck.TileBounds(60);Check(Math.Abs(first.Y-195)<.01f,"Slot 61 is the first visible tile after jumping to bank 4");
   // Pfeiltasten und Bild runter
   f.bank=0;f.deck.Focus();void Key(Keys k)=>f.deck.GetType().GetMethod("OnKeyDown",flags)!.Invoke(f.deck,new object[]{new KeyEventArgs(k)});
   Key(Keys.Down);Settle();Check(Math.Abs(f.scrollY-ReferenceDeck.RowStep)<.01f,"Arrow down scrolls one row");Key(Keys.PageDown);Settle();Check(Math.Abs(f.scrollY-ReferenceDeck.RowStep-ReferenceDeck.BankHeight)<.01f&&f.bank==1,"Page down scrolls one bank");
   // Bildlaufleiste ziehen
   f.bank=0;Paint().Dispose();Mouse("OnMouseDown",1518,200);Mouse("OnMouseMove",1518,860);Mouse("OnMouseUp",1518,860);Check(f.scrollY==f.deck.MaxScroll&&f.bank==4,"Dragging the scrollbar to the bottom reaches bank 5");
   Mouse("OnMouseDown",1518,860);Mouse("OnMouseMove",1518,190);Mouse("OnMouseUp",1518,190);Check(f.scrollY==0&&f.bank==0,"Dragging the scrollbar to the top reaches bank 1");
   using(var b=Paint("wall-top")){var thumb=At(b,1518,205);Check(thumb.R>60,"Scrollbar thumb is drawn");}
   // Audio-Einstellungen unter dem Raster bleiben anklickbar
   f.scrollY=100;f.deck.SnapScroll();Paint().Dispose();Check(f.deck.TileAt(new PointF(1400,885))<0,"Audio settings row is not covered by a tile");

   // Audio starten pulsiert, bis Audio läuft
   if(f.audio.Running)f.ToggleAudio();Paint().Dispose();
   var pulse=(System.Windows.Forms.Timer)typeof(ReferenceDeck).GetField("pulse",flags)!.GetValue(f.deck)!;Check(pulse.Enabled,"Pulse timer runs while audio is off");
   var ring=new List<int>();for(int i=0;i<16;i++){using var b=Paint();var c=At(b,1302-5,938+24);ring.Add(c.R+c.G);Pump(90);}
   Check(ring.Max()-ring.Min()>40,"Ring around Start audio visibly pulses");
   using(var b=Paint("pulse"))Check(At(b,1310,946).R>200,"Start audio stays gold");
   f.ToggleAudio();Pump(300);Check(f.audio.Running,"Audio starts");
   using(var b=Paint("audio-active-green")){var c=At(b,283,954);Check(c.G>170&&c.R<100,"Audio active LED is green");}
   f.Dispatch("phone-status",0);Paint().Dispose();Pump(200);Check(!pulse.Enabled,"Pulse stops after audio starts and phone is known");
   var after=new List<int>();for(int i=0;i<6;i++){using var b=Paint();var c=At(b,1302-5,938+24);after.Add(c.R+c.G);Pump(90);}Check(after.Max()-after.Min()<6,"No ring after Start audio was pressed");
   f.ToggleAudio();Pump(200);

   // Handy verbinden hervorgehoben, bis sich einmal ein Handy meldet
   f.settings.PhoneConnected=false;f.lastPhoneSeen=DateTime.MinValue;f.CheckPhoneOnline();f.audio.StopSounds();
   using(var b=Paint("phone-hint")){var c=At(b,1031+150,11+30);Check(f.PhoneHint&&f.PhoneButtonText.Trim()=="Handy verbinden","Never connected phone keeps Connect phone label");}
   var phoneText=new List<int>();using(var b=Paint()){for(int x=1062;x<1178;x++)for(int y=20;y<32;y++){var c=At(b,x,y);if(c.R>200&&c.G>170&&c.B<150)phoneText.Add(x);}}
   Check(phoneText.Count>8,"Connect phone label is gold while highlighted");
   f.Dispatch("status",0);Check(!f.settings.PhoneConnected,"Plugin status call does not count as phone");
   f.Dispatch("phone-status",0);Check(f.settings.PhoneConnected&&Settings.Load().PhoneConnected&&!f.PhoneHint,"First phone status removes the hint permanently");Check(f.PhoneButtonText.Trim()=="Handy verbunden","Online phone label says Handy verbunden");
   using(var b=Paint()){int gold=0;for(int x=1062;x<1178;x++)for(int y=20;y<32;y++){var c=At(b,x,y);if(c.R>200&&c.G>170&&c.B<150)gold++;}Check(gold==0,"Connect phone label is normal after a phone connected");}
   // Lampe im Knopf: grün solange das Handy sich meldet, sonst gelb; Kopplungsfenster mit Warte-Lampe
   using(var b=Paint("phone-lamp-green")){var c=At(b,1188,31);Check(c.G>170&&c.R<130,"Lamp in Connect phone is green while the phone reports");}
   f.lastPhoneSeen=DateTime.UtcNow-TimeSpan.FromSeconds(5);f.CheckPhoneOnline();Check(!f.PhoneOnline,"Phone counts as offline after 4 s without status");Check(f.PhoneButtonText.Trim()=="App öffnen"&&f.PhoneHint,"Known offline phone shows Open app with gold pulse");Check(Settings.Load().PhoneConnected,"Phone history survives saving and loading across updates");
   using(var b=Paint("phone-lamp-yellow")){var c=At(b,1188,31);Check(!(c.R>220&&c.G>160&&c.B<100),"No yellow lamp when the phone is gone");}
   using(var d=new PhonePairDialog(f,"123456")){f.pairDialog=d;d.CreateControl();
    var texts=new List<string>();void Walk(Control c){texts.Add(c.Text);foreach(Control k in c.Controls)Walk(k);}Walk(d);
    Check(texts.Any(t=>t.StartsWith("Nur bei der ersten Verbindung nötig")),"Dialog says the code is only needed the first time");
    Check(d.Code.Text=="123456"&&texts.Any(t=>t.StartsWith("Code in der Android-App")),"Dialog shows the pairing code");
    Check(!d.Light.On&&d.State.Text.StartsWith("Warte auf Handyverbindung")&&d.State.ForeColor==PhonePairDialog.Yellow,"Dialog waits with a yellow lamp");
    d.Show(f);Pump(400);using(var img=new Bitmap(d.Width,d.Height)){d.DrawToBitmap(img,new Rectangle(0,0,d.Width,d.Height));img.Save(Path.Combine(AppContext.BaseDirectory,"pair-waiting.png"));}
    f.Dispatch("phone-status",0);Pump(200);Check(d.Light.On&&d.State.Text=="Handy verbunden"&&d.State.ForeColor==PhonePairDialog.Green,"Dialog lamp turns green as soon as the phone reports");
    using(var img=new Bitmap(d.Width,d.Height)){d.DrawToBitmap(img,new Rectangle(0,0,d.Width,d.Height));img.Save(Path.Combine(AppContext.BaseDirectory,"pair-connected.png"));}Pump(100);
    f.lastPhoneSeen=DateTime.UtcNow-TimeSpan.FromSeconds(5);f.CheckPhoneOnline();Check(!d.Light.On&&d.State.Text.StartsWith("Warte auf"),"Dialog falls back to waiting when the phone disappears");
    f.pairDialog=null;d.Close();}
   f.Dispatch("phone-status",0);var watch=typeof(MainForm).GetField("phoneWatch",flags)!.GetValue(f) as System.Windows.Forms.Timer;Check(watch!=null&&watch.Enabled,"Phone watch timer runs");
   f.lastPhoneSeen=DateTime.UtcNow-TimeSpan.FromSeconds(3.6);Check(f.phoneWasOnline,"Lamp is green before the timeout");Pump(1300);Check(!f.phoneWasOnline,"Timer switches the lamp off by itself");
   using(var b=Paint()){var c=At(b,1188,31);Check(!(c.R>220&&c.G>160&&c.B<100),"No yellow lamp after timeout");}

   // YouTube-Knopf im YouTube-Rot mit weißem Logo
   using(var b=Paint("youtube")){var bg=At(b,200,712);Check(bg.R>180&&bg.G<60&&bg.B<80,"YouTube button is red");var logo=At(b,19+16,707+24);Check(logo.R>240&&logo.G>240&&logo.B>240,"White logo on the YouTube button");var play=At(b,19+13+15,707+24);Check(play.R>200&&play.G<60,"Red play triangle in the logo");}
   Check(typeof(ReferenceDeck).GetField("hits",flags)!.GetValue(f.deck) is System.Collections.IList,"Hit list readable");

   // 150 % für neue Sounds in freien Plätzen, bestehende Pegel bleiben
   var free=f.settings.Slots[95];Check(string.IsNullOrEmpty(free.File)&&free.Volume==.65f,"Free slot has the old default");
   f.ReplaceSoundSource(free,wav,"neu","");Check(Math.Abs(free.Volume-1.5f)<1e-6&&Math.Abs(Settings.Load().Slots[95].Volume-1.5f)<1e-6,"Import into a free slot sets 150 %");
   var used=f.settings.Slots[5];f.ReplaceSoundSource(used,wav,"ersetzt","");Check(Math.Abs(used.Volume-2.2f)<1e-6,"Replacing an existing sound keeps its 220 %");
   var used2=f.settings.Slots[6];f.ReplaceSoundSource(used2,wav,"ersetzt","");Check(Math.Abs(used2.Volume-.65f)<1e-6,"Replacing an existing sound keeps its 65 %");
   var keepFree=f.settings.Slots[96];keepFree.Volume=.4f;f.ReplaceSoundSource(keepFree,wav,"eigen","");Check(Math.Abs(keepFree.Volume-.4f)<1e-6,"A level set by the user before import stays");
   var fail=f.settings.Slots[97];Directory.CreateDirectory(Settings.Config+".tmp");try{f.ReplaceSoundSource(fail,wav,"fail","");}catch{}finally{Directory.Delete(Settings.Config+".tmp");}Check(string.IsNullOrEmpty(fail.File)&&fail.Volume==.65f,"Failed save restores file and level");
   f.Close();
   result=new{ok=true,checks};
  }catch(Exception e){result=new{ok=false,error=e.ToString(),checks};Environment.ExitCode=1;}
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"wall-test.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
 }
}
