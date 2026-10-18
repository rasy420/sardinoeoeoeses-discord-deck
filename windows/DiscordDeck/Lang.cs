namespace DiscordDeck;
// Sprache der Oberfläche: Deutsch (Standard) oder Englisch, umschaltbar in der App.
public static class Lang {
 public static bool En {get;set;}
 public static string T(string de,string en)=>En?en:de;
}
