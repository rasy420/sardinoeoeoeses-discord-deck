using System.Drawing.Drawing2D;
using static DiscordDeck.Lang;
namespace DiscordDeck;

public sealed partial class MainForm {
 // Hinweis oben auf „Handy verbinden“, solange sich noch nie ein Handy gemeldet hat (nicht während der Einführung).
 internal bool PhoneHint=>!PhoneOnline&&tourStep<0;
 internal string PhoneButtonText=>!PhoneOnline&&settings.PhoneConnected?T("    App öffnen","    Open app"):T("    Handy verbinden","    Connect phone");
 internal void NotePhoneConnected(){if(settings.PhoneConnected)return;settings.PhoneConnected=true;settings.Save();deck.Invalidate();}
}

public sealed partial class MainForm {
 sealed partial class ReferenceDeck {
  internal const float RowStep=135.5f,BankHeight=RowStep*5,ViewTop=190;
  // Fenstergröße: ein einheitlicher Maßstab ohne Verzerrung. Mehr Platz als 1536 × 1024 macht das Raster breiter und höher,
  // rechte Teile wandern um DX nach rechts, untere um DY nach unten.
  internal float UiScale=>Width<=0||Height<=0?1:Math.Min(Width/1536f,Height/1024f);
  internal float LogicalWidth=>Width<=0?1536:Math.Max(1536,Width/UiScale);
  internal float LogicalHeight=>Height<=0?1024:Math.Max(1024,Height/UiScale);
  internal float DX=>LogicalWidth-1536;
  internal float DY=>LogicalHeight-1024;
  internal float PanelLeft=>1169+DX;
  internal float ViewBottom=>865+DY;
  internal float SliderWidth=>401+DX;
  internal Point ToClient(float x,float y)=>new((int)(x*UiScale),(int)(y*UiScale));
  PointF shift;
  // Eine Gruppe verschoben zeichnen: Klickflächen und Mausprüfungen beim Zeichnen gelten dann für die neue Lage.
  void Shifted(Graphics g,float dx,float dy,Action draw){var state=g.Save();var before=shift;var realMouse=mouse;g.TranslateTransform(dx,dy);shift=new(before.X+dx,before.Y+dy);mouse=new(realMouse.X-dx,realMouse.Y-dy);try{draw();}finally{shift=before;mouse=realMouse;g.Restore(state);}}
  protected override void WndProc(ref Message m){if(m.Msg==0x84&&owner.ResizeHit(owner.PointToClient(new Point(unchecked((short)(long)m.LParam),unchecked((short)((long)m.LParam>>16)))))!=0){m.Result=(IntPtr)(-1);return;}base.WndProc(ref m);}
  internal RectangleF Viewport=>new(263,ViewTop,GridRight-263+12,ViewBottom-ViewTop);
  internal RectangleF ScrollbarArea=>new(GridRight-12,ViewTop,12,ViewBottom-ViewTop);
  // Ziel der weichen Scrollbewegung; lockedBank hält die angeklickte Bank gold, bis die Bewegung dort ankommt.
  float scrollTarget;int lockedBank=-1;
  readonly System.Windows.Forms.Timer motion=new(){Interval=15},pulse=new(){Interval=60};
  internal bool Scrolling=>motion.Enabled;
  void InitMotion(){
   motion.Tick+=(_,_)=>StepScroll();
   pulse.Tick+=(_,_)=>{if(owner.audio.Running&&!owner.PhoneHint){pulse.Stop();return;}if(!Visible||owner.WindowState==FormWindowState.Minimized)return;if(!owner.audio.Running)InvalidateLogical(Moved(RectangleF.Inflate(AudioButtonBounds,14,14),DX,DY));if(owner.PhoneHint)InvalidateLogical(Moved(RectangleF.Inflate(PhoneButtonBounds,12,12),DX,0));};
  }
  internal static RectangleF AudioButtonBounds=>new(1302,938,209,48);
  internal static RectangleF PhoneButtonBounds=>new(1031,11,170,39);
  static RectangleF Moved(RectangleF r,float dx,float dy)=>new(r.X+dx,r.Y+dy,r.Width,r.Height);
  void InvalidateLogical(RectangleF r){float s=UiScale;Invalidate(Rectangle.FromLTRB((int)(r.Left*s)-1,(int)(r.Top*s)-1,(int)Math.Ceiling(r.Right*s)+1,(int)Math.Ceiling(r.Bottom*s)+1));}
  // Ruhige Position übernehmen (Sprung ohne Animation), Bank aus der Position ableiten.
  internal void SnapScroll(){motion.Stop();lockedBank=-1;owner.scrollY=Math.Clamp(owner.scrollY,0,MaxScroll);scrollTarget=owner.scrollY;owner.bankIndex=BankAtScroll(owner.scrollY);Invalidate();}
  internal static int BankAtScroll(float y)=>Math.Clamp((int)((y+BankHeight/2)/BankHeight),0,4);
  internal void ScrollBy(float delta){if(owner.tourStep>=0)return;lockedBank=-1;scrollTarget=Math.Clamp((motion.Enabled?scrollTarget:owner.scrollY)+delta,0,MaxScroll);motion.Start();}
  internal void ScrollToBank(int b){b=Math.Clamp(b,0,4);lockedBank=b;owner.bankIndex=b;scrollTarget=b*BankHeight;motion.Start();Invalidate();}
  // Für Tests: laufende Bewegung sofort zu Ende führen.
  internal void FinishScroll(){if(!motion.Enabled)return;owner.scrollY=scrollTarget;motion.Stop();lockedBank=-1;owner.bankIndex=BankAtScroll(owner.scrollY);Invalidate();}
  void StepScroll(){
   float diff=scrollTarget-owner.scrollY;
   if(Math.Abs(diff)<.6f){owner.scrollY=scrollTarget;motion.Stop();lockedBank=-1;}else owner.scrollY+=diff*.28f;
   if(lockedBank<0)owner.bankIndex=BankAtScroll(owner.scrollY);
   if(reorderFrom>=0)reorderOver=TileAt(mouse);
   Invalidate();
  }
  // Klickbereiche der Kacheln enden am sichtbaren Rasterausschnitt, damit nichts darunter oder darüber getroffen wird.
  void ClippedHit(RectangleF rect,Action action,string name){var r=RectangleF.Intersect(rect,Viewport);if(r.Width>0&&r.Height>0)Hit(r,action,name);}
  float ThumbHeight=>Math.Max(48,(ViewBottom-ViewTop-8)*(ViewBottom-ViewTop)/(MaxScroll+ViewBottom-ViewTop));
  RectangleF ThumbBounds{get{float track=ViewBottom-ViewTop-8-ThumbHeight;return new(GridRight-8,ViewTop+4+track*(owner.scrollY/MaxScroll),5,ThumbHeight);}}
  void DrawScrollbar(Graphics g){
   using(var trackPath=Shape(new(GridRight-8,ViewTop+4,5,ViewBottom-ViewTop-8),2.5f))using(var b=new SolidBrush(C(24,32,38)))g.FillPath(b,trackPath);
   bool hot=dragging==6||ScrollbarArea.Contains(mouse);
   using(var thumb=Shape(ThumbBounds,2.5f))using(var b=new SolidBrush(hot?green:C(84,100,113)))g.FillPath(b,thumb);
  }
  void DragScrollbar(float y){float track=ViewBottom-ViewTop-8-ThumbHeight;float t=(y-ViewTop-4-ThumbHeight/2)/track;owner.scrollY=Math.Clamp(t,0,1)*MaxScroll;SnapScroll();}
  // Pulsierender goldener Ring um einen Knopf, der Aufmerksamkeit braucht.
  internal static float PulsePhase=>Environment.TickCount64%1400/1400f;
  void Pulse(Graphics g,RectangleF r,float reach){
   if(!pulse.Enabled)pulse.Start();
   float t=PulsePhase;var ring=RectangleF.Inflate(r,1+t*reach,1+t*reach);
   using var path=Shape(ring,8+t*reach);using var pen=new Pen(Color.FromArgb((int)(210*(1-t)),232,200,122),2.6f);g.DrawPath(pen,path);
   float t2=(t+.5f)%1;var ring2=RectangleF.Inflate(r,1+t2*reach,1+t2*reach);using var path2=Shape(ring2,8+t2*reach);using var pen2=new Pen(Color.FromArgb((int)(120*(1-t2)),232,200,122),1.6f);g.DrawPath(pen2,path2);
  }
  void PulseGlow(Graphics g,RectangleF r){float a=(float)(.5-.5*Math.Cos(PulsePhase*Math.PI*2));using var path=Shape(r,8);using var b=new SolidBrush(Color.FromArgb((int)(55*a),255,255,255));g.FillPath(b,path);}
  // YouTube/TikTok-Import im YouTube-Look: rot, weiße Schrift, kleines Play-Logo.
  void YouTubeButton(Graphics g,RectangleF r,Action action){
   bool hot=r.Contains(mouse);Box(g,r,hot?C(255,40,70):C(255,0,51),hot?C(214,0,30):C(196,0,22),8,C(255,110,120));
   var logo=new RectangleF(r.X+13,r.Y+r.Height/2-11,32,22);using(var lp=Shape(logo,6))using(var wb=new SolidBrush(Color.White))g.FillPath(wb,lp);
   using(var tri=new GraphicsPath()){tri.AddPolygon(new[]{new PointF(logo.X+12.5f,logo.Y+6),new PointF(logo.X+12.5f,logo.Bottom-6),new PointF(logo.X+22,logo.Y+11)});using var rb=new SolidBrush(C(255,0,51));g.FillPath(rb,tri);}
   using var font=new Font("Segoe UI",12.5f,FontStyle.Bold,GraphicsUnit.Pixel);using var brush=new SolidBrush(Color.White);using var fmt=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter};
   var text=T("Von YouTube / TikTok\nimportieren","Import from\nYouTube / TikTok");g.DrawString(text,font,brush,RectangleF.FromLTRB(logo.Right+4,r.Y+3,r.Right-6,r.Bottom-3),fmt);Hit(r,action,text.Replace('\n',' '));
  }
 }
}
