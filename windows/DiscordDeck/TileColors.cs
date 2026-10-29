using System.Drawing.Drawing2D;
using static DiscordDeck.Lang;
namespace DiscordDeck;

// Dieselben zehn Kachelfarben wie in der Android-App. 0 heißt: keine Farbe.
static class TilePalette {
 internal static readonly int[] Colors={unchecked((int)0xFFE8C87A),unchecked((int)0xFFF0605A),unchecked((int)0xFFFF9F43),unchecked((int)0xFFF6E05E),unchecked((int)0xFF3DDC97),unchecked((int)0xFF2EC4B6),unchecked((int)0xFF5B8DEF),unchecked((int)0xFFB07CFF),unchecked((int)0xFFFF6B9A),unchecked((int)0xFFF4F7FA)};
 internal static readonly string[] NamesDe={"Gold","Rot","Orange","Gelb","Grün","Türkis","Blau","Lila","Pink","Weiß"},NamesEn={"Gold","Red","Orange","Yellow","Green","Teal","Blue","Purple","Pink","White"};
 internal static bool Known(int c)=>c!=0&&Array.IndexOf(Colors,c)>=0;
 internal static int Toggle(int current,int selected)=>!Known(selected)?current:current==selected?0:selected;
 internal static string Name(int c){int i=Array.IndexOf(Colors,c);return i<0?"":T(NamesDe[i],NamesEn[i]);}
 // Kontrast nach WCAG: dunkle Schrift, sobald sie auf der Füllfarbe stärker absticht als weiße.
 static double Lin(int v){double s=v/255.0;return s<=.03928?s/12.92:Math.Pow((s+.055)/1.055,2.4);}
 internal static double Luminance(int c)=>.2126*Lin((c>>16)&255)+.7152*Lin((c>>8)&255)+.0722*Lin(c&255);
 internal static bool DarkText(int c){double l=Luminance(c);return (l+.05)/.05>=1.05/(l+.05);}
 internal static Color TextOn(int c)=>DarkText(c)?Color.FromArgb(22,18,12):Color.White;
 internal static double Contrast(int fill,Color text){double a=Luminance(fill),b=Luminance(text.ToArgb());return (Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);}
 internal static Color Shade(Color c,float f)=>Color.FromArgb(c.A,(int)(c.R*f),(int)(c.G*f),(int)(c.B*f));
}

public sealed partial class MainForm {
 internal int selectedTileColor=TilePalette.Colors[0];
 internal void PaintTile(int index){
  if(index<0||index>=settings.Slots.Count)return;var slot=settings.Slots[index];int before=slot.TileColor;slot.TileColor=TilePalette.Toggle(before,selectedTileColor);
  try{settings.Save();}catch{slot.TileColor=before;throw;}
  status.Text="Sortieren: "+(slot.TileColor==0?T($"Farbe von „{slot.Name}“ entfernt.",$"Removed the color from “{slot.Name}”."):T($"„{slot.Name}“ ist jetzt {TilePalette.Name(slot.TileColor)}.",$"“{slot.Name}” is now {TilePalette.Name(slot.TileColor)}."));deck.Invalidate();
 }
 internal void ChooseTileColor(int color){if(!TilePalette.Known(color))return;selectedTileColor=color;status.Text="Sortieren: "+T($"Farbe {TilePalette.Name(color)} gewählt. Kacheln anklicken zum Färben, nochmal anklicken entfernt die Farbe.",$"{TilePalette.Name(color)} selected. Click tiles to color them, click again to remove the color.");deck.Invalidate();}
 // Vom Handy: Farbe direkt setzen (kein Umschalten), damit doppelte Anfragen nichts verdrehen.
 internal object SetTileColorFromPhone(int index,int palette){
  if(index<0||index>=settings.Slots.Count||palette<0||palette>TilePalette.Colors.Length)throw new ArgumentOutOfRangeException(nameof(palette));
  var slot=settings.Slots[index];int before=slot.TileColor,color=palette==0?0:TilePalette.Colors[palette-1];
  if(before!=color){slot.TileColor=color;try{settings.Save();}catch{slot.TileColor=before;throw;}deck.Invalidate();}
  return new{ok=true,soundId=slot.Id,color};
 }
 internal object SetTileFillFromPhone(bool fill){if(settings.TileFill!=fill){settings.TileFill=fill;try{settings.Save();}catch{settings.TileFill=!fill;throw;}deck.Invalidate();}return new{ok=true,tileFill=fill};}
 internal void SetTileFill(bool fill){if(settings.TileFill==fill)return;settings.TileFill=fill;settings.Save();status.Text="Sortieren: "+(fill?T("Farbe füllt jetzt die ganze Kachel.","The color now fills the whole tile."):T("Farbe nur als Rand.","Color only as a border."));deck.Invalidate();}

 sealed partial class ReferenceDeck {
  internal static RectangleF SwatchBounds(int i)=>new(19+i%5*45.25f,564+i/5*37,30,30);
  internal static RectangleF RimModeBounds=>new(19,640,103,34);
  internal static RectangleF FillModeBounds=>new(127,640,103,34);
  // Farbauswahl unter Bearbeiten: zehn Farben und der Schalter Rand oder Vollflächig.
  void DrawTileColors(Graphics g){
   if(!owner.sorting)return;
   for(int i=0;i<TilePalette.Colors.Length;i++){int color=TilePalette.Colors[i];var r=SwatchBounds(i);bool on=color==owner.selectedTileColor;var c=Color.FromArgb(color);
    if(on){using var ring=new Pen(ink,2.5f);g.DrawEllipse(ring,RectangleF.Inflate(r,3,3));}
    using(var b=new SolidBrush(c))g.FillEllipse(b,r);using(var p=new Pen(on?TilePalette.TextOn(color):C(90,104,116),on?2:1))g.DrawEllipse(p,on?RectangleF.Inflate(r,-1,-1):r);
    Hit(r,()=>owner.ChooseTileColor(color),T("Farbe ","Color ")+TilePalette.Name(color));}
   Segment(g,RimModeBounds,T("Rand","Border"),!owner.settings.TileFill,()=>owner.SetTileFill(false));
   Segment(g,FillModeBounds,T("Vollflächig","Filled"),owner.settings.TileFill,()=>owner.SetTileFill(true));
  }
  void Segment(Graphics g,RectangleF r,string text,bool on,Action action){Box(g,r,on?C(236,206,134):C(28,36,42),on?C(214,180,104):C(22,29,35),8,on?C(244,222,166):C(64,77,87));using var font=new Font("Segoe UI",13,on?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(on?C(24,20,12):ink);using var fmt=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter};g.DrawString(text,font,brush,RectangleF.Inflate(r,-4,0),fmt);Hit(r,action,text);}
 }
}
