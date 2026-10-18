using System.Reflection;
using System.Text.Json;
namespace DiscordDeck;
public sealed partial class MainForm {
 public static void RunBankTest(){
  var checks=new List<string>();var errors=new List<string>();MainForm? f=null;
  void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
  void Reject(Action a,string name){try{a();}catch{checks.Add(name);return;}throw new Exception(name);}
  try{
   var initial=new Settings{TourDone=true};for(int i=0;i<100;i++)initial.Slots[i].Name=$"Original {i+1}";initial.Save();f=new MainForm();void Prepare(Control c){_=c.Handle;foreach(Control child in c.Controls)Prepare(child);}Prepare(f);var form=f;
   void Paint(string? name=null){using var image=new Bitmap(form.deck.Width,form.deck.Height);form.deck.DrawToBitmap(image,form.deck.ClientRectangle);if(name!=null)image.Save(Path.Combine(AppContext.BaseDirectory,name+".png"));}
   void Event(Control c,string method,EventArgs e)=>c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(c,new[]{e});
   void Mouse(string method,float x,float y){Event(form.deck,method,new MouseEventArgs(MouseButtons.Left,1,(int)(x*form.deck.Width/1536),(int)(y*form.deck.Height/1024),0));}
   int PencilCount(){Paint();var a=form.deck.AccessibilityObject;return Enumerable.Range(0,a.GetChildCount()).Count(i=>a.GetChild(i)?.Name?.StartsWith("Banknamen bearbeiten:")==true);}
   Check(f.settings.Banks.Count==5&&f.settings.Banks.Select(b=>b.Id).Distinct().Count()==5,"Five banks migrate with stable IDs");Check(PencilCount()==0,"Bank pencils hidden outside edit mode");Paint();Mouse("OnMouseDown",120,532);Mouse("OnMouseUp",120,532);Check(f.sorting,"Edit button below Stop activates editing");Check(PencilCount()==5,"All bank pencils appear only in editing");
   var banks=f.settings.Banks.ToArray();var slots=f.settings.Slots.ToArray();f.RenameBank(banks[0],"  Memes 日本語  ");Check(banks[0].Name=="Memes 日本語"&&Settings.Load().Banks[0].Name==banks[0].Name,"Unicode bank name trimmed and persisted");Reject(()=>f.RenameBank(banks[0],"  "),"Blank name rejected");Reject(()=>f.RenameBank(banks[0],new string('a',41)),"Overlong name rejected");
   Paint();Mouse("OnMouseDown",212,145);Mouse("OnMouseUp",212,145);Check(f.soundEditor!=null,"Bank pencil opens inline name editor without dragging");f.soundEditor!.Text="Cancelled";Event(f.soundEditor,"OnKeyDown",new KeyEventArgs(Keys.Escape));Check(banks[0].Name=="Memes 日本語","Escape discards bank rename");
   f.EditBankName(0);f.soundEditor!.Text="Soundbank Eins";Event(f.soundEditor,"OnKeyDown",new KeyEventArgs(Keys.Enter));Check(banks[0].Name=="Soundbank Eins"&&!f.draftInvalid,"Enter saves bank name without changing trim validity");
   Paint();Mouse("OnMouseDown",110,147);Mouse("OnMouseMove",110,402);Mouse("OnMouseUp",110,402);Check(ReferenceEquals(f.settings.Banks[4],banks[0]),"Drag moves whole first bank to fifth position");Check(f.settings.Slots.Skip(80).Take(20).SequenceEqual(slots.Take(20)),"All twenty sounds move together in original order");Check(f.settings.Slots.Take(80).SequenceEqual(slots.Skip(20)),"Other banks shift without losing sounds");Check(f.bank==4&&f.selectedSound==80,"Visible bank and edited sound follow stable identities");Check(Settings.Load().Banks[4].Name=="Soundbank Eins","Bank name follows bank after reload");
   f.MoveBank(4,0);Check(f.settings.Slots.SequenceEqual(slots)&&f.settings.Banks.SequenceEqual(banks),"Moving bank back restores exact original order");
   Directory.CreateDirectory(Settings.Config+".tmp");try{Reject(()=>f.MoveBank(0,3),"Bank move save failure reported");Check(f.settings.Slots.SequenceEqual(slots)&&f.settings.Banks.SequenceEqual(banks),"Bank move rollback preserves bank and sound order");Reject(()=>f.RenameBank(banks[0],"Cannot save"),"Bank name save failure reported");Check(banks[0].Name=="Soundbank Eins","Bank name rollback preserves saved name");}finally{Directory.Delete(Settings.Config+".tmp");}
   Paint();Mouse("OnMouseDown",110,147);Mouse("OnMouseMove",110,274);Event(f.deck,"OnKeyDown",new KeyEventArgs(Keys.Escape));Mouse("OnMouseUp",110,274);Check(f.settings.Banks.SequenceEqual(banks)&&f.sorting,"Escape cancels bank drag while retaining edit mode");
   f.EditBankName(0);f.soundEditor!.Text="";Event(f.soundEditor,"OnKeyDown",new KeyEventArgs(Keys.Enter));f.ToggleSorting();Check(f.soundEditor==null&&!f.sorting&&PencilCount()==0,"Leaving edit mode dismisses invalid bank editor and hides pencils");
   f.ToggleSorting();f.MoveSound(0,25);Check(ReferenceEquals(f.settings.Slots[25],slots[0]),"Individual sound movement remains available in edit mode");f.MoveBank(0,1);var payload=JsonSerializer.SerializeToElement(f.Dispatch("status",0));Check(payload.GetProperty("slots").GetArrayLength()==100,"Phone protocol retains all 100 sounds after bank and sound moves");
   f.bank=0;Paint("banks-edit-de");f.SetLanguage(true);f.ClientSize=new(1080,720);Paint("banks-edit-small-en");Check(f.settings.Banks.Count==5,"Small English layout retains all banks");
   var saved=Settings.Load();Check(saved.Banks.Select(b=>b.Id).SequenceEqual(f.settings.Banks.Select(b=>b.Id))&&saved.Slots.Select(s=>s.Id).SequenceEqual(f.settings.Slots.Select(s=>s.Id)),"Combined bank and sound edits persist across reload");
  }catch(Exception e){errors.Add(e.ToString());}
  finally{if(f!=null){f.exitRequested=true;f.Close();f.Dispose();}File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"bank-test.json"),JsonSerializer.Serialize(new{ok=errors.Count==0,checks,errors},new JsonSerializerOptions{WriteIndented=true}));if(errors.Count>0)Environment.ExitCode=1;}
 }
}
