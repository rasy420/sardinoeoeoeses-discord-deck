package de.cocoa.discorddeck;

import java.net.URI;
import java.io.InputStream;
import java.security.MessageDigest;

final class UpdateInfo {
 static final String REPOSITORY="rasy420/sardinoeoeoeses-soundboard";
 final long versionCode;final String versionName,url,sha256;
 UpdateInfo(long code,String name,String url,String sha){
  if(code<=0||name==null||!name.matches("[0-9]+\\.[0-9]+\\.[0-9]+"))throw new IllegalArgumentException("Invalid Android version");
  validateUrl(url);if(sha==null||!sha.matches("(?i)[0-9a-f]{64}"))throw new IllegalArgumentException("Missing SHA-256");
  versionCode=code;versionName=name;this.url=url;sha256=sha;
 }
 boolean newerThan(long installed){return versionCode>installed;}
 static boolean matches(InputStream in,String expected)throws Exception{
  MessageDigest hash=MessageDigest.getInstance("SHA-256");byte[] b=new byte[16384];int n;while((n=in.read(b))!=-1)hash.update(b,0,n);
  StringBuilder hex=new StringBuilder();for(byte value:hash.digest())hex.append(String.format(java.util.Locale.ROOT,"%02x",value&255));return hex.toString().equalsIgnoreCase(expected);
 }
 static void validateUrl(String url){
  URI u=URI.create(url);
  if(!"https".equals(u.getScheme())||!"github.com".equals(u.getHost())||u.getUserInfo()!=null||u.getPort()!=-1||!u.getPath().startsWith("/"+REPOSITORY+"/releases/download/")||u.getFragment()!=null)throw new IllegalArgumentException("Invalid update destination");
 }
}
