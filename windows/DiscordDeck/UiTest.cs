using System.Text.Json;
namespace DiscordDeck;
// Automatischer Oberflächentest (--ui-test): Gerätemenüs öffnen/schließen/wählen, auch während Audio läuft.
public sealed partial class MainForm {
 public static void RunUiTest(){
  var log=new List<string>();var errors=new List<string>();
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
  Application.ThreadException+=(_,e)=>errors.Add(e.Exception.ToString());
  void Pump(int ms){var until=DateTime.Now.AddMilliseconds(ms);while(DateTime.Now<until){Application.DoEvents();Thread.Sleep(10);}}
  AutoTour=false;using var form=new MainForm();form.Show();Pump(1500);
  var combos=new (string name,ComboBox combo,RectangleF rect)[]{("Mithören über",form.monitorOutput,new(1194,805,319,35)),("Discord-Ausgabe",form.output,new(1194,579,319,35)),("Mikrofon",form.input,new(1194,653,319,35))};
  void Round(string phase){
   foreach(var (name,combo,rect) in combos){
    for(int i=0;i<5;i++){form.deck.OpenDeviceMenu(combo,rect);Pump(80);form.deck.DeviceMenu.Close(ToolStripDropDownCloseReason.AppClicked);Pump(80);
     // Klick daneben auf eine leere Fläche – genau der frühere Absturzfall.
     Native.SendMessage(form.deck.Handle,0x201,(IntPtr)1,(IntPtr)((300<<16)|700));Native.SendMessage(form.deck.Handle,0x202,IntPtr.Zero,(IntPtr)((300<<16)|700));Pump(80);}
    foreach(AudioDevice device in combo.Items.Cast<AudioDevice>().ToList()){
     form.deck.OpenDeviceMenu(combo,rect);Pump(80);var item=form.deck.DeviceMenu.Items.Cast<ToolStripMenuItem>().First(x=>x.Text==device.Name);item.PerformClick();form.deck.DeviceMenu.Close(ToolStripDropDownCloseReason.ItemClicked);Pump(250);
     log.Add($"{phase} · {name} → {device.Name} · ausgewählt={(combo.SelectedItem as AudioDevice)?.Name==device.Name} · audio={form.audio.Running} · status={form.status.Text}");
    }
   }
  }
  Round("Audio aus");
  form.Try(form.StartAudio);Pump(500);log.Add("Audio gestartet: "+form.audio.Running+" · "+form.status.Text);
  Round("Audio an");
  foreach(var mic in new[]{true,false}){form.ToggleRouting(mic);Pump(200);form.ToggleRouting(mic);Pump(200);log.Add($"Routing {(mic?"Mikrofon":"Mithören")} umgeschaltet · audio={form.audio.Running} · {form.status.Text}");}
  // Ursprüngliche Auswahl wiederherstellen.
  form.ToggleAudio();Pump(300);log.Add("Audio beendet: "+!form.audio.Running);
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"ui-test.json"),JsonSerializer.Serialize(new{ok=errors.Count==0,errors,log},new JsonSerializerOptions{WriteIndented=true}));
  form.exitRequested=true;form.Close();
 }
}
