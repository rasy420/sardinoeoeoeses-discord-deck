using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using NAudio.Wave;
using static DiscordDeck.Lang;
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
 void SelectSound(int index){if(index!=selectedSound)ForgetTrim();selectedSound=index;SyncSelected();deck.Invalidate();}
 void SyncSelected(){syncing=true;selectedGain.Value=(decimal)Math.Round(Math.Clamp(settings.Slots[selectedSound].Volume,0,4)*100);syncing=false;}
 void ToggleAudio(){Try(()=>{if(audio.Running){audio.Stop();settings.AudioEnabled=false;settings.Save();input.Enabled=output.Enabled=mic.Enabled=monitorOutput.Enabled=monitorCheck.Enabled=true;start.Text=T("Audio starten","Start audio");status.Text=T("Audio ist aus","Audio is off");}else StartAudio();});deck.Invalidate();}
 void SetGain(float value){selectedGain.Value=(decimal)Math.Clamp(Math.Round(value),0,400);}
 void SetMaster(float value){soundVolume!.Value=(int)Math.Clamp(Math.Round(value),0,100);}
 void ChooseFile(){Try(()=>{using var picker=new OpenFileDialog{Filter=T("Audio","Audio")+"|*.wav;*.mp3;*.flac;*.aiff;*.wma;*.m4a|"+T("Alle Dateien","All files")+"|*.*",Title=T($"Sound {selectedSound+1} zuweisen",$"Assign sound {selectedSound+1}")};if(picker.ShowDialog(this)!=DialogResult.OK)return;using var probe=new AudioFileReader(picker.FileName);if(probe.TotalTime.TotalSeconds<=0)throw new InvalidDataException(T("Audiodatei ist leer.","Audio file is empty."));var slot=settings.Slots[selectedSound];slot.File=picker.FileName;slot.TrimStart=slot.TrimEnd=0;slot.Name=Path.GetFileNameWithoutExtension(slot.File);slot.SourceUrl="";settings.Save();deck.Invalidate();});}
 async void LinkSound(){int index=selectedSound;var slot=settings.Slots[index];using var dialog=new Form{Text=T("MyInstants verknüpfen","Link MyInstants"),Size=new Size(650,180),StartPosition=FormStartPosition.CenterParent};var url=new TextBox{Dock=DockStyle.Top,Text=slot.SourceUrl,PlaceholderText="https://www.myinstants.com/.../instant/..."};var save=new Button{Text=T("Verknüpfen","Link"),Dock=DockStyle.Bottom,DialogResult=DialogResult.OK};dialog.Controls.Add(url);dialog.Controls.Add(save);dialog.AcceptButton=save;if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{var result=await MyInstants.Import(url.Text.Trim());slot.File=result.File;slot.TrimStart=slot.TrimEnd=0;slot.Name=result.Name;slot.SourceUrl=result.Source;settings.Save();deck.Invalidate();}catch(Exception e){status.Text=e.Message;}}
 void PairPhone(){Try(()=>{if(lan==null)throw new Exception(T("LAN-Server ist nicht aktiv.","LAN server is not running."));MessageBox.Show(this,T("Code in der Android-App eingeben (2 Minuten gültig):","Enter this code in the Android app (valid for 2 minutes):")+"\n\n"+lan.BeginPairing()+"\n\n"+T("PC und Handy müssen im selben lokalen Netz sein.","PC and phone must be on the same local network."),T("Handy verbinden","Connect phone"));});}
 void ChangeDevice(ComboBox combo,AudioDevice device){combo.SelectedItem=device;bool running=audio.Running;ReadSettings();settings.Save();status.Text=T("Gewählt: ","Selected: ")+device.Name;CheckDiscord();if(running){audio.Stop();Try(StartAudio);}deck.Invalidate();}
 void ToggleRouting(bool microphone){if(audio.Running){audio.Stop();}if(microphone)mic.Checked=!mic.Checked;else monitorCheck.Checked=!monitorCheck.Checked;ReadSettings();settings.Save();if(settings.AudioEnabled)Try(StartAudio);deck.Invalidate();}
 sealed partial class ReferenceDeck:Control {
  readonly MainForm owner; readonly Image reference; readonly List<(RectangleF rect,Action action)> hits=new();int dragging;bool trimStart;PointF mouse;int hover=-1;
  static Color C(int r,int g,int b)=>Color.FromArgb(r,g,b);
  readonly Color ink=C(240,239,231),muted=C(170,190,210),line=C(43,54,63),green=C(232,200,122);
  public ReferenceDeck(MainForm form){owner=form;DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);using var stream=typeof(MainForm).Assembly.GetManifestResourceStream("DiscordDeck.reference.png")!;using var source=Image.FromStream(stream);reference=new Bitmap(source);TabStop=true;AccessibleName="Sardinöööses Soundboard";InitReorder();}
  readonly ContextMenuStrip deviceMenu=new(){BackColor=C(25,34,41),ForeColor=C(240,239,231),ShowImageMargin=false,ShowCheckMargin=true};
  protected override void Dispose(bool disposing){if(disposing){reorderTimer.Dispose();reference.Dispose();deviceMenu.Dispose();}base.Dispose(disposing);}
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
   TextAt(g,"Sardinöööses",140,8.5f,27,ink,true,width:200,height:34);{using var tf=new Font("Bahnschrift SemiCondensed",27,FontStyle.Regular,GraphicsUnit.Pixel);using var sf=new Font("Segoe UI",10.5f,FontStyle.Regular,GraphicsUnit.Pixel);using var sb=new SolidBrush(C(214,180,110));var tw=g.MeasureString("Sardinöööses",tf,1000,StringFormat.GenericTypographic).Width;const string sub="SOUNDBOARD";float x0=143,x1=140+tw-2,cw=g.MeasureString("D",sf,100,StringFormat.GenericTypographic).Width;for(int i=0;i<sub.Length;i++){var w=g.MeasureString(sub[i].ToString(),sf,100,StringFormat.GenericTypographic).Width;g.DrawString(sub[i].ToString(),sf,sb,x0+(x1-cw-x0)*i/(sub.Length-1)+(cw-w)/2,41.5f,StringFormat.GenericTypographic);}}
   Button(g,new(1031,11,170,39),T("▯  Handy verbinden","▯  Connect phone"),owner.PairPhone);Button(g,new(1216,11,122,39),"MyInstants  ↗",()=>SoundCatalog.CreateButton().PerformClick());Line(g,1358,12,1358,49);
   {var lang=new RectangleF(922,11,56,39);Box(g,lang,C(28,36,42),C(22,29,35),8,C(64,77,87));TextAt(g,"DE",lang.X+9,lang.Y+10,14,En?muted:green,true,width:22,height:20);TextAt(g,"/",lang.X+26,lang.Y+9,14,C(90,104,116),width:8,height:20);TextAt(g,"EN",lang.X+33,lang.Y+10,14,En?green:muted,true,width:22,height:20);Hit(lang,()=>owner.SetLanguage(!En));
    var help=new RectangleF(984,11,40,39);Box(g,help,C(28,36,42),C(22,29,35),8,C(64,77,87));TextAt(g,"?",help.X+14,help.Y+7,19,ink,true,width:20,height:26);Hit(help,owner.StartTour);}
   if(owner.discordWarning is {} warn){var w=new RectangleF(466,9,450,44);Box(g,w,C(176,42,42),C(140,30,32),8,C(236,92,84));TextAt(g,"⚠  "+warn.Title,w.X+12,w.Y+4,13,C(255,226,220),width:warn.OpensDiscord?320:426,height:18);TextAt(g,warn.Detail,w.X+12,w.Y+21,15,C(255,255,255),true,warn.OpensDiscord?330:426,22);if(warn.OpensDiscord){var o=new RectangleF(w.Right-98,w.Y+7,90,30);Box(g,o,C(236,88,80),C(214,64,60),6,C(255,150,140));TextAt(g,T("Öffnen  ↗","Open  ↗"),o.X+12,o.Y+6,14,C(255,255,255),width:80,height:20);Hit(w,owner.OpenDiscordVoice);}}
   TextAt(g,"—",1387,16,24,ink);Hit(new(1368,0,52,61),()=>owner.WindowState=FormWindowState.Minimized);
   TextAt(g,"□",1442,16,23,ink);Hit(new(1420,0,53,61),()=>owner.WindowState=owner.WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized);
   TextAt(g,"×",1493,16,24,ink);Hit(new(1473,0,63,61),()=>owner.Close());
   TextAt(g,T("S O U N D - B Ä N K E","S O U N D   B A N K S"),21,91,12,muted);
   for(int i=0;i<5;i++){int page=i;bool on=owner.bank==i;var r=new RectangleF(14,123+i*64,219,47);Box(g,r,on?C(236,206,134):C(24,33,39),on?C(226,192,116):C(18,26,32),10);Box(g,new(26,r.Y+8,37,32),on?C(164,134,76):C(39,49,58),on?C(128,102,52):C(29,38,46),5,on?C(200,168,100):C(70,84,97));TextAt(g,$"{i+1:00}",34,r.Y+11,17,on?C(29,26,19):ink,true);TextAt(g,$"Sounds {i*20+1}–{i*20+20}",82,r.Y+12,16,on?C(10,19,14):ink);Hit(r,()=>{owner.bank=page;owner.scrollRow=0;owner.SelectSound(page*20);});}
   Line(g,21,687,228,687);Button(g,new(19,707,211,48),"＋   YouTube / TikTok",()=>YouTubeImport.Show(owner,owner.settings,()=>{owner.SyncSelected();Invalidate();}));Button(g,new(19,769,211,48),T("⟳   Geräte neu laden","⟳   Reload devices"),()=>{if(owner.audio.Running){owner.status.Text=T("Audio erst beenden, dann Geräte neu laden.","Stop audio first, then reload devices.");return;}owner.ReloadDevices();Invalidate();});
   Button(g,new(19,831,211,42),owner.updating?T("Update wird geprüft …","Checking update …"):T("↓   Updates suchen","↓   Check for updates"),owner.CheckForUpdates);
   g.DrawImage(reference,new RectangleF(40,888,140,57),new RectangleF(40,888,140,57),GraphicsUnit.Pixel);TextAt(g,T("D E I N E  S O U N D S.","Y O U R  S O U N D S."),38,954,12,muted);TextAt(g,T("D E I N  M O M E N T.","Y O U R  M O M E N T."),38,973,12,muted);
   if(!owner.sorting)TextAt(g,T("D E I N E  S O U N D B A N K","Y O U R  S O U N D B A N K"),276,87,12,muted);TextAt(g,T("Auf Knopfdruck.","Push to play."),273,112,50,ink,true);TextAt(g,$"{owner.bank*20+1:00} — {owner.bank*20+20:00}",En?603:681,127,36,C(102,116,137));
   Button(g,new(932,76,214,31),owner.sorting?T("✓   Fertig","✓   Done"):T("↕   Umsortieren","↕   Reorder"),owner.ToggleSorting);
   Button(g,new(932,116,214,50),T("■   Alle Sounds stoppen","■   Stop all sounds"),()=>{owner.audio.StopSounds();owner.status.Text=T("Sounds gestoppt · Mikrofon bleibt an","Sounds stopped · microphone stays on");Invalidate();},true);
   var clip=g.Save();g.SetClip(new RectangleF(263,190,895,699));
   for(int j=0;j<20;j++){int index=owner.bank*20+j;int row=j/3-owner.scrollRow;float x=273+(j%3==0?0:j%3==1?306:602),y=195+row*135.5f;var r=new RectangleF(x,y,j%3==0?292:283,120);if(y<190||y>858)continue;var slot=owner.settings.Slots[index];bool selected=index==owner.selectedSound;Box(g,r,selected?C(56,48,30):C(26,34,40),selected?C(34,31,24):C(18,26,31),12,selected?green:C(39,51,60));TextAt(g,$"{index+1:00}",x+19,y+16,17,ink);Wave(g,x+61,y+39,116,42,index,selected);TextAt(g,owner.sorting?"↕":"⚙",x+(j%3==0?254:244),y+12,22,owner.sorting?green:muted);TextAt(g,slot.Name,x+19,y+72,20,ink,true,203,34);
    using(var p=new Pen(selected?green:C(107,119,129),1.5f))g.DrawEllipse(p,x+(j%3==0?228:218),y+59,49,49);using(var b=new SolidBrush(ink))g.FillPolygon(b,new[]{new PointF(x+(j%3==0?248:238),y+75),new PointF(x+(j%3==0?248:238),y+92),new PointF(x+(j%3==0?261:251),y+83.5f)});
    if(owner.sorting)continue;
    Hit(new(x+(j%3==0?228:218),y+59,49,49),()=>owner.Try(()=>owner.Play(index)));{var gear=new RectangleF(x+(j%3==0?249:239),y+7,33,33);Hit(gear,()=>owner.SoundMenu(index,PointToScreen(new Point((int)(gear.X*Width/1536),(int)(gear.Bottom*Height/1024)))));}Hit(new(x,y,216,120),()=>owner.SelectSound(index));
   }g.Restore(clip);
   if(owner.scrollRow<2){TextAt(g,"⌄",1137,862,17,muted);Hit(new(1136,840,24,48),()=>{owner.scrollRow++;Invalidate();});}if(owner.scrollRow>0){Hit(new(1136,190,24,38),()=>{owner.scrollRow--;Invalidate();});}
   var selectedSlot=owner.settings.Slots[owner.selectedSound];TextAt(g,T("S O U N D - E I N S T E L L U N G E N","S O U N D   S E T T I N G S"),1194,91,12,muted);TextAt(g,$"SLOT {owner.selectedSound+1:00}",1194,122,12,muted);TextAt(g,selectedSlot.Name,1194,144,27,ink,true,292);TextAt(g,"⚙",1492,123,22,muted);Hit(new(1486,118,32,34),()=>owner.SoundMenu(owner.selectedSound,PointToScreen(new Point(1486*Width/1536,152*Height/1024))));TrimWave(g,selectedSlot);
   TextAt(g,T("D A T E I","F I L E"),1194,272,12,muted);TextAt(g,string.IsNullOrEmpty(selectedSlot.File)?T("Noch keine Datei","No file yet"):Path.GetFileName(selectedSlot.File),1194,299,15,ink,width:153);Button(g,new(1352,288,165,40),T("▱  Datei auswählen","▱  Choose file"),owner.ChooseFile);
   Line(g,1194,341,1512,341);TextAt(g,T("Pegel","Level"),1194,365,19,ink);Box(g,new(1416,357,97,35),C(20,28,34),C(16,23,29),5,C(57,70,81));TextAt(g,$"{owner.selectedGain.Value:0}   %",1426,362,18,ink,width:72);TextAt(g,"▴",1494,357,12,ink);TextAt(g,"▾",1494,375,12,ink);Hit(new(1489,357,24,17),()=>owner.SetGain((float)owner.selectedGain.Value+5));Hit(new(1489,374,24,18),()=>owner.SetGain((float)owner.selectedGain.Value-5));Hit(new(1416,357,73,35),()=>EditGain());Slider(g,1200,412,306,GainPosition(selectedSlot.Volume*100));Hit(new(1188,398,330,30),()=>owner.SetGain(GainValue((mouse.X-1200)/306)));
   TrimRow(g);TextAt(g,T("oder MyInstants-Link ↗","or MyInstants link ↗"),1262,272,12,C(155,211,245));Hit(new(1258,266,150,22),owner.LinkSound);
   Line(g,1194,505,1512,505);TextAt(g,"A U D I O - R O U T I N G",1194,525,12,muted);TextAt(g,T("Discord-Ausgabe","Discord output"),1194,555,15,muted);TextAt(g,T("Mikrofon","Microphone"),1194,629,15,muted);
   Toggle(g,1194,704,owner.mic.Checked);TextAt(g,T("Mikrofon beimischen","Mix in microphone"),1250,708,15,ink);Hit(new(1190,697,323,35),()=>owner.ToggleRouting(true));Toggle(g,1194,739,owner.monitorCheck.Checked);TextAt(g,T("Sounds selbst mithören","Hear sounds yourself"),1250,743,15,ink);Hit(new(1190,733,323,34),()=>owner.ToggleRouting(false));TextAt(g,T("Mithören über","Listen on"),1194,780,15,muted);Device(g,owner.output,new(1194,579,319,35));Device(g,owner.input,new(1194,653,319,35));Device(g,owner.monitorOutput,new(1194,805,319,35));TextAt(g,"ⓘ",1197,856,22,muted);TextAt(g,T("Discord: virtuelles Mikrofon verwenden ·","Discord: use the virtual microphone ·"),1232,861,13,muted);TextAt(g,T("Rauschunterdrückung für Sounds ausschalten","turn off noise suppression for sounds"),1232,879,13,muted);
   using(var b=new SolidBrush(owner.audio.Running?green:C(116,132,148)))g.FillEllipse(b,273,944,20,20);TextAt(g,owner.audio.Running?T("Audio aktiv","Audio on"):T("Audio bereit","Audio ready"),314,940,20,owner.audio.Running?green:ink,true);TextAt(g,owner.audio.Running?T("Virtueller Eingang verbunden","Virtual input connected"):T("Zum Starten Audio aktivieren","Activate audio to start"),314,968,16,muted,width:250);
   Line(g,573,927,573,996);TextAt(g,"◖))",617,946,25,ink);TextAt(g,T("Sound-Lautstärke","Sound volume"),665,952,15,ink);Slider(g,811,961,401,owner.settings.MasterVolume);Hit(new(799,942,426,39),()=>owner.SetMaster((mouse.X-811)/401*100));Button(g,new(1302,938,209,48),owner.audio.Running?T("⏻    Audio beenden","⏻    Stop audio"):T("⏻    Audio starten","⏻    Start audio"),owner.ToggleAudio);
   if(ShowStatus(owner.status.Text))TextAt(g,owner.status.Text,280,887,12,muted,width:850,height:18);
   DrawReorder(g);
   if(owner.tourStep>=0)DrawTour(g);
  }
  static bool ShowStatus(string s)=>new[]{"fehl","erst","fail","first","not "}.Any(k=>s.Contains(k,StringComparison.OrdinalIgnoreCase))||new[]{"Sortieren:","Update:","Spielt:","Playing:","Discord","Gewählt","Selected","Fehler","Error","Kein","No "}.Any(s.StartsWith);
  static float GainPosition(float value)=>value<=60?value/120:.5f+(value-60)/680;
  static float GainValue(float position)=>position<=.5f?position*120:60+(position-.5f)*680;
  // Wellenform rechts mit zwei ziehbaren Markern (Start/Ende) und Abspielposition beim Vorhören.
  static readonly RectangleF WaveArea=new(1193,186,319,58);
  float WaveX(double s,double total)=>WaveArea.X+(float)(WaveArea.Width*s/total);
  void TrimWave(Graphics g,SoundSlot slot){
   if(owner.PanelTrim() is not {} t){Wave(g,1195,215,317,53,owner.selectedSound,false);TextAt(g,T("Datei wählen, dann hier die Länge ziehen","Choose a file, then drag the length here"),1194,249,12,muted,width:318,height:18);return;}
   var peaks=owner.Peaks(slot.File);float mid=WaveArea.Y+WaveArea.Height/2;
   for(int i=0;i<MainForm.PeakBins;i++){float x=WaveArea.X+1.5f+i*WaveArea.Width/MainForm.PeakBins;double s=(i+.5)*t.total/MainForm.PeakBins;bool inside=s>=t.from&&s<=t.to;
    float h=peaks==null?2:Math.Max(2,peaks[i]*(WaveArea.Height-6));using var p=new Pen(inside?green:C(66,82,96),2);g.DrawLine(p,x,mid-h/2,x,mid+h/2);}
   float a=WaveX(t.from,t.total),b=WaveX(t.to,t.total);
   using(var band=new SolidBrush(Color.FromArgb(28,232,200,122)))g.FillRectangle(band,a,WaveArea.Y,b-a,WaveArea.Height);
   foreach(var x in new[]{a,b}){using var p=new Pen(green,2);g.DrawLine(p,x,WaveArea.Y-2,x,WaveArea.Bottom+2);Box(g,new(x-6,WaveArea.Y-4,12,18),green,C(196,160,84),4);using var grip=new Pen(C(44,38,24));g.DrawLine(grip,x-2,WaveArea.Y+1,x-2,WaveArea.Y+9);g.DrawLine(grip,x+2,WaveArea.Y+1,x+2,WaveArea.Y+9);}
   if(owner.PanelPreviewing){var x=WaveX(Math.Min(owner.PanelPosition,t.to),t.total);using var p=new Pen(Color.White,2);g.DrawLine(p,x,WaveArea.Y,x,WaveArea.Bottom);}
   string Sec(double s)=>MainForm.Seconds(s)+" s";
   TextAt(g,$"{Sec(t.from)}  –  {Sec(t.to)}   ·   {Sec(t.to-t.from)} "+T("von","of")+$" {Sec(t.total)}",1194,250,13,muted,width:318,height:20);
  }
  // Zeile „Länge“: Anhören, Speichern (gold, solange ungespeichert), ganzer Sound.
  void TrimRow(Graphics g){
   TextAt(g,T("Länge","Length"),1194,456,19,ink);bool has=owner.PanelTrim()!=null;
   var play=new RectangleF(1290,448,46,36);Box(g,play,C(28,36,42),C(22,29,35),8,has?C(64,77,87):C(44,54,62));TextAt(g,owner.PanelPreviewing?"■":"▶",play.X+15,play.Y+7,17,has?ink:C(96,110,122));if(has)Hit(play,owner.TogglePanelPreview);
   var save=new RectangleF(1342,448,110,36);bool dirty=owner.TrimDirty;Box(g,save,dirty?C(236,206,134):C(28,36,42),dirty?C(214,180,104):C(22,29,35),8,dirty?C(244,222,166):C(64,77,87));TextAt(g,T("✓ Speichern","✓ Save"),save.X+12,save.Y+9,15,dirty?C(24,20,12):C(110,124,136),width:96);if(dirty)Hit(save,owner.SaveTrim);
   var full=new RectangleF(1458,448,55,36);Box(g,full,C(28,36,42),C(22,29,35),8,C(64,77,87));TextAt(g,"⟲",full.X+18,full.Y+6,18,has?ink:C(96,110,122));if(has)Hit(full,owner.ResetTrim);
  }
  void Device(Graphics g,ComboBox combo,RectangleF r){Box(g,r,C(28,37,44),C(23,31,37),5,C(64,77,87));TextAt(g,(combo.SelectedItem as AudioDevice)?.Name??T("Kein Gerät verfügbar","No device available"),r.X+12,r.Y+8,14,ink,width:r.Width-37,height:25);TextAt(g,"⌄",r.Right-23,r.Y+6,18,muted);Hit(r,()=>OpenDeviceMenu(combo,r));}
  internal ToolStripDropDown DeviceMenu=>deviceMenu;
  internal void OpenDeviceMenu(ComboBox combo,RectangleF r){if(combo.Items.Count==0){owner.status.Text=T("Kein passendes Gerät gefunden – fehlt das virtuelle Kabel, bitte den Installer erneut ausführen.","No matching device found – the virtual cable is missing, please run the installer again.");Invalidate();return;}
   // Ein einziges Menü wiederverwenden: ein im Closed-Ereignis entsorgtes Menü bringt WinForms beim Klick daneben zum Absturz.
   var old=deviceMenu.Items.Cast<ToolStripItem>().ToList();deviceMenu.Items.Clear();foreach(var item in old)item.Dispose();
   foreach(AudioDevice device in combo.Items){var item=(ToolStripMenuItem)deviceMenu.Items.Add(device.Name);item.Checked=ReferenceEquals(device,combo.SelectedItem);item.Click+=(_,_)=>owner.ChangeDevice(combo,device);}
   deviceMenu.Show(this,new Point((int)(r.X*Width/1536),(int)(r.Bottom*Height/1024)));}
  void EditGain(){using var dialog=new Form{Text=T("Sound-Pegel (0–400 %)","Sound level (0–400 %)"),ClientSize=new Size(260,110),StartPosition=FormStartPosition.CenterParent};var value=new NumericUpDown{Minimum=0,Maximum=400,Value=owner.selectedGain.Value,Location=new Point(20,18),Width=220};var save=new Button{Text=T("Übernehmen","Apply"),Location=new Point(120,65),Width=120,DialogResult=DialogResult.OK};dialog.Controls.Add(value);dialog.Controls.Add(save);dialog.AcceptButton=save;if(dialog.ShowDialog(owner)==DialogResult.OK)owner.SetGain((float)value.Value);}
  void Toggle(Graphics g,float x,float y,bool on){Box(g,new(x,y,42,24),on?green:C(75,85,93),on?C(196,160,84):C(54,65,73),12);using var b=new SolidBrush(ink);g.FillEllipse(b,x+(on?22:3),y+3,18,18);}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();mouse=Logical(e.Location);if(e.Button!=MouseButtons.Left)return;if(owner.tourStep>=0){foreach(var h in hits)if(h.rect.Contains(mouse)){h.action();break;}return;}if(BeginReorder(mouse))return;if(mouse.Y<61&&mouse.X<1020&&!hits.Any(h=>h.rect.Contains(mouse))){Native.ReleaseCapture();Native.SendMessage(owner.Handle,0xA1,(IntPtr)2,IntPtr.Zero);return;}if(owner.PanelTrim() is {} t&&RectangleF.Inflate(WaveArea,8,6).Contains(mouse)){dragging=3;trimStart=Math.Abs(mouse.X-WaveX(t.from,t.total))<=Math.Abs(mouse.X-WaveX(t.to,t.total));owner.DragTrim(trimStart,(mouse.X-WaveArea.X)/WaveArea.Width*t.total);Capture=true;return;}if(new RectangleF(1188,398,330,30).Contains(mouse))dragging=1;else if(new RectangleF(799,942,426,39).Contains(mouse))dragging=2;Capture=dragging!=0;foreach(var h in hits)if(h.rect.Contains(mouse)){h.action();break;}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);mouse=Logical(e.Location);if(ContinueReorder(mouse))return;if(dragging==1)owner.SetGain(GainValue((mouse.X-1200)/306));if(dragging==2)owner.SetMaster((mouse.X-811)/401*100);if(dragging==3&&owner.PanelTrim() is {} t)owner.DragTrim(trimStart,(mouse.X-WaveArea.X)/WaveArea.Width*t.total);bool onWave=owner.tourStep<0&&owner.PanelTrim()!=null&&RectangleF.Inflate(WaveArea,8,6).Contains(mouse);int hit=onWave?-2:hits.FindIndex(h=>h.rect.Contains(mouse));if(hit!=hover){hover=hit;Cursor=onWave?Cursors.SizeWE:hit>=0?Cursors.Hand:Cursors.Default;}}
  protected override bool IsInputKey(Keys k)=>k is Keys.Left or Keys.Right or Keys.Up or Keys.Down||base.IsInputKey(k);
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);mouse=Logical(e.Location);if(FinishReorder())return;dragging=0;Capture=false;}
  protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(owner.tourStep>=0)return;var p=Logical(e.Location);if(p.X>247&&p.X<1169){owner.scrollRow=Math.Clamp(owner.scrollRow+(e.Delta<0?1:-1),0,2);Invalidate();}}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(owner.tourStep>=0){if(e.KeyCode==Keys.Escape)owner.EndTour();else if(e.KeyCode is Keys.Right or Keys.Enter or Keys.Space)owner.TourNext();else if(e.KeyCode==Keys.Left)owner.TourBack();e.Handled=true;return;}if(e.KeyCode==Keys.Escape&&owner.sorting){if(reorderFrom>=0)CancelReorder();else owner.ToggleSorting();e.Handled=true;return;}if(e.KeyCode==Keys.Escape){owner.audio.StopSounds();Invalidate();}if(e.KeyCode==Keys.Space){owner.Try(()=>owner.Play(owner.selectedSound));e.Handled=true;}if(e.KeyCode==Keys.Down){owner.scrollRow=Math.Min(2,owner.scrollRow+1);Invalidate();}if(e.KeyCode==Keys.Up){owner.scrollRow=Math.Max(0,owner.scrollRow-1);Invalidate();}}
 }
 static class Native{[DllImport("user32.dll")]public static extern bool ReleaseCapture();[DllImport("user32.dll")]public static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);}
}
