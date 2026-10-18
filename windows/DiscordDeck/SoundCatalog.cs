using System.Diagnostics;
using static DiscordDeck.Lang;
namespace DiscordDeck;
static class SoundCatalog {
 public static Button CreateButton(){var button=new Button{Text=T("MyInstants öffnen","Open MyInstants"),AutoSize=true};button.Click+=(_,_)=>{try{Process.Start(new ProcessStartInfo{FileName="https://www.myinstants.com/",UseShellExecute=true});}catch(Exception e){MessageBox.Show(T("Webseite konnte nicht geöffnet werden: ","Could not open the website: ")+e.Message);}};return button;}
}
