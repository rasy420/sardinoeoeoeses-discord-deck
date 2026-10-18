using System.Reflection;
using System.Text.Json;
using NAudio.Wave;
namespace DiscordDeck;
public sealed partial class MainForm {
 public static void RunSoundSettingsTest(){
  var checks=new List<string>();void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
  void Reject(Action action,string name){try{action();}catch{checks.Add(name);return;}throw new Exception(name);}
  Directory.CreateDirectory(Settings.Folder);var source=Path.Combine(Settings.Folder,"goonen(1).wav");var replacement=Path.Combine(Settings.Folder,"Neuer Sound.wav");
  foreach(var path in new[]{source,replacement})using(var writer=new WaveFileWriter(path,WaveFormat.CreateIeeeFloatWaveFormat(44100,1))){var samples=new float[44100];for(int i=0;i<samples.Length;i++)samples[i]=.005f*(float)Math.Sin(i*2*Math.PI*440/44100);writer.WriteSamples(samples,0,samples.Length);}
  var initial=new Settings{TourDone=true};initial.Slots[1]=new SoundSlot{Name="legacy",File=source,Volume=.65f,TrimStart=.08,TrimEnd=.76};initial.Save();Check(Settings.Load().Slots[1].Name=="goonen(1)","Initial local filename is used automatically");
  AutoTour=false;using var form=new MainForm();void Prepare(Control c){_=c.Handle;foreach(Control child in c.Controls)Prepare(child);}Prepare(form);form.PerformLayout();form.SelectSound(1);
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  void Event(Control c,string name,EventArgs e)=>c.GetType().GetMethod(name,flags)!.Invoke(c,new object[]{e});
  void Click(float x,float y){var pt=new Point((int)(x*form.deck.Width/1536),(int)(y*form.deck.Height/1024));Event(form.deck,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,pt.X,pt.Y,0));Event(form.deck,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,pt.X,pt.Y,0));}
  void Key(TextBox editor,Keys key)=>Event(editor,"OnKeyDown",new KeyEventArgs(key));
  void Pump(int ms){var until=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}}
  void Shot(string file){using var b=new Bitmap(form.deck.Width,form.deck.Height);form.deck.DrawToBitmap(b,form.deck.ClientRectangle);b.Save(Path.Combine(AppContext.BaseDirectory,file));}
  Shot("sound-settings-de.png");Pump(300);Shot("sound-settings-de.png");var slot=form.settings.Slots[1];RectangleF pencil;using(var g=form.deck.CreateGraphics())pencil=form.deck.NamePencil(g,slot.Name);Check(pencil.Right<=1513&&pencil.X>1194,"Pencil sits beside the name within the panel");
  Click(pencil.X+12,pencil.Y+12);Check(form.soundEditor!=null,"Pencil opens inline name editor");form.soundEditor!.Text="Mein eigener Sound";Key(form.soundEditor,Keys.Enter);Check(form.soundEditor==null&&slot.Name=="Mein eigener Sound"&&slot.CustomName,"Enter saves custom display name");Check(Path.GetFileName(slot.File)=="goonen(1).wav"&&File.Exists(source),"Renaming leaves physical audio filename untouched");Check(Settings.Load().Slots[1].Name==slot.Name,"Custom name survives settings reload");
  form.EditSoundName();form.soundEditor!.Text="Verwerfen";Key(form.soundEditor,Keys.Escape);Check(slot.Name=="Mein eigener Sound"&&form.soundEditor==null,"Escape discards name changes");
  form.EditSoundName();form.soundEditor!.Text="   ";Key(form.soundEditor,Keys.Enter);Check(slot.Name=="Mein eigener Sound"&&form.soundEditor!=null,"Blank name rejected while editor stays open");Key(form.soundEditor!,Keys.Escape);
  form.EditSoundName();form.soundEditor!.Text="Name nach Fokuswechsel";Event(form.soundEditor,"OnLeave",EventArgs.Empty);Check(slot.Name=="Name nach Fokuswechsel"&&form.soundEditor==null,"Leaving the name editor saves the name");
  Reject(()=>form.RenameSound(slot,new string('x',101)),"Overlong name rejected");Reject(()=>form.RenameSound(slot,"bad\nname"),"Control characters rejected");
  var order=(slot.Name,slot.CustomName);var before=File.ReadAllBytes(Settings.Config);Directory.CreateDirectory(Settings.Config+".tmp");try{Reject(()=>form.RenameSound(slot,"Cannot save"),"Failed rename save reports an error");}finally{Directory.Delete(Settings.Config+".tmp");}Check((slot.Name,slot.CustomName)==order&&File.ReadAllBytes(Settings.Config).SequenceEqual(before),"Failed rename save rolls back name and flag");
  var menu=form.deck.PrepareReplaceMenu();Check(menu.Items.Count==2&&menu.Items[0].Text=="Audiodatei auswählen …"&&menu.Items[1].Text=="MyInstants-Link einfügen …","Replace menu contains exactly the requested two actions");for(int i=0;i<20;i++)form.deck.PrepareReplaceMenu();Check(menu.Items.Count==2,"Repeated replace-menu opens do not accumulate items");
  menu.Size=menu.GetPreferredSize(Size.Empty);menu.PerformLayout();using(var image=new Bitmap(menu.Width,menu.Height)){menu.DrawToBitmap(image,menu.ClientRectangle);image.Save(Path.Combine(AppContext.BaseDirectory,"sound-replace-menu.png"));}
  Check(form.PanelTrim() is {} initialTrim&&Math.Abs(initialTrim.from-.08)<.001&&Math.Abs(initialTrim.to-.76)<.001,"Panel shows saved start/end values");Shot("sound-settings-reference.png");
  Click(1250,262);Check(form.soundEditor!=null,"Start field opens inline seconds editor");form.soundEditor!.Text="0,12";Key(form.soundEditor,Keys.Enter);Check(Math.Abs(form.PanelTrim()!.Value.from-.12)<.001&&form.TrimDirty,"German decimal time entry updates marker and dirty state");
  Click(1370,262);form.soundEditor!.Text="0.82";Key(form.soundEditor,Keys.Enter);Check(Math.Abs(form.PanelTrim()!.Value.to-.82)<.001,"End field accepts decimal dot");
  Click(1370,262);form.soundEditor!.Text="NaN";Key(form.soundEditor,Keys.Enter);Check(form.soundEditor!=null&&Math.Abs(form.PanelTrim()!.Value.to-.82)<.001,"Invalid seconds do not change the range");Key(form.soundEditor!,Keys.Escape);
  Shot("sound-settings-edited.png");Click(1292,256);Click(1292,256);Check(Math.Abs(form.PanelTrim()!.Value.from-.14)<.001,"Rapid spinner clicks retain both increments");
  form.DragTrim(true,99);Check(Math.Abs(form.PanelTrim()!.Value.from-.77)<.001,"Start clamps before end");form.DragTrim(false,-5);Check(form.PanelTrim()!.Value.to>form.PanelTrim()!.Value.from,"End clamps after start");form.DragTrim(true,.08);form.DragTrim(false,.76);form.SaveTrim();
  Check(Math.Abs(Settings.Load().Slots[1].TrimStart-.08)<.001&&Math.Abs(Settings.Load().Slots[1].TrimEnd-.76)<.001,"Numeric range saved and restored");
  form.TogglePanelPreview();Pump(80);Check(form.PanelPreviewing,"Existing headphone preview still plays");form.DragTrim(true,.09);Check(!form.PanelPreviewing,"Editing range stops stale preview");
  form.ReplaceSoundSource(slot,replacement,"Neuer Sound","");Check(slot.Name=="Name nach Fokuswechsel"&&slot.CustomName&&slot.File==replacement&&slot.TrimStart==0&&slot.TrimEnd==0,"Replacing audio preserves custom name and resets the range");Check(File.Exists(source)&&slot.Volume==.65f,"Replace keeps original file and gain");
  var normal=form.settings.Slots[2];form.ReplaceSoundSource(normal,source,"goonen(1)","");Check(normal.Name=="goonen(1)"&&!normal.CustomName,"Uncustomized slot gets new source filename");form.ReplaceSoundSource(normal,replacement,"MyInstants title","https://www.myinstants.com/en/instant/test/");Check(normal.Name=="MyInstants title","Uncustomized MyInstants source gets its title");
  var old=(slot.File,slot.Name);Reject(()=>form.ReplaceSoundSource(slot,source+".missing","missing",""),"Missing replacement file rejected");Check((slot.File,slot.Name)==old,"Failed replacement keeps existing source and name");
  var payload=JsonSerializer.SerializeToElement(form.Dispatch("status",0));Check(payload.GetProperty("slots")[1].GetProperty("name").GetString()==slot.Name,"Existing phone protocol includes custom name");form.MoveSound(1,5);Check(ReferenceEquals(form.settings.Slots[5],slot)&&slot.CustomName&&Settings.Load().Slots[5].Name==slot.Name,"Custom name follows reorder and survives reload");
  form.SetLanguage(true);Check(form.deck.PrepareReplaceMenu().Items[0].Text=="Choose audio file …","Replacement options translated to English");Shot("sound-settings-en.png");
  form.EditSoundName();form.ClientSize=new Size(1080,720);form.PerformLayout();Check(form.soundEditor!.Left==(int)(1194*form.deck.Width/1536f),"Active inline editor stays aligned when the window resizes");Key(form.soundEditor,Keys.Escape);Shot("sound-settings-small.png");Click(1250,262);Check(form.soundEditor!=null,"Time editing works with scaled small-window coordinates");Key(form.soundEditor!,Keys.Escape);
  using var reopened=new MainForm();Check(reopened.settings.Slots[5].Name==slot.Name&&reopened.settings.Slots[5].CustomName,"New app instance retains independent display name");
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"sound-settings-test.json"),JsonSerializer.Serialize(new{ok=true,checks},new JsonSerializerOptions{WriteIndented=true}));
 }
}
