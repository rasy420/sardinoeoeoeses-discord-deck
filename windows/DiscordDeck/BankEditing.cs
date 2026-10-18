using static DiscordDeck.Lang;
namespace DiscordDeck;
public sealed partial class MainForm {
 TextBox? bankNameEditor;
 string BankName(int index)=>string.IsNullOrWhiteSpace(settings.Banks[index].Name)?$"Sounds {index*20+1}–{index*20+20}":settings.Banks[index].Name;
 internal void RenameBank(SoundBank bank,string name){name=name.Trim();if(name.Length==0||name.Length>40||name.Any(char.IsControl))throw new ArgumentException(T("Bitte einen Banknamen mit 1 bis 40 Zeichen eingeben.","Please enter a bank name with 1 to 40 characters."));var before=bank.Name;bank.Name=name;try{settings.Save();}catch{bank.Name=before;throw;}deck.Invalidate();}
 void EditBankName(int index){if(!sorting)return;if(soundEditor!=null){soundEditor.Focus();return;}var target=settings.Banks[index];var starting=BankName(index);BeginSoundEdit(new(78,134+index*64,120,26),starting,text=>{if(string.IsNullOrEmpty(target.Name)&&text.Trim()==starting)return;RenameBank(target,text);},16,"Segoe UI",T("Soundbank umbenennen","Rename sound bank"));bankNameEditor=soundEditor;}
 internal bool MoveBank(int from,int to){
  if(from<0||from>=5||to<0||to>=5||from==to)return false;
  var oldSlots=settings.Slots.ToList();var oldBanks=settings.Banks.ToList();var edited=settings.Slots[selectedSound];var visible=settings.Banks[bank];var pending=trimSlot>=0?settings.Slots[trimSlot]:null;
  var chunks=Enumerable.Range(0,5).Select(i=>oldSlots.Skip(i*20).Take(20).ToList()).ToList();var chunk=chunks[from];chunks.RemoveAt(from);chunks.Insert(to,chunk);var moving=settings.Banks[from];settings.Banks.RemoveAt(from);settings.Banks.Insert(to,moving);settings.Slots=chunks.SelectMany(x=>x).ToList();
  try{settings.Save();}catch{settings.Slots=oldSlots;settings.Banks=oldBanks;throw;}
  selectedSound=settings.Slots.IndexOf(edited);if(pending!=null)trimSlot=settings.Slots.IndexOf(pending);bank=settings.Banks.IndexOf(visible);deck.Invalidate();return true;
 }
 sealed partial class ReferenceDeck {
  int bankFrom=-1,bankOver=-1;
  internal static RectangleF BankPencilBounds(int b)=>new(201,132+b*64,27,29);
  static int BankAt(PointF p){for(int i=0;i<5;i++)if(new RectangleF(14,123+i*64,219,47).Contains(p))return i;return -1;}
  bool BeginBankReorder(PointF p){int b=BankAt(p);if(b<0||BankPencilBounds(b).Contains(p))return false;bankFrom=bankOver=b;reorderDown=p;reorderMoved=false;Capture=true;Cursor=Cursors.SizeAll;Invalidate();return true;}
  bool ContinueBankReorder(PointF p){if(bankFrom<0)return false;if(Math.Abs(p.X-reorderDown.X)+Math.Abs(p.Y-reorderDown.Y)>6)reorderMoved=true;bankOver=BankAt(p);Cursor=bankOver>=0?Cursors.SizeAll:Cursors.No;Invalidate();return true;}
  bool FinishBankReorder(){if(bankFrom<0)return false;int from=bankFrom,to=BankAt(mouse);bool moved=reorderMoved;CancelReorder();if(moved&&to>=0)owner.Try(()=>owner.MoveBank(from,to));else if(!moved){owner.bank=from;owner.scrollRow=0;Invalidate();}return true;}
  void DrawBankReorder(Graphics g){if(bankFrom<0)return;if(bankOver>=0){var r=new RectangleF(14,123+bankOver*64,219,47);using var pen=new Pen(green,3);using var path=Shape(r,10);g.DrawPath(pen,path);}if(!reorderMoved)return;var ghost=new RectangleF(Math.Clamp(mouse.X+14,20,1200),Math.Clamp(mouse.Y-15,72,860),220,40);Box(g,ghost,C(73,61,34),C(39,34,24),8,green);TextAt(g,"↕ "+owner.BankName(bankFrom),ghost.X+12,ghost.Y+10,15,ink,width:196,height:24);}
 }
}
