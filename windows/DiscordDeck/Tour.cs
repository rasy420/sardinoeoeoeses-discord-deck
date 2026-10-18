using System.Drawing.Drawing2D;
using static DiscordDeck.Lang;
namespace DiscordDeck;
// Kurze Einführung beim ersten Start: dunkler Hintergrund, hervorgehobener Bereich, Karte mit Schritt n von N.
public sealed partial class MainForm {
 public static bool AutoTour=true;
 internal int tourStep=-1;
 internal sealed record TourStep(string Title,string Text,RectangleF[] Spots);
 internal static TourStep[] TourSteps()=>new TourStep[]{
  new(T("Willkommen!","Welcome!"),T("Schön, dass du das Sardinöööses Soundboard nutzt. Diese kurze Tour zeigt dir in ein paar Schritten, wie alles funktioniert. Du kannst sie jederzeit über den „?“-Knopf oben wieder öffnen.","Thanks for using the Sardinöööses Soundboard. This short tour shows you how everything works in a few steps. You can reopen it any time with the “?” button at the top."),Array.Empty<RectangleF>()),
  new(T("Sound-Bänke","Sound banks"),T("Hier wechselst du zwischen 5 Bänken mit je 20 Sounds – insgesamt 100 Plätze. Über Bearbeiten unter dem Stoppen-Knopf verschiebst du Sounds und ganze Bänke. Nur dann erscheinen Stifte zum Umbenennen der Bänke. Während des Ziehens links über eine Bank halten, um sie zu wechseln. Das Handy übernimmt die Reihenfolge automatisch.","Switch between 5 banks with 20 sounds each – 100 slots in total. Edit below Stop lets you move sounds and entire banks. Pencils for renaming banks only appear in edit mode. Hold over a bank on the left while dragging to switch banks. Your phone receives the order automatically."),new RectangleF[]{new(10,119,227,311)}),
  new(T("Sound abspielen und auswählen","Play and select a sound"),T("Ein Klick auf die ganze Kachel spielt den Sound ab. Leere Plätze öffnen die Quellenauswahl. Über ⚙ öffnest du seine Einstellungen rechts. Ein Kachelklick schließt sie wieder. Ungespeicherte Änderungen musst du zuerst speichern. Das X schließt sie ebenfalls. Der Stift rechts ändert den Anzeigenamen, ohne die Datei umzubenennen.","Click anywhere on a tile to play it. Empty slots open the source settings. ⚙ opens its settings on the right; A tile click closes them again; save pending changes first. X also closes them. The pencil on the right changes the display name without renaming the file."),new RectangleF[]{new(269,191,300,128)}),
  new(T("Sound hinzufügen","Add a sound"),T("Über „Sound ersetzen“ wählst du eine Audiodatei oder einen MyInstants-Link. Links unten importierst du über „YouTube / TikTok“.","Use “Replace sound” to choose an audio file or a MyInstants link. Import via “YouTube / TikTok” at the bottom left."),new RectangleF[]{new(1186,297,334,57),new(15,703,219,56)}),
  new(T("Länge festlegen","Set the length"),T("Zieh die zwei goldenen Marker mit der Maus auf Start und Ende. ▶ hört den Ausschnitt vorher an – nur auf deinem Kopfhörer. ✓ Speichern übernimmt ihn, ⟲ setzt auf die zuletzt gespeicherten Werte zurück. Genaue Sekunden kannst du direkt in Start und Ende unter der Wellenform eingeben.","Drag the two gold markers to the start and end. ▶ previews the part – only on your headphones. ✓ Save keeps it, ⟲ resets to the last saved values. Enter exact seconds directly in Start and End below the waveform."),new RectangleF[]{new(1186,179,334,117),new(1186,442,334,48)}),
  new(T("Audio-Routing","Audio routing"),T("Über „Audio-Einstellungen“ unten rechts öffnest du das globale Routing. „Discord-Ausgabe“ ist das virtuelle Mikrofon (z. B. CABLE oder Steam Streaming). Stell in Discord genau dieses als Eingabegerät ein und schalte dort die Rauschunterdrückung aus. „Mikrofon beimischen“ schickt deine Stimme mit.","“Audio settings” at the bottom right opens global routing. “Discord output” is the virtual microphone (e.g. CABLE or Steam Streaming). Select exactly this one as the input device in Discord and turn off noise suppression there. “Mix in microphone” sends your voice too."),new RectangleF[]{new(947,867,211,37)}),
  new(T("Loslegen","Get started"),T("Mit „Audio starten“ geht es los. Der Regler stellt die Lautstärke aller Sounds ein, „Alle Sounds stoppen“ beendet alles sofort. Esc schließt zuerst die Bearbeitung.","“Start audio” gets things going. The slider sets the volume of all sounds; “Stop all sounds” stops everything at once. Esc closes editing first."),new RectangleF[]{new(600,930,926,66),new(15,447,219,56)}),
  new(T("Vom Handy steuern","Control from your phone"),T("„Handy verbinden“ zeigt einen Code. Gib ihn in der Android-App ein – PC und Handy müssen im selben WLAN sein.","“Connect phone” shows a code. Enter it in the Android app – PC and phone must be on the same Wi-Fi."),new RectangleF[]{new(1027,7,178,47)}),
  new(T("Anleitung und Sprache","Guide and language"),T("Über „?“ öffnest du diese Tour wieder, mit DE / EN stellst du die Sprache um. Viel Spaß!","“?” reopens this tour, DE / EN switches the language. Have fun!"),new RectangleF[]{new(922,7,106,47)}),
 };
 bool panelBeforeTour;int bankBeforeTour,scrollBeforeTour;
 void PrepareTourStep(){if(tourStep>=2&&tourStep<=4){if(tourStep==2)soundPanelOpen=false;else if(!soundPanelOpen)SelectEdited(selectedSound);bank=selectedSound/20;scrollRow=0;}else if(!panelBeforeTour)soundPanelOpen=false;deck.ClampScroll();}
 internal void StartTour(){panelBeforeTour=soundPanelOpen;bankBeforeTour=bank;scrollBeforeTour=scrollRow;tourStep=0;Show();WindowState=FormWindowState.Normal;Activate();deck.Focus();deck.Invalidate();}
 internal void TourNext(){if(tourStep<0)return;if(tourStep>=TourSteps().Length-1)EndTour();else{tourStep++;PrepareTourStep();deck.Invalidate();}}
 internal void TourBack(){if(tourStep>0){tourStep--;PrepareTourStep();deck.Invalidate();}}
 internal void EndTour(){tourStep=-1;soundPanelOpen=panelBeforeTour;bank=bankBeforeTour;scrollRow=scrollBeforeTour;deck.ClampScroll();if(!settings.TourDone){settings.TourDone=true;settings.Save();}deck.Invalidate();}
 internal void SetLanguage(bool english){Lang.En=english;settings.Language=english?"en":"de";settings.Save();BuildTrayMenu();if(discordWarning!=null)CheckDiscord();status.Text=T("Sprache: Deutsch","Language: English");deck.Invalidate();}
 sealed partial class ReferenceDeck {
  internal RectangleF[] TourSpots(int step){
   return step switch{
    1=>new[]{new RectangleF(10,119,227,311),RectangleF.Inflate(ReorderButtonBounds,4,4)},
    2=>new[]{RectangleF.Inflate(TileBounds(owner.selectedSound),4,4)},
    5=>new[]{new RectangleF(GridRight-215,866,213,39)},
    _=>TourSteps()[step].Spots
   };
  }
  void DrawTour(Graphics g){
   var steps=TourSteps();int n=owner.tourStep;if(n<0||n>=steps.Length)return;var step=steps[n] with{Spots=TourSpots(n)};hits.Clear();
   using(var dim=new Region(new RectangleF(0,0,1536,1024))){foreach(var s in step.Spots){using var p=Shape(s,12);dim.Exclude(p);}using var b=new SolidBrush(Color.FromArgb(185,4,8,11));g.FillRegion(b,dim);}
   foreach(var s in step.Spots){using var p=Shape(s,12);using var glow=new Pen(Color.FromArgb(90,232,200,122),7);g.DrawPath(glow,p);using var pen=new Pen(green,2);g.DrawPath(pen,p);}
   const float w=430;float h=MeasureBody(g,step.Text,w-56)+196;RectangleF card;
   if(step.Spots.Length==0)card=new((1536-w)/2,(1024-h)/2,w,h);
   else{var s=step.Spots[0];float x,y;
    if(s.Right+w+40<=1536&&s.Right<900){x=s.Right+28;y=s.Y;}
    else if(s.X-w-28>=0){x=s.X-w-28;y=s.Y;}
    else{x=Math.Clamp(s.X+s.Width/2-w/2,20,1536-w-20);y=s.Bottom+24;}
    card=new(x,Math.Clamp(y,72,1024-h-20),w,h);}
   using(var shadow=new SolidBrush(Color.FromArgb(120,0,0,0))){using var p=Shape(new(card.X+6,card.Y+10,card.Width,card.Height),16);g.FillPath(shadow,p);}
   Box(g,card,C(33,44,52),C(20,28,34),16,C(86,104,116));
   TextAt(g,T($"S C H R I T T   {n+1}   V O N   {steps.Length}",$"S T E P   {n+1}   O F   {steps.Length}"),card.X+28,card.Y+24,12,green);
   for(int i=0;i<steps.Length;i++){using var b=new SolidBrush(i==n?green:C(70,84,97));g.FillEllipse(b,card.Right-28-(steps.Length-i)*14,card.Y+28,8,8);}
   TextAt(g,step.Title,card.X+28,card.Y+50,24,ink,true,w-56,34);
   using(var font=new Font("Segoe UI",15,GraphicsUnit.Pixel))using(var brush=new SolidBrush(C(214,222,228)))g.DrawString(step.Text,font,brush,new RectangleF(card.X+28,card.Y+94,w-56,h-170));
   float by=card.Bottom-62;
   TextAt(g,T("Tour überspringen","Skip tour"),card.X+28,by+12,14,muted);using(var p=new Pen(C(110,128,142)))g.DrawLine(p,card.X+28,by+31,card.X+(En?92:146),by+31);Hit(new(card.X+22,by,En?80:134,40),owner.EndTour);
   bool last=n==steps.Length-1;var next=new RectangleF(card.Right-28-118,by,118,40);
   Box(g,next,C(236,206,134),C(214,180,104),9,C(244,222,166));TextAt(g,last?T("Fertig  ✓","Done  ✓"):n==0?T("Los geht's  →","Let's go  →"):T("Weiter  →","Next  →"),next.X+16,next.Y+11,16,C(24,20,12),true,next.Width-20,24);Hit(next,owner.TourNext);
   if(n>0){var back=new RectangleF(next.X-104,by,94,40);Box(g,back,C(28,36,42),C(22,29,35),9,C(64,77,87));TextAt(g,T("←  Zurück","←  Back"),back.X+14,back.Y+10,15,ink,width:back.Width-16,height:24);Hit(back,owner.TourBack);}
  }
  internal static float MeasureBody(Graphics g,string text,float width){using var font=new Font("Segoe UI",15,GraphicsUnit.Pixel);return g.MeasureString(text,font,(int)width).Height;}
 }
}
public sealed partial class MainForm {
 void BuildTrayMenu(){var old=tray.ContextMenuStrip;var menu=new ContextMenuStrip();
  menu.Items.Add(T("Öffnen","Open"),null,(_,_)=>{Show();WindowState=FormWindowState.Normal;Activate();});
  menu.Items.Add(T("Alle Sounds stoppen","Stop all sounds"),null,(_,_)=>audio.StopSounds());
  menu.Items.Add(T("Anleitung / Tour","Guide / tour"),null,(_,_)=>StartTour());
  menu.Items.Add(T("Nach Updates suchen","Check for updates"),null,(_,_)=>CheckForUpdates());
  menu.Items.Add(T("English","Deutsch"),null,(_,_)=>SetLanguage(!Lang.En));
  menu.Items.Add(new ToolStripSeparator());
  menu.Items.Add(T("Beenden","Quit"),null,(_,_)=>{exitRequested=true;Close();});
  tray.ContextMenuStrip=menu;old?.Dispose();}
}
