using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 // Fenster frei in der Größe ändern: gleichmäßiger Maßstab, Kacheln füllen den Platz, nichts ragt heraus oder überlappt.
 public static void RunResizeTest(){
  var checks=new List<string>();object result;
  try{
   Directory.CreateDirectory(Settings.Folder);var wav=Path.Combine(Settings.Folder,"resize.wav");
   using(var w=new WaveFileWriter(wav,WaveFormat.CreateIeeeFloatWaveFormat(48000,1))){var s=new float[24000];for(int i=0;i<s.Length;i++)s[i]=.001f*(float)Math.Sin(i*2*Math.PI*440/48000);w.WriteSamples(s,0,s.Length);}
   var cfg=new Settings{TourDone=true,AudioEnabled=false,MixMicrophone=false,MonitorEnabled=false};
   for(int i=0;i<40;i++)cfg.Slots[i]=new SoundSlot{Name=$"Klang {i+1}",File=wav};cfg.Slots[2].TileColor=TilePalette.Colors[6];cfg.Save();AutoTour=false;
   void Check(bool b,string why){if(!b)throw new Exception(why);checks.Add(why);}
   using var f=new MainForm();void Prepare(Control c){_=c.Handle;foreach(Control child in c.Controls)Prepare(child);}Prepare(f);
   const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var d=f.deck;
   List<(RectangleF rect,Action action,string name)> Hits()=>(List<(RectangleF rect,Action action,string name)>)typeof(ReferenceDeck).GetField("hits",flags)!.GetValue(d)!;
   Bitmap Paint(string? name=null){var image=new Bitmap(d.Width,d.Height);d.DrawToBitmap(image,d.ClientRectangle);if(name!=null)image.Save(Path.Combine(AppContext.BaseDirectory,name+".png"));return image;}
   Color At(Bitmap b,float x,float y){var p=d.ToClient(x,y);return b.GetPixel(Math.Clamp(p.X,0,b.Width-1),Math.Clamp(p.Y,0,b.Height-1));}
   void Mouse(string name,float x,float y){var p=d.ToClient(x,y);d.GetType().GetMethod(name,flags)!.Invoke(d,new object[]{new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0)});}
   void Click(float x,float y){Paint().Dispose();Mouse("OnMouseDown",x,y);Mouse("OnMouseUp",x,y);}
   RectangleF Named(string n){Paint().Dispose();return Hits().First(h=>h.name==n).rect;}
   float referenceTileWidth=0;
   foreach(var size in new[]{new Size(1536,1024),new Size(1920,1080),new Size(2400,1000),new Size(1080,720),new Size(1200,1000),new Size(1300,760)}){
    f.ClientSize=size;f.PerformLayout();var tag=$"{d.Width}x{d.Height}";
    float s=d.UiScale,LW=d.LogicalWidth,LH=d.LogicalHeight;
    Check(Math.Abs(LW*s-d.Width)<1.5f&&Math.Abs(LH*s-d.Height)<1.5f&&LW>=1535.9f&&LH>=1023.9f,$"{tag}: one scale for both directions, nothing is stretched");
    using(var shot=Paint("resize-"+tag)){}
    var hits=Hits();
    Check(hits.All(h=>h.rect.Left>=-.5f&&h.rect.Top>=-.5f&&h.rect.Right<=LW+.5f&&h.rect.Bottom<=LH+.5f),$"{tag}: every button lies inside the window");
    var close=hits.Single(h=>h.rect.Y==0&&h.rect.Width==63).rect;Check(Math.Abs(close.Right-LW)<.5f,$"{tag}: close button sits at the right edge");
    var header=hits.Where(h=>h.rect.Bottom<=62).Select(h=>h.rect).ToList();
    Check(header.All(a=>header.All(b=>a==b||!RectangleF.Intersect(a,b).IsEmpty==false||RectangleF.Intersect(a,b).Width<.5f||RectangleF.Intersect(a,b).Height<.5f)),$"{tag}: header buttons do not overlap");
    var t0=d.TileBounds(0);var t3=d.TileBounds(3);var t4=d.TileBounds(4);
    Check(t4.Y>t0.Y&&Math.Abs(t4.X-t0.X)<.5f&&t3.Right<=d.GridRight-8&&t3.Right>d.GridRight-30,$"{tag}: four columns fill the full width up to the scrollbar");
    if(size==new Size(1536,1024))referenceTileWidth=t0.Width;else if(LW>1600)Check(t0.Width>referenceTileWidth+10,$"{tag}: tiles get wider when the window gets wider");
    int visibleRows=Enumerable.Range(0,40).Count(i=>i%4==0&&d.TileBounds(i).Bottom<=d.ViewBottom);
    Check(d.ViewBottom<=LH-158&&visibleRows>=(LH>1100?6:5),$"{tag}: grid uses the extra height ({visibleRows} full rows) and ends above the bottom bar");
    var audioHit=hits.Single(h=>h.action==(Action)f.ToggleAudio||h.name.Contains("Audio starten")).rect;
    Check(Math.Abs(audioHit.Right-(LW-25))<1&&audioHit.Bottom<=LH-30&&audioHit.Top>=d.ViewBottom+40,$"{tag}: Audio starten stays at the bottom right");
    var settingsButton=Named("☷   Audio-Einstellungen");Check(settingsButton.Bottom<=908+d.DY&&settingsButton.Top>=d.ViewBottom,$"{tag}: Audio-Einstellungen stays between grid and bottom bar");
    float sliderY=946+d.DY;Click(811+d.SliderWidth*.5f,sliderY);Check(Math.Abs(f.settings.MasterVolume-.5f)<.02f,$"{tag}: app volume slider follows the mouse");Click(811+d.SliderWidth*.8f,sliderY);Check(Math.Abs(f.settings.MasterVolume-.8f)<.02f,$"{tag}: app volume slider reaches its new length");
    using(var b=Paint()){var c=At(b,audioHit.X+audioHit.Width/2,audioHit.Bottom-6);Check(c.R>190&&c.G>150,$"{tag}: gold audio button is drawn where it can be clicked");
     var tile=d.TileBounds(2);var rim=At(b,tile.X+1.8f,tile.Y+60);Check(rim.B>200&&rim.R<120,$"{tag}: colored tile is drawn at its tile position");
     var line=d.ToClient(700,908+d.DY);Check(Enumerable.Range(line.Y-2,5).Any(y=>{var c=b.GetPixel(line.X,Math.Clamp(y,0,b.Height-1));return c.R>=20&&c.R<70;})&&At(b,700,880+d.DY).R<30,$"{tag}: bottom bar line moved with the window bottom");}
    // Feld rechts: öffnet am rechten Rand, Kacheln darunter bleiben unberührt, Bearbeiten-Feld liegt richtig.
    f.OpenSoundSettings(5);var panelClose=Named("Sound-Einstellungen schließen");Check(Math.Abs(panelClose.Right-(LW-16))<.5f,$"{tag}: sound settings panel sits at the right edge");
    Check(!Hits().Any(h=>h.rect.Top>=62&&h.rect.Bottom<=908+d.DY&&h.rect.Left<d.PanelLeft&&h.rect.Right>d.PanelLeft+.5f),$"{tag}: nothing under the panel can be clicked through it");
    var gain=f.draftGain;Click(1200+d.DX+306*.25f,412);Check(Math.Abs(f.draftGain*100-30)<2,$"{tag}: level slider in the panel follows the mouse");f.ResetTrim();
    f.EditSoundName();Check(f.soundEditor!=null&&Math.Abs(f.soundEditor.Left-(int)((1194+d.DX)*s))<=1&&f.soundEditor.Right<=d.Width,$"{tag}: name editor opens on the panel");f.soundEditor!.Dispose();f.soundEditor=null;
    using(var b=Paint("resize-panel-"+tag)){var p=At(b,LW-30,700);Check(p.R<40&&p.G<45,$"{tag}: panel background reaches the right edge");}
    Click(panelClose.X+10,panelClose.Y+10);Check(!f.soundPanelOpen,$"{tag}: panel closes with its X");
    // Fensterränder: Größe ziehen, solange das Fenster nicht maximiert ist.
    Check(f.ResizeHit(new Point(d.Width-2,d.Height-2))==17&&f.ResizeHit(new Point(1,d.Height/2))==10&&f.ResizeHit(new Point(d.Width/2,1))==12&&f.ResizeHit(new Point(d.Width/2,d.Height/2))==0,$"{tag}: window edges and corners resize");
    var screen=f.PointToScreen(new Point(d.Width-2,d.Height-2));var lp=(IntPtr)((screen.Y<<16)|(screen.X&0xFFFF));
    Check((int)Native.SendMessage(d.Handle,0x84,IntPtr.Zero,lp)==-1&&(int)Native.SendMessage(f.Handle,0x84,IntPtr.Zero,lp)==17,$"{tag}: the deck lets the window handle its corner");
    var middle=f.PointToScreen(new Point(d.Width/2,d.Height/2));Check((int)Native.SendMessage(d.Handle,0x84,IntPtr.Zero,(IntPtr)((middle.Y<<16)|(middle.X&0xFFFF)))==1,$"{tag}: inside the window clicks stay normal");
    // Einführung: Markierungen folgen den verschobenen Bereichen.
    for(int step=0;step<TourSteps().Length;step++){f.tourStep=step;f.PrepareTourStep();foreach(var r in d.TourSpots(step))Check(r.Left>=0&&r.Right<=LW&&r.Bottom<=LH,$"{tag}: tour step {step+1} inside the window");}
    f.tourStep=7;var spot=d.TourSpots(7)[0];Check(Math.Abs(spot.X-(1027+d.DX))<.5f,$"{tag}: tour points at the moved phone button");f.tourStep=5;spot=d.TourSpots(5)[0];Check(RectangleF.Inflate(spot,2,2).Contains(settingsButton),$"{tag}: tour points at the moved audio settings");f.EndTour();
   }
   f.ClientSize=new Size(1536,1024);f.PerformLayout();Check(d.DX==0&&d.DY==0&&d.GridRight==1524&&d.ViewBottom==865,"Reference size keeps the original layout exactly");
   Check(f.MinimumSize==new Size(1080,720)&&f.FormBorderStyle==FormBorderStyle.None,"Minimum window size stays 1080 x 720");
   f.WindowState=FormWindowState.Maximized;Check(f.ResizeHit(new Point(1,1))==0,"Maximized window has no resize edges");f.WindowState=FormWindowState.Normal;
   result=new{ok=true,checks};
  }catch(Exception e){result=new{ok=false,error=e.ToString(),checks};Environment.ExitCode=1;}
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"resize-test.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
 }
}
