package de.cocoa.discorddeck;

import android.app.*;
import android.content.*;
import android.content.pm.*;
import android.database.Cursor;
import android.net.Uri;
import android.os.*;
import android.provider.Settings;
import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.concurrent.*;

final class MobileUpdater {
 interface Banner {void show(String text,boolean enabled,Runnable action);void hide();}
 final MainActivity app;final SharedPreferences prefs;final Banner banner;
 final ExecutorService worker=Executors.newSingleThreadExecutor();final DownloadManager downloads;
 volatile UpdateInfo available;volatile boolean checking,closed,verifying;volatile long lastCheck;
 final BroadcastReceiver receiver=new BroadcastReceiver(){public void onReceive(Context c,Intent i){if(i.getLongExtra(DownloadManager.EXTRA_DOWNLOAD_ID,-2)==prefs.getLong("updateDownload",-1))restoreDownload();}};
 MobileUpdater(MainActivity app,SharedPreferences prefs,Banner banner){
  this.app=app;this.prefs=prefs;this.banner=banner;downloads=(DownloadManager)app.getSystemService(Context.DOWNLOAD_SERVICE);
  IntentFilter filter=new IntentFilter(DownloadManager.ACTION_DOWNLOAD_COMPLETE);
  if(Build.VERSION.SDK_INT>=33)app.registerReceiver(receiver,filter,Context.RECEIVER_EXPORTED);else app.registerReceiver(receiver,filter);
 }
 String t(String de,String en){return app.t(de,en);}
 static long version(PackageInfo info){return Build.VERSION.SDK_INT>=28?info.getLongVersionCode():info.versionCode;}
 static int signatureFlags(){return Build.VERSION.SDK_INT>=28?PackageManager.GET_SIGNING_CERTIFICATES:PackageManager.GET_SIGNATURES;}
 static Signature[] signatures(PackageInfo info){return Build.VERSION.SDK_INT>=28?info.signingInfo.getApkContentsSigners():info.signatures;}
 long installed(){try{return version(app.getPackageManager().getPackageInfo(app.getPackageName(),0));}catch(Exception e){return 0;}}
 void ui(Runnable action){app.runOnUiThread(()->{if(!closed&&!app.isFinishing()&&!app.isDestroyed())action.run();});}
 void show(String text,boolean enabled,Runnable action){ui(()->banner.show(text,enabled,action));}
 void resume(){
  long target=prefs.getLong("updateCode",0);
  if(target>0&&installed()>=target){clearDownload();banner.hide();}
  if(prefs.getLong("updateDownload",-1)>=0){restoreDownload();return;}
  if(!checking&&(lastCheck==0||System.currentTimeMillis()-lastCheck>10*60*1000))check();
 }
 void check(){
  if(checking||closed)return;checking=true;lastCheck=System.currentTimeMillis();
  worker.execute(()->{try{
   JSONObject release=json("https://api.github.com/repos/"+UpdateInfo.REPOSITORY+"/releases/latest",300000);
   if(release.optBoolean("draft")||release.optBoolean("prerelease"))return;
   JSONObject apk=null,meta=null;JSONArray assets=release.getJSONArray("assets");
   for(int i=0;i<assets.length();i++){JSONObject a=assets.getJSONObject(i);if("SSB-Android.apk".equals(a.optString("name")))apk=a;if("SSB-Android-update.json".equals(a.optString("name")))meta=a;}
   if(apk==null||meta==null)return;
   String metadataUrl=meta.getString("browser_download_url");UpdateInfo.validateUrl(metadataUrl);
   JSONObject data=json(metadataUrl,8192);String digest=apk.optString("digest");
   if(!digest.startsWith("sha256:"))throw new IOException("Missing GitHub digest");
   UpdateInfo info=new UpdateInfo(data.getLong("versionCode"),data.getString("versionName"),apk.getString("browser_download_url"),digest.substring(7));
   if(!info.sha256.equalsIgnoreCase(data.getString("sha256")))throw new IOException("Metadata checksum mismatch");
   if(info.newerThan(installed())){available=info;show(t("Update verfügbar: ","Update available: ")+info.versionName+t(" · Herunterladen"," · Download"),true,this::download);}
   else ui(banner::hide);
  }catch(Exception e){/* Offline: keep the sound remote usable, retry on the next check. */lastCheck=0;}
  finally{checking=false;}});
 }
 static JSONObject json(String url,int limit)throws Exception{
  HttpURLConnection c=(HttpURLConnection)new URL(url).openConnection();c.setConnectTimeout(10000);c.setReadTimeout(15000);c.setRequestProperty("User-Agent","SSB-Android-Updater/1.3");
  try{if(c.getResponseCode()!=200)throw new IOException("HTTP "+c.getResponseCode());try(InputStream in=c.getInputStream();ByteArrayOutputStream bytes=new ByteArrayOutputStream()){byte[] b=new byte[4096];int n;while((n=in.read(b))!=-1){if(bytes.size()+n>limit)throw new IOException("Update response too large");bytes.write(b,0,n);}return new JSONObject(bytes.toString(StandardCharsets.UTF_8.name()));}}finally{c.disconnect();}
 }
 void download(){
  if(available==null||prefs.getLong("updateDownload",-1)>=0)return;
  try{
   File file=new File(app.getExternalFilesDir(Environment.DIRECTORY_DOWNLOADS),"SSB-update.apk");if(file.exists()&&!file.delete())throw new IOException("Cannot replace update file");
   DownloadManager.Request request=new DownloadManager.Request(Uri.parse(available.url));request.setTitle("Sardinöööses Soundboard "+available.versionName);request.setMimeType("application/vnd.android.package-archive");request.setNotificationVisibility(DownloadManager.Request.VISIBILITY_VISIBLE_NOTIFY_COMPLETED);request.setDestinationInExternalFilesDir(app,Environment.DIRECTORY_DOWNLOADS,"SSB-update.apk");
   long id=downloads.enqueue(request);prefs.edit().putLong("updateDownload",id).putLong("updateCode",available.versionCode).putString("updateHash",available.sha256).putString("updateVersion",available.versionName).apply();
   show(t("Update wird heruntergeladen …","Downloading update …"),false,()->{});
  }catch(Exception e){show(t("Download fehlgeschlagen · Erneut versuchen","Download failed · Retry"),true,this::download);}
 }
 void restoreDownload(){
  long id=prefs.getLong("updateDownload",-1);if(id<0||closed)return;
  try(Cursor c=downloads.query(new DownloadManager.Query().setFilterById(id))){
   if(!c.moveToFirst()){failed();return;}int status=c.getInt(c.getColumnIndexOrThrow(DownloadManager.COLUMN_STATUS));
   if(status==DownloadManager.STATUS_SUCCESSFUL){verify(id,false);}
   else if(status==DownloadManager.STATUS_FAILED)failed();
   else show(t("Update wird heruntergeladen …","Downloading update …"),false,()->{});
  }catch(Exception e){failed();}
 }
 void failed(){clearDownload();show(t("Update fehlgeschlagen · Erneut prüfen","Update failed · Check again"),true,this::check);}
 void verify(long id,boolean install){
  if(verifying||closed)return;verifying=true;show(t("Update wird geprüft …","Verifying update …"),false,()->{});
  worker.execute(()->{try{
   Uri uri=downloads.getUriForDownloadedFile(id);if(uri==null)throw new IOException("Download missing");
   try(InputStream in=app.getContentResolver().openInputStream(uri)){if(in==null||!UpdateInfo.matches(in,prefs.getString("updateHash","")))throw new IOException("Update checksum mismatch");}
   File file=new File(app.getExternalFilesDir(Environment.DIRECTORY_DOWNLOADS),"SSB-update.apk");PackageInfo apk=app.getPackageManager().getPackageArchiveInfo(file.getAbsolutePath(),signatureFlags());PackageInfo current=app.getPackageManager().getPackageInfo(app.getPackageName(),signatureFlags());
   if(apk==null||!app.getPackageName().equals(apk.packageName)||version(apk)!=prefs.getLong("updateCode",0)||!java.util.Arrays.equals(signatures(apk),signatures(current)))throw new IOException("Wrong package, version or signing key");
   ui(()->{if(install)install(uri);else banner.show(t("Update bereit · Installieren","Update ready · Install"),true,()->verify(id,true));});
  }catch(Exception e){failed();}finally{verifying=false;}});
 }
 void install(Uri uri){
  try{
   if(!app.getPackageManager().canRequestPackageInstalls()){
    new AlertDialog.Builder(app).setTitle(t("App-Updates erlauben","Allow app updates")).setMessage(t("Erlaube SSB einmalig die Installation von Updates. Danach hier erneut Installieren antippen.","Allow SSB to install updates once. Then tap Install here again.")).setNegativeButton(t("Abbrechen","Cancel"),null).setPositiveButton(t("Einstellungen öffnen","Open settings"),(d,w)->app.startActivity(new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,Uri.parse("package:"+app.getPackageName())))).show();return;
   }
   Intent intent=new Intent(Intent.ACTION_VIEW).setDataAndType(uri,"application/vnd.android.package-archive").addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);app.startActivity(intent);
   show(t("Android-Installation bestätigen · Erneut öffnen","Confirm Android installation · Open again"),true,()->verify(prefs.getLong("updateDownload",-1),true));
  }catch(Exception e){show(t("Installation nicht geöffnet · Erneut versuchen","Installer did not open · Retry"),true,()->verify(prefs.getLong("updateDownload",-1),true));}
 }
 void clearDownload(){long id=prefs.getLong("updateDownload",-1);if(id>=0)downloads.remove(id);prefs.edit().remove("updateDownload").remove("updateCode").remove("updateHash").remove("updateVersion").apply();}
 void close(){closed=true;app.unregisterReceiver(receiver);worker.shutdownNow();}
}
