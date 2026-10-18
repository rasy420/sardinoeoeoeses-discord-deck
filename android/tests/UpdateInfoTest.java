package de.cocoa.discorddeck;
import java.io.ByteArrayInputStream;
import java.nio.charset.StandardCharsets;
public class UpdateInfoTest {
 static int count;
 static void check(boolean ok,String name){if(!ok)throw new AssertionError(name);count++;System.out.println("PASS "+name);}
 static void reject(Runnable action,String name){try{action.run();}catch(Exception e){count++;System.out.println("PASS "+name);return;}throw new AssertionError(name);}
 public static void main(String[] args)throws Exception{
  String url="https://github.com/"+UpdateInfo.REPOSITORY+"/releases/download/v1.3.0/SSB-Android.apk";
  String sha="a".repeat(64);UpdateInfo info=new UpdateInfo(8,"1.3.0",url,sha);
  check(info.newerThan(7),"Older Android sees update");check(!info.newerThan(8),"Installed update is not offered again");check(!info.newerThan(9),"Downgrade is not offered");
  reject(()->new UpdateInfo(0,"1.3.0",url,sha),"Invalid version code");reject(()->new UpdateInfo(8,"invalid",url,sha),"Malformed version name");reject(()->new UpdateInfo(8,"1.3.0",url,"bad"),"Missing checksum");
  reject(()->UpdateInfo.validateUrl(url.replace("https:","http:")),"Plain HTTP rejected");reject(()->UpdateInfo.validateUrl(url.replace("github.com","github.com.evil.test")),"Spoofed host rejected");reject(()->UpdateInfo.validateUrl(url.replace("github.com","github.com@evil.test")),"Credentials in URL rejected");reject(()->UpdateInfo.validateUrl(url.replace("github.com","github.com:443")),"Noncanonical port rejected");reject(()->UpdateInfo.validateUrl(url.replace(UpdateInfo.REPOSITORY,"other/repo")),"Other repository rejected");reject(()->UpdateInfo.validateUrl(url+"#bad"),"Fragment rejected");
  String abcHash="ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
  check(UpdateInfo.matches(new ByteArrayInputStream("abc".getBytes(StandardCharsets.UTF_8)),abcHash),"Known SHA256 accepted");check(!UpdateInfo.matches(new ByteArrayInputStream("tampered".getBytes(StandardCharsets.UTF_8)),abcHash),"Tampered APK rejected");
  System.out.println("All "+count+" checks passed");
 }
}
