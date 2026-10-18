## Großes Soundboard-Update 1.4.0

- Ganze Kachel spielt, Zahnrad bearbeitet. Gold markiert tatsächlich laufende Sounds, auch bei paralleler Wiedergabe.
- Schließbare Sound-Seitenleiste; geschlossen erhält das Raster mehr Platz. Umsortieren bleibt erhalten, einschließlich Bankwechsel und automatischer Handy-Synchronisierung.
- Anzeigename per Stift unabhängig vom Dateinamen. Unicode, Enter/Fokuswechsel zum Speichern, Escape zum Verwerfen.
- Präzise Start-/Endfelder und ziehbare Marker. Ausschnitt und Pegel als Entwurf vorhören und gemeinsam speichern. Zurücksetzen lädt die gespeicherten Werte. Speichern/Verwerfen/Abbrechen schützt Änderungen beim Schließen und Wechseln.
- Dunkles Quellenmenü mit Audiodatei/MyInstants, sichere Quellenwechsel und Mülleimer mit Bestätigung. Originaldateien und andere Slots bleiben erhalten.
- Globale Audio-Einstellungen unten rechts, getrennt vom bearbeiteten Sound.
- Automatische PC-Updateprüfung beim Start und alle zehn Minuten. Neue Versionen zeigen einen goldenen pulsierenden Installieren-Knopf; SHA-256 wird vor dem Start des Installers geprüft.
- Richtiger Handy-Umriss statt Platzhalter, zentrierte Knöpfe und „Von YouTube / TikTok importieren“. Instagram ist nicht enthalten.
- Setup enthält .NET, FFmpeg/FFprobe, yt-dlp, Deno und die Android-App; Import-Werkzeuge werden vor dem Austausch der Installation tatsächlich gestartet und geprüft. Fehlendes VB-CABLE wird vom Hersteller geladen, signaturgeprüft und automatisch installiert. Windows-Administrator-/Treiberabfragen und ggf. Neustart bleiben erforderlich.
- Android 1.4.0 (versionCode 9): bestehender Update-Hinweis mit geprüftem Download und Android-Installation, Prüfung alle zehn Minuten bei geöffneter App und beim Wiederöffnen; verbesserte Buttonabstände. Signatur identisch zur Vorversion.

Geprüft: 72 Integrationsfälle mit echter Audioausgabe, Vorschau, paralleler Wiedergabe, Entwürfen, Speicherfehlern, Quellenwechsel, Entfernen, gemeinsamen Quelldateien, stabilen IDs, Neustart und Fenstergrößen. Zusätzlich Umsortieren (28), Sound-Einstellungen (38), Schnitt-/Wiedergabetests (26), Tour (24), LAN, Importkonvertierung, Pegel, PC-Updater (13) und Android-Updatevalidierung (14). Der endgültige Installer wurde separat frisch installiert und als Update getestet; die aus dem Installer entpackte App besteht dieselben 72 Integrationsfälle. Vorhandene Einstellungen bleiben erhalten.

Echte Grenzen: Eine frische VB-CABLE-Treiberinstallation auf einem PC ohne Treiber und der vollständige Android-Installationsablauf am Handy konnten hier nicht beobachtet werden. Die signierte APK ist gebaut; Android verlangt die Installationsbestätigung. Keine Garantie für jeden externen YouTube-/TikTok-Beitrag bei Plattform-Sperren.

SSB-Setup.exe ist der vollständige Windows-Installer. Vorhandene Sounds/Einstellungen bleiben erhalten; neue Installationen enthalten keine Sounds.
