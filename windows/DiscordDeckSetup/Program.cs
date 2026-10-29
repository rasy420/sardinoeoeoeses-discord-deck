using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Microsoft.Win32;

static class Program {
 const string AppName="Sardinöööses Soundboard",Version="1.4.11";
 static readonly Color Bg=Color.FromArgb(13,20,26),Card=Color.FromArgb(20,28,35),Stroke=Color.FromArgb(38,49,58),Gold=Color.FromArgb(232,200,122),Fg=Color.FromArgb(241,244,246),Muted=Color.FromArgb(139,151,161),Red=Color.FromArgb(240,96,90);
 // Setup-Sprache folgt der Windows-Sprache: Deutsch auf deutschem Windows, sonst Englisch. SSB_LANG=de|en erzwingt eine.
 public static readonly bool De=Environment.GetEnvironmentVariable("SSB_LANG")!="en";
 public static string L(string de,string en)=>De?de:en;
 // DISCORDDECK_HOME: Testinstallation in einen anderen Ordner, ohne Autostart, Verknuepfungen, Registry und Firewall.
 static string? TestRoot=>Environment.GetEnvironmentVariable("DISCORDDECK_HOME") is {Length:>0} home?home:null;
 static string Root=>TestRoot??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DiscordDeck");

 [STAThread] static void Main(string[] args){
  // An updater launched from App inherits that directory. Release our own directory handle before swapping it.
  Environment.CurrentDirectory=Path.GetTempPath();
  ApplicationConfiguration.Initialize();
  if(args.Contains("--install")){
   try{Install(_=>{},launch:false);if(TestRoot==null&&Cable.Find()==null){var note=CableStep(_=>{});if(note.StartsWith("!"))throw new IOException(note[1..]);}if(!args.Contains("--no-launch"))Launch();File.WriteAllText(Path.Combine(Root,"install-result.txt"),"Installed successfully "+Version);}
   catch(Exception e){File.WriteAllText(Path.Combine(Path.GetTempPath(),"DiscordDeck-install-error.txt"),e.ToString());Environment.ExitCode=1;}
   return;
  }
  if(args.Contains("--cable-test")){CableTest(args.Contains("--switch-defaults"));return;}
  if(args.Contains("--screenshot")){using var f=new SetupForm();f.Show();Application.DoEvents();using var bmp=new Bitmap(f.Width,f.Height);f.DrawToBitmap(bmp,new Rectangle(0,0,f.Width,f.Height));bmp.Save(Path.Combine(AppContext.BaseDirectory,"setup-"+(De?"de":"en")+(Cable.Find()==null?"-nocable":"")+".png"));return;}
  Application.Run(new SetupForm());
 }

 // Prüft Erkennung, Download + Signatur des Originalpakets und das Sichern/Wiederherstellen der Standardgeräte, ohne zu installieren.
 static void CableTest(bool switchDefaults){
  var log=new List<string>();bool ok=true;void Check(bool c,string m){log.Add((c?"OK   ":"FAIL ")+m);ok&=c;}
  var found=Cable.Find();log.Add("Gefunden: "+(found??"(nichts)"));
  try{var setup=Cable.Download(s=>log.Add("… "+s));Check(File.Exists(setup),"Download + Entpacken: "+Path.GetFileName(setup));Check(Cable.SignedByVbAudio(setup),"Signatur VB-Audio gültig");
   var fake=Path.Combine(Path.GetTempPath(),"ssb-fake-"+Guid.NewGuid().ToString("N")[..6]+".exe");File.Copy(setup,fake);using(var fs=File.OpenWrite(fake)){fs.Seek(2000,SeekOrigin.Begin);fs.WriteByte(0x5A);}Check(!Cable.SignedByVbAudio(fake),"Manipulierte Datei wird abgelehnt");File.Delete(fake);
  }catch(Exception e){Check(false,"Download: "+e.Message);}
  var before=Defaults.Read();Check(before.Ids.Count>0,"Standardgeräte gelesen: "+before.Ids.Count);
  if(switchDefaults&&before.Ids.TryGetValue((0,0),out var current)){
   // Kurz ein anderes Ausgabegerät zum Standard machen (wie es ein Treiber-Installer tut) und zurücksetzen lassen.
   using var render=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render")!;
   var other=render.GetSubKeyNames().Where(k=>render.OpenSubKey(k)?.GetValue("DeviceState") is 1).Select(k=>"{0.0.0.00000000}."+k).FirstOrDefault(id=>!string.Equals(id,current,StringComparison.OrdinalIgnoreCase));
   if(other!=null){Defaults.Set(other,0);Defaults.Set(other,1);Thread.Sleep(400);var mid=Defaults.Read();Check(string.Equals(mid.Ids[(0,0)],other,StringComparison.OrdinalIgnoreCase),"Standard testweise umgestellt");var n=Defaults.Restore(before);Thread.Sleep(400);var after=Defaults.Read();Check(after.Ids[(0,0)]==current,$"Standard wiederhergestellt ({n} Rolle(n))");Check(before.Ids.All(kv=>after.Ids.TryGetValue(kv.Key,out var v)&&v==kv.Value),"Alle Rollen wie vorher");}
  }
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"cable-test.json"),JsonSerializer.Serialize(new{ok,log},new JsonSerializerOptions{WriteIndented=true}));
 }

 sealed class SetupForm:Form{
  readonly Label status=new(){Text=L("Voraussetzungen werden geprüft. Weiter installiert Programm und fehlende Komponenten; Windows fragt ggf. nach Administratorrechten.","Prerequisites are checked. Continue installs the app and missing components; Windows may request administrator permission."),ForeColor=Muted,AutoSize=false,Font=new Font("Segoe UI",9.5f)};
  readonly ProgressBar progress=new(){Style=ProgressBarStyle.Continuous,Maximum=100,Visible=false};
  readonly FlatButton install=new(L("Weiter","Continue"),Gold,Bg),cancel=new(L("Abbrechen","Cancel"),Card,Fg);
  readonly CheckBox cable=new(){Checked=true,ForeColor=Fg,Font=new Font("Segoe UI",10f),AutoSize=false};
  readonly Label cableInfo=new(){ForeColor=Muted,Font=new Font("Segoe UI",9f),AutoSize=false};
  bool done;
  public SetupForm(){
   Text=AppName+L(" installieren"," setup");ClientSize=new Size(620,460);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;BackColor=Bg;ForeColor=Fg;Font=new Font("Segoe UI",10f);AutoScaleMode=AutoScaleMode.Dpi;
   try{Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}
   var fish=new PictureBox{Image=Resource("sardine.png") is {} s?Image.FromStream(s):null,SizeMode=PictureBoxSizeMode.Zoom,Bounds=new Rectangle(28,28,132,66)};
   var title=new Label{Text="Sardinöööses",Font=new Font("Segoe UI Semibold",24f),ForeColor=Fg,AutoSize=true,Location=new Point(172,30)};
   var sub=new Label{Text="S O U N D B O A R D   ·   "+Version,Font=new Font("Segoe UI",9f),ForeColor=Gold,AutoSize=true,Location=new Point(176,76)};
   var features=new Label{Text=L("✓  100 Soundplätze in 5 Bänken\n✓  Handy-Fernbedienung im lokalen WLAN (Android-App inklusive)\n✓  YouTube / TikTok-Import und MyInstants\n✓  Mikrofon beimischen und Sounds selbst mithören\n✓  .NET und Import-Tools inklusive, automatisch geprüft","✓  100 sound slots in 5 banks\n✓  Phone remote over your local Wi-Fi (Android app included)\n✓  YouTube / TikTok import and MyInstants\n✓  Mix in your microphone and hear the sounds yourself\n✓  .NET and import tools included and checked automatically"),ForeColor=Fg,Font=new Font("Segoe UI",9.5f),AutoSize=false,Bounds=new Rectangle(32,126,556,112)};
   var cableCard=new Panel{BackColor=Card,Bounds=new Rectangle(32,244,556,84)};
   var found=Cable.Find();
   if(found!=null){cable.Visible=false;cable.Checked=false;cableInfo.ForeColor=Gold;cableInfo.Font=new Font("Segoe UI",10f);cableInfo.Text=L("✓  Virtuelles Mikrofon für Discord gefunden:\n     ","✓  Virtual microphone for Discord found:\n     ")+found;cableInfo.Bounds=new Rectangle(14,16,528,52);}
   else{cable.Text=L("Erforderlich: VB-CABLE herunterladen und installieren","Required: download and install VB-CABLE (free)");cable.AutoCheck=false;cable.Bounds=new Rectangle(14,10,528,26);cableInfo.Text=L("Erforderlicher Audiotreiber von vb-audio.com (Donationware). Wird nach Weiter automatisch installiert. Spenden an VB-Audio sind willkommen.","Required audio driver from vb-audio.com (donationware). Installed automatically after Continue. Donations to VB-Audio are welcome.");cableInfo.Bounds=new Rectangle(34,38,508,42);}
   cableCard.Controls.AddRange(new Control[]{cable,cableInfo});
   cableCard.Paint+=(_,e)=>{using var pen=new Pen(Stroke);e.Graphics.DrawRectangle(pen,0,0,cableCard.Width-1,cableCard.Height-1);};
   status.Bounds=new Rectangle(32,340,556,44);progress.Bounds=new Rectangle(32,388,556,8);
   install.Bounds=new Rectangle(408,404,180,44);cancel.Bounds=new Rectangle(250,404,146,44);
   cancel.Click+=(_,_)=>Close();
   install.Click+=async(_,_)=>{
    if(done){Close();return;}
    install.Enabled=cancel.Enabled=false;progress.Visible=true;status.ForeColor=Muted;
    var withCable=cable.Visible;string? note=null;
    try{
     await Task.Run(()=>Install(p=>BeginInvoke(()=>{progress.Value=Math.Clamp(p.percent,0,100);status.Text=p.text;}),launch:false));
     if(withCable)note=await Task.Run(()=>CableStep(p=>BeginInvoke(()=>{progress.Value=Math.Clamp(p.percent,0,100);status.Text=p.text;})));
     if(note!=null&&note.StartsWith("!"))throw new IOException(note[1..]);
     ValidateInstalledTools(Path.Combine(Root,"App"));Launch();progress.Value=100;
     done=true;install.Text=L("Schließen","Close");install.Enabled=true;cancel.Visible=false;
     if(note!=null&&note.StartsWith("!")){status.ForeColor=Red;status.Text=L("Soundboard installiert und gestartet. ","Soundboard installed and started. ")+note[1..];}
     else{status.ForeColor=Gold;status.Text=L("Fertig! Das Soundboard läuft jetzt (Symbol unten rechts im Infobereich).\n","Done! The soundboard is now running (icon in the bottom-right tray).\n")+(note??L("Verknüpfung auf dem Desktop und im Startmenü angelegt.","Shortcuts added to the desktop and Start menu."));}
    }catch(Exception e){try{File.AppendAllText(Path.Combine(Root,"setup-error.log"),DateTime.Now.ToString("O")+" "+e+Environment.NewLine);}catch{}status.ForeColor=Red;status.Text=L("Installation fehlgeschlagen: ","Installation failed: ")+e.Message;install.Text=L("Erneut versuchen","Try again");install.Enabled=cancel.Enabled=true;}
   };
   Controls.AddRange(new Control[]{fish,title,sub,features,cableCard,status,progress,install,cancel});
   Load+=(_,_)=>{sub.Left=title.Left+4;sub.Top=title.Bottom-2;fish.Top=title.Top+(sub.Bottom-title.Top-fish.Height)/2;};
  }
 }

 // Rückgabe: Hinweistext für die Abschlussmeldung; mit "!" am Anfang, wenn VB-CABLE nicht bereit ist.
 static string CableStep(Action<(int percent,string text)> report){
  try{
   report((90,L("VB-CABLE wird von vb-audio.com geladen …","Downloading VB-CABLE from vb-audio.com …")));
   var setup=Cable.Download(t=>report((93,t)));
   var (installed,device)=Cable.Install(setup,t=>report((96,t)));
   return installed?L("VB-CABLE ist bereit: ","VB-CABLE is ready: ")+device:"!"+L("VB-CABLE ist noch nicht sichtbar. Bitte den PC neu starten.","VB-CABLE is not visible yet. Please restart your PC.");
  }catch(System.ComponentModel.Win32Exception){return "!"+L("VB-CABLE wurde nicht installiert (Administrator-Abfrage abgelehnt). Setup später erneut ausführen oder VB-CABLE von vb-audio.com installieren.","VB-CABLE was not installed (administrator prompt declined). Run setup again later or install VB-CABLE from vb-audio.com.");}
  catch(Exception e){return "!"+L("VB-CABLE konnte nicht installiert werden: ","VB-CABLE could not be installed: ")+e.Message;}
 }

 static void ValidateInstalledTools(string app){foreach(var tool in new[]{"ffmpeg","ffprobe","yt-dlp","deno"}){var path=Path.Combine(app,"tools",tool+".exe");if(!File.Exists(path)||new FileInfo(path).Length<1024)throw new IOException(L("Import-Komponente fehlt im Installationspaket: ","Import component missing from installation package: ")+tool);using var p=Process.Start(new ProcessStartInfo(path,tool.StartsWith("ff")?"-version":"--version"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})??throw new IOException(tool);var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();if(!p.WaitForExit(20000)){p.Kill(true);throw new IOException(L("Komponente reagiert nicht: ","Component did not respond: ")+tool);}if(p.ExitCode!=0)throw new IOException(L("Komponente startet nicht: ","Component did not start: ")+tool);Task.WaitAll(stdout,stderr);}}
 static void Launch(){var app=Path.Combine(Root,"App");Process.Start(new ProcessStartInfo(Path.Combine(app,"DiscordDeck.exe")){UseShellExecute=true,WorkingDirectory=app});}

 sealed class FlatButton:Button{
  public FlatButton(string text,Color back,Color fore){Text=text;BackColor=back;ForeColor=fore;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderColor=back==Card?Stroke:back;FlatAppearance.BorderSize=1;FlatAppearance.MouseOverBackColor=ControlPaint.Light(back,.15f);Font=new Font("Segoe UI Semibold",10.5f);Cursor=Cursors.Hand;}
 }

 static Stream? Resource(string name)=>Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

 static void Install(Action<(int percent,string text)> report,bool launch){
  if(!Environment.Is64BitOperatingSystem||System.Runtime.InteropServices.RuntimeInformation.OSArchitecture!=System.Runtime.InteropServices.Architecture.X64)throw new PlatformNotSupportedException(L("Dieses Setup benötigt Windows x64.","This setup requires Windows x64."));
  var root=Root;var app=Path.Combine(root,"App");var exe=Path.Combine(app,"DiscordDeck.exe");var test=TestRoot!=null;
  Directory.CreateDirectory(root);
  report((2,L("Laufendes Soundboard wird beendet …","Closing the running soundboard …")));
  foreach(var process in Process.GetProcessesByName("DiscordDeck")){try{if(string.Equals(process.MainModule?.FileName,exe,StringComparison.OrdinalIgnoreCase)){process.Kill();process.WaitForExit(5000);}}catch{}finally{process.Dispose();}}
  var config=Path.Combine(root,"settings.json");if(File.Exists(config))File.Copy(config,Path.Combine(root,"settings.before-update.json"),true);else if(!De)File.WriteAllText(config,"{\"Language\":\"en\"}");

  // In einen frischen Ordner entpacken und erst danach austauschen: kein halb installierter Zustand.
  var staging=app+".new";if(Directory.Exists(staging))Directory.Delete(staging,true);Directory.CreateDirectory(staging);
  using(var resource=Resource("payload.zip")??throw new IOException(L("Installationspaket fehlt","Installation package missing")))
  using(var zip=new ZipArchive(resource)){
   long total=Math.Max(1,zip.Entries.Sum(e=>e.Length)),written=0;
   foreach(var entry in zip.Entries){
    var path=Path.GetFullPath(Path.Combine(staging,entry.FullName));
    if(!path.StartsWith(staging+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException(L("Ungültiger Paketpfad","Invalid package path"));
    if(entry.Name=="")continue;Directory.CreateDirectory(Path.GetDirectoryName(path)!);entry.ExtractToFile(path,true);
    written+=entry.Length;report((5+(int)(80*written/total),L("Dateien werden kopiert … ","Copying files … ")+(100*written/total)+" %"));
   }
  }
  if(!File.Exists(Path.Combine(staging,"DiscordDeck.exe")))throw new IOException(L("DiscordDeck.exe fehlt im Paket","DiscordDeck.exe missing from the package"));
  ValidateInstalledTools(staging);
  report((86,L("Programm wird eingerichtet …","Setting up the program …")));
  var backup=app+".previous";
  if(Directory.Exists(backup))Directory.Delete(backup,true);
  for(int attempt=0;;attempt++){try{if(Directory.Exists(app))Directory.Move(app,backup);break;}catch(IOException) when(attempt<10){Thread.Sleep(500);}catch(UnauthorizedAccessException) when(attempt<10){Thread.Sleep(500);}}
  try{Directory.Move(staging,app);}catch{if(Directory.Exists(backup)&&!Directory.Exists(app))Directory.Move(backup,app);throw;}
  WriteUninstaller(root,app);
  if(!test){
   using(var run=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))run.SetValue("DiscordDeck","\""+exe+"\" --tray");
   // Verknüpfungen früherer Namen entfernen.
   foreach(var folder in new[]{Environment.SpecialFolder.DesktopDirectory,Environment.SpecialFolder.Programs})foreach(var old in new[]{"Discord Deck.lnk","Sardinöööses Discord Deck.lnk"}){var p=Path.Combine(Environment.GetFolderPath(folder),old);if(File.Exists(p))File.Delete(p);}
   Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),AppName+".lnk"),exe,app);
   Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),AppName+".lnk"),exe,app);
   using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\DiscordDeck")){
    key.SetValue("DisplayName",AppName);key.SetValue("DisplayVersion",Version);key.SetValue("Publisher","Cocoa");key.SetValue("DisplayIcon",exe);key.SetValue("InstallLocation",app);
    key.SetValue("UninstallString","powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \""+Path.Combine(root,"Uninstall.ps1")+"\"");
    key.SetValue("NoModify",1,RegistryValueKind.DWord);key.SetValue("NoRepair",1,RegistryValueKind.DWord);
    key.SetValue("EstimatedSize",(int)(new DirectoryInfo(app).EnumerateFiles("*",SearchOption.AllDirectories).Sum(f=>f.Length)/1024),RegistryValueKind.DWord);
   }
   report((88,L("Handy-Freigabe im lokalen Netzwerk …","Allowing the phone on your local network …")));
   if(!FirewallReady(exe))ConfigureFirewall(root,app);
  }
  if(launch){report((98,L("Soundboard wird gestartet …","Starting the soundboard …")));Process.Start(new ProcessStartInfo(exe){UseShellExecute=true,WorkingDirectory=app});}
  report((89,L("Programm installiert","Program installed")));
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
   else File.WriteAllText(status,L("Firewall-Freigabe fehlgeschlagen. Configure-LAN.ps1 im App-Ordner als Administrator ausführen.","Firewall rule failed. Run Configure-LAN.ps1 in the app folder as administrator."));
  }catch(System.ComponentModel.Win32Exception){File.WriteAllText(status,L("Firewall-Freigabe abgelehnt. Handy-Verbindung braucht sie; Installer erneut ausführen oder Configure-LAN.ps1 als Administrator starten.","Firewall rule declined. The phone connection needs it; run the installer again or start Configure-LAN.ps1 as administrator."));}
 }

 static void WriteUninstaller(string root,string app){
  var desktop=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),AppName+".lnk");
  var start=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),AppName+".lnk");
  string Q(string s)=>"'"+s.Replace("'","''")+"'";
  var script=$$"""
Add-Type -AssemblyName System.Windows.Forms
if([System.Windows.Forms.MessageBox]::Show('{{L("Sardinöööses Soundboard deinstallieren? Deine Sounds und Einstellungen bleiben erhalten in: ","Uninstall Sardinöööses Soundboard? Your sounds and settings are kept in: ")+root.Replace("'","''")}}','{{L("Deinstallieren","Uninstall")}}','YesNo','Question') -ne 'Yes'){exit 0}
$app={{Q(app)}}
Get-Process DiscordDeck -ErrorAction SilentlyContinue | ?{$_.Path -eq (Join-Path $app 'DiscordDeck.exe')} | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 800
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'DiscordDeck' -ErrorAction SilentlyContinue
Remove-Item -LiteralPath {{Q(desktop)}},{{Q(start)}} -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DiscordDeck' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $app -Recurse -Force -ErrorAction SilentlyContinue
try{Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -ArgumentList '-NoProfile -Command "Get-NetFirewallRule -DisplayName ''DiscordDeck LAN *'' | Remove-NetFirewallRule"' -Wait}catch{}
[System.Windows.Forms.MessageBox]::Show('{{L("Sardinöööses Soundboard wurde entfernt.","Sardinöööses Soundboard was removed.")}}','{{L("Deinstallieren","Uninstall")}}') | Out-Null
""";
  File.WriteAllText(Path.Combine(root,"Uninstall.ps1"),script,new System.Text.UTF8Encoding(true));
 }
}
