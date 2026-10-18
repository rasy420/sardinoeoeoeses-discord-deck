package de.cocoa.discorddeck;

import java.util.*;

/** UI state from confirmed PC replies. All network requests are serialized by MainActivity. */
final class TilePlaybackState {
 final String[] ids=new String[100];
 final boolean[] assigned=new boolean[100];
 final Set<String> playing=new HashSet<>(),pending=new HashSet<>();
 boolean connected,supported;
 boolean begin(int slot){String id=ids[slot];return connected&&supported&&assigned[slot]&&id!=null&&!id.isEmpty()&&pending.add(id);}
 boolean playing(int slot){return ids[slot]!=null&&playing.contains(ids[slot]);}
 boolean busy(int slot){return ids[slot]!=null&&pending.contains(ids[slot]);}
 void finish(String id,Boolean isPlaying){pending.remove(id);if(isPlaying!=null){if(isPlaying)playing.add(id);else playing.remove(id);}}
 void snapshot(String[] nextIds,boolean[] nextAssigned,boolean[] nextPlaying,boolean hasStop){
  Set<String> active=new HashSet<>();for(int i=0;i<100;i++){ids[i]=nextIds[i];assigned[i]=nextAssigned[i];if(nextPlaying[i]&&nextIds[i]!=null)active.add(nextIds[i]);}
  playing.clear();playing.addAll(active);connected=true;supported=hasStop;
 }
 void disconnect(){connected=false;pending.clear();}
 void reset(){connected=false;supported=false;Arrays.fill(ids,null);Arrays.fill(assigned,false);playing.clear();pending.clear();}
}
