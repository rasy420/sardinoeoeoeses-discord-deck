using System.Drawing.Drawing2D;
using static DiscordDeck.Lang;
namespace DiscordDeck;

public sealed partial class MainForm {
 // Letzte Meldung der Handy-App (/status kommt etwa jede Sekunde, solange die App offen ist).
 DateTime lastPhoneSeen=DateTime.MinValue;bool phoneWasOnline;
 internal static readonly TimeSpan PhoneTimeout=TimeSpan.FromSeconds(4);
 internal bool PhoneOnline=>DateTime.UtcNow-lastPhoneSeen<PhoneTimeout;
 readonly System.Windows.Forms.Timer phoneWatch=new(){Interval=500};
 internal PhonePairDialog? pairDialog;
 internal void PhoneSeen(){lastPhoneSeen=DateTime.UtcNow;NotePhoneConnected();CheckPhoneOnline();}
 // Lampe oben und im offenen Dialog umschalten, sobald sich der Zustand ändert.
 internal void CheckPhoneOnline(){bool online=PhoneOnline;if(online==phoneWasOnline)return;phoneWasOnline=online;deck.InvalidatePhoneLamp();pairDialog?.UpdateState();}
 void InitPhoneWatch(){phoneWatch.Tick+=(_,_)=>CheckPhoneOnline();phoneWatch.Start();Disposed+=(_,_)=>phoneWatch.Dispose();}
 internal void PairPhone(){Try(()=>{if(lan==null)throw new Exception(T("LAN-Server ist nicht aktiv.","LAN server is not running."));using var d=new PhonePairDialog(this,lan.BeginPairing());pairDialog=d;try{d.ShowDialog(this);}finally{pairDialog=null;}});}

 sealed partial class ReferenceDeck {
  internal static RectangleF PhoneLampBounds=>new(1183,26,10,10);
  internal void InvalidatePhoneLamp()=>InvalidateLogical(Moved(new RectangleF(1025,5,182,51),DX,0));
  void PhoneLamp(Graphics g){if(!owner.PhoneOnline)return;var r=PhoneLampBounds;var c=PhonePairDialog.Green;using(var glow=new SolidBrush(Color.FromArgb(70,c)))g.FillEllipse(glow,RectangleF.Inflate(r,2.5f,2.5f));using var b=new SolidBrush(c);g.FillEllipse(b,r);}
 }
}

// Kopplungsfenster: Code für die erste Verbindung, unten eine Lampe mit dem echten Verbindungszustand.
sealed class PhonePairDialog:Form {
 internal static readonly Color Yellow=Color.FromArgb(244,196,48),Green=Color.FromArgb(72,206,112);
 static readonly Color Gold=Color.FromArgb(232,200,122),Ink=Color.FromArgb(240,239,231),Muted=Color.FromArgb(170,190,210),Card=Color.FromArgb(25,34,41);
 readonly MainForm owner;
 internal readonly Label Code=new(),State=new();internal readonly Lamp Light=new();internal readonly Button Ok=new();
 readonly System.Windows.Forms.Timer dots=new(){Interval=450};int dot;
 public PhonePairDialog(MainForm owner,string code){
  this.owner=owner;Text=T("Handy verbinden","Connect phone");ClientSize=new(460,292);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(13,20,25);ForeColor=Ink;Font=new("Segoe UI",10);AutoScaleDimensions=new(96,96);AutoScaleMode=AutoScaleMode.Dpi;
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(24,18,24,18),ColumnCount=1,RowCount=6};Controls.Add(root);
  foreach(var h in new[]{38,48,26,62,34})root.RowStyles.Add(new(SizeType.Absolute,h));root.RowStyles.Add(new(SizeType.Percent,100));
  root.Controls.Add(new Label{Text=T("HANDY VERBINDEN","CONNECT PHONE"),Dock=DockStyle.Fill,Font=new("Segoe UI Semibold",15),ForeColor=Gold,TextAlign=ContentAlignment.MiddleLeft},0,0);
  root.Controls.Add(new Label{Text=T("Nur bei der ersten Verbindung nötig. Danach verbindet sich das Handy automatisch, sobald die App offen ist.","Only needed the first time. After that the phone connects automatically whenever the app is open."),Dock=DockStyle.Fill,ForeColor=Ink},0,1);
  root.Controls.Add(new Label{Text=T("Code in der Android-App eingeben (2 Minuten gültig):","Enter this code in the Android app (valid for 2 minutes):"),Dock=DockStyle.Fill,ForeColor=Muted,TextAlign=ContentAlignment.BottomLeft},0,2);
  Code.Text=code;Code.Dock=DockStyle.Fill;Code.Font=new("Segoe UI Semibold",28);Code.ForeColor=Ink;Code.TextAlign=ContentAlignment.MiddleLeft;Code.AccessibleName=T("Kopplungscode","Pairing code");root.Controls.Add(Code,0,3);
  root.Controls.Add(new Label{Text=T("PC und Handy müssen im selben lokalen Netz sein.","PC and phone must be on the same local network."),Dock=DockStyle.Fill,ForeColor=Muted,TextAlign=ContentAlignment.TopLeft},0,4);
  var footer=new TableLayoutPanel{Dock=DockStyle.Bottom,Height=44,ColumnCount=3,Margin=Padding.Empty};footer.ColumnStyles.Add(new(SizeType.Absolute,26));footer.ColumnStyles.Add(new(SizeType.Percent,100));footer.ColumnStyles.Add(new(SizeType.Absolute,110));
  Light.Dock=DockStyle.Fill;Light.Margin=new(0,0,6,0);footer.Controls.Add(Light,0,0);
  State.Dock=DockStyle.Fill;State.TextAlign=ContentAlignment.MiddleLeft;State.Margin=Padding.Empty;footer.Controls.Add(State,1,0);
  Ok.Text="OK";Ok.FlatStyle=FlatStyle.Flat;Ok.FlatAppearance.BorderColor=Gold;Ok.BackColor=Gold;Ok.ForeColor=Color.FromArgb(24,20,12);Ok.Dock=DockStyle.Fill;Ok.Margin=new(0,4,0,4);Ok.Cursor=Cursors.Hand;Ok.DialogResult=DialogResult.OK;footer.Controls.Add(Ok,2,0);
  root.Controls.Add(footer,0,5);AcceptButton=Ok;CancelButton=Ok;
  dots.Tick+=(_,_)=>{dot=(dot+1)%3;UpdateState();};dots.Start();UpdateState();
 }
 internal void UpdateState(){bool online=owner.PhoneOnline;Light.On=online;State.ForeColor=online?Green:Yellow;State.Text=online?T("Handy verbunden","Phone connected"):T("Warte auf Handyverbindung","Waiting for phone connection")+new string('.',dot+1);}
 protected override void Dispose(bool disposing){if(disposing)dots.Dispose();base.Dispose(disposing);}

 internal sealed class Lamp:Control {
  bool on;internal bool On{get=>on;set{if(on==value)return;on=value;Invalidate();}}
  public Lamp(){DoubleBuffered=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent?.BackColor??Color.Black);float s=Math.Min(Width,Height)*.5f;var r=new RectangleF((Width-s)/2,(Height-s)/2,s,s);var c=on?Green:Yellow;using(var glow=new SolidBrush(Color.FromArgb(70,c)))g.FillEllipse(glow,RectangleF.Inflate(r,s*.3f,s*.3f));using var b=new SolidBrush(c);g.FillEllipse(b,r);}
 }
}
