package de.cocoa.discorddeck;

/** Tile colors. 0 means the default gray rim. With a PC from 1.4.10 the colors are synced with the PC. */
final class TileBorders {
 static final int[] PALETTE={0xFFE8C87A,0xFFF0605A,0xFFFF9F43,0xFFF6E05E,0xFF3DDC97,0xFF2EC4B6,0xFF5B8DEF,0xFFB07CFF,0xFFFF6B9A,0xFFF4F7FA};
 static final String[] NAMES_DE={"Gold","Rot","Orange","Gelb","Grün","Türkis","Blau","Lila","Pink","Weiß"};
 static final String[] NAMES_EN={"Gold","Red","Orange","Yellow","Green","Teal","Blue","Purple","Pink","White"};
 static boolean known(int color){for(int c:PALETTE)if(c==color)return true;return false;}
 static boolean light(int color){return ((color>>16)&255)+((color>>8)&255)+(color&255)>680;}
 /** Schrift auf vollflächiger Farbe: dunkel, sobald sie nach WCAG stärker absticht als weiß. */
 static final int DARK_TEXT=0xFF16120C;
 static double lin(int v){double c=v/255.0;return c<=.03928?c/12.92:Math.pow((c+.055)/1.055,2.4);}
 static double luminance(int color){return .2126*lin((color>>16)&255)+.7152*lin((color>>8)&255)+.0722*lin(color&255);}
 static boolean darkText(int color){double l=luminance(color);return (l+.05)/.05>=1.05/(l+.05);}
 static int textOn(int color){return darkText(color)?DARK_TEXT:0xFFFFFFFF;}
 static double contrast(int a,int b){double x=luminance(a),y=luminance(b);return (Math.max(x,y)+.05)/(Math.min(x,y)+.05);}
 static int toggle(int current,int selected){if(!known(selected))return current;return current==selected?0:selected;}
 /** 0 = keine Farbe, 1 bis 10 = Platz in der Palette; so schickt das Handy Farben an den PC. */
 static int paletteNumber(int color){for(int i=0;i<PALETTE.length;i++)if(PALETTE[i]==color)return i+1;return 0;}
 static int strokeDp(int border){return border==0?1:3;}
 static String name(int color,boolean en){for(int i=0;i<PALETTE.length;i++)if(PALETTE[i]==color)return en?NAMES_EN[i]:NAMES_DE[i];return "";}
 static void load(String raw,int[] dest){if(raw==null||raw.isEmpty())return;String[] parts=raw.split(",",-1);for(int i=0;i<dest.length&&i<parts.length;i++){try{int c=Integer.parseInt(parts[i].trim());dest[i]=known(c)?c:0;}catch(RuntimeException e){dest[i]=0;}}}
 static String store(int[] colors){StringBuilder sb=new StringBuilder();for(int i=0;i<colors.length;i++){if(i>0)sb.append(',');sb.append(colors[i]);}return sb.toString();}
}
