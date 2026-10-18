using System.Reflection;
using System.Text.Json;
namespace DiscordDeck;
static class DesignTest {
 public static void Run(){var original=File.ReadAllBytes(Settings.Config);try{using var form=new MainForm();var flags=BindingFlags.Instance|BindingFlags.NonPublic;object Field(string n)=>typeof(MainForm).GetField(n,flags)!.GetValue(form)!;void Call(string n,params object[] args)=>typeof(MainForm).GetMethod(n,flags)!.Invoke(form,args);void Check(bool ok,string why){if(!ok)throw new Exception(why);}var deck=(Control)Field("deck");form.CreateControl();deck.CreateControl();form.PerformLayout();using var image=new Bitmap(1536,1024);deck.DrawToBitmap(image,deck.ClientRectangle);
 void Click(int x,int y)=>deck.GetType().GetMethod("OnMouseDown",flags)!.Invoke(deck,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});
 Click(100,400);Check((int)Field("bank")==4,"Bank 5 unreachable");
 deck.GetType().GetMethod("OnMouseWheel",flags)!.Invoke(deck,new object[]{new MouseEventArgs(MouseButtons.None,0,700,700,-120)});deck.GetType().GetMethod("OnMouseWheel",flags)!.Invoke(deck,new object[]{new MouseEventArgs(MouseButtons.None,0,700,700,-120)});deck.DrawToBitmap(image,deck.ClientRectangle);Click(600,785);Check((int)Field("selectedSound")==99,"Slot 100 unreachable");
 Click(1506,412);Check(((NumericUpDown)Field("selectedGain")).Value==400,"400 percent unreachable");Call("Dispatch","gain",99*401+137);Check(((NumericUpDown)Field("selectedGain")).Value==137,"Phone gain did not synchronize");
 Click(811,946);Check(((Settings)Field("settings")).MasterVolume==0,"Master zero failed");Click(1212,946);Check(((Settings)Field("settings")).MasterVolume==1,"Master maximum failed");
 form.ClientSize=new Size(1080,720);form.PerformLayout();using var small=new Bitmap(1080,720);deck.DrawToBitmap(small,deck.ClientRectangle);small.Save(Path.Combine(AppContext.BaseDirectory,"design-small.png"));
 File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"design-test.json"),JsonSerializer.Serialize(new{ok=true,bank5=true,slot100=true,gain400=true,phoneGainSync=true,masterRange=true,smallWindow=true}));}finally{File.WriteAllBytes(Settings.Config,original);}}
}
