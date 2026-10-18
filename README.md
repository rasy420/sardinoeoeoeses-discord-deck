# Sardinöööses Soundboard · SSB

<a href="https://github.com/rasy420/sardinoeoeoeses-soundboard/releases/latest/download/SSB-Setup.exe"><img src="docs/download.svg" alt="KOMPLETTEN INSTALLER HERUNTERLADEN" width="900"></a>

**[⬇ Kompletten Windows-Installer herunterladen](https://github.com/rasy420/sardinoeoeoeses-soundboard/releases/latest/download/SSB-Setup.exe)** · **[Android-App herunterladen](https://github.com/rasy420/sardinoeoeoeses-soundboard/releases/latest/download/SSB-Android.apk)**

Windows 10/11 (64 Bit). Der Installer enthält .NET-Laufzeit, Audio-Bibliotheken, yt-dlp, FFmpeg/FFprobe, Deno und Android-App. Kein SDK erforderlich. Fehlt ein virtuelles Audiogerät, bietet das Setup den Download des originalen VB-CABLE-Installers vom Hersteller an. Windows fragt für Treiber und Firewall nach Administratorrechten; die Treiberinstallation bestätigst du im VB-Audio-Fenster. Ein Neustart kann erforderlich sein.

Neue Funktionen: Gold-Design, Deutsch/Englisch (DE / EN), Einführung beim ersten Öffnen und erneut über ?, Schnittmarker mit Mausbedienung, Vorhören und Speichern. Android enthält eine eigene Kurzanleitung. **Updates suchen** unten links und im Infobereich lädt das neueste stabile GitHub-Release, prüft SHA-256 und öffnet den Installer. Sounds und Einstellungen bleiben erhalten.

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

## Prüfen

`--update-test`, `--trim-test`, `--tour-test`, `--ui-test`, `--design-test`, `--lan-test`, `--gain-test`, `--import-test`. `DISCORDDECK_HOME` auf einen leeren Testordner setzen und die normale App vorher beenden, da Tests dieselben Ports nutzen. JSON-Ergebnisse auf `ok` prüfen.
