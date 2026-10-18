using System.Diagnostics;
namespace DiscordDeck;
static class SoundCatalog {
 public static Button CreateButton(){var button=new Button{Text="MyInstants öffnen",AutoSize=true};button.Click+=(_,_)=>{try{Process.Start(new ProcessStartInfo{FileName="https://www.myinstants.com/",UseShellExecute=true});}catch(Exception e){MessageBox.Show("Webseite konnte nicht geöffnet werden: "+e.Message);}};return button;}
}
