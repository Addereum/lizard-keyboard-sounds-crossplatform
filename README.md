# Lizard Keyboard Sounds

Eine Desktop-App für Windows und Linux, die Tastendrücke systemweit mit Sounds belegt. Die Oberfläche basiert auf Avalonia, die globale Tastenerkennung auf SharpHook und die Audio-Wiedergabe auf ManagedBass.

## Funktionen

- Einen Standardsound für alle Tasten festlegen
- Für einzelne Tasten eigene Sounds aufnehmen und zuordnen
- Einzelne Tasten stummschalten, zum Beispiel linke oder rechte Umschalttaste
- Optional beim Gedrückthalten nur einmal pro Tastendruck abspielen
- Mechvibes-Soundpacks als ZIP importieren; Tastensegmente werden in lokale WAV-Dateien umgewandelt
- Eine komplette Tastatur anklicken: Deutsch QWERTZ oder Englisch QWERTY, inklusive Funktionstasten, Navigation und Nummernfeld
- Die App-Sprache zwischen Deutsch und Englisch umschalten
- Sounds direkt in der App anhören
- Zwei enthaltene, eigens synthetisierte Presets: „Thock“ und „Creamy“, jeweils als Standard oder pro Taste auswählbar
- Gesamtlautstärke einstellen und Tastensounds pausieren
- Einstellungen und importierte Sounds lokal speichern
- Im Tray weiterlaufen lassen; über das Tray-Menü öffnen oder beenden

Unterstützte Sounddateien: MP3, WAV und OGG. Tastenbelegungen speichern nur Tastencodes. Der Inhalt getippter Texte wird weder gespeichert noch übertragen.

## Starten

Voraussetzung: .NET 8 SDK.

```bash
dotnet run
```

Die native BASS-Bibliothek muss neben der App liegen: `bass.dll` unter Windows oder `libbass.so` unter Linux. Die Bibliothek ist nicht im Repository enthalten. Ohne sie öffnet sich die Oberfläche weiterhin, aber Audio kann nicht abgespielt werden.

## Plattformhinweise

- Windows: globaler Tastatur-Hook über SharpHook.
- Linux: SharpHook unterstützt X11 und mit Version 8 auch Wayland. Das Wayland-Backend ist noch experimentell, hat Einschränkungen und kann erhöhte Rechte erfordern. Die Alpha-Releases dienen zum Testen dieses Backends.

## Veröffentlichen

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

Wenn du einen stabilen Versions-Tag im Format `vJJJJ.MM.TT` pushst, baut der Workflow automatisch Pakete für Windows x64 und Linux x64, erstellt ein GitHub Release mit generierten Release-Notes und hängt beide ZIP-Dateien an. Er lädt dafür die jeweilige native BASS-Bibliothek direkt aus dem offiziellen Un4seen-Paket herunter; sie wird nicht im Git-Repository gespeichert.

```bash
git tag v2026.11.22
git push origin v2026.11.22
```

Alpha-Tags folgen dem Format `vJJJJ.MM.TT-alpha.N`, zum Beispiel `v2026.11.21-alpha.1`. Sie erzeugen GitHub-Prereleases und ersetzen nicht das stabile Latest-Release.

```bash
git tag v2026.11.21-alpha.1
git push origin v2026.11.21-alpha.1
```

BASS ist laut Anbieter für nicht kommerzielle Nutzung kostenlos. Wenn du mit der App Geld verdienst (zum Beispiel durch Verkäufe oder Werbung) oder sie kommerziell nutzt, ist eine passende, plattformbezogene Lizenz erforderlich. BASS darf nicht separat weiterverkauft oder unterlizenziert werden. Details stehen auf der [offiziellen BASS-Lizenzseite](https://www.un4seen.com/bass.html#licensing).
