using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using static DiscordDeck.Lang;
namespace DiscordDeck;
// Abspielbereich (von–bis) je Sound; gilt für jede Quelle (Datei, MyInstants, YouTube) und überall, wo der Sound abgespielt wird.
public sealed partial class MainForm {
 readonly Dictionary<string,(DateTime stamp,double seconds)> durations=new(StringComparer.OrdinalIgnoreCase);
 readonly Dictionary<string,float[]?> peaks=new(StringComparer.OrdinalIgnoreCase);
 internal double Duration(string path){
  if(string.IsNullOrEmpty(path)||!File.Exists(path))return 0;var stamp=File.GetLastWriteTimeUtc(path);
  if(durations.TryGetValue(path,out var known)&&known.stamp==stamp)return known.seconds;
  double seconds;try{using var reader=new AudioFileReader(path);seconds=reader.TotalTime.TotalSeconds;}catch{seconds=0;}
  durations[path]=(stamp,seconds);return seconds;
 }
 // Echte Wellenform (Spitzenwerte) für die Vorschau; wird im Hintergrund berechnet.
 public const int PeakBins=106;
 internal float[]? Peaks(string path){
  if(peaks.TryGetValue(path,out var known))return known;peaks[path]=null;
  Task.Run(()=>{var result=new float[PeakBins];try{using var r=new AudioFileReader(path);long total=Math.Max(1,r.Length/4);var buf=new float[16384];long pos=0;int n;
    while((n=r.Read(buf,0,buf.Length))>0){for(int i=0;i<n;i++){int bin=(int)Math.Min(PeakBins-1,(pos+i)*PeakBins/total);var v=Math.Abs(buf[i]);if(v>result[bin])result[bin]=v;}pos+=n;}
    var max=Math.Max(.05f,result.Max());for(int i=0;i<PeakBins;i++)result[i]=(float)Math.Sqrt(result[i]/max);}catch{}
   if(!IsDisposed&&IsHandleCreated)try{BeginInvoke(()=>{if(!IsDisposed){peaks[path]=result;deck.Invalidate();}});}catch(InvalidOperationException){}});
  return null;
 }
 internal static string Seconds(double s)=>s.ToString("0.00",System.Globalization.CultureInfo.GetCultureInfo(En?"en-US":"de-DE"));
 internal string TrimLabel(SoundSlot slot){var total=Duration(slot.File);if(total<=0)return "–";var (start,end)=AudioEngine.Clip.Range(slot.TrimStart,slot.TrimEnd,total);return start==0&&end==total?T("Ganz","Full")+$" · {Seconds(total)} s":$"{Seconds(start)} – {Seconds(end)} s";}

 // Bereich direkt in der Vorschau rechts: ziehen, anhören, speichern.
 int trimSlot=-1;double trimFrom,trimTo;string trimFile="";
 internal (double from,double to,double total)? PanelTrim(){
  var slot=settings.Slots[selectedSound];var total=Duration(slot.File);if(total<=0)return null;
  if(trimSlot!=selectedSound||trimFile!=slot.File){StopPanelPreview();(trimFrom,trimTo)=AudioEngine.Clip.Range(slot.TrimStart,slot.TrimEnd,total);trimSlot=selectedSound;trimFile=slot.File;}
  return (trimFrom,trimTo,total);
 }
 internal bool TrimDirty{get{if(PanelTrim() is not {} t)return draftInvalid||Math.Abs(draftGain-settings.Slots[selectedSound].Volume)>1e-6;var (s,e)=AudioEngine.Clip.Range(settings.Slots[selectedSound].TrimStart,settings.Slots[selectedSound].TrimEnd,t.total);return Math.Abs(s-t.from)>1e-9||Math.Abs(e-t.to)>1e-9||Math.Abs(draftGain-settings.Slots[selectedSound].Volume)>1e-6||draftInvalid;}}
 internal void DragTrim(bool start,double seconds){
  if(PanelTrim() is not {} t)return;StopPanelPreview();seconds=Math.Clamp(seconds,0,t.total);draftInvalid=false;editError="";
  if(start)trimFrom=Math.Min(seconds,trimTo-Math.Min(.001,t.total/100));else trimTo=Math.Max(seconds,trimFrom+Math.Min(.001,t.total/100));
  trimFrom=Math.Max(0,trimFrom);trimTo=Math.Min(t.total,trimTo);deck.Invalidate();
 }
 internal void ResetTrim(){ForgetTrim();draftGain=settings.Slots[selectedSound].Volume;draftInvalid=false;editError="";soundEditor?.Dispose();soundEditor=null;SyncSelected();deck.Invalidate();}
 internal void SaveTrim(){
  if(draftInvalid)throw new InvalidOperationException(T("Bitte die markierte Eingabe korrigieren.","Please correct the marked value."));
  if(PanelTrim() is not {} t)throw new InvalidOperationException(T("Die Soundquelle fehlt oder kann nicht gelesen werden.","The sound source is missing or cannot be read."));if(t.from<0||t.from>=t.to||t.to>t.total)throw new InvalidOperationException(T("Start muss vor Ende und innerhalb der Audiodauer liegen.","Start must precede end and be within the audio duration."));var slot=settings.Slots[selectedSound];bool full=trimFrom<=0&&trimTo>=t.total;var before=(slot.TrimStart,slot.TrimEnd,slot.Volume);
  slot.TrimStart=full?0:trimFrom;slot.TrimEnd=full||trimTo>=t.total?0:trimTo;slot.Volume=draftGain;try{settings.Save();}catch{(slot.TrimStart,slot.TrimEnd,slot.Volume)=before;throw;}editError="";
  status.Text=T("Gespeichert: ","Saved: ")+slot.Name+" · "+(full?T("ganzer Sound","full sound"):$"{Seconds(trimFrom)} – {Seconds(trimTo)} s");deck.Invalidate();
 }
 void ForgetTrim(){trimSlot=-1;StopPanelPreview();}

 // Vorhören über das Mithör-Gerät (bzw. Windows-Standard), nie ins virtuelle Mikrofon.
 IWavePlayer? panelPlayer;AudioEngine.Clip? panelClip;readonly Stopwatch panelClock=new();double panelFrom;System.Windows.Forms.Timer? panelTicker;
 internal bool PanelPreviewing=>panelPlayer!=null;
 internal double PanelPosition=>panelFrom+panelClock.Elapsed.TotalSeconds;
 internal IWavePlayer StartPreview(AudioEngine.Clip clip,Action stopped){
  MMDevice? device=null;try{using var en=new MMDeviceEnumerator();device=string.IsNullOrEmpty(settings.MonitorId)?en.GetDefaultAudioEndpoint(DataFlow.Render,Role.Multimedia):en.GetDevice(settings.MonitorId);}catch{}
  IWavePlayer player=device!=null?new WasapiOut(device,AudioClientShareMode.Shared,true,80):new WaveOutEvent();
  player.PlaybackStopped+=(_,_)=>{if(!IsDisposed)BeginInvoke(stopped);};player.Init(clip);player.Play();return player;
 }
 internal void TogglePanelPreview(){
  if(panelPlayer!=null){StopPanelPreview();return;}
  if(draftInvalid||PanelTrim() is not {} t)return;var slot=settings.Slots[selectedSound];
  try{panelClip=new AudioEngine.Clip(slot.File,draftGain,null,t.from,t.to);var mine=panelClip;panelFrom=t.from;
   panelPlayer=StartPreview(panelClip,()=>{if(ReferenceEquals(panelClip,mine))StopPanelPreview();});panelClock.Restart();
   panelTicker=new System.Windows.Forms.Timer{Interval=33};panelTicker.Tick+=(_,_)=>deck.Invalidate();panelTicker.Start();
  }catch(Exception e){StopPanelPreview();status.Text=T("Vorhören fehlgeschlagen: ","Preview failed: ")+e.Message;}
  deck.Invalidate();
 }
 void StopPanelPreview(){panelTicker?.Dispose();panelTicker=null;panelPlayer?.Stop();panelPlayer?.Dispose();panelPlayer=null;panelClip?.Dispose();panelClip=null;panelClock.Reset();deck?.Invalidate();}

 // Zahnrad-Menü eines Soundplatzes.
 internal void SoundMenu(int index,Point screen){OpenSoundSettings(index);}
 internal void EditTrim(){
  var slot=settings.Slots[selectedSound];var total=Duration(slot.File);
  if(total<=0){status.Text=T("Kein Sound auf diesem Platz – zuerst eine Datei auswählen.","No sound on this slot – choose a file first.");deck.Invalidate();return;}
  StopPanelPreview();var (start,end)=PanelTrim() is {} p?(p.from,p.to):AudioEngine.Clip.Range(slot.TrimStart,slot.TrimEnd,total);
  using var form=new Form{Text=T("Länge zuschneiden","Trim length")+$" · Sound {selectedSound+1:00}",ClientSize=new Size(520,230),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=new Font("Segoe UI",10)};form.SuspendLayout();
  var title=new Label{Text=slot.Name+"   ·   "+T("Gesamtlänge","Total length")+$" {Seconds(total)} s",AutoEllipsis=true,Bounds=new Rectangle(16,14,488,24)};
  decimal max=(decimal)Math.Floor(total*100)/100;
  NumericUpDown Number(int x,decimal value)=>new(){DecimalPlaces=2,Increment=.1m,Minimum=0,Maximum=max,Value=Math.Clamp(value,0,max),Bounds=new Rectangle(x,50,110,28),TextAlign=HorizontalAlignment.Right};
  var fromLabel=new Label{Text=T("Von (Sekunden)","From (seconds)"),Bounds=new Rectangle(16,53,110,24)};var from=Number(128,(decimal)Math.Round(start,2));
  var toLabel=new Label{Text=T("Bis (Sekunden)","To (seconds)"),Bounds=new Rectangle(268,53,110,24)};var to=Number(380,(decimal)Math.Round(end,2));
  var bar=new TrimBar{Bounds=new Rectangle(16,92,488,34),Total=total};
  var info=new Label{Bounds=new Rectangle(16,132,488,22),ForeColor=Color.DimGray};
  string play=T("▶  Vorhören","▶  Preview"),stop=T("■  Stopp","■  Stop");
  var preview=new Button{Text=play,Bounds=new Rectangle(16,180,110,34)};var whole=new Button{Text=T("Ganzer Sound","Full sound"),Bounds=new Rectangle(134,180,110,34)};
  var save=new Button{Text=T("Übernehmen","Apply"),Bounds=new Rectangle(276,180,110,34),DialogResult=DialogResult.OK};var cancel=new Button{Text=T("Abbrechen","Cancel"),Bounds=new Rectangle(394,180,110,34),DialogResult=DialogResult.Cancel};
  IWavePlayer? player=null;AudioEngine.Clip? clip=null;
  void StopPreview(){player?.Stop();player?.Dispose();player=null;clip?.Dispose();clip=null;preview.Text=play;}
  void Sync(){bar.From=(double)from.Value;bar.To=(double)to.Value;bool ok=to.Value-from.Value>=.05m;info.Text=ok?T("Spielt ","Plays ")+$"{Seconds((double)(to.Value-from.Value))} s":T("Ende muss mindestens 0,05 s nach dem Start liegen.","End must be at least 0.05 s after start.");info.ForeColor=ok?Color.DimGray:Color.Firebrick;save.Enabled=preview.Enabled=ok;bar.Invalidate();}
  from.ValueChanged+=(_,_)=>Sync();to.ValueChanged+=(_,_)=>Sync();
  bar.Moved+=(isStart,value)=>{var v=Math.Clamp((decimal)Math.Round(value,2),0,max);if(isStart)from.Value=Math.Min(v,to.Value);else to.Value=Math.Max(v,from.Value);};
  whole.Click+=(_,_)=>{from.Value=0;to.Value=max;};
  preview.Click+=(_,_)=>{
   if(player!=null){StopPreview();return;}
   try{clip=new AudioEngine.Clip(slot.File,Math.Max(slot.Volume,.05f),null,(double)from.Value,(double)to.Value);var mine=clip;player=StartPreview(clip,()=>{if(!form.IsDisposed&&ReferenceEquals(clip,mine))StopPreview();});preview.Text=stop;}
   catch(Exception e){StopPreview();info.Text=T("Vorhören fehlgeschlagen: ","Preview failed: ")+e.Message;info.ForeColor=Color.Firebrick;}
  };
  form.FormClosing+=(_,_)=>StopPreview();
  form.Controls.AddRange(new Control[]{title,fromLabel,from,toLabel,to,bar,info,preview,whole,save,cancel});form.AcceptButton=save;form.CancelButton=cancel;Sync();
  // Koordinaten sind für 96 dpi angegeben; bei Windows-Skalierung mitwachsen, damit nichts abgeschnitten wird.
  form.AutoScaleDimensions=new SizeF(96f,96f);form.AutoScaleMode=AutoScaleMode.Dpi;form.ResumeLayout(false);
  if(form.ShowDialog(this)!=DialogResult.OK)return;
  trimSlot=selectedSound;trimFrom=(double)from.Value;trimTo=(double)to.Value;SaveTrim();
 }

 // Balken mit zwei ziehbaren Markern für Start und Ende.
 sealed class TrimBar:Control {
  public double Total=1,From,To;public event Action<bool,double>? Moved;int dragging;
  public TrimBar(){DoubleBuffered=true;Cursor=Cursors.Hand;}
  float X(double s)=>(float)(8+(Width-16)*s/Total);double S(int x)=>Math.Clamp((x-8)*Total/(Width-16),0,Total);
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var mid=Height/2f;
   using(var b=new SolidBrush(Color.FromArgb(225,228,232)))g.FillRectangle(b,8,mid-5,Width-16,10);
   using(var b=new SolidBrush(Color.FromArgb(0,120,212)))g.FillRectangle(b,X(From),mid-5,X(To)-X(From),10);
   foreach(var s in new[]{From,To}){using var b=new SolidBrush(Color.White);using var p=new Pen(Color.FromArgb(0,90,170),2);g.FillEllipse(b,X(s)-7,mid-8,14,16);g.DrawEllipse(p,X(s)-7,mid-8,14,16);}}
  protected override void OnMouseDown(MouseEventArgs e){dragging=Math.Abs(e.X-X(From))<=Math.Abs(e.X-X(To))?1:2;if(e.X>X(To))dragging=2;if(e.X<X(From))dragging=1;Capture=true;Moved?.Invoke(dragging==1,S(e.X));}
  protected override void OnMouseMove(MouseEventArgs e){if(dragging!=0)Moved?.Invoke(dragging==1,S(e.X));}
  protected override void OnMouseUp(MouseEventArgs e){dragging=0;Capture=false;}
 }
}
