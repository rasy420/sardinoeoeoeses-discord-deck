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
 MainActivity app;int checks;String pin;android.content.SharedPreferences prefs;boolean guidePresent,guideValue;
 @Override public void onCreate(Bundle b){super.onCreate(b);pin=b.getString("pin");start();}
 void check(boolean good,String message){if(!good)throw new AssertionError(message);checks++;}
 boolean ui(java.util.function.BooleanSupplier value){AtomicBoolean b=new AtomicBoolean();runOnMainSync(()->b.set(value.getAsBoolean()));return b.get();}
 void waitFor(java.util.function.BooleanSupplier condition,String name)throws Exception{long end=System.currentTimeMillis()+7000;while(System.currentTimeMillis()<end){if(ui(condition)){checks++;return;}Thread.sleep(50);}throw new AssertionError(name);}
 void shot(String name)throws Exception{android.graphics.Bitmap b=getUiAutomation().takeScreenshot();try(FileOutputStream out=new FileOutputStream(new File(app.getExternalFilesDir(null),name))){b.compress(android.graphics.Bitmap.CompressFormat.PNG,100,out);}b.recycle();}
 @Override public void onStart(){Bundle result=new Bundle();try{
  prefs=getTargetContext().getSharedPreferences("remote",0);guidePresent=prefs.contains("guideDone");guideValue=prefs.getBoolean("guideDone",false);prefs.edit().putBoolean("guideDone",true).commit();
  app=(MainActivity)startActivitySync(new Intent(getTargetContext(),MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));
  MainActivity.Server server=app.identify("127.0.0.1",47846);JSONObject pairing=app.request(server,"/pair","POST",new JSONObject().put("pin",pin).toString(),false);
  runOnMainSync(()->{app.selected=server;app.pairedId=server.id();app.key=pairing.optString("key");app.pair.setVisibility(View.GONE);app.tiles.reset();app.lastStatusAt=0;});app.tick();
  waitFor(()->app.pads[0].isEnabled(),"Paired status not received");
  check(ui(()->app.help.getText().toString().equals("?")&&app.help.getWidth()<=app.dp(44)&&app.help.getCurrentTextColor()==MainActivity.GREEN),"Small golden header help");
  check(ui(()->!contains((ViewGroup)app.findViewById(android.R.id.content),"Alle Sounds stoppen")&&!contains((ViewGroup)app.findViewById(android.R.id.content),"Kurzanleitung")),"Large help and global stop removed");
  runOnMainSync(()->{app.pads[0].performClick();app.pads[0].performClick();});
  waitFor(()->app.stopPads[0].getVisibility()==View.VISIBLE&&app.stopPads[0].isEnabled(),"Red stop missing after playback");
  check(ui(()->app.pads[0].getVisibility()==View.INVISIBLE&&app.stopPads[0].getWidth()==app.pads[0].getWidth()),"Stop occupies entire playing tile");
  runOnMainSync(()->app.pads[1].performClick());waitFor(()->app.tiles.playing(0)&&app.tiles.playing(1),"Parallel playback");shot("usb-playing144.png");
  long start=System.currentTimeMillis();runOnMainSync(()->app.stopPads[0].performClick());waitFor(()->!app.tiles.playing(0)&&app.pads[0].isEnabled(),"Target stop failed");check(System.currentTimeMillis()-start<1800,"Stop latency");
  JSONObject status=app.request(server,"/status","GET",null,true);check(!status.getJSONArray("slots").getJSONObject(0).getBoolean("playing")&&status.getJSONArray("slots").getJSONObject(1).getBoolean("playing"),"Real PC confirms only target stopped");
  runOnMainSync(()->app.stopPads[1].performClick());waitFor(()->!app.tiles.playing(1),"Second stop");
  runOnMainSync(()->app.pads[2].performClick());waitFor(()->app.tiles.playing(2),"Short clip starts");waitFor(()->!app.tiles.playing(2)&&app.stopPads[2].getVisibility()==View.GONE,"Natural end restores play");
  runOnMainSync(()->app.pads[0].performClick());waitFor(()->app.tiles.playing(0),"Background test starts");runOnMainSync(()->callActivityOnStop(app));app.request(server,"/stop-sound/"+app.tiles.ids[0],"POST",null,true);runOnMainSync(()->callActivityOnStart(app));waitFor(()->!app.tiles.playing(0),"Resume reconciles actual PC state");
  shot("usb-idle144.png");result.putString("result","PASS "+checks+" real-device checks");finish(Activity.RESULT_OK,result);
 }catch(Throwable e){result.putString("result","FAIL "+e);finish(Activity.RESULT_CANCELED,result);}finally{if(prefs!=null){android.content.SharedPreferences.Editor edit=prefs.edit();if(guidePresent)edit.putBoolean("guideDone",guideValue);else edit.remove("guideDone");edit.commit();}if(app!=null)runOnMainSync(()->app.finish());}}
 boolean contains(ViewGroup group,String text){for(int i=0;i<group.getChildCount();i++){View v=group.getChildAt(i);if(v instanceof TextView&&((TextView)v).getText().toString().contains(text))return true;if(v instanceof ViewGroup&&contains((ViewGroup)v,text))return true;}return false;}
}
