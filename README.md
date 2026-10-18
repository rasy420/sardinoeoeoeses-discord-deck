# Sardinöööses Soundboard · SSB

<a href="https://github.com/rasy420/sardinoeoeoeses-soundboard/releases/latest/download/SSB-Setup.exe"><img src="docs/download.svg" alt="KOMPLETTEN INSTALLER HERUNTERLADEN" width="900"></a>

**[⬇ Kompletten Windows-Installer herunterladen](https://github.com/rasy420/sardinoeoeoeses-soundboard/releases/latest/download/SSB-Setup.exe)** · **[Android-App herunterladen](https://github.com/rasy420/sardinoeoeoeses-soundboard/releases/latest/download/SSB-Android.apk)**

Windows 10/11 (64 Bit). Der Installer enthält .NET-Laufzeit, Audio-Bibliotheken, yt-dlp, FFmpeg/FFprobe, Deno und Android-App. Kein SDK erforderlich. Fehlt ein virtuelles Audiogerät, bietet das Setup den Download des originalen VB-CABLE-Installers vom Hersteller an. Windows fragt für Treiber und Firewall nach Administratorrechten; die Treiberinstallation bestätigst du im VB-Audio-Fenster. Ein Neustart kann erforderlich sein.

Neue Funktionen: Gold-Design, Deutsch/Englisch (DE / EN), Einführung beim ersten Öffnen und erneut über ?, Schnittmarker mit Mausbedienung, Vorhören und Speichern. Android enthält eine eigene Kurzanleitung. **Updates suchen** unten links und im Infobereich lädt das neueste stabile GitHub-Release, prüft SHA-256 und öffnet den Installer. Sounds und Einstellungen bleiben erhalten.

**Neu in 1.4.0:** Die Sound-Einstellungen rechts haben einen Stift zum Ändern des Anzeigenamens, ohne die Audiodatei umzubenennen. Der Name bleibt beim Neustart, Umsortieren und Ersetzen der Quelle erhalten und erscheint automatisch auf dem Handy. **Sound ersetzen** enthält genau **Audiodatei auswählen …** und **MyInstants-Link einfügen …**. Der bisherige blaue Link und das Zahnrad oben rechts entfallen. Unter der Wellenform lassen sich **Start** und **Ende** direkt eingeben oder mit den kleinen Pfeilen ändern; **Dauer** zeigt die Länge des Ausschnitts. Die vorhandenen Knöpfe zum Vorhören und Speichern bleiben erhalten. Eigene Namen mit Enter oder beim Verlassen des Feldes speichern, Esc verwirft die Eingabe.

**Neu in 1.3.0:** Oben über den Sounds **Umsortieren** anklicken, einen Sound an die gewünschte Position ziehen und loslassen. Andere Sounds rücken nach; Datei, Pegel und Schnittbereich bleiben zusammen. Während des Ziehens über einer Bank links halten, um die Bank zu wechseln; am oberen/unteren Rand scrollt die Liste. Mit **Fertig** den Modus beenden. Die Handy-App übernimmt die Reihenfolge beim nächsten Abruf automatisch, auch mit einer älteren APK.

Android ab 1.3.0 blendet verfügbare App-Updates oben ein. **Herunterladen** startet den Download, **Installieren** öffnet nach Prüfung von SHA-256, Paketname und Signatur die Android-Installation. Android verlangt dafür einmal die Erlaubnis „Unbekannte Apps installieren“ und bei jedem Update eine Bestätigung. Bestehende ältere APKs müssen einmal manuell aktualisiert werden, um diesen Hinweis zu erhalten.

Die Soundnamen im Bild sind Beispiele. Neue Installationen enthalten **keine vorbelegten Sounds und keine Audiodateien**.

Soundboard für Discord unter Windows – mit Android-App als Fernbedienung im WLAN.

![Screenshot](docs/screenshot.png)

## Funktionen
- 100 Sound-Plätze in 5 Bänken, Pegel pro Sound, „Alle Sounds stoppen“
- Mikrofon + Sounds werden auf ein virtuelles Mikrofon gemischt (z. B. VB-CABLE), das du in Discord als Eingang wählst
- Sounds selbst mithören über Kopfhörer
- Import von YouTube-, YouTube-Shorts- und TikTok-Audio inkl. Zuschneiden
- MyInstants-Suche
- Android-App: verbindet sich automatisch im WLAN (Kopplung per PIN), startet/stoppt Sounds
- Optional: Stream-Controller-/StreamDock-Plugin (`streamdock-plugin/`)

## Ordner
| Ordner | Inhalt |
|---|---|
| `windows/DiscordDeck` | PC-App (C#, .NET 8, WinForms, NAudio, ASP.NET-LAN-Server) |
| `windows/DiscordDeckSetup` | Ein-Datei-Installer (enthält die App als payload.zip) |
| `android` | Android-App (Java, Gradle) |
| `streamdock-plugin` | Plugin für Stream Controller / StreamDock |

## Voraussetzungen für Entwickler (im Installer bereits enthalten)
- Windows 10/11, .NET 8 SDK
- Virtuelles Audiokabel, z. B. [VB-CABLE](https://vb-audio.com/Cable/)
- Für den Import: `yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`, `deno.exe` im Ordner `tools\` neben `DiscordDeck.exe` (nicht im Repo enthalten)
- Android: JDK 17+, Android SDK 35, Gradle 8.13

## Bauen
```powershell
# PC-App
dotnet publish windows\DiscordDeck\DiscordDeck.csproj -c Release -r win-x64 --self-contained true -o dist\windows
# tools\ (yt-dlp, ffmpeg, ffprobe, deno) nach dist\windows\tools kopieren, dann:
New-Item -ItemType Directory -Force windows\DiscordDeck\release | Out-Null
Compress-Archive dist\windows\* windows\DiscordDeck\release\windows-payload.zip -Force
# Installer
dotnet publish windows\DiscordDeckSetup\DiscordDeckSetup.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist\setup
```
```bash
# Android (in android/)
gradle assembleRelease   # danach mit eigenem Keystore signieren
```

## Ports
TCP 47836 (Handy-Verbindung), UDP 47837 (Suche im WLAN), lokal 127.0.0.1:47835 (API). Der Installer legt bei Bedarf Firewall-Regeln an.

## Updates veröffentlichen

Stabiles Release mit Tag `vMAJOR.MINOR.PATCH` erstellen. Installer immer **SSB-Setup.exe**, Android-App **SSB-Android.apk** nennen. Windows-Assembly-Version anpassen. Der Updater verlangt die SHA-256-Prüfsumme, die GitHub für das Release-Asset liefert. Drafts und Vorabversionen werden ignoriert.

Für Android zusätzlich **SSB-Android-update.json** veröffentlichen, z. B. `{"versionCode":8,"versionName":"1.3.0","sha256":"SHA256_DER_SIGNIERTEN_APK"}`. Den Android-versionCode bei jedem Android-Update erhöhen und denselben Signierschlüssel behalten. Bei reinen PC-Updates dürfen APK und Android-Metadaten gleich bleiben: das Handy bietet dann kein unnötiges Update an.

## Prüfen

`--sound-settings-test`, `--reorder-test`, `--update-test`, `--trim-test`, `--tour-test`, `--ui-test`, `--design-test`, `--lan-test`, `--gain-test`, `--import-test`. `DISCORDDECK_HOME` auf einen leeren Testordner setzen. Testfenster starten keine normalen Server; LAN-Prüfungen verwenden TCP 47846 / UDP 47847. JSON-Ergebnisse auf `ok` prüfen. Die Sound-Einstellungsprüfung testet Umbenennen, Ersetzen, Sekundenfelder, Speicherung, Fehlerfälle und kleine Fenster. Der Reorder-Test prüft Mausbedienung, Speichern, Neustart, Fehlerfälle und die Reihenfolge über echte LAN-Abfragen.

Android-Updater-Kern ohne Handy prüfen: `javac -d test-classes app/src/main/java/de/cocoa/discorddeck/UpdateInfo.java tests/UpdateInfoTest.java`, danach `java -cp test-classes de.cocoa.discorddeck.UpdateInfoTest` im Android-Ordner. Der DownloadManager und die Android-Installation müssen zusätzlich auf einem Gerät geprüft werden.
