using System.Text.Json;
namespace DiscordDeck;
// Automatischer Test (--tour-test, mit DISCORDDECK_HOME auf einen leeren Testordner): Tour beim ersten Start, Bedienung, Sprache.
public sealed partial class MainForm {
 public static void RunTourTest(){
  var log=new List<string>();var errors=new List<string>();
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(_,e)=>errors.Add(e.Exception.ToString());
  void Check(bool ok,string what){log.Add((ok?"OK   ":"FEHL ")+what);if(!ok)errors.Add(what);}
  void Pump(int ms){var until=DateTime.Now.AddMilliseconds(ms);while(DateTime.Now<until){Application.DoEvents();Thread.Sleep(10);}}
  var dir=AppContext.BaseDirectory;
  AutoTour=true;var form=new MainForm();form.Show();Pump(1500);
  IntPtr L(float x,float y)=>(IntPtr)(((int)(y*form.deck.Height/1024)<<16)|(int)(x*form.deck.Width/1536));
  void Click(float x,float y){Native.SendMessage(form.deck.Handle,0x201,(IntPtr)1,L(x,y));Native.SendMessage(form.deck.Handle,0x202,IntPtr.Zero,L(x,y));Pump(150);}
  void Key(Keys k){Native.SendMessage(form.deck.Handle,0x100,(IntPtr)(int)k,IntPtr.Zero);Pump(120);}
  void Shot(string name){form.deck.Invalidate();Pump(80);using var b=new Bitmap(form.deck.Width,form.deck.Height);form.deck.DrawToBitmap(b,form.deck.ClientRectangle);b.Save(Path.Combine(dir,name));}
  Check(form.tourStep==0,"Tour startet beim ersten Start");
  int n=TourSteps().Length;Check(n>=8,$"{n} Schritte");
  Check(TourSteps().Any(s=>s.Title.Contains("Länge")),"Schritt zur Länge vorhanden");
  // Während der Tour darf ein Klick auf eine Kachel nichts auslösen.
  int bank=form.bank,sel=form.selectedSound;Click(400,800);Check(form.selectedSound==sel&&form.tourStep==0,"Klick neben der Karte wird ignoriert");
  for(int i=0;i<n;i++){Shot($"tour-de-{i+1}.png");if(i<n-1){Key(Keys.Right);Check(form.tourStep==i+1,$"Weiter → Schritt {i+2}");}}
  Key(Keys.Left);Check(form.tourStep==n-2,"Zurück");Key(Keys.Right);Key(Keys.Enter);Check(form.tourStep==-1,"Fertig beendet die Tour");
  Check(Settings.Load().TourDone,"TourDone gespeichert");
  // „?“ oben öffnet die Tour wieder, Esc überspringt.
  Click(1004,30);Check(form.tourStep==0,"„?“ öffnet die Tour erneut");Key(Keys.Escape);Check(form.tourStep==-1,"Esc überspringt");
  // „Tour überspringen“ per Mausklick: Karte des ersten Schritts steht mittig.
  form.StartTour();Pump(100);Shot("tour-skip.png");
  float h=0;using(var g=form.deck.CreateGraphics())h=ReferenceDeck.MeasureBody(g,TourSteps()[0].Text,430-56)+196;float cardBottom=(1024+h)/2,cardX=(1536-430)/2f;
  Click(cardX+60,cardBottom-62+16);Check(form.tourStep==-1,"„Tour überspringen“ per Klick");
  // Sprache: DE/EN-Knopf oben.
  Click(950,30);Check(Lang.En&&Settings.Load().Language=="en","Umschalten auf Englisch");Check(form.tray.ContextMenuStrip!.Items[0].Text=="Open","Tray-Menü englisch");
  form.StartTour();Key(Keys.Right);Key(Keys.Right);Key(Keys.Right);Key(Keys.Right);Shot("tour-en-5.png");form.EndTour();Shot("deck-en.png");
  form.exitRequested=true;form.Close();form.Dispose();Pump(300);
  // Neustart: Sprache bleibt, Tour kommt nicht noch einmal.
  Lang.En=false;form=new MainForm();form.Show();Pump(1500);Check(Lang.En,"Englisch nach Neustart");Check(form.tourStep==-1,"keine Tour nach Neustart");
  Click(950,30);Check(!Lang.En&&Settings.Load().Language=="de","zurück auf Deutsch");Check(form.tray.ContextMenuStrip!.Items[0].Text=="Öffnen","Tray-Menü deutsch");
  form.exitRequested=true;form.Close();form.Dispose();
  File.WriteAllText(Path.Combine(dir,"tour-test.json"),JsonSerializer.Serialize(new{ok=errors.Count==0,errors,log},new JsonSerializerOptions{WriteIndented=true}));
 }
}
