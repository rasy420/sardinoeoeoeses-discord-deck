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
  selectedGain.ValueChanged+=(_,_)=>{if(syncing)return;draftGain=(float)selectedGain.Value/100;StopPanelPreview();deck.Invalidate();};
  soundVolume=new TrackBar{Minimum=0,Maximum=100,Value=(int)Math.Round(settings.MasterVolume*100)};
  soundVolume.ValueChanged+=(_,_)=>{settings.MasterVolume=soundVolume.Value/100f;audio.SetVolume(settings.MasterVolume,settings.MicrophoneVolume);settings.Save();deck.Invalidate();};
  status.TextChanged+=(_,_)=>deck.Invalidate();
  string lastPlaying="";playbackRefresh.Tick+=(_,_)=>{var current=audio.PlayingSignature;if(current!=lastPlaying||availableUpdate!=null){lastPlaying=current;deck.Invalidate();}};playbackRefresh.Start();
  deck.Resize+=(_,_)=>PlaceReferenceControls();PlaceReferenceControls();SyncSelected();
  start.Click+=(_,_)=>ToggleAudio();
 }
 void PlaceReferenceControls(){if(deck==null)return;float x=deck.Width/1536f,y=deck.Height/1024f;void Place(Control c,int a,int b,int w,int h){c.Bounds=new Rectangle((int)(a*x),(int)(b*y),(int)(w*x),(int)(h*y));}
  Place(output,1194,579,319,35);Place(input,1194,653,319,35);Place(monitorOutput,1194,805,319,35);Place(selectedGain,1416,357,97,35);LayoutSoundEditor();
 }
 internal void SelectSound(int index){OpenSoundSettings(index);}
 void SelectEdited(int index){if(index!=selectedSound||!soundPanelOpen){ForgetTrim();selectedSound=index;draftGain=settings.Slots[index].Volume;}soundPanelOpen=true;SyncSelected();deck.Invalidate();}
 void SyncSelected(){syncing=true;selectedGain.Value=(decimal)Math.Round(Math.Clamp(draftGain,0,4)*100);syncing=false;}
 void ToggleAudio(){Try(()=>{if(audio.Running){audio.Stop();settings.AudioEnabled=false;settings.Save();input.Enabled=output.Enabled=mic.Enabled=monitorOutput.Enabled=monitorCheck.Enabled=true;start.Text=T("Audio starten","Start audio");status.Text=T("Audio ist aus","Audio is off");}else StartAudio();});deck.Invalidate();}
 void SetGain(float value){if(PanelTrim()==null)return;selectedGain.Value=(decimal)Math.Clamp(Math.Round(value),0,400);}
 internal void SetMicrophoneLevel(int percent){settings.MicrophoneVolume=Math.Clamp(percent,0,(int)(AudioEngine.MaxMicrophone*100))/100f;audio.SetVolume(settings.MasterVolume,settings.MicrophoneVolume);settings.Save();deck.Invalidate();}
 void SetMaster(float value){soundVolume!.Value=(int)Math.Clamp(Math.Round(value),0,100);}
 void ChooseFile(){if(importing||!ResolveDraft())return;var slot=settings.Slots[selectedSound];Try(()=>{using var picker=new OpenFileDialog{Filter=T("Audio","Audio")+"|*.wav;*.mp3;*.flac;*.aiff;*.wma;*.m4a|"+T("Alle Dateien","All files")+"|*.*",Title=T("Audiodatei auswählen","Choose audio file")};if(picker.ShowDialog(this)!=DialogResult.OK)return;ReplaceSoundSource(slot,picker.FileName,Path.GetFileNameWithoutExtension(picker.FileName),"");});}
 async void LinkSound(){
  if(importing||!ResolveDraft())return;var slot=settings.Slots[selectedSound];var id=slot.Id;
  using var dialog=new Form{Text="MyInstants-Link",ClientSize=new(540,160),StartPosition=FormStartPosition.CenterParent,BackColor=Color.FromArgb(19,27,33),ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,AutoScaleMode=AutoScaleMode.Dpi};
  var label=new Label{Text="MyInstants-Link",Bounds=new(20,16,500,24)};var url=new TextBox{Text=slot.SourceUrl,PlaceholderText="https://www.myinstants.com/.../instant/...",Bounds=new(20,46,500,28),BackColor=Color.FromArgb(31,41,49),ForeColor=Color.White};
  var save=new Button{Text=T("Übernehmen","Apply"),Bounds=new(376,103,144,36),DialogResult=DialogResult.OK,FlatStyle=FlatStyle.Flat};var cancel=new Button{Text=T("Abbrechen","Cancel"),Bounds=new(222,103,144,36),DialogResult=DialogResult.Cancel,FlatStyle=FlatStyle.Flat};dialog.Controls.AddRange(new Control[]{label,url,save,cancel});dialog.AcceptButton=save;dialog.CancelButton=cancel;
  if(dialog.ShowDialog(this)!=DialogResult.OK)return;importing=true;status.Text=T("Quelle wird geladen …","Loading source …");deck.Invalidate();
  try{var result=await MyInstants.Import(url.Text.Trim());if(IsDisposed)return;var target=settings.Slots.SingleOrDefault(s=>s.Id==id);if(target!=null){if(target.Id==settings.Slots[selectedSound].Id&&!ResolveDraft())return;ReplaceSoundSource(target,result.File,result.Name,result.Source);status.Text=T("Quelle gespeichert.","Source saved.");}}
  catch(Exception e){if(!IsDisposed){editError=e.Message;status.Text=e.Message;}}
  finally{importing=false;if(!IsDisposed)deck.Invalidate();}
 }
 void PairPhone(){Try(()=>{if(lan==null)throw new Exception(T("LAN-Server ist nicht aktiv.","LAN server is not running."));MessageBox.Show(this,T("Code in der Android-App eingeben (2 Minuten gültig):","Enter this code in the Android app (valid for 2 minutes):")+"\n\n"+lan.BeginPairing()+"\n\n"+T("PC und Handy müssen im selben lokalen Netz sein.","PC and phone must be on the same local network."),T("Handy verbinden","Connect phone"));});}
 void ChangeDevice(ComboBox combo,AudioDevice device){combo.SelectedItem=device;bool running=audio.Running;ReadSettings();settings.Save();status.Text=T("Gewählt: ","Selected: ")+device.Name;CheckDiscord();if(running){audio.Stop();Try(StartAudio);}deck.Invalidate();}
 void ToggleRouting(bool microphone){if(audio.Running){audio.Stop();}if(microphone)mic.Checked=!mic.Checked;else monitorCheck.Checked=!monitorCheck.Checked;ReadSettings();settings.Save();if(settings.AudioEnabled)Try(StartAudio);deck.Invalidate();}
 sealed partial class ReferenceDeck:Control {
  readonly MainForm owner; readonly Image reference; readonly List<(RectangleF rect,Action action,string name)> hits=new();int dragging;bool trimStart;PointF mouse;int hover=-1;
  static Color C(int r,int g,int b)=>Color.FromArgb(r,g,b);
  readonly Color ink=C(240,239,231),muted=C(170,190,210),line=C(43,54,63),green=C(232,200,122);
  public ReferenceDeck(MainForm form){owner=form;DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);using var stream=typeof(MainForm).Assembly.GetManifestResourceStream("DiscordDeck.reference.png")!;using var source=Image.FromStream(stream);reference=new Bitmap(source);TabStop=true;AccessibleName="Sardinöööses Soundboard";InitReorder();}
  readonly ContextMenuStrip deviceMenu=new(){BackColor=C(25,34,41),ForeColor=C(240,239,231),ShowImageMargin=false,ShowCheckMargin=true};
  protected override void Dispose(bool disposing){if(disposing){sourceTip.Dispose();reorderTimer.Dispose();reference.Dispose();deviceMenu.Dispose();replaceMenu.Dispose();fileSymbol.Dispose();linkSymbol.Dispose();}base.Dispose(disposing);}
  PointF Logical(Point p)=>new(p.X*1536f/Width,p.Y*1024f/Height);
  static GraphicsPath Shape(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
  void Box(Graphics g,RectangleF r,Color top,Color bottom,float radius=9,Color? border=null){using var path=Shape(r,radius);using var b=new LinearGradientBrush(r,top,bottom,90);g.FillPath(b,path);if(border.HasValue){using var pen=new Pen(border.Value);g.DrawPath(pen,path);}}
  void TextAt(Graphics g,string text,float x,float y,float size,Color color,bool bold=false,float width=1000,float height=65){using var font=new Font(bold?(size>=45?"Segoe UI Semibold":"Bahnschrift SemiCondensed"):"Segoe UI",size,FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(color);using var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};g.DrawString(text,font,brush,new RectangleF(x,y,width,height),format);}
  void Line(Graphics g,float x,float y,float xx,float yy){using var pen=new Pen(line);g.DrawLine(pen,x,y,xx,yy);}
  void Hit(RectangleF rect,Action action,string name="Aktion")=>hits.Add((rect,action,name));
  internal static RectangleF StopBounds(RectangleF tile)=>new(tile.X+54,tile.Y+12,tile.Width-54-48,56);
  void Button(Graphics g,RectangleF r,string text,Action action,bool danger=false,bool gold=false){Box(g,r,danger?C(250,100,87):gold?C(236,206,134):C(28,36,42),danger?C(239,86,77):gold?C(214,180,104):C(22,29,35),8,danger?C(255,128,111):gold?C(244,222,166):C(64,77,87));using var font=new Font("Segoe UI",text.Contains('\n')?12:14,gold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(gold?C(24,20,12):ink);using var fmt=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter};g.DrawString(text,font,brush,RectangleF.Inflate(r,-12,-4),fmt);Hit(r,action,text);}
  void Phone(Graphics g){using var pen=new Pen(muted,1.6f);using var shape=Shape(new(1045,20,12,21),2);g.DrawPath(pen,shape);g.DrawLine(pen,1049,23,1053,23);g.DrawLine(pen,1050,38,1052,38);}
  void Wave(Graphics g,float x,float y,float width,float height,int seed,bool active){var rng=new Random(seed+71);int n=(int)(width/5);for(int i=0;i<n;i++){float t=i/(float)n;float envelope=(float)(Math.Exp(-Math.Pow((t-.26)*4,2))*.85+.06);float h=Math.Max(2,(float)rng.NextDouble()*height*envelope);using var p=new Pen(active&&t<.36?green:C(88,108,126),2);g.DrawLine(p,x+i*5,y-h/2,x+i*5,y+h/2);}}
  void Slider(Graphics g,float x,float y,float w,float ratio,float knob=12,Color? fill=null){float t=knob<12?9:12;using var p=new Pen(C(53,63,73),t){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(p,x,y,x+w,y);using var a=new Pen(fill??green,t){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(a,x,y,x+w*Math.Clamp(ratio,0,1),y);using var b=new SolidBrush(ink);g.FillEllipse(b,x+w*Math.Clamp(ratio,0,1)-knob,y-knob,knob*2,knob*2);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(Width/1536f,Height/1024f);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;hits.Clear();
   using(var b=new LinearGradientBrush(new Rectangle(0,0,1536,1024),C(16,23,28),C(8,14,18),45))g.FillRectangle(b,0,0,1536,1024);
   using(var b=new LinearGradientBrush(new Rectangle(0,0,1536,62),C(24,31,37),C(16,23,28),90))g.FillRectangle(b,0,0,1536,62);
   Line(g,0,62,1536,62);Line(g,247,62,247,1024);if(owner.soundPanelOpen)Line(g,1169,62,1169,908);Line(g,247,908,1536,908);
   // The two sardine illustrations are taken from the supplied design itself.
   g.DrawImage(reference,new RectangleF(14,8,107,43),new RectangleF(14,8,107,43),GraphicsUnit.Pixel);
   TextAt(g,"Sardinöööses",140,8.5f,27,ink,true,width:200,height:34);{using var tf=new Font("Bahnschrift SemiCondensed",27,FontStyle.Regular,GraphicsUnit.Pixel);using var sf=new Font("Segoe UI",10.5f,FontStyle.Regular,GraphicsUnit.Pixel);using var sb=new SolidBrush(C(214,180,110));var tw=g.MeasureString("Sardinöööses",tf,1000,StringFormat.GenericTypographic).Width;const string sub="SOUNDBOARD";float x0=143,x1=140+tw-2,cw=g.MeasureString("D",sf,100,StringFormat.GenericTypographic).Width;for(int i=0;i<sub.Length;i++){var w=g.MeasureString(sub[i].ToString(),sf,100,StringFormat.GenericTypographic).Width;g.DrawString(sub[i].ToString(),sf,sb,x0+(x1-cw-x0)*i/(sub.Length-1)+(cw-w)/2,41.5f,StringFormat.GenericTypographic);}}
   Button(g,new(1031,11,170,39),T("    Handy verbinden","    Connect phone"),owner.PairPhone);Phone(g);Button(g,new(1216,11,122,39),"MyInstants  ↗",()=>SoundCatalog.CreateButton().PerformClick());Line(g,1358,12,1358,49);
   {var lang=new RectangleF(922,11,56,39);Box(g,lang,C(28,36,42),C(22,29,35),8,C(64,77,87));TextAt(g,"DE",lang.X+9,lang.Y+10,14,En?muted:green,true,width:22,height:20);TextAt(g,"/",lang.X+26,lang.Y+9,14,C(90,104,116),width:8,height:20);TextAt(g,"EN",lang.X+33,lang.Y+10,14,En?green:muted,true,width:22,height:20);Hit(lang,()=>owner.SetLanguage(!En));
    var help=new RectangleF(984,11,40,39);Box(g,help,C(28,36,42),C(22,29,35),8,C(64,77,87));TextAt(g,"?",help.X+14,help.Y+7,19,ink,true,width:20,height:26);Hit(help,owner.StartTour);}
   if(owner.discordWarning is {} warn){var w=new RectangleF(466,9,450,44);Box(g,w,C(176,42,42),C(140,30,32),8,C(236,92,84));TextAt(g,"⚠  "+warn.Title,w.X+12,w.Y+4,13,C(255,226,220),width:warn.OpensDiscord?320:426,height:18);TextAt(g,warn.Detail,w.X+12,w.Y+21,15,C(255,255,255),true,warn.OpensDiscord?330:426,22);if(warn.OpensDiscord){var o=new RectangleF(w.Right-98,w.Y+7,90,30);Box(g,o,C(236,88,80),C(214,64,60),6,C(255,150,140));TextAt(g,T("Öffnen  ↗","Open  ↗"),o.X+12,o.Y+6,14,C(255,255,255),width:80,height:20);Hit(w,owner.OpenDiscordVoice);}}
   TextAt(g,"—",1387,16,24,ink);Hit(new(1368,0,52,61),()=>owner.WindowState=FormWindowState.Minimized);
   TextAt(g,"□",1442,16,23,ink);Hit(new(1420,0,53,61),()=>owner.WindowState=owner.WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized);
   TextAt(g,"×",1493,16,24,ink);Hit(new(1473,0,63,61),()=>owner.Close());
   TextAt(g,T("S O U N D - B Ä N K E","S O U N D   B A N K S"),21,91,12,muted);
   for(int i=0;i<5;i++){int page=i;bool on=owner.bank==i;var r=new RectangleF(14,123+i*64,219,47);Box(g,r,on?C(236,206,134):C(24,33,39),on?C(226,192,116):C(18,26,32),10);Box(g,new(26,r.Y+8,37,32),on?C(164,134,76):C(39,49,58),on?C(128,102,52):C(29,38,46),5,on?C(200,168,100):C(70,84,97));TextAt(g,$"{i+1:00}",34,r.Y+11,17,on?C(29,26,19):ink,true);TextAt(g,owner.BankName(i),82,r.Y+12,16,on?C(10,19,14):ink,width:owner.sorting?117:143,height:26);if(owner.sorting){var pencil=BankPencilBounds(i);Pencil(g,pencil,on?C(29,26,19):muted);Hit(pencil,()=>owner.EditBankName(page),T("Banknamen bearbeiten: ","Edit bank name: ")+owner.BankName(i));}Hit(r,()=>{owner.bank=page;owner.scrollRow=0;},owner.BankName(page));}
   Line(g,21,687,228,687);Button(g,new(19,707,211,48),T("Von YouTube / TikTok\nimportieren","Import from\nYouTube / TikTok"),()=>YouTubeImport.Show(owner,owner.settings,()=>{owner.SyncSelected();Invalidate();}));Button(g,new(19,769,211,48),T("⟳   Geräte neu laden","⟳   Reload devices"),()=>{if(owner.audio.Running){owner.status.Text=T("Audio erst beenden, dann Geräte neu laden.","Stop audio first, then reload devices.");return;}owner.ReloadDevices();Invalidate();});
   UpdateButton(g);

   g.DrawImage(reference,new RectangleF(40,888,140,57),new RectangleF(40,888,140,57),GraphicsUnit.Pixel);TextAt(g,T("D E I N E  S O U N D S.","Y O U R  S O U N D S."),38,954,12,muted);TextAt(g,T("D E I N  M O M E N T.","Y O U R  M O M E N T."),38,973,12,muted);
   if(!owner.sorting)TextAt(g,T("D E I N E  S O U N D B A N K","Y O U R  S O U N D B A N K"),276,87,12,muted);DrawBankHeading(g);
   DrawReorderButton(g);
   Button(g,new(19,451,211,48),T("■   Alle Sounds stoppen","■   Stop all sounds"),()=>{owner.audio.StopSounds();owner.status.Text=T("Sounds gestoppt · Mikrofon bleibt an","Sounds stopped · microphone stays on");Invalidate();},true);
   var clip=g.Save();g.SetClip(new RectangleF(263,190,GridRight-263,675));
   for(int j=0;j<20;j++){int index=owner.bank*20+j;var r=TileBounds(index);float x=r.X,y=r.Y;if(y<190||y>858)continue;var slot=owner.settings.Slots[index];bool active=owner.audio.IsPlaying(slot);bool edited=owner.soundPanelOpen&&index==owner.selectedSound;
    Box(g,r,active?C(56,48,30):C(26,34,40),active?C(34,31,24):C(18,26,31),12,active?green:edited?C(114,133,148):C(39,51,60));TextAt(g,$"{index+1:00}",x+19,y+16,17,ink);if(File.Exists(slot.File))Wave(g,x+61,y+39,116,42,index,active);TextAt(g,owner.sorting?"↕":"⚙",r.Right-36,y+12,22,edited?ink:muted);TextAt(g,slot.Name,x+19,y+72,20,ink,true,r.Width-38,34);
    // Laufender Sound: rote Stopp-Fläche auf der Kachel beendet nur diesen Sound.
    if(active&&!owner.sorting){var stop=StopBounds(r);Box(g,stop,C(250,100,87),C(222,70,62),10,C(255,140,124));using var sf=new Font("Segoe UI",17,FontStyle.Bold,GraphicsUnit.Pixel);using var sb=new SolidBrush(Color.White);using var fmt=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};g.DrawString(T("■  Stopp","■  Stop"),sf,sb,stop,fmt);Hit(stop,()=>{owner.audio.StopSlot(slot);owner.status.Text=T("Gestoppt: ","Stopped: ")+slot.Name;Invalidate();},T("Stoppen: ","Stop: ")+slot.Name);}
    if(owner.sorting)continue;
    var gear=new RectangleF(r.Right-42,y+7,33,33);Hit(gear,()=>owner.OpenSoundSettings(index),T("Einstellungen: ","Settings: ")+slot.Name);Hit(r,()=>{keyboardTile=index;owner.Try(()=>owner.ActivateTile(index));},slot.Name);
   }g.Restore(clip);
   if(owner.scrollRow<MaxScroll){TextAt(g,"⌄",GridRight-15,843,17,muted);Hit(new(GridRight-12,835,12,28),()=>{owner.scrollRow++;Invalidate();});}if(owner.scrollRow>0)Hit(new(GridRight-24,190,24,30),()=>{owner.scrollRow--;Invalidate();});
   Button(g,new(GridRight-211,870,205,31),T("☷   Audio-Einstellungen","☷   Audio settings"),owner.OpenAudioSettings);
   if(owner.soundPanelOpen){
   // Keep the full grid in place; mask both painting and input beneath the right overlay.
   for(int i=hits.Count-1;i>=0;i--){var h=hits[i];if(h.rect.Top>=62&&h.rect.Bottom<=908&&h.rect.Right>1169){if(h.rect.Left>=1169)hits.RemoveAt(i);else hits[i]=(RectangleF.FromLTRB(h.rect.Left,h.rect.Top,1169,h.rect.Bottom),h.action,h.name);}}
   using(var shadow=new LinearGradientBrush(new RectangleF(1157,62,12,846),Color.Transparent,Color.FromArgb(100,0,0,0),0f))g.FillRectangle(shadow,1157,62,12,846);
   using(var panel=new LinearGradientBrush(new RectangleF(1169,62,367,846),C(22,30,36),C(13,20,25),90))g.FillRectangle(panel,1169,62,367,846);
   Line(g,1169,62,1169,908);
   var selectedSlot=owner.settings.Slots[owner.selectedSound];TextAt(g,T("S O U N D - E I N S T E L L U N G E N","S O U N D   S E T T I N G S"),1194,91,12,muted);Box(g,new(1480,78,40,36),C(130,43,43),C(91,32,35),8,C(232,99,91));TextAt(g,"×",1491,81,27,C(255,231,225),width:25);Hit(new(1480,78,40,36),owner.CloseSoundSettings,T("Sound-Einstellungen schließen","Close sound settings"));TextAt(g,$"SLOT {owner.selectedSound+1:00}",1194,122,12,muted);TextAt(g,selectedSlot.Name,1194,144,27,ink,true,270);var pencil=NamePencil(g,selectedSlot.Name);Pencil(g,pencil);Hit(pencil,owner.EditSoundName,T("Soundname bearbeiten","Edit sound name"));TrimWave(g,selectedSlot);
   SourceRow(g,selectedSlot);
   Line(g,1194,357,1512,357);TextAt(g,T("Pegel","Level"),1194,365,19,ink);Box(g,new(1416,357,97,35),C(20,28,34),C(16,23,29),5,C(57,70,81));TextAt(g,$"{owner.selectedGain.Value:0}   %",1426,362,18,ink,width:72);TextAt(g,"▴",1494,357,12,ink);TextAt(g,"▾",1494,375,12,ink);Hit(new(1489,357,24,17),()=>owner.SetGain((float)owner.selectedGain.Value+5));Hit(new(1489,374,24,18),()=>owner.SetGain((float)owner.selectedGain.Value-5));Hit(new(1416,357,73,35),()=>EditGain());Slider(g,1200,412,306,GainPosition(owner.draftGain*100));Hit(new(1188,398,330,30),()=>owner.SetGain(GainValue((mouse.X-1200)/306)));
   TrimRow(g);
   Line(g,1194,505,1512,505);if(!string.IsNullOrEmpty(owner.editError))TextAt(g,owner.editError,1194,owner.soundEditor!=null&&owner.editorFontSize>20?174:520,owner.soundEditor!=null&&owner.editorFontSize>20?10:12,C(255,145,135),width:318,height:45);
   }
   using(var b=new SolidBrush(owner.audio.Running?green:C(116,132,148)))g.FillEllipse(b,273,944,20,20);TextAt(g,owner.audio.Running?T("Audio aktiv","Audio on"):T("Audio bereit","Audio ready"),314,940,20,owner.audio.Running?green:ink,true);TextAt(g,owner.audio.Running?T("Virtueller Eingang verbunden","Virtual input connected"):T("Zum Starten Audio aktivieren","Activate audio to start"),314,968,16,muted,width:250);
   Line(g,573,927,573,996);TextAt(g,"◖))",617,946,25,ink);
   // App-Lautstärke (alle Sounds) und darunter Mikrofonlautstärke im Mix (0–400 %).
   TextAt(g,T("App-Lautstärke","App volume"),665,936,14,ink);Slider(g,811,946,401,owner.settings.MasterVolume,9);TextAt(g,$"{owner.settings.MasterVolume*100:0} %",1226,937,13,muted,width:66);Hit(new(799,930,426,32),()=>owner.SetMaster((mouse.X-811)/401*100));
   bool micOn=owner.settings.MixMicrophone;TextAt(g,T("Mikrofonlautstärke","Mic volume"),665,969,14,micOn?ink:muted);Slider(g,811,979,401,owner.settings.MicrophoneVolume/AudioEngine.MaxMicrophone,9,micOn?(owner.settings.MicrophoneVolume>1?C(240,150,70):green):C(96,110,122));using(var tick=new Pen(C(170,182,192),2))g.DrawLine(tick,811+401/AudioEngine.MaxMicrophone,988,811+401/AudioEngine.MaxMicrophone,995);TextAt(g,micOn?$"{owner.settings.MicrophoneVolume*100:0} %":T("aus","off"),1226,970,13,owner.settings.MicrophoneVolume>1&&micOn?C(240,150,70):muted,width:66);Hit(new(799,963,426,32),()=>owner.SetMicrophoneLevel((int)Math.Round((mouse.X-811)/401*AudioEngine.MaxMicrophone*100)));
   Button(g,new(1302,938,209,48),owner.audio.Running?T("⏻    Audio beenden","⏻    Stop audio"):T("⏻    Audio starten","⏻    Start audio"),owner.ToggleAudio,gold:!owner.audio.Running);
   if(ShowStatus(owner.status.Text))TextAt(g,owner.status.Text,280,887,12,muted,width:(owner.soundPanelOpen?1163:GridRight)-505,height:18);
   DrawActionFocus(g);DrawReorder(g);
   if(owner.tourStep>=0)DrawTour(g);
  }
  internal RectangleF HeadingBankBounds,HeadingRangeBounds;
  void DrawBankHeading(Graphics g){
   var heading=T("Auf Knopfdruck.","Push to play.");using var titleFont=new Font("Segoe UI Semibold",50,FontStyle.Regular,GraphicsUnit.Pixel);float titleWidth=g.MeasureString(heading,titleFont,1000,StringFormat.GenericTypographic).Width;
   TextAt(g,heading,273,112,50,ink,true,width:titleWidth+48);float left=273+titleWidth+52;
   var name=string.IsNullOrWhiteSpace(owner.settings.Banks[owner.bank].Name)?"Sounds":owner.settings.Banks[owner.bank].Name;using var nameFont=new Font("Segoe UI",26,FontStyle.Regular,GraphicsUnit.Pixel);
   float width=Math.Min(g.MeasureString(name,nameFont).Width+2,Math.Max(100,1138-left-160));
   HeadingBankBounds=new(left,134,width,38);HeadingRangeBounds=new(left+width+10,132,150,42);
   TextAt(g,name,left,134,26,green,width:width,height:38);TextAt(g,$"{owner.bank*20+1:00} \u2014 {owner.bank*20+20:00}",HeadingRangeBounds.X,132,28,C(102,116,137),width:150,height:42);
  }
  static bool ShowStatus(string s)=>new[]{"fehl","erst","fail","first","not "}.Any(k=>s.Contains(k,StringComparison.OrdinalIgnoreCase))||new[]{"Sortieren:","Update:","Spielt:","Playing:","Discord","Gewählt","Selected","Fehler","Error","Kein","No "}.Any(s.StartsWith);
  static float GainPosition(float value)=>value<=60?value/120:.5f+(value-60)/680;
  static float GainValue(float position)=>position<=.5f?position*120:60+(position-.5f)*680;
  // Wellenform rechts mit zwei ziehbaren Markern (Start/Ende) und Abspielposition beim Vorhören.
  static readonly RectangleF WaveArea=new(1193,186,319,58);
  float WaveX(double s,double total)=>WaveArea.X+(float)(WaveArea.Width*s/total);
  void TrimWave(Graphics g,SoundSlot slot){
   if(owner.PanelTrim() is not {} t){TimeFields(g);return;}
   var peaks=owner.Peaks(slot.File);float mid=WaveArea.Y+WaveArea.Height/2;
   for(int i=0;i<MainForm.PeakBins;i++){float x=WaveArea.X+1.5f+i*WaveArea.Width/MainForm.PeakBins;double s=(i+.5)*t.total/MainForm.PeakBins;bool inside=s>=t.from&&s<=t.to;
    float h=peaks==null?2:Math.Max(2,peaks[i]*(WaveArea.Height-6));using var p=new Pen(inside?green:C(66,82,96),2);g.DrawLine(p,x,mid-h/2,x,mid+h/2);}
   float a=WaveX(t.from,t.total),b=WaveX(t.to,t.total);
   using(var band=new SolidBrush(Color.FromArgb(28,232,200,122)))g.FillRectangle(band,a,WaveArea.Y,b-a,WaveArea.Height);
   foreach(var x in new[]{a,b}){using var p=new Pen(green,2);g.DrawLine(p,x,WaveArea.Y-2,x,WaveArea.Bottom+2);Box(g,new(x-6,WaveArea.Y-4,12,18),green,C(196,160,84),4);using var grip=new Pen(C(44,38,24));g.DrawLine(grip,x-2,WaveArea.Y+1,x-2,WaveArea.Y+9);g.DrawLine(grip,x+2,WaveArea.Y+1,x+2,WaveArea.Y+9);}
   if(owner.PanelPreviewing){var x=WaveX(Math.Min(owner.PanelPosition,t.to),t.total);using var p=new Pen(Color.White,2);g.DrawLine(p,x,WaveArea.Y,x,WaveArea.Bottom);}
   TimeFields(g);
  }
  // Zeile „Länge“: Anhören, Speichern (gold, solange ungespeichert), ganzer Sound.
  void TrimRow(Graphics g){
   TextAt(g,T("Länge","Length"),1194,456,19,ink);bool has=owner.PanelTrim()!=null&&!owner.draftInvalid;
   var play=new RectangleF(1290,448,46,36);Box(g,play,C(28,36,42),C(22,29,35),8,has?C(64,77,87):C(44,54,62));TextAt(g,owner.PanelPreviewing?"■":"▶",play.X+15,play.Y+7,17,has?ink:C(96,110,122));if(has)Hit(play,owner.TogglePanelPreview,T("Ausschnitt vorhören","Preview trim"));
   var save=new RectangleF(1342,448,110,36);bool dirty=owner.TrimDirty&&!owner.draftInvalid;Box(g,save,dirty?C(236,206,134):C(28,36,42),dirty?C(214,180,104):C(22,29,35),8,dirty?C(244,222,166):C(64,77,87));TextAt(g,T("✓ Speichern","✓ Save"),save.X+12,save.Y+9,15,dirty?C(24,20,12):C(110,124,136),width:96);if(dirty)Hit(save,()=>owner.Try(owner.SaveTrim),T("Ausschnitt und Pegel speichern","Save trim and level"));
   var full=new RectangleF(1458,448,55,36);Box(g,full,C(28,36,42),C(22,29,35),8,C(64,77,87));TextAt(g,"⟲",full.X+18,full.Y+6,18,has?ink:C(96,110,122));if(has)Hit(full,owner.ResetTrim,T("Letzte gespeicherte Werte wiederherstellen","Restore last saved values"));
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
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();mouse=Logical(e.Location);focusHit=hits.FindIndex(h=>h.rect.Contains(mouse));if(e.Button!=MouseButtons.Left)return;if(owner.tourStep>=0){foreach(var h in hits)if(h.rect.Contains(mouse)){h.action();break;}return;}if(owner.soundPanelOpen&&new RectangleF(247,62,922,846).Contains(mouse)&&focusHit<0&&TileAt(mouse)<0){owner.DismissSoundSettings();return;}if(BeginReorder(mouse))return;if(mouse.Y<61&&mouse.X<1020&&!hits.Any(h=>h.rect.Contains(mouse))){Native.ReleaseCapture();Native.SendMessage(owner.Handle,0xA1,(IntPtr)2,IntPtr.Zero);return;}if(owner.soundPanelOpen&&owner.PanelTrim() is {} t&&RectangleF.Inflate(WaveArea,8,6).Contains(mouse)){dragging=3;trimStart=Math.Abs(mouse.X-WaveX(t.from,t.total))<=Math.Abs(mouse.X-WaveX(t.to,t.total));owner.DragTrim(trimStart,(mouse.X-WaveArea.X)/WaveArea.Width*t.total);Capture=true;return;}if(owner.soundPanelOpen&&new RectangleF(1188,398,330,30).Contains(mouse))dragging=1;else if(new RectangleF(799,930,426,32).Contains(mouse))dragging=2;else if(new RectangleF(799,963,426,32).Contains(mouse))dragging=5;Capture=dragging!=0;foreach(var h in hits)if(h.rect.Contains(mouse)){h.action();break;}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);mouse=Logical(e.Location);if(ContinueReorder(mouse))return;if(dragging==1)owner.SetGain(GainValue((mouse.X-1200)/306));if(dragging==2)owner.SetMaster((mouse.X-811)/401*100);if(dragging==5)owner.SetMicrophoneLevel((int)Math.Round((mouse.X-811)/401*AudioEngine.MaxMicrophone*100));if(dragging==3&&owner.PanelTrim() is {} t)owner.DragTrim(trimStart,(mouse.X-WaveArea.X)/WaveArea.Width*t.total);bool onWave=owner.soundPanelOpen&&owner.tourStep<0&&owner.PanelTrim()!=null&&RectangleF.Inflate(WaveArea,8,6).Contains(mouse);int hit=onWave?-2:hits.FindIndex(h=>h.rect.Contains(mouse));if(hit!=hover){hover=hit;Invalidate();Cursor=onWave?Cursors.SizeWE:hit>=0?Cursors.Hand:Cursors.Default;}}
  protected override bool IsInputKey(Keys k)=>k is Keys.Left or Keys.Right or Keys.Up or Keys.Down||base.IsInputKey(k);
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);mouse=Logical(e.Location);if(FinishReorder())return;dragging=0;Capture=false;}
  protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(owner.tourStep>=0)return;var p=Logical(e.Location);if(p.X>247&&p.X<(owner.soundPanelOpen?1169:GridRight)){owner.scrollRow=Math.Clamp(owner.scrollRow+(e.Delta<0?1:-1),0,MaxScroll);Invalidate();}}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(owner.tourStep>=0){if(e.KeyCode==Keys.Escape)owner.EndTour();else if(e.KeyCode is Keys.Right or Keys.Enter or Keys.Space)owner.TourNext();else if(e.KeyCode==Keys.Left)owner.TourBack();e.Handled=true;return;}if(e.KeyCode==Keys.Escape&&owner.sorting){if(reorderFrom>=0||bankFrom>=0)CancelReorder();else owner.ToggleSorting();e.Handled=true;return;}if(e.KeyCode==Keys.Escape){if(replaceMenu.Visible){replaceMenu.Close();return;}if(owner.soundPanelOpen)owner.CloseSoundSettings();else owner.audio.StopSounds();Invalidate();}if(e.KeyCode is Keys.Space or Keys.Enter){if(!activationHeld){activationHeld=true;if(focusHit>=0&&focusHit<hits.Count)owner.Try(hits[focusHit].action);else ActivateKeyboard();}e.Handled=true;e.SuppressKeyPress=true;}if(e.KeyCode==Keys.Down){owner.scrollRow=Math.Min(MaxScroll,owner.scrollRow+1);Invalidate();}if(e.KeyCode==Keys.Up){owner.scrollRow=Math.Max(0,owner.scrollRow-1);Invalidate();}}
 }
 static class Native{[DllImport("user32.dll")]public static extern bool ReleaseCapture();[DllImport("user32.dll")]public static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);}
}
