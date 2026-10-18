using System.Drawing.Drawing2D;
using static DiscordDeck.Lang;
namespace DiscordDeck;

public sealed partial class MainForm {
 bool sorting;
 void ToggleSorting(){
  if(sorting&&soundEditor!=null&&ReferenceEquals(soundEditor,bankNameEditor)){soundEditor.Dispose();soundEditor=null;bankNameEditor=null;editError="";}sorting=!sorting;deck.CancelReorder();
  status.Text="Sortieren: "+(sorting?T("Sounds oder ganze Bänke ziehen. Stifte an den Bänken ändern deren Namen.","Drag sounds or entire banks. The pencils beside banks rename them."):T("Reihenfolge gespeichert – auch auf dem Handy.","Order saved – also on your phone."));deck.Invalidate();
 }
 internal bool MoveSound(int from,int to){
  if(from<0||to<0||from>=settings.Slots.Count||to>=settings.Slots.Count||from==to)return false;
  var before=settings.Slots.ToList();var selected=settings.Slots[selectedSound];
  var pending=trimSlot>=0&&trimSlot<settings.Slots.Count?settings.Slots[trimSlot]:null;
  var sound=settings.Slots[from];settings.Slots.RemoveAt(from);settings.Slots.Insert(to,sound);
  try{settings.Save();}catch{settings.Slots=before;throw;}
  selectedSound=settings.Slots.IndexOf(selected);if(pending!=null)trimSlot=settings.Slots.IndexOf(pending);
  if(!TrimDirty)SyncSelected();status.Text="Sortieren: "+T($"„{sound.Name}“ auf Platz {to+1} verschoben. Handy übernimmt die Reihenfolge automatisch.",$"Moved “{sound.Name}” to slot {to+1}. Your phone receives the new order automatically.");deck.Invalidate();return true;
 }

 sealed partial class ReferenceDeck {
  internal RectangleF ReorderButtonBounds=>new(19,512,211,42);
  void DrawReorderButton(Graphics g){Button(g,ReorderButtonBounds,owner.sorting?T("✓   Fertig","✓   Done"):T("Bearbeiten","Edit"),owner.ToggleSorting);}
  int reorderFrom=-1,reorderOver=-1;PointF reorderDown;bool reorderMoved;
  int hoveredBank=-1;DateTime hoverSince,lastScroll;
  readonly System.Windows.Forms.Timer reorderTimer=new(){Interval=100};
  internal int Columns=>4;
  internal float GridRight=>1524;
  internal int MaxScroll=>Math.Max(0,(int)Math.Ceiling(20d/Columns)-5);
  internal RectangleF TileBounds(int index){int j=index-owner.bank*20;float step=(GridRight-273)/Columns;return new(273+j%Columns*step,195+(j/Columns-owner.scrollRow)*135.5f,step-13,120);}
  internal int TileAt(PointF p){
   if(owner.soundPanelOpen&&p.X>=1169||!new RectangleF(263,190,GridRight-263,675).Contains(p))return -1;
   for(int j=0;j<20;j++){int index=owner.bank*20+j;var r=TileBounds(index);if(r.Y>=190&&r.Y<=858&&r.Contains(p))return index;}return -1;
  }
  void InitReorder(){reorderTimer.Tick+=(_,_)=>ReorderTick(DateTime.UtcNow);}
  internal void CancelReorder(){bankFrom=bankOver=-1;reorderFrom=reorderOver=hoveredBank=-1;reorderMoved=false;reorderTimer.Stop();Capture=false;Cursor=Cursors.Default;Invalidate();}
  internal void ReorderTick(DateTime now){
   if(reorderFrom<0)return;
   int target=-1;for(int b=0;b<5;b++)if(new RectangleF(14,123+b*64,219,47).Contains(mouse)){target=b;break;}
   if(target!=hoveredBank){hoveredBank=target;hoverSince=now;}
   if(target>=0&&target!=owner.bank&&(now-hoverSince).TotalMilliseconds>=550){owner.bank=target;owner.scrollRow=0;reorderOver=-1;Invalidate();}
   if(mouse.X>=263&&mouse.X<=GridRight&&(now-lastScroll).TotalMilliseconds>=450){
    int next=mouse.Y>=837&&mouse.Y<=889?Math.Min(MaxScroll,owner.scrollRow+1):mouse.Y>=190&&mouse.Y<=220?Math.Max(0,owner.scrollRow-1):owner.scrollRow;
    if(next!=owner.scrollRow){owner.scrollRow=next;lastScroll=now;Invalidate();}
   }
   reorderOver=TileAt(mouse);
  }
  bool BeginReorder(PointF p){
   if(!owner.sorting)return false;if(BeginBankReorder(p))return true;int index=TileAt(p);if(index<0)return false;
   reorderFrom=reorderOver=index;reorderDown=p;reorderMoved=false;lastScroll=DateTime.UtcNow;hoveredBank=-1;Capture=true;Cursor=Cursors.SizeAll;reorderTimer.Start();Invalidate();return true;
  }
  bool ContinueReorder(PointF p){
   if(ContinueBankReorder(p))return true;if(reorderFrom<0)return false;
   if(Math.Abs(p.X-reorderDown.X)+Math.Abs(p.Y-reorderDown.Y)>6)reorderMoved=true;
   ReorderTick(DateTime.UtcNow);Cursor=TileAt(p)>=0?Cursors.SizeAll:Cursors.No;Invalidate();return true;
  }
  bool FinishReorder(){
   if(FinishBankReorder())return true;if(reorderFrom<0)return false;int from=reorderFrom,to=TileAt(mouse);bool moved=reorderMoved;CancelReorder();
   if(moved&&to>=0)owner.Try(()=>owner.MoveSound(from,to));return true;
  }
  protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&(reorderFrom>=0||bankFrom>=0))CancelReorder();}
  void DrawReorder(Graphics g){
   if(!owner.sorting)return;
   TextAt(g,T("Ziehen · Bänke links halten · Esc beendet","Drag · hold over a bank · Esc to finish"),276,87,12,green,width:650,height:20);
   DrawBankReorder(g);if(reorderFrom<0)return;
   if(reorderOver>=0&&reorderOver!=reorderFrom){var r=TileBounds(reorderOver);using var path=Shape(r,12);using var p=new Pen(green,3);g.DrawPath(p,path);TextAt(g,T("Hier einfügen","Insert here"),r.X+12,r.Y+46,15,green,true,width:r.Width-24,height:24);}
   var x=Math.Clamp(mouse.X+16,263,928);var y=Math.Clamp(mouse.Y-20,194,846);var ghost=new RectangleF(x,y,225,42);Box(g,ghost,C(73,61,34),C(39,34,24),8,green);TextAt(g,"↕  "+owner.settings.Slots[reorderFrom].Name,x+12,y+10,16,ink,true,width:201,height:24);
  }
 }
}
