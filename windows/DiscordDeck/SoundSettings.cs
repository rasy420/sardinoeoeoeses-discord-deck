using System.Drawing.Drawing2D;
using System.Globalization;
using NAudio.Wave;
using static DiscordDeck.Lang;
namespace DiscordDeck;

public sealed partial class MainForm {
 TextBox? soundEditor;RectangleF editorLogical;float editorFontSize;string editorFontFamily="Segoe UI";
 void LayoutSoundEditor(){
  if(soundEditor==null||soundEditor.IsDisposed)return;var r=editorLogical;
  soundEditor.Bounds=new((int)(r.X*deck.Width/1536),(int)(r.Y*deck.Height/1024),(int)(r.Width*deck.Width/1536),(int)(r.Height*deck.Height/1024));var old=soundEditor.Font;soundEditor.Font=new Font(editorFontFamily,editorFontSize*deck.Height/1024f,FontStyle.Regular,GraphicsUnit.Pixel);old.Dispose();
 }
 internal void RenameSound(SoundSlot slot,string name){
  name=name.Trim();if(name.Length==0||name.Length>100||name.Any(char.IsControl))throw new ArgumentException(T("Bitte einen Namen mit 1 bis 100 Zeichen eingeben.","Please enter a name with 1 to 100 characters."));
  var before=(slot.Name,slot.CustomName);slot.Name=name;slot.CustomName=true;
  try{settings.Save();}catch{(slot.Name,slot.CustomName)=before;throw;}
  status.Text=T("Name gespeichert: ","Name saved: ")+name;deck.Invalidate();
 }
 internal void ReplaceSoundSource(SoundSlot slot,string file,string suggestedName,string source){
  using(var probe=new AudioFileReader(file))if(probe.TotalTime<=TimeSpan.Zero)throw new IOException(T("Audiodatei ist leer.","Audio file is empty."));
  var before=(slot.File,slot.Name,slot.SourceUrl,slot.TrimStart,slot.TrimEnd);
  slot.File=file;slot.SourceUrl=source;slot.TrimStart=slot.TrimEnd=0;if(!slot.CustomName)slot.Name=suggestedName;
  try{settings.Save();}catch{(slot.File,slot.Name,slot.SourceUrl,slot.TrimStart,slot.TrimEnd)=before;throw;}
  if(ReferenceEquals(slot,settings.Slots[selectedSound])){ForgetTrim();SyncSelected();}deck.Invalidate();
 }
 internal void EditSoundName(){
  var slot=settings.Slots[selectedSound];BeginSoundEdit(new(1194,143,269,35),slot.Name,text=>RenameSound(slot,text),27,"Bahnschrift SemiCondensed");
 }
 internal void EditPanelTime(bool start){
  if(PanelTrim() is not {} trim)return;var slot=settings.Slots[selectedSound];
  BeginSoundEdit(start?new(1232,252,49,22):new(1352,252,49,22),Seconds(start?trim.from:trim.to),text=>{
   if(!double.TryParse(text.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double value)||!double.IsFinite(value))throw new ArgumentException(T("Bitte eine gültige Sekundenangabe eingeben.","Please enter a valid time in seconds."));
   if(ReferenceEquals(slot,settings.Slots[selectedSound]))DragTrim(start,value);
  },13,"Segoe UI");
 }
 void BeginSoundEdit(RectangleF logical,string text,Action<string> commit,float size,string family){
  if(soundEditor!=null){soundEditor.Focus();return;}
  var editor=new TextBox{Text=text,MaxLength=100,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(23,31,37),ForeColor=Color.FromArgb(240,239,231),Font=new Font(family,size*deck.Height/1024f,FontStyle.Regular,GraphicsUnit.Pixel),AccessibleName=size>20?T("Soundname bearbeiten","Edit sound name"):T("Sekunden bearbeiten","Edit seconds")};
  soundEditor=editor;editorLogical=logical;editorFontSize=size;editorFontFamily=family;editor.Parent=deck;LayoutSoundEditor();bool closing=false;
  void Close(bool save,bool keepInvalid){
   if(closing)return;
   if(save)try{commit(editor.Text);}catch(Exception e){status.Text=T("Fehler: ","Error: ")+e.Message;deck.Invalidate();if(keepInvalid){editor.SelectAll();return;}}
   closing=true;soundEditor=null;var font=editor.Font;editor.Dispose();font.Dispose();deck.Invalidate();
  }
  editor.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Close(true,true);if(soundEditor==null)deck.Focus();}else if(e.KeyCode==Keys.Escape){e.SuppressKeyPress=true;Close(false,false);deck.Focus();}};
  editor.Leave+=(_,_)=>Close(true,false);editor.Focus();editor.SelectAll();
 }

 sealed partial class ReferenceDeck {
  readonly ContextMenuStrip replaceMenu=new(){ShowImageMargin=true,BackColor=C(25,34,41),ForeColor=C(240,239,231),Font=new Font("Segoe UI",9.5f),Padding=new Padding(5),Renderer=new ReplaceMenuRenderer()};
  sealed class ReplaceMenuRenderer:ToolStripProfessionalRenderer {
   protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){using var b=new LinearGradientBrush(e.AffectedBounds,C(31,41,49),C(23,31,38),90);e.Graphics.FillRectangle(b,e.AffectedBounds);}
   protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
   protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var shape=Shape(new(.5f,.5f,e.ToolStrip.Width-1,e.ToolStrip.Height-1),8);using var pen=new Pen(C(75,91,102),1.4f);e.Graphics.DrawPath(pen,shape);}
   protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){if(!e.Item.Selected)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var shape=Shape(new(2,1,e.Item.Width-4,e.Item.Height-2),5);using var b=new SolidBrush(C(52,59,58));e.Graphics.FillPath(b,shape);}
  }
  readonly Bitmap fileSymbol=SourceSymbol(false),linkSymbol=SourceSymbol(true);
  static Bitmap SourceSymbol(bool link){var b=new Bitmap(24,24);using var g=Graphics.FromImage(b);g.SmoothingMode=SmoothingMode.AntiAlias;using var pen=new Pen(C(170,190,210),1.8f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};if(!link)g.DrawPolygon(pen,new[]{new PointF(3,6),new PointF(10,6),new PointF(13,9),new PointF(21,9),new PointF(21,20),new PointF(3,20)});else{g.TranslateTransform(12,12);g.RotateTransform(43);g.DrawArc(pen,-4,-12,8,14,150,240);g.DrawArc(pen,-4,-2,8,14,-30,240);g.DrawLine(pen,0,-4,0,4);}return b;}
  internal ContextMenuStrip PrepareReplaceMenu(){
   foreach(var item in replaceMenu.Items.Cast<ToolStripItem>().ToArray())item.Dispose();replaceMenu.Items.Clear();
   replaceMenu.Items.Add(T("Audiodatei auswählen …","Choose audio file …"),fileSymbol,(_,_)=>owner.ChooseFile());
   replaceMenu.Items.Add(T("MyInstants-Link einfügen …","Paste MyInstants link …"),linkSymbol,(_,_)=>owner.LinkSound());
   foreach(ToolStripItem item in replaceMenu.Items)item.Padding=new Padding(4,4,8,4);
   return replaceMenu;
  }
  internal void OpenReplaceMenu(){PrepareReplaceMenu().Show(this,new Point((int)(1374*Width/1536f),(int)(328*Height/1024f)));}
  internal RectangleF NamePencil(Graphics g,string name){using var font=new Font("Bahnschrift SemiCondensed",27,FontStyle.Regular,GraphicsUnit.Pixel);float width=Math.Min(259,g.MeasureString(name,font,1000,StringFormat.GenericTypographic).Width);return new(1194+width+11,145,25,28);}
  void Pencil(Graphics g,RectangleF r){using var pen=new Pen(muted,1.5f){LineJoin=LineJoin.Round};float x=r.X+3,y=r.Y+4;g.DrawPolygon(pen,new[]{new PointF(x,y+19),new PointF(x+3,y+11),new PointF(x+15,y-1),new PointF(x+20,y+4),new PointF(x+8,y+16)});g.DrawLine(pen,x+3,y+11,x+8,y+16);g.DrawLine(pen,x+12,y+2,x+17,y+7);}
  void SourceRow(Graphics g,SoundSlot slot){
   TextAt(g,T("Q U E L L E","S O U R C E"),1194,295,12,muted);
   TextAt(g,string.IsNullOrEmpty(slot.File)?T("Noch keine Datei","No file yet"):Path.GetFileName(slot.File),1194,315,15,ink,width:170,height:24);
   var r=new RectangleF(1374,290,140,38);Box(g,r,C(36,46,54),C(26,35,42),8,C(79,96,108));TextAt(g,T("Sound ersetzen","Replace sound"),r.X+13,r.Y+10,14,ink,width:110,height:24);
   using var p=new Pen(muted,1.7f){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLines(p,new[]{new PointF(r.Right-23,r.Y+16),new PointF(r.Right-18,r.Y+21),new PointF(r.Right-13,r.Y+16)});Hit(r,OpenReplaceMenu);
  }
  void TimeFields(Graphics g){
   var trim=owner.PanelTrim();var color=trim.HasValue?ink:C(105,120,131);
   TextAt(g,"Start",1194,257,13,muted,width:34,height:22);TextAt(g,T("Ende","End"),1314,257,13,muted,width:34,height:22);
   void Field(RectangleF r,bool start,double seconds){
    var text=trim.HasValue?Seconds(seconds)+" s":"–";using var measure=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel);float fontSize=Math.Clamp(13*49/Math.Max(49,g.MeasureString(text,measure,1000,StringFormat.GenericTypographic).Width+3),9,13);
    Box(g,r,C(29,38,45),C(22,30,36),6,C(58,73,84));TextAt(g,text,r.X+8,r.Y+6+(13-fontSize)/2,fontSize,color,width:49,height:20);
    using(var b=new SolidBrush(C(29,39,46)))g.FillRectangle(b,r.Right-17,r.Y+3,14,r.Height-6);
    using var p=new Pen(trim.HasValue?muted:C(76,91,103),1.4f){StartCap=LineCap.Round,EndCap=LineCap.Round};float x=r.Right-10;g.DrawLines(p,new[]{new PointF(x-3,r.Y+9),new PointF(x,r.Y+6),new PointF(x+3,r.Y+9)});g.DrawLines(p,new[]{new PointF(x-3,r.Y+18),new PointF(x,r.Y+21),new PointF(x+3,r.Y+18)});
    if(!trim.HasValue)return;void Step(double delta){if(owner.PanelTrim() is {} now)owner.DragTrim(start,(start?now.from:now.to)+delta);}Hit(new(r.X,r.Y,r.Width-18,r.Height),()=>owner.EditPanelTime(start));Hit(new(r.Right-18,r.Y,18,r.Height/2),()=>Step(.01));Hit(new(r.Right-18,r.Y+r.Height/2,18,r.Height/2),()=>Step(-.01));
   }
   Field(new(1230,249,71,27),true,trim?.from??0);Field(new(1350,249,71,27),false,trim?.to??0);
   TextAt(g,T("Dauer: ","Time: ")+(trim.HasValue?Seconds(trim.Value.to-trim.Value.from)+" s":"–"),1438,257,12,muted,width:76,height:22);
  }
 }
}
