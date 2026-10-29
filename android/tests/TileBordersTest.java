package de.cocoa.discorddeck;
public class TileBordersTest {
 static int n;static void check(boolean c){if(!c)throw new AssertionError("Check "+n);n++;}
 public static void main(String[] args){
  check(TileBorders.PALETTE.length==10&&TileBorders.NAMES_DE.length==10&&TileBorders.NAMES_EN.length==10);
  int red=TileBorders.PALETTE[1],orange=TileBorders.PALETTE[2],green=TileBorders.PALETTE[4],purple=TileBorders.PALETTE[7];
  check(TileBorders.toggle(0,red)==red);
  check(TileBorders.toggle(red,red)==0);
  check(TileBorders.toggle(red,orange)==orange);
  check(TileBorders.toggle(red,1234)==red);
  check(TileBorders.strokeDp(0)==1&&TileBorders.strokeDp(red)==3);
  check(!TileBorders.known(0)&&TileBorders.known(red));
  check(TileBorders.light(0xFFF4F7FA)&&!TileBorders.light(red));
  int[] slots=new int[100];
  TileBorders.load(red+",0,nope,"+green,slots);
  check(slots[0]==red&&slots[1]==0&&slots[2]==0&&slots[3]==green&&slots[4]==0);
  slots[9]=purple;int[] again=new int[100];TileBorders.load(TileBorders.store(slots),again);
  check(again[0]==red&&again[9]==purple&&again[50]==0&&again[99]==0);
  check("Rot".equals(TileBorders.name(red,false))&&"Red".equals(TileBorders.name(red,true))&&TileBorders.name(0,false).isEmpty());
  TileBorders.load(null,slots);check(slots[0]==red);
  System.out.println("PASS "+n+" border checks");
 }
}
