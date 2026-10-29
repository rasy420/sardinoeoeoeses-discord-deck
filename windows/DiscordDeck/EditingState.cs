using static DiscordDeck.Lang;
namespace DiscordDeck;

public sealed partial class MainForm {
 internal SoundSlot EditedSlot=>settings.Slots[selectedSound];
 bool soundPanelOpen,draftInvalid,importing;float draftGain=.65f;string editError="";
 readonly System.Windows.Forms.Timer playbackRefresh=new(){Interval=100};
 internal bool ResolveDraft(){
  if(!TrimDirty&&!draftInvalid)return true;
  var result=DarkChoice(T("Änderungen speichern?","Save changes?"),T("Ausschnitt und Pegel wurden geändert.","Trim and level have changed."),T("Speichern","Save"),T("Verwerfen","Discard"),T("Abbrechen","Cancel"));
  if(result==DialogResult.Cancel)return false;
  if(result==DialogResult.Yes){try{SaveTrim();}catch(Exception e){editError=e.Message;deck.Invalidate();return false;}}
  else ResetTrim();return true;
 }
 internal void OpenSoundSettings(int index){
  if(index<0||index>=settings.Slots.Count)return;
  if(soundPanelOpen&&index==selectedSound)return;
  if(soundPanelOpen&&!ResolveDraft())return;
  soundEditor?.Dispose();soundEditor=null;draftInvalid=false;editError="";SelectEdited(index);deck.ClampScroll();
 }
 internal bool DismissSoundSettings(){
  if(soundPanelOpen&&(TrimDirty||draftInvalid)){editError=draftInvalid?T("Bitte zuerst die markierte Eingabe korrigieren und speichern.","Please correct the marked value and save first."):T("Bitte die Änderungen zuerst mit Speichern übernehmen.","Please save your changes first.");status.Text=T("Fehler: ","Error: ")+editError;deck.Invalidate();return false;}
  if(soundPanelOpen)CloseSoundSettings();return !soundPanelOpen;
 }
 internal void ActivateTile(int index){
  if(!DismissSoundSettings())return;
  if(string.IsNullOrEmpty(settings.Slots[index].File))return;
  Play(index);
 }
 internal void CloseSoundSettings(){if(!ResolveDraft())return;soundEditor?.Dispose();soundEditor=null;StopPanelPreview();soundPanelOpen=false;deck.ClampScroll();deck.Focus();deck.Invalidate();}
 internal void RemoveSound(SoundSlot slot){
  var before=(slot.File,slot.Name,slot.SourceUrl,slot.TrimStart,slot.TrimEnd,slot.Volume,slot.CustomName);
  slot.File=slot.SourceUrl="";slot.Name=$"Sound {settings.Slots.IndexOf(slot)+1}";slot.TrimStart=slot.TrimEnd=0;slot.Volume=.65f;slot.CustomName=false;
  try{settings.Save();}catch{(slot.File,slot.Name,slot.SourceUrl,slot.TrimStart,slot.TrimEnd,slot.Volume,slot.CustomName)=before;throw;}
  audio.StopSlot(slot);if(settings.Slots[selectedSound].Id==slot.Id){StopPanelPreview();ForgetTrim();draftGain=.65f;soundPanelOpen=false;draftInvalid=false;editError="";soundEditor?.Dispose();soundEditor=null;}
  deck.ClampScroll();deck.Invalidate();
 }
 void ConfirmRemoveSound(){
  var slot=settings.Slots[selectedSound];if(string.IsNullOrEmpty(slot.File))return;
  if(DarkChoice(T("Sound entfernen?","Remove sound?"),T("Soll dieser Sound aus diesem Slot entfernt werden? Die ursprüngliche Audiodatei bleibt erhalten.","Remove this sound from this slot? The original audio file will be kept."),T("Entfernen","Remove"),null,T("Abbrechen","Cancel"))==DialogResult.Yes)Try(()=>RemoveSound(slot));
 }
 DialogResult DarkChoice(string title,string text,string yes,string? no,string cancel){
  using var f=new Form{Text=title,ClientSize=new(490,190),StartPosition=FormStartPosition.CenterParent,BackColor=Color.FromArgb(19,27,33),ForeColor=Color.FromArgb(240,239,231),FormBorderStyle=FormBorderStyle.FixedDialog,MinimizeBox=false,MaximizeBox=false,Font=new("Segoe UI",10),AutoScaleMode=AutoScaleMode.Dpi};
  f.Controls.Add(new Label{Text=text,Bounds=new(22,20,446,84)});
  Button Add(string label,int x,DialogResult r){var b=new Button{Text=label,Bounds=new(x,128,138,40),DialogResult=r,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(31,41,49),ForeColor=Color.FromArgb(240,239,231)};b.FlatAppearance.BorderColor=Color.FromArgb(75,91,102);f.Controls.Add(b);return b;}
  var abort=Add(cancel,22,DialogResult.Cancel);if(no!=null)Add(no,176,DialogResult.No);Add(yes,330,DialogResult.Yes);f.CancelButton=abort;return f.ShowDialog(this);
 }
 void OpenAudioSettings(){
  using var f=new Form{Text=T("Audio-Einstellungen","Audio settings"),ClientSize=new(530,440),StartPosition=FormStartPosition.CenterParent,BackColor=Color.FromArgb(19,27,33),ForeColor=Color.FromArgb(240,239,231),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=new("Segoe UI",10),AutoScaleMode=AutoScaleMode.Dpi};
  void Label(string text,int y)=>f.Controls.Add(new Label{Text=text,Bounds=new(24,y,480,25)});
  void Combo(ComboBox c,int y){c.Parent=f;c.Bounds=new(24,y,482,32);c.Visible=true;c.Enabled=true;}
  Label(T("Discord-Ausgabe","Discord output"),20);Combo(output,48);Label(T("Mikrofon","Microphone"),96);Combo(input,124);Label(T("Mithören über","Listen on"),256);Combo(monitorOutput,284);
  mic.Text=T("Mikrofon beimischen","Mix in microphone");monitorCheck.Text=T("Sounds selbst mithören","Hear sounds yourself");mic.Parent=f;monitorCheck.Parent=f;mic.BackColor=monitorCheck.BackColor=f.BackColor;mic.Location=new(24,180);monitorCheck.Location=new(24,216);mic.Visible=monitorCheck.Visible=true;mic.Enabled=monitorCheck.Enabled=true;mic.ForeColor=monitorCheck.ForeColor=f.ForeColor;
  EventHandler change=(_,_)=>Try(()=>{bool running=audio.Running;ReadSettings();settings.Save();if(running){audio.Stop();StartAudio();}input.Enabled=output.Enabled=monitorOutput.Enabled=mic.Enabled=monitorCheck.Enabled=true;});
  input.SelectedIndexChanged+=change;output.SelectedIndexChanged+=change;monitorOutput.SelectedIndexChanged+=change;mic.CheckedChanged+=change;monitorCheck.CheckedChanged+=change;
  Label(T("Discord: virtuelles Mikrofon verwenden.\nRauschunterdrückung für Sounds ausschalten.","Discord: use the virtual microphone.\nTurn off noise suppression for sounds."),338);f.Controls[f.Controls.Count-1].Height=55;
  var close=new Button{Text=T("Schließen","Close"),Bounds=new(366,389,140,34),DialogResult=DialogResult.OK,FlatStyle=FlatStyle.Flat};f.Controls.Add(close);f.CancelButton=close;
  try{f.ShowDialog(this);}finally{input.SelectedIndexChanged-=change;output.SelectedIndexChanged-=change;monitorOutput.SelectedIndexChanged-=change;mic.CheckedChanged-=change;monitorCheck.CheckedChanged-=change;foreach(Control c in new Control[]{input,output,monitorOutput,mic,monitorCheck}){c.Parent=deck;c.Visible=false;}PlaceReferenceControls();deck.Focus();}
 }
 sealed partial class ReferenceDeck {
  int keyboardTile;
  internal void ClampScroll(){owner.scrollY=Math.Clamp(owner.scrollY,0,MaxScroll);SnapScroll();}
  void ActivateKeyboard(){owner.Try(()=>owner.ActivateTile(keyboardTile));}
  protected override bool ProcessDialogKey(Keys key){if(key==Keys.Tab||key==(Keys.Shift|Keys.Tab)){if(hits.Count>0)focusHit=(focusHit+(key.HasFlag(Keys.Shift)?hits.Count-1:1)+hits.Count)%hits.Count;Invalidate();return true;}return base.ProcessDialogKey(key);}
 }
}
