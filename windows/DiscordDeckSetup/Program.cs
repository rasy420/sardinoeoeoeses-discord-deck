using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

static class Program {
 const string AppName="Sardinöööses Discord Deck",Version="1.1.0";
 static readonly Color Bg=Color.FromArgb(13,20,26),Card=Color.FromArgb(20,28,35),Stroke=Color.FromArgb(38,49,58),Green=Color.FromArgb(168,226,140),Fg=Color.FromArgb(241,244,246),Muted=Color.FromArgb(139,151,161),Red=Color.FromArgb(240,96,90);
 // DISCORDDECK_HOME: Testinstallation in einen anderen Ordner, ohne Autostart, Verknuepfungen, Registry und Firewall.
 static string? TestRoot=>Environment.GetEnvironmentVariable("DISCORDDECK_HOME") is {Length:>0} home?home:null;
 static string Root=>TestRoot??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DiscordDeck");

 [STAThread] static void Main(string[] args){
  ApplicationConfiguration.Initialize();
  if(args.Contains("--install")){
   try{Install(_=>{},launch:!args.Contains("--no-launch"));File.WriteAllText(Path.Combine(Root,"install-result.txt"),"Installed successfully "+Version);}
   catch(Exception e){File.WriteAllText(Path.Combine(Path.GetTempPath(),"DiscordDeck-install-error.txt"),e.ToString());Environment.ExitCode=1;}
   return;
  }
  Application.Run(new SetupForm());
 }

 sealed class SetupForm:Form{
  readonly Label status=new(){Text="Installation für deinen Windows-Benutzer. Keine Administratorrechte nötig.",ForeColor=Muted,AutoSize=false,Font=new Font("Segoe UI",9.5f)};
  readonly ProgressBar progress=new(){Style=ProgressBarStyle.Continuous,Maximum=100,Visible=false};
  readonly FlatButton install=new("Installieren",Green,Bg),cancel=new("Abbrechen",Card,Fg);
  bool done;
  public SetupForm(){
   Text=AppName+" installieren";ClientSize=new Size(620,380);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;BackColor=Bg;ForeColor=Fg;Font=new Font("Segoe UI",10f);AutoScaleMode=AutoScaleMode.Dpi;
   try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
   var fish=new PictureBox{Image=Resource("sardine.png") is {} s?Image.FromStream(s):null,SizeMode=PictureBoxSizeMode.Zoom,Bounds=new Rectangle(28,28,132,66)};
   var title=new Label{Text="Sardinöööses",Font=new Font("Segoe UI Semibold",24f),ForeColor=Fg,AutoSize=true,Location=new Point(172,30)};
   var sub=new Label{Text="D I S C O R D   D E C K   ·   "+Version,Font=new Font("Segoe UI",9f),ForeColor=Muted,AutoSize=true,Location=new Point(176,76)};
   var features=new Label{Text="✓  100 Soundplätze in 5 Bänken\n✓  Handy-Fernbedienung im lokalen WLAN (Android-App inklusive)\n✓  YouTube-Import mit Zuschneiden und MyInstants-Links\n✓  Mikrofon beimischen und Sounds selbst mithören",ForeColor=Fg,Font=new Font("Segoe UI",10.5f),AutoSize=false,Bounds=new Rectangle(32,136,556,100)};
   status.Bounds=new Rectangle(32,236,556,44);progress.Bounds=new Rectangle(32,284,556,8);
   install.Bounds=new Rectangle(408,312,180,44);cancel.Bounds=new Rectangle(250,312,146,44);
   cancel.Click+=(_,_)=>Close();
   install.Click+=async(_,_)=>{
    if(done){Close();return;}
    install.Enabled=cancel.Enabled=false;progress.Visible=true;status.ForeColor=Muted;
    try{
     await Task.Run(()=>Install(p=>BeginInvoke(()=>{progress.Value=Math.Clamp(p.percent,0,100);status.Text=p.text;}),launch:true));
     done=true;status.ForeColor=Green;status.Text="Fertig! Das Deck läuft jetzt (Symbol unten rechts im Infobereich).\nVerknüpfung auf dem Desktop und im Startmenü angelegt. Handy: Am PC auf „Handy verbinden“ klicken.";install.Text="Schließen";install.Enabled=true;cancel.Visible=false;
    }catch(Exception e){status.ForeColor=Red;status.Text="Installation fehlgeschlagen: "+e.Message;install.Text="Erneut versuchen";install.Enabled=cancel.Enabled=true;}
   };
   Controls.AddRange(new Control[]{fish,title,sub,features,status,progress,install,cancel});
   Load+=(_,_)=>{sub.Left=title.Left+4;sub.Top=title.Bottom-2;fish.Top=title.Top+(sub.Bottom-title.Top-fish.Height)/2;};
  }
 }

 sealed class FlatButton:Button{
  public FlatButton(string text,Color back,Color fore){Text=text;BackColor=back;ForeColor=fore;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderColor=back==Card?Stroke:back;FlatAppearance.BorderSize=1;FlatAppearance.MouseOverBackColor=ControlPaint.Light(back,.15f);Font=new Font("Segoe UI Semibold",10.5f);Cursor=Cursors.Hand;}
 }

 static Stream? Resource(string name)=>Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

 static void Install(Action<(int percent,string text)> report,bool launch){
  var root=Root;var app=Path.Combine(root,"App");var exe=Path.Combine(app,"DiscordDeck.exe");var test=TestRoot!=null;
  Directory.CreateDirectory(root);
  report((2,"Laufendes Deck wird beendet …"));
  foreach(var process in Process.GetProcessesByName("DiscordDeck")){try{if(string.Equals(process.MainModule?.FileName,exe,StringComparison.OrdinalIgnoreCase)){process.Kill();process.WaitForExit(5000);}}catch{}finally{process.Dispose();}}
  var config=Path.Combine(root,"settings.json");if(File.Exists(config))File.Copy(config,Path.Combine(root,"settings.before-update.json"),true);

  // In einen frischen Ordner entpacken und erst danach austauschen: kein halb installierter Zustand.
  var staging=app+".new";if(Directory.Exists(staging))Directory.Delete(staging,true);Directory.CreateDirectory(staging);
  using(var resource=Resource("payload.zip")??throw new IOException("Installationspaket fehlt"))
  using(var zip=new ZipArchive(resource)){
   long total=Math.Max(1,zip.Entries.Sum(e=>e.Length)),written=0;
   foreach(var entry in zip.Entries){
    var path=Path.GetFullPath(Path.Combine(staging,entry.FullName));
    if(!path.StartsWith(staging+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Ungültiger Paketpfad");
    if(entry.Name=="")continue;Directory.CreateDirectory(Path.GetDirectoryName(path)!);entry.ExtractToFile(path,true);
    written+=entry.Length;report((5+(int)(80*written/total),"Dateien werden kopiert … "+(100*written/total)+" %"));
   }
  }
  if(!File.Exists(Path.Combine(staging,"DiscordDeck.exe")))throw new IOException("DiscordDeck.exe fehlt im Paket");
  report((86,"Programm wird eingerichtet …"));
  for(int attempt=0;;attempt++){try{if(Directory.Exists(app))Directory.Delete(app,true);Directory.Move(staging,app);break;}catch(IOException) when(attempt<10){Thread.Sleep(500);}catch(UnauthorizedAccessException) when(attempt<10){Thread.Sleep(500);}}
  WriteUninstaller(root,app);
  if(!test){
   using(var run=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))run.SetValue("DiscordDeck","\""+exe+"\" --tray");
   foreach(var old in new[]{"Discord Deck.lnk"}){var p=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),old);if(File.Exists(p))File.Delete(p);}
   Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),AppName+".lnk"),exe,app);
   Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),AppName+".lnk"),exe,app);
   using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\DiscordDeck")){
    key.SetValue("DisplayName",AppName);key.SetValue("DisplayVersion",Version);key.SetValue("Publisher","Cocoa");key.SetValue("DisplayIcon",exe);key.SetValue("InstallLocation",app);
    key.SetValue("UninstallString","powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \""+Path.Combine(root,"Uninstall.ps1")+"\"");
    key.SetValue("NoModify",1,RegistryValueKind.DWord);key.SetValue("NoRepair",1,RegistryValueKind.DWord);
    key.SetValue("EstimatedSize",(int)(new DirectoryInfo(app).EnumerateFiles("*",SearchOption.AllDirectories).Sum(f=>f.Length)/1024),RegistryValueKind.DWord);
   }
   report((92,"Handy-Freigabe im lokalen Netzwerk …"));
   if(!FirewallReady(exe))ConfigureFirewall(root,app);
  }
  if(launch){report((98,"Deck wird gestartet …"));Process.Start(new ProcessStartInfo(exe,"--tray"){UseShellExecute=true,WorkingDirectory=app});}
  report((100,"Fertig"));
 }

 static void Shortcut(string path,string target,string folder){
  Directory.CreateDirectory(Path.GetDirectoryName(path)!);
  Type shellType=Type.GetTypeFromProgID("WScript.Shell")!;dynamic shell=Activator.CreateInstance(shellType)!;dynamic link=shell.CreateShortcut(path);
  link.TargetPath=target;link.WorkingDirectory=folder;link.IconLocation=target+",0";link.Description=AppName;link.Save();
 }

 static bool FirewallReady(string exe){
  // Lesen geht ohne Administratorrechte; nur wenn Regeln fehlen, fragt Windows einmal nach.
  try{
   var check=new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -Command \"$n=0;foreach($p in 'TCP','UDP'){$r=Get-NetFirewallRule -DisplayName ('DiscordDeck LAN '+$p) -ErrorAction SilentlyContinue|?{$_.Enabled -eq 'True'};if($r -and (($r|Get-NetFirewallApplicationFilter).Program -contains '"+exe.Replace("'","''")+"')){$n++}};exit (2-$n)\""){CreateNoWindow=true,UseShellExecute=false};
   using var p=Process.Start(check)!;return p.WaitForExit(20000)&&p.ExitCode==0;
  }catch{return false;}
 }

 static void ConfigureFirewall(string root,string app){
  var status=Path.Combine(root,"firewall-status.txt");
  try{
   using var process=Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \""+Path.Combine(app,"Configure-LAN.ps1")+"\""){UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Hidden});
   if(process!=null&&process.WaitForExit(60000)&&process.ExitCode==0){if(File.Exists(status))File.Delete(status);}
   else File.WriteAllText(status,"Firewall-Freigabe fehlgeschlagen. Configure-LAN.ps1 im App-Ordner als Administrator ausführen.");
  }catch(System.ComponentModel.Win32Exception){File.WriteAllText(status,"Firewall-Freigabe abgelehnt. Handy-Verbindung braucht sie; Installer erneut ausführen oder Configure-LAN.ps1 als Administrator starten.");}
 }

 static void WriteUninstaller(string root,string app){
  var desktop=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),AppName+".lnk");
  var start=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),AppName+".lnk");
  string Q(string s)=>"'"+s.Replace("'","''")+"'";
  var script=$$"""
Add-Type -AssemblyName System.Windows.Forms
if([System.Windows.Forms.MessageBox]::Show('{{AppName}} deinstallieren? Deine Sounds und Einstellungen bleiben in {{root.Replace("'","''")}} erhalten.','Deinstallieren','YesNo','Question') -ne 'Yes'){exit 0}
$app={{Q(app)}}
Get-Process DiscordDeck -ErrorAction SilentlyContinue | ?{$_.Path -eq (Join-Path $app 'DiscordDeck.exe')} | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 800
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'DiscordDeck' -ErrorAction SilentlyContinue
Remove-Item -LiteralPath {{Q(desktop)}},{{Q(start)}} -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DiscordDeck' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $app -Recurse -Force -ErrorAction SilentlyContinue
try{Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -ArgumentList '-NoProfile -Command "Get-NetFirewallRule -DisplayName ''DiscordDeck LAN *'' | Remove-NetFirewallRule"' -Wait}catch{}
[System.Windows.Forms.MessageBox]::Show('{{AppName}} wurde entfernt.','Deinstallieren') | Out-Null
""";
  File.WriteAllText(Path.Combine(root,"Uninstall.ps1"),script,new System.Text.UTF8Encoding(true));
 }
}
