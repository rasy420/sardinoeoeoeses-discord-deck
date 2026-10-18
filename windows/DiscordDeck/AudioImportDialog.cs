using System.Diagnostics;
using System.Drawing.Drawing2D;
using NAudio.Wave;
using static DiscordDeck.Lang;
namespace DiscordDeck;

sealed class AudioImportDialog:Form {
 internal readonly ImportWave Wave=new(){Dock=DockStyle.Fill,Margin=new Padding(0,10,0,10)};
 internal readonly NumericUpDown From=new(),To=new();
 internal readonly Button Preview=new(),Save=new();
 readonly TextBox url=new();readonly ComboBox slots=new();readonly Label status=new(),duration=new();readonly Button load=new(),whole=new();
 readonly Form host;readonly Settings settings;readonly Action changed;readonly CancellationTokenSource cancel=new();
 readonly string folder=Path.Combine(Path.GetTempPath(),"DiscordDeck-"+Guid.NewGuid().ToString("N"));
 string? source;string loadedUrl="";bool busy,syncing,closing;IWavePlayer? player;AudioEngine.Clip? clip;
 readonly Stopwatch clock=new();readonly System.Windows.Forms.Timer ticker=new(){Interval=33};
 static readonly Color Gold=Color.FromArgb(232,200,122),Ink=Color.FromArgb(240,239,231),Muted=Color.FromArgb(170,190,210),Card=Color.FromArgb(25,34,41);
 internal bool Previewing=>player!=null;
 public AudioImportDialog(Form host,Settings settings,Action changed){
  this.host=host;this.settings=settings;this.changed=changed;Text=T("YouTube / TikTok importieren","Import from YouTube / TikTok");ClientSize=new(760,602);MinimumSize=new(740,630);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(13,20,25);ForeColor=Ink;Font=new("Segoe UI",10);AutoScaleDimensions=new(96,96);AutoScaleMode=AutoScaleMode.Dpi;
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(24),ColumnCount=1,RowCount=10};Controls.Add(root);
  var rowHeights=new[]{44,46,58,154,48,44,30,44,54};foreach(var h in rowHeights)root.RowStyles.Add(new(SizeType.Absolute,h));root.RowStyles.Add(new(SizeType.Percent,100));root.Layout+=(_,_)=>{for(int i=0;i<rowHeights.Length;i++){float height=rowHeights[i]*DeviceDpi/96f;if(Math.Abs(root.RowStyles[i].Height-height)>.1f)root.RowStyles[i].Height=height;}};
  root.Controls.Add(new Label{Text=T("AUDIO IMPORTIEREN","IMPORT AUDIO"),Dock=DockStyle.Fill,Font=new("Segoe UI Semibold",16),ForeColor=Gold,TextAlign=ContentAlignment.MiddleLeft},0,0);
  var link=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};link.ColumnStyles.Add(new(SizeType.Percent,100));link.ColumnStyles.Add(new(SizeType.Absolute,170));url.Dock=DockStyle.Fill;url.Margin=new(0,6,12,4);url.PlaceholderText=T("YouTube-, Shorts- oder TikTok-Link","YouTube, Shorts or TikTok link");url.BackColor=Card;url.ForeColor=Ink;link.Controls.Add(url);Style(load,T("Audio laden","Load audio"));load.Dock=DockStyle.Fill;load.Margin=new(0,0,0,4);link.Controls.Add(load);root.Controls.Add(link,0,1);
  status.Text=T("Link laden. Danach die goldenen Marker auf den gewünschten Ausschnitt ziehen.","Load a link, then drag the gold markers to select the clip.");status.ForeColor=Muted;status.Dock=DockStyle.Fill;status.Padding=new(0,7,0,0);root.Controls.Add(status,0,2);root.Controls.Add(Wave,0,3);
  var times=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=5,Margin=Padding.Empty};foreach(var width in new[]{55,145,55,145})times.ColumnStyles.Add(new(SizeType.Absolute,width));times.ColumnStyles.Add(new(SizeType.Percent,100));
  times.Controls.Add(Label(T("Start","Start")));times.Controls.Add(From);times.Controls.Add(Label(T("Ende","End")));times.Controls.Add(To);duration.Dock=DockStyle.Fill;duration.ForeColor=Muted;duration.TextAlign=ContentAlignment.MiddleRight;times.Controls.Add(duration);root.Controls.Add(times,0,4);
  foreach(var n in new[]{From,To}){n.DecimalPlaces=3;n.Increment=.05m;n.Maximum=7200;n.Enabled=false;n.BackColor=Card;n.ForeColor=Ink;n.TextAlign=HorizontalAlignment.Right;n.Dock=DockStyle.Fill;n.Margin=new(0,6,12,6);n.ValueChanged+=(_,_)=>{if(!syncing)Sync();};}
  var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};actions.ColumnStyles.Add(new(SizeType.Percent,50));actions.ColumnStyles.Add(new(SizeType.Percent,50));Style(Preview,T("▶  Vorhören","▶  Preview"));Style(whole,T("Ganzer Sound","Full sound"));foreach(var b in new[]{Preview,whole}){b.Dock=DockStyle.Fill;b.Enabled=false;}Preview.Margin=new(0,0,6,4);whole.Margin=new(6,0,0,4);actions.Controls.Add(Preview);actions.Controls.Add(whole);root.Controls.Add(actions,0,5);
  root.Controls.Add(Label(T("AUF SOUNDPLATZ SPEICHERN","SAVE TO SOUND SLOT")),0,6);slots.Dock=DockStyle.Fill;slots.DropDownStyle=ComboBoxStyle.DropDownList;slots.BackColor=Card;slots.ForeColor=Ink;slots.FlatStyle=FlatStyle.Flat;slots.DrawMode=DrawMode.OwnerDrawFixed;slots.ItemHeight=26;slots.DrawItem+=(_,e)=>{if(e.Index<0)return;using var fill=new SolidBrush(Card);e.Graphics.FillRectangle(fill,e.Bounds);TextRenderer.DrawText(e.Graphics,slots.Items[e.Index].ToString(),slots.Font,Rectangle.Inflate(e.Bounds,-8,-2),Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);};for(int i=0;i<settings.Slots.Count;i++)slots.Items.Add($"{i+1:00} · {settings.Slots[i].Name}");slots.SelectedIndex=0;root.Controls.Add(slots,0,7);
  var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=new(0,8,0,0)};footer.ColumnStyles.Add(new(SizeType.Percent,35));footer.ColumnStyles.Add(new(SizeType.Percent,65));var close=new Button();Style(close,T("Schließen","Close"));Style(Save,T("Ausschnitt speichern","Save clip"),true);Save.Enabled=false;close.Dock=Save.Dock=DockStyle.Fill;close.Margin=new(0,0,8,0);Save.Margin=Padding.Empty;footer.Controls.Add(close);footer.Controls.Add(Save);root.Controls.Add(footer,0,8);
  load.Click+=async(_,_)=>await DownloadAudio();Preview.Click+=(_,_)=>TogglePreview();whole.Click+=(_,_)=>{syncing=true;From.Value=0;To.Value=To.Maximum;syncing=false;Sync();};Save.Click+=async(_,_)=>await SaveAudio();close.Click+=(_,_)=>Close();Wave.Moved+=(start,value)=>{if(busy||source==null)return;var v=(decimal)Math.Round(value,3);if(start)From.Value=Math.Clamp(v,0,Math.Max(0,To.Value-.05m));else To.Value=Math.Clamp(v,Math.Min(To.Maximum,From.Value+.05m),To.Maximum);};
  ticker.Tick+=(_,_)=>{Wave.Position=(double)From.Value+clock.Elapsed.TotalSeconds;Wave.Invalidate();};
  FormClosing+=(_,e)=>{StopPreview();if(busy){closing=true;cancel.Cancel();e.Cancel=true;status.Text=T("Vorgang wird abgebrochen …","Cancelling …");}};
 }
 static Label Label(string text)=>new(){Text=text,Dock=DockStyle.Fill,ForeColor=Muted,TextAlign=ContentAlignment.MiddleLeft};
 static void Style(Button b,string text,bool gold=false){b.Text=text;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=gold?Gold:Color.FromArgb(64,77,87);b.BackColor=gold?Gold:Card;b.ForeColor=gold?Color.FromArgb(24,20,12):Ink;b.Padding=new(10,4,10,4);b.Cursor=Cursors.Hand;b.AutoEllipsis=true;}
 void Busy(bool value){busy=value;load.Enabled=url.Enabled=slots.Enabled=!value;From.Enabled=To.Enabled=Wave.Enabled=!value&&source!=null;whole.Enabled=!value&&source!=null;Preview.Enabled=Save.Enabled=!value&&source!=null&&To.Value>From.Value;if(!value&&closing)BeginInvoke(Close);}
 internal async Task LoadSource(string file,string originalUrl=""){
  StopPreview();using(var r=new AudioFileReader(file)){if(r.TotalTime.TotalSeconds<.05)throw new IOException(T("Audio ist zu kurz.","Audio is too short."));Wave.Total=r.TotalTime.TotalSeconds;}
  var peaks=await Task.Run(()=>{var p=new float[240];using var r=new AudioFileReader(file);long total=Math.Max(1,r.Length/4),pos=0;var b=new float[32768];int n;while((n=r.Read(b,0,b.Length))>0){cancel.Token.ThrowIfCancellationRequested();for(int i=0;i<n;i++){int bin=(int)Math.Min(p.Length-1,(pos+i)*p.Length/total);p[bin]=Math.Max(p[bin],Math.Abs(b[i]));}pos+=n;}var max=Math.Max(.05f,p.Max());for(int i=0;i<p.Length;i++)p[i]=(float)Math.Sqrt(p[i]/max);return p;},cancel.Token);
  source=file;loadedUrl=originalUrl;syncing=true;From.Value=0;From.Maximum=To.Maximum=(decimal)Math.Floor(Wave.Total*1000)/1000;To.Value=To.Maximum;syncing=false;Wave.Peaks=peaks;status.Text=Path.GetFileNameWithoutExtension(file);Sync();Busy(false);
 }
 async Task DownloadAudio(){Busy(true);StopPreview();try{var link=url.Text.Trim();var file=await YouTubeImport.Download(link,Path.Combine(folder,Guid.NewGuid().ToString("N")),cancel.Token);await LoadSource(file,link);}catch(Exception e){status.Text=e is OperationCanceledException?T("Abgebrochen","Cancelled"):e.Message;}finally{Busy(false);}}
 void Sync(){StopPreview();Wave.From=(double)From.Value;Wave.To=(double)To.Value;duration.Text=To.Value>From.Value?T("Dauer: ","Length: ")+MainForm.Seconds((double)(To.Value-From.Value))+" s":T("Ende nach Start wählen","Choose end after start");duration.ForeColor=To.Value>From.Value?Muted:Color.FromArgb(255,145,135);Preview.Enabled=Save.Enabled=!busy&&source!=null&&To.Value-From.Value>=.001m;Wave.Invalidate();}
 internal void TogglePreview(){if(player!=null){StopPreview();return;}if(source==null||To.Value<=From.Value)return;try{clip=new AudioEngine.Clip(source,1,null,(double)From.Value,(double)To.Value);var mine=clip;if(host is MainForm main)player=main.StartPreview(clip,()=>{if(!IsDisposed&&ReferenceEquals(clip,mine))StopPreview();});else{player=new WaveOutEvent();player.PlaybackStopped+=(_,_)=>{if(IsHandleCreated&&!IsDisposed)BeginInvoke(()=>{if(ReferenceEquals(clip,mine))StopPreview();});};player.Init(clip);player.Play();}clock.Restart();ticker.Start();Preview.Text=T("■  Stopp","■  Stop");}catch(Exception e){StopPreview();status.Text=e.Message;}}
 void StopPreview(){ticker.Stop();clock.Reset();var p=player;player=null;p?.Stop();p?.Dispose();clip?.Dispose();clip=null;Wave.Position=-1;Wave.Invalidate();Preview.Text=T("▶  Vorhören","▶  Preview");}
 internal async Task SaveAudio(){if(source==null||busy||To.Value<=From.Value)return;Busy(true);StopPreview();string? target=null;bool saved=false;try{var slot=settings.Slots[slots.SelectedIndex];if(host is MainForm editing&&ReferenceEquals(slot,editing.EditedSlot)&&!editing.ResolveDraft())return;var title=Path.GetFileNameWithoutExtension(source);var dest=Path.Combine(Settings.Folder,"Sounds","YouTube",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dest);target=Path.Combine(dest,title+".mp3");await YouTubeImport.Trim(source,target,(double)From.Value,(double)To.Value,cancel.Token);if(host is MainForm main)main.ReplaceSoundSource(slot,target,title,loadedUrl);else{slot.File=target;slot.TrimStart=slot.TrimEnd=0;if(!slot.CustomName)slot.Name=title;slot.SourceUrl=loadedUrl;settings.Save();}saved=true;changed();status.Text=T("Gespeichert: ","Saved: ")+slot.Name;}catch(Exception e){if(!saved&&target!=null&&File.Exists(target))File.Delete(target);status.Text=e.Message;}finally{Busy(false);}}
 protected override void Dispose(bool disposing){if(disposing){StopPreview();cancel.Cancel();cancel.Dispose();ticker.Dispose();try{if(Directory.Exists(folder))Directory.Delete(folder,true);}catch{}}base.Dispose(disposing);}
}

sealed class ImportWave:Control {
 public double Total=1,From,To,Position=-1;public float[]? Peaks;public event Action<bool,double>? Moved;int drag;
 public ImportWave(){DoubleBuffered=true;Cursor=Cursors.SizeWE;AccessibleName="Audio waveform: start and end";TabStop=true;}
 public float X(double value)=>(float)(14+(Width-28)*value/Math.Max(.001,Total));
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(19,27,33));var gold=Color.FromArgb(232,200,122);float mid=Height/2f;
  if(Peaks==null){using var font=new Font("Segoe UI",11);using var brush=new SolidBrush(Color.FromArgb(170,190,210));g.DrawString(T("Nach dem Laden hier den Ausschnitt wählen","Load audio to select a clip here"),font,brush,new RectangleF(18,mid-16,Width-36,40));return;}
  using(var band=new SolidBrush(Color.FromArgb(24,232,200,122)))g.FillRectangle(band,X(From),10,Math.Max(0,X(To)-X(From)),Height-20);
  for(int i=0;i<Peaks.Length;i++){double t=(i+.5)*Total/Peaks.Length;float x=14+(Width-28)*(i+.5f)/Peaks.Length,h=Math.Max(2,Peaks[i]*(Height-42));using var pen=new Pen(t>=From&&t<=To?gold:Color.FromArgb(65,81,95),Math.Max(1,(Width-28)/Peaks.Length*.65f));g.DrawLine(pen,x,mid-h/2,x,mid+h/2);}
  foreach(var s in new[]{From,To}){float x=X(s);using var pen=new Pen(gold,2);g.DrawLine(pen,x,10,x,Height-10);using var brush=new SolidBrush(gold);g.FillRectangle(brush,new RectangleF(x-6,6,12,22));using var grip=new Pen(Color.FromArgb(44,38,24));g.DrawLine(grip,x-2,11,x-2,22);g.DrawLine(grip,x+2,11,x+2,22);}
  if(Position>=From&&Position<=To){using var pen=new Pen(Color.White,2);g.DrawLine(pen,X(Position),10,X(Position),Height-10);}
 }
 void MoveTo(int x)=>Moved?.Invoke(drag==1,Math.Clamp((x-14)*Total/Math.Max(1,Width-28),0,Total));
 protected override void OnMouseDown(MouseEventArgs e){if(e.Button!=MouseButtons.Left||Peaks==null)return;Focus();drag=Math.Abs(e.X-X(From))<=Math.Abs(e.X-X(To))?1:2;Capture=true;MoveTo(e.X);}
 protected override void OnMouseMove(MouseEventArgs e){if(drag!=0)MoveTo(e.X);}
 protected override void OnMouseUp(MouseEventArgs e){drag=0;Capture=false;}
 protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)drag=0;}
}
