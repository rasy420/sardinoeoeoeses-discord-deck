using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 // Kachelfarben unter Bearbeiten: zehn Farben, Rand oder vollflächig, lesbare Schrift.
 public static void RunColorTest(){
  var checks=new List<string>();object result;
  try{
   Directory.CreateDirectory(Settings.Folder);var wav=Path.Combine(Settings.Folder,"color.wav");
   using(var w=new WaveFileWriter(wav,WaveFormat.CreateIeeeFloatWaveFormat(48000,1))){var s=new float[24000];for(int i=0;i<s.Length;i++)s[i]=.001f*(float)Math.Sin(i*2*Math.PI*440/48000);w.WriteSamples(s,0,s.Length);}
   var cfg=new Settings{TourDone=true,AudioEnabled=false,MixMicrophone=false,MonitorEnabled=false};
   for(int i=0;i<40;i++)cfg.Slots[i]=new SoundSlot{Name=$"Klang {i+1}",File=wav};cfg.Slots[39].TileColor=12345;cfg.Save();AutoTour=false;
   void Check(bool b,string why){if(!b)throw new Exception(why);checks.Add(why);}
   Check(Settings.Load().Slots[39].TileColor==0,"Unknown saved color is reset when loading");
   using var f=new MainForm();void Prepare(Control c){_=c.Handle;foreach(Control child in c.Controls)Prepare(child);}Prepare(f);f.ClientSize=new Size(1536,1024);f.PerformLayout();
   const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
   Bitmap Paint(string? name=null){var image=new Bitmap(f.deck.Width,f.deck.Height);f.deck.DrawToBitmap(image,f.deck.ClientRectangle);if(name!=null)image.Save(Path.Combine(AppContext.BaseDirectory,name+".png"));return image;}
   Color At(Bitmap b,float x,float y)=>b.GetPixel((int)(x*f.deck.Width/1536),(int)(y*f.deck.Height/1024));
   void Mouse(string name,float x,float y){var p=new Point((int)(x*f.deck.Width/1536),(int)(y*f.deck.Height/1024));f.deck.GetType().GetMethod(name,flags)!.Invoke(f.deck,new object[]{new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0)});}
   void Click(float x,float y){Paint().Dispose();Mouse("OnMouseDown",x,y);Mouse("OnMouseUp",x,y);}
   void ClickRect(RectangleF r)=>Click(r.X+r.Width/2,r.Y+r.Height/2);
   List<string> Names(){Paint().Dispose();var a=f.deck.AccessibilityObject;return Enumerable.Range(0,a.GetChildCount()).Select(i=>a.GetChild(i)?.Name??"").ToList();}
   var red=TilePalette.Colors[1];var yellow=TilePalette.Colors[3];var blue=TilePalette.Colors[6];var white=TilePalette.Colors[9];
   Check(TilePalette.Colors.Length==10&&TilePalette.NamesDe[3]=="Gelb"&&TilePalette.NamesDe[9]=="Weiß","Same ten colors as the Android app");
   Check(TilePalette.Colors.SequenceEqual(new[]{unchecked((int)0xFFE8C87A),unchecked((int)0xFFF0605A),unchecked((int)0xFFFF9F43),unchecked((int)0xFFF6E05E),unchecked((int)0xFF3DDC97),unchecked((int)0xFF2EC4B6),unchecked((int)0xFF5B8DEF),unchecked((int)0xFFB07CFF),unchecked((int)0xFFFF6B9A),unchecked((int)0xFFF4F7FA)}),"Color values match the phone palette exactly");
   Check(TilePalette.Colors.All(c=>TilePalette.Contrast(c,TilePalette.TextOn(c))>=4.5),"Every filled color keeps text contrast of at least 4.5 to 1");
   Check(TilePalette.DarkText(yellow)&&TilePalette.DarkText(white)&&!TilePalette.DarkText(unchecked((int)0xFF1A2A6C)),"Yellow and white get dark text, a dark color would get white text");
   Check(!f.settings.TileFill&&f.settings.Slots.All(s=>s.TileColor==0),"Default is border mode without colors");
   Check(!Names().Any(n=>n.StartsWith("Farbe ")),"Colors are hidden outside edit mode");
   ClickRect(f.deck.ReorderButtonBounds);Check(f.sorting,"Edit opens edit mode");
   var names=Names();Check(TilePalette.NamesDe.All(n=>names.Contains("Farbe "+n))&&names.Contains("Rand")&&names.Contains("Vollflächig"),"Edit shows ten named colors and the Rand / Vollflächig switch");
   var tile0=f.deck.TileBounds(0);var tile1=f.deck.TileBounds(1);var tile2=f.deck.TileBounds(2);
   ClickRect(ReferenceDeck.SwatchBounds(1));Check(f.selectedTileColor==red,"Clicking a color selects it");
   ClickRect(tile0);Check(f.settings.Slots[0].TileColor==red&&Settings.Load().Slots[0].TileColor==red,"Clicking a tile colors it and saves");
   ClickRect(tile0);Check(f.settings.Slots[0].TileColor==0,"Same color again removes it");
   ClickRect(tile0);ClickRect(ReferenceDeck.SwatchBounds(3));ClickRect(tile1);ClickRect(ReferenceDeck.SwatchBounds(6));ClickRect(tile2);ClickRect(ReferenceDeck.SwatchBounds(9));ClickRect(f.deck.TileBounds(3));
   Check(f.settings.Slots[1].TileColor==yellow&&f.settings.Slots[2].TileColor==blue&&f.settings.Slots[3].TileColor==white,"Several tiles keep their own colors");
   ClickRect(ReferenceDeck.SwatchBounds(4));ClickRect(f.deck.TileBounds(4));ClickRect(ReferenceDeck.SwatchBounds(7));ClickRect(f.deck.TileBounds(5));ClickRect(ReferenceDeck.SwatchBounds(0));ClickRect(f.deck.TileBounds(6));ClickRect(ReferenceDeck.SwatchBounds(2));ClickRect(f.deck.TileBounds(7));
   using(var b=Paint("colors-rim")){var edge=At(b,tile0.X+2.5f,tile0.Y+60);var inner=At(b,tile0.Right-20,tile0.Bottom-10);Check(edge.R>200&&edge.G<130&&inner.R<60&&inner.G<70,"Border mode: colored rim, dark tile inside");}
   ClickRect(ReferenceDeck.FillModeBounds);Check(f.settings.TileFill&&Settings.Load().TileFill,"Vollflächig switches and saves");
   using(var b=Paint("colors-fill")){var inner=At(b,tile0.Right-20,tile0.Bottom-10);Check(inner.R>170&&inner.G<110,"Filled mode: whole tile is red");
    var y=At(b,tile1.Right-20,tile1.Bottom-10);Check(y.R>190&&y.G>160&&y.B<110,"Filled mode: yellow tile is yellow");
    int dark=0;for(float px=tile1.X+19;px<tile1.X+160;px+=1)for(float py=tile1.Y+74;py<tile1.Y+100;py+=1){var c=At(b,px,py);if(c.R+c.G+c.B<180)dark++;}Check(dark>40,"Name on the yellow tile is drawn in dark text");
    var plain=At(b,f.deck.TileBounds(20).Right-20,f.deck.TileBounds(20).Bottom-10);Check(plain.R<60,"Tiles without a color stay dark in filled mode");}
   var moving=f.settings.Slots[0];Check(f.MoveSound(0,9)&&f.settings.Slots[9].TileColor==red&&Settings.Load().Slots[9].TileColor==red,"Color moves with the sound when reordering");f.MoveSound(9,0);
   Mouse("OnMouseDown",tile2.X+40,tile2.Y+40);Mouse("OnMouseMove",f.deck.TileBounds(10).X+40,f.deck.TileBounds(10).Y+40);Mouse("OnMouseUp",f.deck.TileBounds(10).X+40,f.deck.TileBounds(10).Y+40);
   Check(f.settings.Slots[10].TileColor==blue&&f.settings.Slots.Count(s=>s.TileColor==blue)==1,"Dragging moves without painting");f.MoveSound(10,2);
   var payload=JsonSerializer.SerializeToElement(f.Dispatch("status",0));Check(payload.GetProperty("slots").GetArrayLength()==100&&payload.GetProperty("slots")[0].GetProperty("name").GetString()=="Klang 1","Phone status protocol unchanged");
   var colorStatus=JsonSerializer.SerializeToElement(f.Dispatch("status",0));Check(colorStatus.GetProperty("colorSync").GetBoolean()&&colorStatus.GetProperty("tileFill").GetBoolean()&&colorStatus.GetProperty("slots")[1].GetProperty("color").GetInt32()==yellow,"Status carries PC colors and mode for the phone");
   f.Dispatch("tile-color",15*16+7);Check(f.settings.Slots[15].TileColor==TilePalette.Colors[6]&&Settings.Load().Slots[15].TileColor==TilePalette.Colors[6],"Color from the phone is saved on the PC");
   using(var b=Paint("colors-from-phone")){var c=At(b,f.deck.TileBounds(15).Right-20,f.deck.TileBounds(15).Bottom-10);Check(c.B>200&&c.R<120,"Color from the phone is drawn on the PC tile");}f.Dispatch("tile-color",15*16);Check(f.settings.Slots[15].TileColor==0,"Phone removes the color on the PC");
   f.Dispatch("tile-fill",0);Check(!f.settings.TileFill,"Rand from the phone switches the PC");f.Dispatch("tile-fill",1);
   ClickRect(ReferenceDeck.RimModeBounds);Check(!f.settings.TileFill&&!Settings.Load().TileFill,"Rand switches back and saves");
   ClickRect(ReferenceDeck.FillModeBounds);ClickRect(f.deck.ReorderButtonBounds);Check(!f.sorting&&!Names().Any(n=>n.StartsWith("Farbe ")),"Done hides the colors");
   ClickRect(f.deck.TileBounds(8));Check(f.settings.Slots[8].TileColor==0,"Normal click outside edit mode never paints");f.audio.Stop();
   using(var b=Paint("colors-fill-normal")){var inner=At(b,tile1.Right-20,tile1.Bottom-10);Check(inner.R>190&&inner.G>160,"Filled colors stay visible after leaving edit mode");}
   f.Dispose();using var again=new MainForm();Check(again.settings.TileFill&&again.settings.Slots[1].TileColor==yellow,"Colors and mode survive a restart");
   result=new{ok=true,checks};
  }catch(Exception e){result=new{ok=false,error=e.ToString(),checks};Environment.ExitCode=1;}
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"color-test.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
 }
}
