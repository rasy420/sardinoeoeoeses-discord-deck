using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using NAudio.Wave;
using static DiscordDeck.Lang;
namespace DiscordDeck;

sealed class AudioImportDialog:Form {
 internal readonly ImportWave Wave=new(){Dock=DockStyle.Fill,Margin=new Padding(0,10,0,10)};
 internal readonly TimeUpDown From=new(),To=new();
 internal readonly Button Preview=new(),Save=new();
 internal readonly TextBox SoundName=new();internal readonly ComboBox Slots=new();
 readonly TextBox url=new();readonly ComboBox slots;readonly Label status=new(),duration=new();readonly Button load=new(),whole=new();
 readonly Form host;readonly Settings settings;readonly Action changed;readonly CancellationTokenSource cancel=new();
 readonly string folder=Path.Combine(Path.GetTempPath(),"DiscordDeck-"+Guid.NewGuid().ToString("N"));
 string? source;string loadedUrl="";bool busy,syncing,closing,nameEdited,naming;IWavePlayer? player;AudioEngine.Clip? clip;double previewFrom;
 readonly Stopwatch clock=new();readonly System.Windows.Forms.Timer ticker=new(){Interval=33};
 static readonly Color Gold=Color.FromArgb(232,200,122),Ink=Color.FromArgb(240,239,231),Muted=Color.FromArgb(170,190,210),Card=Color.FromArgb(25,34,41);
 internal bool Previewing=>player!=null;
 internal double PreviewFrom=>previewFrom;
 // Zeitangabe: unter einer Minute in Sekunden (45,30), ab 60 Sekunden als Minuten (1:01,50).
 internal static string Clock(double s,int decimals){var c=CultureInfo.GetCultureInfo(En?"en-US":"de-DE");s=Math.Round(Math.Max(0,s),decimals);var zeros=decimals>0?"."+new string('0',decimals):"";if(s<60)return s.ToString("0"+zeros,c);int m=(int)Math.Floor(s/60);return m+":"+(s-m*60).ToString("00"+zeros,c);}
 internal static string Length(double s)=>s<60?Clock(s,2)+" s":Clock(s,2)+" min";
 internal static bool TryParseClock(string text,out decimal value){
  value=0;text=text.Trim().Replace("min","",StringComparison.OrdinalIgnoreCase).Replace("s","",StringComparison.OrdinalIgnoreCase).Trim().Replace(',','.');if(text.Length==0)return false;
  var parts=text.Split(':');if(parts.Length>3)return false;decimal total=0;
  for(int i=0;i<parts.Length;i++){if(!decimal.TryParse(parts[i].Trim(),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var part)||part<0)return false;if(i<parts.Length-1&&part!=Math.Floor(part))return false;total=total*60+part;}
  value=total;return true;
 }
 public AudioImportDialog(Form host,Settings settings,Action changed){
  slots=Slots;this.host=host;this.settings=settings;this.changed=changed;Text=T("YouTube / TikTok importieren","Import from YouTube / TikTok");ClientSize=new(760,602);MinimumSize=new(740,630);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(13,20,25);ForeColor=Ink;Font=new("Segoe UI",10);AutoScaleDimensions=new(96,96);AutoScaleMode=AutoScaleMode.Dpi;
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(24),ColumnCount=1,RowCount=10};Controls.Add(root);
  var rowHeights=new[]{44,46,58,154,48,44,30,44,54};foreach(var h in rowHeights)root.RowStyles.Add(new(SizeType.Absolute,h));root.RowStyles.Add(new(SizeType.Percent,100));root.Layout+=(_,_)=>{for(int i=0;i<rowHeights.Length;i++){float height=rowHeights[i]*DeviceDpi/96f;if(Math.Abs(root.RowStyles[i].Height-height)>.1f)root.RowStyles[i].Height=height;}};
  root.Controls.Add(new Label{Text=T("AUDIO IMPORTIEREN","IMPORT AUDIO"),Dock=DockStyle.Fill,Font=new("Segoe UI Semibold",16),ForeColor=Gold,TextAlign=ContentAlignment.MiddleLeft},0,0);
  var link=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};link.ColumnStyles.Add(new(SizeType.Percent,100));link.ColumnStyles.Add(new(SizeType.Absolute,170));url.Dock=DockStyle.Fill;url.Margin=new(0,6,12,4);url.PlaceholderText=T("YouTube-, Shorts- oder TikTok-Link","YouTube, Shorts or TikTok link");url.BackColor=Card;url.ForeColor=Ink;link.Controls.Add(url);Style(load,T("Audio laden","Load audio"));load.Dock=DockStyle.Fill;load.Margin=new(0,0,0,4);link.Controls.Add(load);root.Controls.Add(link,0,1);
  status.Text=T("Link laden. Marker ziehen oder I/O über der Timeline drücken = Start/Ende, in die Timeline klicken = ab dort anhören.","Load a link. Drag the markers or press I/O over the timeline = in/out, click the timeline to listen from there.");status.ForeColor=Muted;status.Dock=DockStyle.Fill;status.Padding=new(0,7,0,0);root.Controls.Add(status,0,2);root.Controls.Add(Wave,0,3);
  var times=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=5,Margin=Padding.Empty};foreach(var width in new[]{55,145,55,145})times.ColumnStyles.Add(new(SizeType.Absolute,width));times.ColumnStyles.Add(new(SizeType.Percent,100));
  times.Controls.Add(Label(T("Start","Start")));times.Controls.Add(From);times.Controls.Add(Label(T("Ende","End")));times.Controls.Add(To);duration.Dock=DockStyle.Fill;duration.ForeColor=Muted;duration.TextAlign=ContentAlignment.MiddleRight;times.Controls.Add(duration);root.Controls.Add(times,0,4);
  foreach(var n in new[]{From,To}){n.DecimalPlaces=3;n.Increment=.05m;n.Maximum=7200;n.Enabled=false;n.BackColor=Card;n.ForeColor=Ink;n.TextAlign=HorizontalAlignment.Right;n.Dock=DockStyle.Fill;n.Margin=new(0,6,12,6);n.ValueChanged+=(_,_)=>{if(!syncing)Sync();};}
  var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};actions.ColumnStyles.Add(new(SizeType.Percent,50));actions.ColumnStyles.Add(new(SizeType.Percent,50));Style(Preview,T("▶  Vorhören","▶  Preview"));Style(whole,T("Ganzer Sound","Full sound"));foreach(var b in new[]{Preview,whole}){b.Dock=DockStyle.Fill;b.Enabled=false;}Preview.Margin=new(0,0,6,4);whole.Margin=new(6,0,0,4);actions.Controls.Add(Preview);actions.Controls.Add(whole);root.Controls.Add(actions,0,5);
  var heads=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};heads.ColumnStyles.Add(new(SizeType.Percent,50));heads.ColumnStyles.Add(new(SizeType.Percent,50));heads.Controls.Add(Label(T("AUF SOUNDPLATZ SPEICHERN","SAVE TO SOUND SLOT")));var nameHead=Label(T("NAME DES SOUNDS","SOUND NAME"));nameHead.Padding=new(6,0,0,0);heads.Controls.Add(nameHead);root.Controls.Add(heads,0,6);
  var pick=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};pick.ColumnStyles.Add(new(SizeType.Percent,50));pick.ColumnStyles.Add(new(SizeType.Percent,50));
  slots.Dock=DockStyle.Fill;slots.Margin=new(0,3,6,3);slots.DropDownStyle=ComboBoxStyle.DropDownList;slots.BackColor=Card;slots.ForeColor=Ink;slots.FlatStyle=FlatStyle.Flat;slots.DrawMode=DrawMode.OwnerDrawFixed;slots.ItemHeight=26;slots.DrawItem+=(_,e)=>{if(e.Index<0)return;using var fill=new SolidBrush(Card);e.Graphics.FillRectangle(fill,e.Bounds);TextRenderer.DrawText(e.Graphics,slots.Items[e.Index].ToString(),slots.Font,Rectangle.Inflate(e.Bounds,-8,-2),Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);};for(int i=0;i<settings.Slots.Count;i++)slots.Items.Add(SlotText(i));slots.SelectedIndex=Math.Max(0,NextFree(settings,-1));pick.Controls.Add(slots);
  SoundName.Dock=DockStyle.Fill;SoundName.Margin=new(6,5,0,3);SoundName.BackColor=Card;SoundName.ForeColor=Ink;SoundName.MaxLength=100;SoundName.PlaceholderText=T("Name für den Knopf","Name for the button");SoundName.AccessibleName=T("Name des Sounds","Sound name");pick.Controls.Add(SoundName);root.Controls.Add(pick,0,7);
  SoundName.TextChanged+=(_,_)=>{if(!naming)nameEdited=true;};slots.SelectedIndexChanged+=(_,_)=>SuggestName();SuggestName();
  var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=new(0,8,0,0)};footer.ColumnStyles.Add(new(SizeType.Percent,35));footer.ColumnStyles.Add(new(SizeType.Percent,65));var close=new Button();Style(close,T("Schließen","Close"));Style(Save,T("Ausschnitt speichern","Save clip"),true);Save.Enabled=false;close.Dock=Save.Dock=DockStyle.Fill;close.Margin=new(0,0,8,0);Save.Margin=Padding.Empty;footer.Controls.Add(close);footer.Controls.Add(Save);root.Controls.Add(footer,0,8);
  load.Click+=async(_,_)=>await DownloadAudio();Preview.Click+=(_,_)=>TogglePreview();whole.Click+=(_,_)=>{syncing=true;From.Value=0;To.Value=To.Maximum;syncing=false;Sync();};Save.Click+=async(_,_)=>await SaveAudio();close.Click+=(_,_)=>Close();Wave.Moved+=(start,value)=>{if(busy||source==null)return;var v=(decimal)Math.Round(value,3);if(start)From.Value=Math.Clamp(v,0,Math.Max(0,To.Value-.05m));else To.Value=Math.Clamp(v,Math.Min(To.Maximum,From.Value+.05m),To.Maximum);};
  Wave.Scrub+=value=>{if(busy||source==null)return;StopPreview();Wave.Position=value;Wave.Invalidate();};Wave.Seek+=value=>{if(!busy&&source!=null)PlayFrom(value);};
  ticker.Tick+=(_,_)=>{Wave.Position=previewFrom+clock.Elapsed.TotalSeconds;Wave.Invalidate();};
  FormClosing+=(_,e)=>{StopPreview();if(busy){closing=true;cancel.Cancel();e.Cancel=true;status.Text=T("Vorgang wird abgebrochen …","Cancelling …");}};
 }
 string SlotText(int i)=>$"{i+1:00} · {settings.Slots[i].Name}"+(IsFree(settings.Slots[i])?T("  (frei)","  (free)"):"");
 // Freier Platz = noch keine Sounddatei zugewiesen. Gesucht wird ab dem Platz nach "after", am Ende geht es vorne weiter.
 internal static bool IsFree(SoundSlot s)=>string.IsNullOrWhiteSpace(s.File);
 internal static int NextFree(Settings s,int after){for(int k=1;k<=s.Slots.Count;k++){int i=((after+k)%s.Slots.Count+s.Slots.Count)%s.Slots.Count;if(IsFree(s.Slots[i]))return i;}return -1;}
 // Vorschlag im Namensfeld: eigener Name des Platzes, sonst der Titel des geladenen Videos. Eigene Eingaben bleiben stehen.
 void SuggestName(){if(nameEdited||slots.SelectedIndex<0)return;var slot=settings.Slots[slots.SelectedIndex];naming=true;SoundName.Text=slot.CustomName||source==null?slot.Name:Path.GetFileNameWithoutExtension(source);naming=false;}
 static Label Label(string text)=>new(){Text=text,Dock=DockStyle.Fill,ForeColor=Muted,TextAlign=ContentAlignment.MiddleLeft};
 static void Style(Button b,string text,bool gold=false){b.Text=text;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=gold?Gold:Color.FromArgb(64,77,87);b.BackColor=gold?Gold:Card;b.ForeColor=gold?Color.FromArgb(24,20,12):Ink;b.Padding=new(10,4,10,4);b.Cursor=Cursors.Hand;b.AutoEllipsis=true;}
 void Busy(bool value){busy=value;load.Enabled=url.Enabled=slots.Enabled=SoundName.Enabled=!value;From.Enabled=To.Enabled=Wave.Enabled=!value&&source!=null;whole.Enabled=!value&&source!=null;Preview.Enabled=Save.Enabled=!value&&source!=null&&To.Value>From.Value;if(!value&&closing)BeginInvoke(Close);}
 internal async Task LoadSource(string file,string originalUrl=""){
  StopPreview();using(var r=new AudioFileReader(file)){if(r.TotalTime.TotalSeconds<.05)throw new IOException(T("Audio ist zu kurz.","Audio is too short."));Wave.Total=r.TotalTime.TotalSeconds;}
  var peaks=await Task.Run(()=>{var p=new float[240];using var r=new AudioFileReader(file);long total=Math.Max(1,r.Length/4),pos=0;var b=new float[32768];int n;while((n=r.Read(b,0,b.Length))>0){cancel.Token.ThrowIfCancellationRequested();for(int i=0;i<n;i++){int bin=(int)Math.Min(p.Length-1,(pos+i)*p.Length/total);p[bin]=Math.Max(p[bin],Math.Abs(b[i]));}pos+=n;}var max=Math.Max(.05f,p.Max());for(int i=0;i<p.Length;i++)p[i]=(float)Math.Sqrt(p[i]/max);return p;},cancel.Token);
  source=file;loadedUrl=originalUrl;syncing=true;From.Value=0;From.Maximum=To.Maximum=(decimal)Math.Floor(Wave.Total*1000)/1000;To.Value=To.Maximum;syncing=false;Wave.Peaks=peaks;status.Text=Path.GetFileNameWithoutExtension(file)+"  ·  "+Length(Wave.Total)+"\n"+T("Marker ziehen oder I/O über der Timeline = Start/Ende · klicken = ab dort anhören","Drag markers or press I/O over the timeline = in/out · click = listen from there");SuggestName();Sync();Busy(false);
 }
 async Task DownloadAudio(){Busy(true);StopPreview();try{var link=url.Text.Trim();var file=await YouTubeImport.Download(link,Path.Combine(folder,Guid.NewGuid().ToString("N")),cancel.Token);await LoadSource(file,link);}catch(Exception e){status.Text=e is OperationCanceledException?T("Abgebrochen","Cancelled"):e.Message;}finally{Busy(false);}}
 void Sync(){StopPreview();Wave.From=(double)From.Value;Wave.To=(double)To.Value;duration.Text=To.Value>From.Value?T("Dauer: ","Length: ")+Length((double)(To.Value-From.Value)):T("Ende nach Start wählen","Choose end after start");duration.ForeColor=To.Value>From.Value?Muted:Color.FromArgb(255,145,135);Preview.Enabled=Save.Enabled=!busy&&source!=null&&To.Value-From.Value>=.001m;Wave.Invalidate();}
 internal void TogglePreview(){if(player!=null){StopPreview();return;}if(source==null||To.Value<=From.Value)return;StartClip((double)From.Value,(double)To.Value);}
 // Bestätigung nach dem Speichern: grüner Haken im Knopf und in der Statuszeile, nach 3 s wieder normal.
 static readonly Color Done=Color.FromArgb(92,196,128);System.Windows.Forms.Timer? savedTimer;
 internal bool SavedConfirmed=>savedTimer?.Enabled==true;
 void ShowSaved(int index,string name,int next){
  Style(Save,T("✓  Gespeichert","✓  Saved"));Save.BackColor=Save.FlatAppearance.BorderColor=Done;Save.ForeColor=Color.FromArgb(14,30,20);
  status.ForeColor=Done;status.Text=$"✓  "+T("Gespeichert auf Platz ","Saved to slot ")+$"{index+1:00}: {name}\n"+(next>=0?T("Nächster freier Platz ist ausgewählt: ","Next free slot selected: ")+$"{next+1:00}":T("Alle Plätze sind belegt – zum Überschreiben einen Platz wählen.","All slots are taken – choose a slot to overwrite."));
  if(savedTimer==null){savedTimer=new System.Windows.Forms.Timer{Interval=3000};status.TextChanged+=(_,_)=>{if(SavedConfirmed&&!status.Text.StartsWith("✓"))EndSaved(null,EventArgs.Empty);};}savedTimer.Tick-=EndSaved;savedTimer.Tick+=EndSaved;savedTimer.Stop();savedTimer.Start();}
 void EndSaved(object? sender,EventArgs e){savedTimer?.Stop();if(IsDisposed)return;Style(Save,T("Ausschnitt speichern","Save clip"),true);status.ForeColor=Muted;}
 // I/O wie im Schnittprogramm: Start- bzw. Ende-Marker an die Mausposition über der Timeline, sonst an den Abspielkopf.
 internal bool SetMark(bool start){
  if(busy||source==null)return false;double at=Wave.Hover>=0?Wave.Hover:Wave.Position;if(at<0){status.Text=T("Für I/O die Maus über die Timeline halten oder abspielen.","For I/O hover over the timeline or play it.");return false;}
  bool playing=Previewing;var v=Math.Clamp((decimal)Math.Round(at,3),0,To.Maximum);syncing=true;
  if(start){if(v>To.Maximum-.05m)v=To.Maximum-.05m;if(v>To.Value-.05m)To.Value=To.Maximum;From.Value=v;}
  else{if(v<.05m)v=.05m;if(v<From.Value+.05m)From.Value=0;To.Value=v;}
  syncing=false;Sync();status.ForeColor=Muted;status.Text=(start?T("Start (I): ","In (I): "):T("Ende (O): ","Out (O): "))+Clock((double)v,2);
  if(playing&&start)PlayFrom(Math.Max(at,(double)From.Value));return true;}
 protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if((keyData==Keys.I||keyData==Keys.O)&&ActiveControl is not (TextBoxBase or UpDownBase)&&SetMark(keyData==Keys.I))return true;return base.ProcessCmdKey(ref msg,keyData);}
 // Klick in die Timeline: ab dieser Stelle anhören – bis zum Ende-Marker, oder hinter dem Ausschnitt bis zum Ende des Audios.
 internal void PlayFrom(double at){
  if(source==null)return;StopPreview();at=Math.Clamp(at,0,Wave.Total);double end=at<(double)To.Value-.05?(double)To.Value:Wave.Total;
  if(end-at<.25){Wave.Position=at;Wave.Invalidate();return;}StartClip(at,end);
 }
 void StartClip(double from,double to){try{clip=new AudioEngine.Clip(source!,1,null,from,to);var mine=clip;previewFrom=from;if(host is MainForm main)player=main.StartPreview(clip,()=>{if(!IsDisposed&&ReferenceEquals(clip,mine))StopPreview();});else{player=new WaveOutEvent();player.PlaybackStopped+=(_,_)=>{if(IsHandleCreated&&!IsDisposed)BeginInvoke(()=>{if(ReferenceEquals(clip,mine))StopPreview();});};player.Init(clip);player.Play();}clock.Restart();ticker.Start();Wave.Position=from;Wave.Invalidate();Preview.Text=T("■  Stopp","■  Stop");}catch(Exception e){StopPreview();status.Text=e.Message;}}
 void StopPreview(){ticker.Stop();clock.Reset();var p=player;player=null;p?.Stop();p?.Dispose();clip?.Dispose();clip=null;Wave.Position=-1;Wave.Invalidate();Preview.Text=T("▶  Vorhören","▶  Preview");}
 internal async Task SaveAudio(){if(source==null||busy||To.Value<=From.Value)return;
  var typed=SoundName.Text.Trim();string? customName=null;
  if(nameEdited&&typed.Length>0){if(typed.Any(char.IsControl)){status.Text=T("Der Name enthält ungültige Zeichen.","The name contains invalid characters.");return;}customName=typed;}
  Busy(true);StopPreview();if(SavedConfirmed)EndSaved(null,EventArgs.Empty);string? target=null;bool saved=false;try{int index=slots.SelectedIndex;var slot=settings.Slots[index];if(host is MainForm editing&&ReferenceEquals(slot,editing.EditedSlot)&&!editing.ResolveDraft())return;var title=Path.GetFileNameWithoutExtension(source);var dest=Path.Combine(Settings.Folder,"Sounds","YouTube",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dest);target=Path.Combine(dest,title+".mp3");await YouTubeImport.Trim(source,target,(double)From.Value,(double)To.Value,cancel.Token);
   if(host is MainForm main)main.ReplaceSoundSource(slot,target,title,loadedUrl,customName);else{slot.File=target;slot.TrimStart=slot.TrimEnd=0;if(customName!=null){slot.Name=customName;slot.CustomName=true;}else if(!slot.CustomName)slot.Name=title;slot.SourceUrl=loadedUrl;settings.Save();}
   saved=true;slots.Items[index]=SlotText(index);nameEdited=false;int next=NextFree(settings,index);if(next>=0)slots.SelectedIndex=next;SuggestName();changed();ShowSaved(index,slot.Name,next);}catch(Exception e){if(!saved&&target!=null&&File.Exists(target))File.Delete(target);status.ForeColor=Muted;status.Text=e.Message;}finally{Busy(false);}}
 protected override void Dispose(bool disposing){if(disposing){savedTimer?.Dispose();StopPreview();cancel.Cancel();cancel.Dispose();ticker.Dispose();try{if(Directory.Exists(folder))Directory.Delete(folder,true);}catch{}}base.Dispose(disposing);}
}

// Zahlenfeld, das Zeiten ab einer Minute als m:ss anzeigt und Eingaben wie "1:01,5" oder "61,5" versteht.
sealed class TimeUpDown:NumericUpDown {
 protected override void UpdateEditText(){ChangingText=true;Text=AudioImportDialog.Clock((double)Value,DecimalPlaces);}
 protected override void ValidateEditText(){if(UserEdit&&AudioImportDialog.TryParseClock(Text,out var v)){UserEdit=false;Value=Math.Clamp(Math.Round(v,DecimalPlaces),Minimum,Maximum);}UserEdit=false;UpdateEditText();}
}

sealed class ImportWave:Control {
 public double Total=1,From,To,Position=-1,Hover=-1;public float[]? Peaks;public event Action<bool,double>? Moved;public event Action<double>? Seek,Scrub;int drag;
 const int Grab=10;
 public ImportWave(){DoubleBuffered=true;Cursor=Cursors.Hand;AccessibleName="Audio waveform: start and end";TabStop=true;}
 public float X(double value)=>(float)(14+(Width-28)*value/Math.Max(.001,Total));
 double Time(int x)=>Math.Clamp((x-14)*Total/Math.Max(1,Width-28),0,Total);
 internal int MarkerAt(int x){var dFrom=Math.Abs(x-X(From));var dTo=Math.Abs(x-X(To));if(Math.Min(dFrom,dTo)>Grab)return 0;return dFrom<=dTo?1:2;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(19,27,33));var gold=Color.FromArgb(232,200,122);float mid=Height/2f;
  if(Peaks==null){using var font=new Font("Segoe UI",11);using var brush=new SolidBrush(Color.FromArgb(170,190,210));g.DrawString(T("Nach dem Laden hier den Ausschnitt wählen","Load audio to select a clip here"),font,brush,new RectangleF(18,mid-16,Width-36,40));return;}
  using(var band=new SolidBrush(Color.FromArgb(24,232,200,122)))g.FillRectangle(band,X(From),10,Math.Max(0,X(To)-X(From)),Height-20);
  for(int i=0;i<Peaks.Length;i++){double t=(i+.5)*Total/Peaks.Length;float x=14+(Width-28)*(i+.5f)/Peaks.Length,h=Math.Max(2,Peaks[i]*(Height-42));using var pen=new Pen(t>=From&&t<=To?gold:Color.FromArgb(65,81,95),Math.Max(1,(Width-28)/Peaks.Length*.65f));g.DrawLine(pen,x,mid-h/2,x,mid+h/2);}
  foreach(var s in new[]{From,To}){float x=X(s);using var pen=new Pen(gold,2);g.DrawLine(pen,x,10,x,Height-10);using var brush=new SolidBrush(gold);g.FillRectangle(brush,new RectangleF(x-6,6,12,22));using var grip=new Pen(Color.FromArgb(44,38,24));g.DrawLine(grip,x-2,11,x-2,22);g.DrawLine(grip,x+2,11,x+2,22);}
  using var small=new Font("Segoe UI",8.5f);
  using(var muted=new SolidBrush(Color.FromArgb(120,140,158))){g.DrawString("0",small,muted,12,Height-17);var end=AudioImportDialog.Clock(Total,1);g.DrawString(end,small,muted,Width-14-g.MeasureString(end,small).Width,Height-17);}
  if(Hover>=0&&drag==0){float x=X(Hover);using var pen=new Pen(Color.FromArgb(150,255,255,255),1){DashStyle=DashStyle.Dash};g.DrawLine(pen,x,10,x,Height-10);Tag(g,small,x,AudioImportDialog.Clock(Hover,1),Color.FromArgb(52,64,74),Color.White);}
  if(Position>=0){float x=X(Position);using var pen=new Pen(Color.White,2);g.DrawLine(pen,x,10,x,Height-10);Tag(g,small,x,"▶ "+AudioImportDialog.Clock(Position,1),Color.White,Color.FromArgb(19,27,33));}
  if(drag is 1 or 2){var s=drag==1?From:To;Tag(g,small,X(s),AudioImportDialog.Clock(s,2),gold,Color.FromArgb(24,20,12));}
 }
 // Kleines Zeit-Schild oben über einer Linie.
 void Tag(Graphics g,Font font,float x,string text,Color back,Color fore){var size=g.MeasureString(text,font);var r=new RectangleF(Math.Clamp(x-size.Width/2-5,2,Width-size.Width-12),Height-34,size.Width+10,size.Height+2);using(var b=new SolidBrush(back))g.FillRectangle(b,r);using var f=new SolidBrush(fore);g.DrawString(text,font,f,r.X+5,r.Y+1);}
 protected override void OnMouseDown(MouseEventArgs e){if(e.Button!=MouseButtons.Left||Peaks==null)return;Focus();drag=MarkerAt(e.X);Capture=true;if(drag==0){drag=3;Scrub?.Invoke(Time(e.X));}else Moved?.Invoke(drag==1,Time(e.X));Invalidate();}
 protected override void OnMouseMove(MouseEventArgs e){if(Peaks==null)return;Hover=Time(e.X);if(drag is 1 or 2)Moved?.Invoke(drag==1,Hover);else if(drag==3)Scrub?.Invoke(Hover);else Cursor=MarkerAt(e.X)!=0?Cursors.SizeWE:Cursors.Hand;Invalidate();}
 protected override void OnMouseUp(MouseEventArgs e){var was=drag;drag=0;Capture=false;if(was==3&&Peaks!=null)Seek?.Invoke(Time(e.X));Invalidate();}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);Hover=-1;Invalidate();}
 protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)drag=0;}
}
