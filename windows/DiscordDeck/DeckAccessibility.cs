namespace DiscordDeck;
public sealed partial class MainForm {
 sealed partial class ReferenceDeck {
  int focusHit=-1;bool activationHeld;
  protected override AccessibleObject CreateAccessibilityInstance()=>new DeckAccessible(this);
  sealed class DeckAccessible:ControlAccessibleObject {
   readonly ReferenceDeck deck;public DeckAccessible(ReferenceDeck d):base(d){deck=d;}
   public override int GetChildCount()=>deck.hits.Count;
   public override AccessibleObject? GetChild(int index)=>index>=0&&index<deck.hits.Count?new ActionAccessible(deck,index):null;
  }
  sealed class ActionAccessible:AccessibleObject {
   readonly ReferenceDeck deck;readonly int index;public ActionAccessible(ReferenceDeck d,int i){deck=d;index=i;}
   public override string? Name{get=>deck.hits[index].name;set{}}
   public override string DefaultAction=>"Activate";
   public override AccessibleRole Role=>AccessibleRole.PushButton;
   public override AccessibleObject? Parent=>deck.AccessibilityObject;
   public override Rectangle Bounds{get{var r=deck.hits[index].rect;float s=deck.UiScale;return deck.RectangleToScreen(new((int)(r.X*s),(int)(r.Y*s),(int)(r.Width*s),(int)(r.Height*s)));}}
   public override AccessibleStates State=>AccessibleStates.Focusable|(deck.Focused&&deck.focusHit==index?AccessibleStates.Focused:AccessibleStates.None);
   public override void DoDefaultAction(){if(index<deck.hits.Count)deck.owner.Try(deck.hits[index].action);}
  }
  void DrawActionFocus(Graphics g){if(!Focused||focusHit<0||focusHit>=hits.Count)return;var r=RectangleF.Inflate(hits[focusHit].rect,-2,-2);using var pen=new Pen(Color.FromArgb(204,218,230),1.5f){DashStyle=System.Drawing.Drawing2D.DashStyle.Dot};g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);}
  protected override void OnKeyUp(KeyEventArgs e){base.OnKeyUp(e);if(e.KeyCode is Keys.Enter or Keys.Space)activationHeld=false;}
 }
}
