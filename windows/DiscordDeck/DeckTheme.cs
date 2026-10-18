using System.Drawing.Drawing2D;
namespace DiscordDeck;
static class DeckTheme {
 static readonly Color Ink=Color.FromArgb(29,29,31),Blue=Color.FromArgb(0,113,227),Canvas=Color.FromArgb(245,245,247);
 public static void Apply(Control root){root.ForeColor=Ink;if(root is Form f){f.BackColor=Canvas;f.Font=new Font("Segoe UI",10);}foreach(Control c in root.Controls){
 if(c is Button b){b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.BackColor=b.Text.Contains("stop",StringComparison.OrdinalIgnoreCase)?Color.FromArgb(255,236,235):Color.FromArgb(232,239,250);b.ForeColor=b.Text.Contains("stop",StringComparison.OrdinalIgnoreCase)?Color.FromArgb(194,39,45):Blue;b.Cursor=Cursors.Hand;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(218,230,248);b.Resize+=(_,_)=>Round(b,10);Round(b,10);}
 if(c is TextBox text){text.BackColor=Color.White;text.ForeColor=Ink;}
 if(c is Label label){label.ForeColor=label.ForeColor==Color.DimGray?Color.FromArgb(110,110,115):Ink;}
 if(c is TabControl tabs){tabs.Font=new Font("Segoe UI Semibold",10);tabs.ItemSize=new Size(120,36);tabs.SizeMode=TabSizeMode.Fixed;}
 if(c is TabPage tab)tab.BackColor=Canvas;
 if(c is TableLayoutPanel card&&card.BackColor==Color.White){card.Padding=new Padding(12);card.Resize+=(_,_)=>Round(card,16);Round(card,16);}
 Apply(c);
 }}
 static void Round(Control c,int radius){if(c.Width<radius*2||c.Height<radius*2)return;using var path=new GraphicsPath();int d=radius*2;path.AddArc(0,0,d,d,180,90);path.AddArc(c.Width-d,0,d,d,270,90);path.AddArc(c.Width-d,c.Height-d,d,d,0,90);path.AddArc(0,c.Height-d,d,d,90,90);path.CloseFigure();var old=c.Region;c.Region=new Region(path);old?.Dispose();}
}
