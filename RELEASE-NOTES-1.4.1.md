## Reparatur des Updates aus der laufenden App

Beim Start über den Update-Knopf erbte das Setup den Programmordner als Arbeitsverzeichnis. Dadurch blockierte der Installer selbst das Umbenennen dieses Ordners und brach bei 86 % mit einer Dateisperre ab.

1.4.1 wechselt vor der Installation in den temporären Ordner. Zusätzlich startet der Updater künftige Installer ausdrücklich im Downloadordner. Setup-Fehler werden mit vollständigen Details in setup-error.log protokolliert.

Geprüft: Fehler mit 1.4.0 und Arbeitsverzeichnis App reproduziert; identischer Start mit 1.4.1 erfolgreich. Reparatur auf dem betroffenen PC erfolgreich, Einstellungen bytegleich erhalten. Die 72 Integrationsfälle der App bestehen weiterhin. Android bleibt unverändert bei 1.4.0 (versionCode 9).

Alle Funktionen aus 1.4.0 bleiben erhalten. Wer bereits das fehlgeschlagene 1.4.0-Setup geöffnet hat, schließt es zuerst und startet den aktuellen Installer.
