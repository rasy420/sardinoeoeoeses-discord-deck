using System.Text.Json;
using static DiscordDeck.Lang;
namespace DiscordDeck;
static class Program {
 internal static bool TestMode;
 [STAThread] static void Main(string[] args){
 TestMode=args.Any(a=>a.EndsWith("-test",StringComparison.Ordinal));ApplicationConfiguration.Initialize();{var di=Array.IndexOf(args,"--download-test");if(di>=0){ImportTest.Download(args[(di+1)..]).GetAwaiter().GetResult();return;}}if(args.Contains("--design-test")){DesignTest.Run();return;}if(args.Contains("--preview")){TestMode=true;using var form=new MainForm();void Prepare(Control control){_=control.Handle;control.PerformLayout();foreach(Control child in control.Controls)Prepare(child);}Prepare(form);form.PerformLayout();var previewArg=args.FirstOrDefault(a=>a.StartsWith("--preview-slot="));if(int.TryParse(previewArg?[15..],out var previewSlot)&&previewSlot>=1&&previewSlot<=100)form.SelectSound(previewSlot-1);using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,form.ClientRectangle);var previewUntil=DateTime.UtcNow.AddMilliseconds(300);while(DateTime.UtcNow<previewUntil){Application.DoEvents();Thread.Sleep(10);}form.DrawToBitmap(bitmap,form.ClientRectangle);bitmap.Save(Path.Combine(AppContext.BaseDirectory,"design-preview.png"));return;}if(args.Contains("--import-test")){ImportTest.Run().GetAwaiter().GetResult();return;}
 if(args.Contains("--discord-check")){var r=DiscordVoice.Read();File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"discord-check.json"),JsonSerializer.Serialize(new{discord=r,outputs=AudioEngine.Devices().Where(d=>d.Direction=="Render"&&AudioEngine.IsVirtualOutput(d.Name)).Select(d=>new{d.Name,paired=MainForm.PairedCapture(d.Id)?.name,warning=MainForm.Evaluate(d.Id)})},new JsonSerializerOptions{WriteIndented=true}));return;}
 if(args.Contains("--mobile-stop-test")){MainForm.RunMobileStopTest(args.Contains("--fixture"));return;}
 if(args.Contains("--bank-test")){MainForm.RunBankTest();return;}if(args.Contains("--layout-test")){MainForm.RunLayoutTest();return;}if(args.Contains("--mega-test")){MainForm.RunMegaTest();return;}if(args.Contains("--update-test")){UpdateTest.Run();return;}
 if(args.Contains("--reorder-test")){MainForm.RunReorderTest();return;}
 if(args.Contains("--sound-settings-test")){MainForm.RunSoundSettingsTest();return;}
 if(args.Contains("--ui-test")){MainForm.RunUiTest();return;}
 if(args.Contains("--trim-test")){MainForm.RunTrimTest();return;}
 if(args.Contains("--tour-test")){MainForm.RunTourTest();return;}
 if(args.Contains("--lan-test")){LanTest.Run();return;}
 if(args.Contains("--gain-test")){GainTest.Run();return;}
 if(args.Contains("--devices")){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"devices.json"),JsonSerializer.Serialize(AudioEngine.Devices(true),new JsonSerializerOptions{WriteIndented=true}));return;}
 if(args.Contains("--self-test")){try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test.json"),JsonSerializer.Serialize(AudioTest.Run(),new JsonSerializerOptions{WriteIndented=true}));}catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test.json"),JsonSerializer.Serialize(new{ok=false,error=e.ToString()}));Environment.ExitCode=1;}return;}
 using var mutex=new Mutex(true,"Local\\CocoaDiscordDeck",out bool first);if(!first){if(args.Contains("--tray"))return;try{using var client=new System.Net.Http.HttpClient{Timeout=TimeSpan.FromSeconds(3)};client.DefaultRequestHeaders.Add("X-DiscordDeck-Key",Settings.Load().Token);client.PostAsync("http://127.0.0.1:47835/show",null).GetAwaiter().GetResult().EnsureSuccessStatusCode();}catch{MessageBox.Show("Das Soundboard läuft, reagiert aber nicht. Bitte über das Symbol im Infobereich öffnen.\n\nThe soundboard is running but not responding. Please open it from the tray icon.");}return;}Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(_,e)=>Report(e.Exception);AppDomain.CurrentDomain.UnhandledException+=(_,e)=>Log(e.ExceptionObject as Exception);TaskScheduler.UnobservedTaskException+=(_,e)=>{Log(e.Exception);e.SetObserved();};Application.Run(new MainForm(args.Contains("--tray")));
 }
 // Unerwartete Fehler nicht als Absturzdialog zeigen, sondern protokollieren und in der Statuszeile melden.
 public static Action<string>? Status;
 static void Report(Exception e){Log(e);try{Status?.Invoke("Fehler abgefangen: "+e.Message+" (Details in error.log)");}catch{}}
 static void Log(Exception? e){try{File.AppendAllText(Path.Combine(Settings.Folder,"error.log"),$"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {e}"+Environment.NewLine+Environment.NewLine);}catch{}}
}
