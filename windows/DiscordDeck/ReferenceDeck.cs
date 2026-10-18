using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 ReferenceDeck deck=null!; int selectedSound,bank; int scrollRow;
 readonly NumericUpDown selectedGain=new(){Minimum=0,Maximum=400,Increment=5,BorderStyle=BorderStyle.FixedSingle};
 bool syncing;
 void BuildReferenceLayout(){
  BackColor=Color.FromArgb(12,18,22);
  deck=new ReferenceDeck(this){Dock=DockStyle.Fill};Controls.Add(deck);
  foreach(var combo in new[]{input,output,monitorOutput}){combo.Visible=false;combo.Parent=deck;combo.DropDownStyle=ComboBoxStyle.DropDownList;combo.FlatStyle=FlatStyle.Flat;combo.BackColor=Color.FromArgb(27,35,41);combo.ForeColor=Color.FromArgb(242,242,237);combo.Font=new Font("Segoe UI",9);}
  selectedGain.Visible=false;selectedGain.Parent=deck;selectedGain.BackColor=Color.FromArgb(20,28,34);selectedGain.ForeColor=Color.White;selectedGain.Font=new Font("Segoe UI",11);
  selectedGain.ValueChanged+=(_,_)=>{if(syncing)return;settings.Slots[selectedSound].Volume=(float)selectedGain.Value/100;audio.SetSlotVolume(settings.Slots[selectedSound]);settings.Save();deck.Invalidate();};
  soundVolume=new TrackBar{Minimum=0,Maximum=100,Value=(int)Math.Round(settings.MasterVolume*100)};
  soundVolume.ValueChanged+=(_,_)=>{settings.MasterVolume=soundVolume.Value/100f;audio.SetVolume(settings.MasterVolume,settings.MicrophoneVolume);settings.Save();deck.Invalidate();};
  status.TextChanged+=(_,_)=>deck.Invalidate();
  deck.Resize+=(_,_)=>PlaceReferenceControls();PlaceReferenceControls();SyncSelected();
  start.Click+=(_,_)=>ToggleAudio();
 }
 void PlaceReferenceControls(){if(deck==null)return;float x=deck.Width/1536f,y=deck.Height/1024f;void Place(Control c,int a,int b,int w,int h){c.Bounds=new Rectangle((int)(a*x),(int)(b*y),(int)(w*x),(int)(h*y));}
  Place(output,1194,579,319,35);Place(input,1194,653,319,35);Place(monitorOutput,1194,805,319,35);Place(selectedGain,1416,357,97,35);
 }
 void SelectSound(int index){selectedSound=index;SyncSelected();deck.Invalidate();}
 void SyncSelected(){syncing=true;selectedGain.Value=(decimal)Math.Round(Math.Clamp(settings.Slots[selectedSound].Volume,0,4)*100);syncing=false;}
 void ToggleAudio(){Try(()=>{if(audio.Running){audio.Stop();settings.AudioEnabled=false;settings.Save();input.Enabled=output.Enabled=mic.Enabled=monitorOutput.Enabled=monitorCheck.Enabled=true;start.Text="Audio starten";status.Text="Audio ist aus";}else StartAudio();});deck.Invalidate();}
 void SetGain(float value){selectedGain.Value=(decimal)Math.Clamp(Math.Round(value),0,400);}
 void SetMaster(float value){soundVolume!.Value=(int)Math.Clamp(Math.Round(value),0,100);}
 void ChooseFile(){Try(()=>{using var picker=new OpenFileDialog{Filter="Audio|*.wav;*.mp3;*.flac;*.aiff;*.wma;*.m4a|Alle Dateien|*.*",Title=$"Sound {selectedSound+1} zuweisen"};if(picker.ShowDialog(this)!=DialogResult.OK)return;using var probe=new AudioFileReader(picker.FileName);if(probe.TotalTime.TotalSeconds<=0)throw new InvalidDataException("Audiodatei ist leer.");var slot=settings.Slots[selectedSound];slot.File=picker.FileName;slot.Name=Path.GetFileNameWithoutExtension(slot.File);slot.SourceUrl="";settings.Save();deck.Invalidate();});}
 async void LinkSound(){int index=selectedSound;var slot=settings.Slots[index];using var dialog=new Form{Text="MyInstants verknüpfen",Size=new Size(650,180),StartPosition=FormStartPosition.CenterParent};var url=new TextBox{Dock=DockStyle.Top,Text=slot.SourceUrl,PlaceholderText="https://www.myinstants.com/.../instant/..."};var save=new Button{Text="Verknüpfen",Dock=DockStyle.Bottom,DialogResult=DialogResult.OK};dialog.Controls.Add(url);dialog.Controls.Add(save);dialog.AcceptButton=save;if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{var result=await MyInstants.Import(url.Text.Trim());slot.File=result.File;slot.Name=result.Name;slot.SourceUrl=result.Source;settings.Save();deck.Invalidate();}catch(Exception e){status.Text=e.Message;}}
 void PairPhone(){Try(()=>{if(lan==null)throw new Exception("LAN-Server ist nicht aktiv.");MessageBox.Show(this,"Code in der Android-App eingeben (2 Minuten gültig):\n\n"+lan.BeginPairing()+"\n\nPC und Handy müssen im selben lokalen Netz sein.","Handy verbinden");});}
 void ToggleRouting(bool microphone){if(audio.Running){audio.Stop();}if(microphone)mic.Checked=!mic.Checked;else monitorCheck.Checked=!monitorCheck.Checked;ReadSettings();settings.Save();if(settings.AudioEnabled)Try(StartAudio);deck.Invalidate();}
 sealed class ReferenceDeck:Control {
  readonly MainForm owner; readonly Image reference; readonly List<(RectangleF rect,Action action)> hits=new();int dragging;PointF mouse;int hover=-1;
  static Color C(int r,int g,int b)=>Color.FromArgb(r,g,b);
  readonly Color ink=C(240,239,231),muted=C(170,190,210),line=C(43,54,63),green=C(189,224,157);
  public ReferenceDeck(MainForm form){owner=form;DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);using var stream=typeof(MainForm).Assembly.GetManifestResourceStream("DiscordDeck.reference.png")!;using var source=Image.FromStream(stream);reference=new Bitmap(source);TabStop=true;AccessibleName="Sardinöööses Discord Deck Soundboard";}
  protected override void Dispose(bool disposing){if(disposing)reference.Dispose();base.Dispose(disposing);}
  PointF Logical(Point p)=>new(p.X*1536f/Width,p.Y*1024f/Height);
  static GraphicsPath Shape(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
  void Box(Graphics g,RectangleF r,Color top,Color bottom,float radius=9,Color? border=null){using var path=Shape(r,radius);using var b=new LinearGradientBrush(r,top,bottom,90);g.FillPath(b,path);if(border.HasValue){using var pen=new Pen(border.Value);g.DrawPath(pen,path);}}
  void TextAt(Graphics g,string text,float x,float y,float size,Color color,bool bold=false,float width=1000,float height=65){using var font=new Font(bold?(size>=45?"Segoe UI Semibold":"Bahnschrift SemiCondensed"):"Segoe UI",size,FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(color);using var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};g.DrawString(text,font,brush,new RectangleF(x,y,width,height),format);}
  void Line(Graphics g,float x,float y,float xx,float yy){using var pen=new Pen(line);g.DrawLine(pen,x,y,xx,yy);}
  void Hit(RectangleF rect,Action action)=>hits.Add((rect,action));
  void Button(Graphics g,RectangleF r,string text,Action action,bool danger=false){Box(g,r,danger?C(250,100,87):C(28,36,42),danger?C(239,86,77):C(22,29,35),8,danger?C(255,128,111):C(64,77,87));TextAt(g,text,r.X+14,r.Y+10,15,ink,width:r.Width-16);Hit(r,action);}
  void Wave(Graphics g,float x,float y,float width,float height,int seed,bool active){var rng=new Random(seed+71);int n=(int)(width/5);for(int i=0;i<n;i++){float t=i/(float)n;float envelope=(float)(Math.Exp(-Math.Pow((t-.26)*4,2))*.85+.06);float h=Math.Max(2,(float)rng.NextDouble()*height*envelope);using var p=new Pen(active&&t<.36?green:C(88,108,126),2);g.DrawLine(p,x+i*5,y-h/2,x+i*5,y+h/2);}}
  void Slider(Graphics g,float x,float y,float w,float ratio){using var p=new Pen(C(53,63,73),12){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(p,x,y,x+w,y);using var a=new Pen(green,12){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(a,x,y,x+w*Math.Clamp(ratio,0,1),y);using var b=new SolidBrush(ink);g.FillEllipse(b,x+w*Math.Clamp(ratio,0,1)-12,y-12,24,24);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(Width/1536f,Height/1024f);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;hits.Clear();
   using(var b=new LinearGradientBrush(new Rectangle(0,0,1536,1024),C(16,23,28),C(8,14,18),45))g.FillRectangle(b,0,0,1536,1024);
   using(var b=new LinearGradientBrush(new Rectangle(0,0,1536,62),C(24,31,37),C(16,23,28),90))g.FillRectangle(b,0,0,1536,62);
   Line(g,0,62,1536,62);Line(g,247,62,247,1024);Line(g,1169,62,1169,908);Line(g,247,908,1536,908);
   // The two sardine illustrations are taken from the supplied design itself.
   g.DrawImage(reference,new RectangleF(14,8,107,43),new RectangleF(14,8,107,43),GraphicsUnit.Pixel);
   TextAt(g,"Sardinöööses",142,14,26,ink,true,width:192);TextAt(g,"D I S C O R D  D E C K",345,24,12,muted);
   Button(g,new(1031,11,170,39),"▯  Handy verbinden",owner.PairPhone);Button(g,new(1216,11,122,39),"MyInstants  ↗",()=>SoundCatalog.CreateButton().PerformClick());Line(g,1358,12,1358,49);
   TextAt(g,"—",1387,16,24,ink);Hit(new(1368,0,52,61),()=>owner.WindowState=FormWindowState.Minimized);
   TextAt(g,"□",1442,16,23,ink);Hit(new(1420,0,53,61),()=>owner.WindowState=owner.WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized);
   TextAt(g,"×",1493,16,24,ink);Hit(new(1473,0,63,61),()=>owner.Close());
   TextAt(g,"S O U N D - B Ä N K E",21,91,12,muted);
   for(int i=0;i<5;i++){int page=i;bool on=owner.bank==i;var r=new RectangleF(14,123+i*64,219,47);Box(g,r,on?C(195,226,164):C(24,33,39),on?C(179,215,149):C(18,26,32),10);Box(g,new(26,r.Y+8,37,32),on?C(122,150,102):C(39,49,58),on?C(92,120,74):C(29,38,46),5,on?C(150,182,125):C(70,84,97));TextAt(g,$"{i+1:00}",34,r.Y+11,17,on?C(19,29,23):ink,true);TextAt(g,$"Sounds {i*20+1}–{i*20+20}",82,r.Y+12,16,on?C(10,19,14):ink);Hit(r,()=>{owner.bank=page;owner.scrollRow=0;owner.SelectSound(page*20);});}
   Line(g,21,687,228,687);Button(g,new(19,707,211,48),"＋   YouTube / TikTok",()=>YouTubeImport.Show(owner,owner.settings,()=>{owner.SyncSelected();Invalidate();}));Button(g,new(19,769,211,48),"⟳   Geräte neu laden",()=>{if(owner.audio.Running){owner.status.Text="Audio erst beenden, dann Geräte neu laden.";return;}owner.ReloadDevices();Invalidate();});
   g.DrawImage(reference,new RectangleF(40,888,140,57),new RectangleF(40,888,140,57),GraphicsUnit.Pixel);TextAt(g,"D E I N E  S O U N D S.",38,954,12,muted);TextAt(g,"D E I N  M O M E N T.",38,973,12,muted);
   TextAt(g,"D E I N E  S O U N D B A N K",276,87,12,muted);TextAt(g,"Auf Knopfdruck.",273,112,50,ink,true);TextAt(g,$"{owner.bank*20+1:00} — {owner.bank*20+20:00}",681,127,36,C(102,116,137));
   Button(g,new(932,116,214,50),"■   Alle Sounds stoppen",()=>{owner.audio.StopSounds();owner.status.Text="Sounds gestoppt · Mikrofon bleibt an";Invalidate();},true);
   var clip=g.Save();g.SetClip(new RectangleF(263,190,895,699));
   for(int j=0;j<20;j++){int index=owner.bank*20+j;int row=j/3-owner.scrollRow;float x=273+(j%3==0?0:j%3==1?306:602),y=195+row*135.5f;var r=new RectangleF(x,y,j%3==0?292:283,120);if(y<190||y>858)continue;var slot=owner.settings.Slots[index];bool selected=index==owner.selectedSound;Box(g,r,selected?C(40,55,39):C(26,34,40),selected?C(24,36,30):C(18,26,31),12,selected?green:C(39,51,60));TextAt(g,$"{index+1:00}",x+19,y+16,17,ink);Wave(g,x+61,y+39,116,42,index,selected);TextAt(g,"⚙",x+(j%3==0?254:244),y+12,22,muted);TextAt(g,slot.Name,x+19,y+72,20,ink,true,203,34);
    using(var p=new Pen(selected?green:C(107,119,129),1.5f))g.DrawEllipse(p,x+(j%3==0?228:218),y+59,49,49);using(var b=new SolidBrush(ink))g.FillPolygon(b,new[]{new PointF(x+(j%3==0?248:238),y+75),new PointF(x+(j%3==0?248:238),y+92),new PointF(x+(j%3==0?261:251),y+83.5f)});
    Hit(new(x+(j%3==0?228:218),y+59,49,49),()=>owner.Try(()=>owner.Play(index)));Hit(new(x+(j%3==0?249:239),y+7,33,33),()=>owner.SelectSound(index));Hit(new(x,y,216,120),()=>owner.SelectSound(index));
   }g.Restore(clip);
   if(owner.scrollRow<2){TextAt(g,"⌄",1137,862,17,muted);Hit(new(1136,840,24,48),()=>{owner.scrollRow++;Invalidate();});}if(owner.scrollRow>0){Hit(new(1136,190,24,38),()=>{owner.scrollRow--;Invalidate();});}
   var selectedSlot=owner.settings.Slots[owner.selectedSound];TextAt(g,"S O U N D - E I N S T E L L U N G E N",1194,91,12,muted);TextAt(g,$"SLOT {owner.selectedSound+1:00}",1194,122,12,muted);TextAt(g,selectedSlot.Name,1194,144,27,ink,true,292);TextAt(g,"⚙",1492,123,22,muted);Wave(g,1195,219,317,53,owner.selectedSound,true);
   TextAt(g,"D A T E I",1194,266,12,muted);TextAt(g,string.IsNullOrEmpty(selectedSlot.File)?"Noch keine Datei":Path.GetFileName(selectedSlot.File),1194,294,15,ink,width:153);Button(g,new(1352,282,165,41),"▱  Datei auswählen",owner.ChooseFile);
   Line(g,1194,341,1512,341);TextAt(g,"Pegel",1194,365,19,ink);Box(g,new(1416,357,97,35),C(20,28,34),C(16,23,29),5,C(57,70,81));TextAt(g,$"{owner.selectedGain.Value:0}   %",1426,362,18,ink,width:72);TextAt(g,"▴",1494,357,12,ink);TextAt(g,"▾",1494,375,12,ink);Hit(new(1489,357,24,17),()=>owner.SetGain((float)owner.selectedGain.Value+5));Hit(new(1489,374,24,18),()=>owner.SetGain((float)owner.selectedGain.Value-5));Hit(new(1416,357,73,35),()=>EditGain());Slider(g,1200,412,306,GainPosition(selectedSlot.Volume*100));Hit(new(1188,398,330,30),()=>owner.SetGain(GainValue((mouse.X-1200)/306)));
   TextAt(g,"MyInstants-Link  ↗",1194,448,16,C(155,211,245));using(var p=new Pen(C(155,211,245)))g.DrawLine(p,1194,468,1320,468);Hit(new(1194,441,180,36),owner.LinkSound);
   Line(g,1194,498,1512,498);TextAt(g,"A U D I O - R O U T I N G",1194,525,12,muted);TextAt(g,"Discord-Ausgabe",1194,555,15,muted);TextAt(g,"Mikrofon",1194,629,15,muted);
   Toggle(g,1194,704,owner.mic.Checked);TextAt(g,"Mikrofon beimischen",1250,708,15,ink);Hit(new(1190,697,323,35),()=>owner.ToggleRouting(true));Toggle(g,1194,739,owner.monitorCheck.Checked);TextAt(g,"Sounds selbst mithören",1250,743,15,ink);Hit(new(1190,733,323,34),()=>owner.ToggleRouting(false));TextAt(g,"Mithören über",1194,780,15,muted);Device(g,owner.output,new(1194,579,319,35));Device(g,owner.input,new(1194,653,319,35));Device(g,owner.monitorOutput,new(1194,805,319,35));TextAt(g,"ⓘ",1197,856,22,muted);TextAt(g,"Discord: virtuelles Mikrofon verwenden ·",1232,861,13,muted);TextAt(g,"Rauschunterdrückung für Sounds ausschalten",1232,879,13,muted);
   using(var b=new SolidBrush(owner.audio.Running?green:C(116,132,148)))g.FillEllipse(b,273,944,20,20);TextAt(g,owner.audio.Running?"Audio aktiv":"Audio bereit",314,940,20,owner.audio.Running?green:ink,true);TextAt(g,owner.audio.Running?"Virtueller Eingang verbunden":"Zum Starten Audio aktivieren",314,968,16,muted,width:250);
   Line(g,573,927,573,996);TextAt(g,"◖))",617,946,25,ink);TextAt(g,"Sound-Lautstärke",665,952,15,ink);Slider(g,811,961,401,owner.settings.MasterVolume);Hit(new(799,942,426,39),()=>owner.SetMaster((mouse.X-811)/401*100));Button(g,new(1302,938,209,48),owner.audio.Running?"⏻    Audio beenden":"⏻    Audio starten",owner.ToggleAudio);
   if(owner.status.Text.Contains("fehl",StringComparison.OrdinalIgnoreCase)||owner.status.Text.Contains("erst",StringComparison.OrdinalIgnoreCase)||owner.status.Text.StartsWith("Spielt:"))TextAt(g,owner.status.Text,280,887,12,muted,width:850,height:18);
  }
  static float GainPosition(float value)=>value<=60?value/120:.5f+(value-60)/680;
  static float GainValue(float position)=>position<=.5f?position*120:60+(position-.5f)*680;
  void Device(Graphics g,ComboBox combo,RectangleF r){Box(g,r,C(28,37,44),C(23,31,37),5,C(64,77,87));TextAt(g,(combo.SelectedItem as AudioDevice)?.Name??"Kein Gerät verfügbar",r.X+12,r.Y+8,14,ink,width:r.Width-37,height:25);TextAt(g,"⌄",r.Right-23,r.Y+6,18,muted);Hit(r,()=>{if(!combo.Enabled)return;var menu=new ContextMenuStrip{BackColor=C(25,34,41),ForeColor=ink,ShowImageMargin=false};foreach(AudioDevice device in combo.Items){var item=menu.Items.Add(device.Name);item.Click+=(_,_)=>{combo.SelectedItem=device;owner.ReadSettings();owner.settings.Save();Invalidate();};}menu.Closed+=(_,_)=>menu.Dispose();menu.Show(this,new Point((int)(r.X*Width/1536),(int)(r.Bottom*Height/1024)));});}
  void EditGain(){using var dialog=new Form{Text="Sound-Pegel (0–400 %)",ClientSize=new Size(260,110),StartPosition=FormStartPosition.CenterParent};var value=new NumericUpDown{Minimum=0,Maximum=400,Value=owner.selectedGain.Value,Location=new Point(20,18),Width=220};var save=new Button{Text="Übernehmen",Location=new Point(120,65),Width=120,DialogResult=DialogResult.OK};dialog.Controls.Add(value);dialog.Controls.Add(save);dialog.AcceptButton=save;if(dialog.ShowDialog(owner)==DialogResult.OK)owner.SetGain((float)value.Value);}
  void Toggle(Graphics g,float x,float y,bool on){Box(g,new(x,y,42,24),on?green:C(75,85,93),on?C(147,187,116):C(54,65,73),12);using var b=new SolidBrush(ink);g.FillEllipse(b,x+(on?22:3),y+3,18,18);}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();mouse=Logical(e.Location);if(e.Button!=MouseButtons.Left)return;if(mouse.Y<61&&mouse.X<1020){Native.ReleaseCapture();Native.SendMessage(owner.Handle,0xA1,(IntPtr)2,IntPtr.Zero);return;}if(new RectangleF(1188,398,330,30).Contains(mouse))dragging=1;else if(new RectangleF(799,942,426,39).Contains(mouse))dragging=2;Capture=dragging!=0;foreach(var h in hits)if(h.rect.Contains(mouse)){h.action();break;}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);mouse=Logical(e.Location);if(dragging==1)owner.SetGain(GainValue((mouse.X-1200)/306));if(dragging==2)owner.SetMaster((mouse.X-811)/401*100);int hit=hits.FindIndex(h=>h.rect.Contains(mouse));if(hit!=hover){hover=hit;Cursor=hit>=0?Cursors.Hand:Cursors.Default;}}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=0;Capture=false;}
  protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);var p=Logical(e.Location);if(p.X>247&&p.X<1169){owner.scrollRow=Math.Clamp(owner.scrollRow+(e.Delta<0?1:-1),0,2);Invalidate();}}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Escape){owner.audio.StopSounds();Invalidate();}if(e.KeyCode==Keys.Space){owner.Try(()=>owner.Play(owner.selectedSound));e.Handled=true;}if(e.KeyCode==Keys.Down){owner.scrollRow=Math.Min(2,owner.scrollRow+1);Invalidate();}if(e.KeyCode==Keys.Up){owner.scrollRow=Math.Max(0,owner.scrollRow-1);Invalidate();}}
 }
 static class Native{[DllImport("user32.dll")]public static extern bool ReleaseCapture();[DllImport("user32.dll")]public static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);}
}
