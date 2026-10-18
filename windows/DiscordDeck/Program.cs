using System.Text.Json;
namespace DiscordDeck;
static class Program {
 [STAThread] static void Main(string[] args){
 ApplicationConfiguration.Initialize();{var di=Array.IndexOf(args,"--download-test");if(di>=0){ImportTest.Download(args[(di+1)..]).GetAwaiter().GetResult();return;}}if(args.Contains("--design-test")){DesignTest.Run();return;}if(args.Contains("--preview")){using var form=new MainForm();void Prepare(Control control){_=control.Handle;control.PerformLayout();foreach(Control child in control.Controls)Prepare(child);}Prepare(form);form.PerformLayout();using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,form.ClientRectangle);bitmap.Save(Path.Combine(AppContext.BaseDirectory,"design-preview.png"));return;}if(args.Contains("--import-test")){ImportTest.Run().GetAwaiter().GetResult();return;}
 if(args.Contains("--lan-test")){LanTest.Run();return;}
 if(args.Contains("--gain-test")){GainTest.Run();return;}
 if(args.Contains("--devices")){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"devices.json"),JsonSerializer.Serialize(AudioEngine.Devices(true),new JsonSerializerOptions{WriteIndented=true}));return;}
 if(args.Contains("--self-test")){try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test.json"),JsonSerializer.Serialize(AudioTest.Run(),new JsonSerializerOptions{WriteIndented=true}));}catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test.json"),JsonSerializer.Serialize(new{ok=false,error=e.ToString()}));Environment.ExitCode=1;}return;}
 using var mutex=new Mutex(true,"Local\\CocoaDiscordDeck",out bool first);if(!first){if(args.Contains("--tray"))return;try{using var client=new System.Net.Http.HttpClient{Timeout=TimeSpan.FromSeconds(3)};client.DefaultRequestHeaders.Add("X-DiscordDeck-Key",Settings.Load().Token);client.PostAsync("http://127.0.0.1:47835/show",null).GetAwaiter().GetResult().EnsureSuccessStatusCode();}catch{MessageBox.Show("Discord Deck läuft, reagiert aber nicht. Bitte über das Symbol im Infobereich öffnen.");}return;}Application.Run(new MainForm(args.Contains("--tray")));
 }
}



