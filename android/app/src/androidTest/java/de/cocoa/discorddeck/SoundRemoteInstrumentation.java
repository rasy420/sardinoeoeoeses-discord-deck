package de.cocoa.discorddeck;
import android.app.*;
import android.content.*;
import android.os.Bundle;
import android.view.*;
import android.widget.*;
import org.json.*;
import java.io.*;
import java.util.concurrent.atomic.AtomicBoolean;

/** Real USB regression test against an isolated PC server, without changing saved pairing. */
public class SoundRemoteInstrumentation extends Instrumentation {
 MainActivity app;int checks;String pin;android.content.SharedPreferences prefs;boolean guidePresent,guideValue,bordersPresent;String bordersValue="";
 @Override public void onCreate(Bundle b){super.onCreate(b);pin=b.getString("pin");start();}
 void check(boolean good,String message){if(!good)throw new AssertionError(message);checks++;}
 boolean ui(java.util.function.BooleanSupplier value){AtomicBoolean b=new AtomicBoolean();runOnMainSync(()->b.set(value.getAsBoolean()));return b.get();}
 void waitFor(java.util.function.BooleanSupplier condition,String name)throws Exception{long end=System.currentTimeMillis()+7000;while(System.currentTimeMillis()<end){if(ui(condition)){checks++;return;}Thread.sleep(50);}throw new AssertionError(name);}
 void shot(String name)throws Exception{android.graphics.Bitmap b=getUiAutomation().takeScreenshot();try(FileOutputStream out=new FileOutputStream(new File(app.getExternalFilesDir(null),name))){b.compress(android.graphics.Bitmap.CompressFormat.PNG,100,out);}b.recycle();}
 @Override public void onStart(){Bundle result=new Bundle();try{
  prefs=getTargetContext().getSharedPreferences("remote",0);guidePresent=prefs.contains("guideDone");guideValue=prefs.getBoolean("guideDone",false);bordersPresent=prefs.contains("tileBorders");bordersValue=prefs.getString("tileBorders","");prefs.edit().putBoolean("guideDone",true).commit();
  app=(MainActivity)startActivitySync(new Intent(getTargetContext(),MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));
  MainActivity.Server server=app.identify("127.0.0.1",47846);JSONObject pairing=app.request(server,"/pair","POST",new JSONObject().put("pin",pin).toString(),false);
  runOnMainSync(()->{app.selected=server;app.pairedId=server.id();app.key=pairing.optString("key");app.pair.setVisibility(View.GONE);app.tiles.reset();app.lastStatusAt=0;});app.tick();
  waitFor(()->app.pads[0].isEnabled(),"Paired status not received");
  check(ui(()->app.help.getText().toString().equals("?")&&app.help.getWidth()<=app.dp(44)&&app.help.getCurrentTextColor()==MainActivity.GREEN),"Small golden header help");
  check(ui(()->("Bearbeiten".equals(app.edit.getText().toString())||"Edit".equals(app.edit.getText().toString()))&&app.edit.getHeight()>0&&app.edit.getHeight()<=app.dp(36)&&app.colorBar.getVisibility()==View.GONE),"Small edit button, colors hidden");
  runOnMainSync(()->app.edit.performClick());
  check(ui(()->app.coloring&&("Fertig".equals(app.edit.getText().toString())||"Done".equals(app.edit.getText().toString()))&&app.colorBar.getVisibility()==View.VISIBLE),"Edit opens ten colors");
  check(ui(()->app.swatches.length==10&&(app.swatches[9].getContentDescription().toString().contains("Weiß")||app.swatches[9].getContentDescription().toString().contains("White"))),"Ten named colors");
  runOnMainSync(()->{app.swatches[1].performClick();app.pads[3].performClick();});
  check(ui(()->app.borders[3]==TileBorders.PALETTE[1]&&app.tiles.pending.isEmpty()&&app.tiles.playing.isEmpty()),"Border only, no playback");
  android.graphics.drawable.GradientDrawable painted=(android.graphics.drawable.GradientDrawable)((android.graphics.drawable.RippleDrawable)app.pads[3].getBackground()).getDrawable(0);
  check(painted.getColor().getDefaultColor()==MainActivity.CARD,"Tile fill stays dark");
  check(app.prefs.getString("tileBorders","").contains(String.valueOf(TileBorders.PALETTE[1])),"Border saved on the phone");
  runOnMainSync(()->app.pads[3].performClick());
  check(ui(()->app.borders[3]==0),"Same color removes the border");
  runOnMainSync(()->{app.swatches[4].performClick();app.pads[0].performClick();});
  shot("usb-colors.png");
  runOnMainSync(()->app.edit.performClick());
  check(ui(()->!app.coloring&&app.colorBar.getVisibility()==View.GONE&&app.borders[0]==TileBorders.PALETTE[4]),"Done closes colors and keeps the border");
  check(ui(()->contains((ViewGroup)app.findViewById(android.R.id.content),"Alle Sounds stoppen")&&!contains((ViewGroup)app.findViewById(android.R.id.content),"Kurzanleitung")),"Global stop at the top, large help removed");
  check(ui(()->!app.stopAll.isEnabled()||app.tiles.connected),"Stop all only active while connected");
  runOnMainSync(()->{for(int k=0;k<6;k++)app.pads[0].performClick();});
  waitFor(()->app.tiles.playing(0)&&app.pads[0].getVisibility()==View.VISIBLE&&app.pads[0].isEnabled(),"Spammed tile keeps playing and stays tappable");
  JSONObject spam=app.request(server,"/status","GET",null,true);check(spam.getJSONArray("slots").getJSONObject(0).getBoolean("playing")&&!ui(()->app.state.getText().toString().contains("16")),"Real PC plays the spammed sound without an error");
  runOnMainSync(()->app.pads[1].performClick());waitFor(()->app.tiles.playing(0)&&app.tiles.playing(1),"Parallel playback");shot("usb-playing146.png");
  long start=System.currentTimeMillis();runOnMainSync(()->app.stopAll.performClick());waitFor(()->!app.tiles.playing(0)&&!app.tiles.playing(1)&&app.pads[0].isEnabled(),"Stop all failed");check(System.currentTimeMillis()-start<1800,"Stop all latency");
  JSONObject status=app.request(server,"/status","GET",null,true);check(!status.getJSONArray("slots").getJSONObject(0).getBoolean("playing")&&!status.getJSONArray("slots").getJSONObject(1).getBoolean("playing"),"Real PC confirms all sounds stopped");
  runOnMainSync(()->app.pads[2].performClick());waitFor(()->app.tiles.playing(2),"Short clip starts");waitFor(()->!app.tiles.playing(2)&&app.pads[2].isEnabled(),"Natural end restores play");
  runOnMainSync(()->app.pads[0].performClick());waitFor(()->app.tiles.playing(0),"Background test starts");runOnMainSync(()->callActivityOnStop(app));app.request(server,"/stop-sound/"+app.tiles.ids[0],"POST",null,true);runOnMainSync(()->callActivityOnStart(app));waitFor(()->!app.tiles.playing(0),"Resume reconciles actual PC state");
  shot("usb-idle146.png");result.putString("result","PASS "+checks+" real-device checks");finish(Activity.RESULT_OK,result);
 }catch(Throwable e){result.putString("result","FAIL "+e);finish(Activity.RESULT_CANCELED,result);}finally{if(prefs!=null){android.content.SharedPreferences.Editor edit=prefs.edit();if(guidePresent)edit.putBoolean("guideDone",guideValue);else edit.remove("guideDone");if(bordersPresent)edit.putString("tileBorders",bordersValue);else edit.remove("tileBorders");edit.commit();}if(app!=null)runOnMainSync(()->app.finish());}}
 boolean contains(ViewGroup group,String text){for(int i=0;i<group.getChildCount();i++){View v=group.getChildAt(i);if(v instanceof TextView&&((TextView)v).getText().toString().contains(text))return true;if(v instanceof ViewGroup&&contains((ViewGroup)v,text))return true;}return false;}
}
