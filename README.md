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

Für Windows werden bei Tag-Releases ein schneller Velopack-Installer (`*-Setup.exe`) und ein gebrandeter, interaktiver Windows-Installer (`*.msi`) veröffentlicht. Der MSI-Assistent zeigt die neue Keycap-Gecko-Grafik, Willkommens-, Lizenz-, Informations- und Abschlussseiten und lässt die Installationsart wählen. Die Installation nur für dein Benutzerkonto benötigt keine Administratorrechte; eine Installation für alle Benutzer benötigt sie. Im Bereich „Updates“ kann zwischen stabilen Releases und Alpha-Vorabversionen gewählt werden; außerdem lässt sich die Update-Prüfung bei der Windows-Anmeldung ein- oder ausschalten. Die Konfiguration liegt in `%APPDATA%\LizardKeyboardSounds\settings.json`. Bei einem gefundenen Update öffnet sich die App und fragt vor Download und Neustart nach. Einstellungen und importierte Sounds bleiben erhalten.

Für die eigenen Bestandteile gilt die [Lizenz mit privater, nicht kommerzieller Nutzung](LICENSE.md); der Installer zeigt sie vor der Installation zur Zustimmung an. Drittanbieter-Komponenten wie BASS bleiben ihren jeweiligen Lizenzen unterworfen.

## Plattformhinweise

- Windows: globaler Tastatur-Hook über SharpHook.
- Linux: SharpHook unterstützt X11 und mit Version 8 auch Wayland. Das Wayland-Backend ist noch experimentell und hat Einschränkungen. Die App zeigt einen verständlichen Status mit Fehlerdetails, wenn der globale Tastatur-Hook nicht startet. Die Alpha-Releases dienen zum Testen dieses Backends.

### Fedora unter Wayland

Das Fedora-RPM richtet beim Installieren automatisch die benötigten Rechte für `/dev/input` und `/dev/uinput` ein. Dafür verwendet es eine eigene Systemgruppe und eine udev-Regel; die App wird mit dieser Gruppe gestartet, nicht als Administrator. Die RPM-Installation benötigt `sudo` beziehungsweise eine Bestätigung durch den Paketmanager. Starte die App anschließend normal aus dem Anwendungsmenü. Du musst sie **nicht** mit `sudo` starten.

```bash
sudo dnf install ./LizardKeyboardSounds-fedora-x86_64.rpm
```

Diese Berechtigung ist nötig, damit die App globale Tastendrücke hören und Tasteneingaben an das System zurückreichen kann. Das betrifft Tastencodes; geschriebener Text wird von der App nicht gespeichert oder übertragen. Das RPM verwendet dafür eine eigene Gruppe und fügt keine zusätzliche `uaccess`-Regel hinzu, die allen Programmen deiner Sitzung Zugriff geben würde. Bei der Deinstallation entfernt das RPM die udev-Regel.

Die portable Linux-ZIP und manuelle Installationen richten keine Systemrechte ein. Für X11 mit dem XRecord-Backend sind diese zusätzlichen Rechte nicht erforderlich. Unter Wayland bleibt SharpHooks Unterstützung experimentell und hängt außerdem von der Fedora- und Desktop-Konfiguration ab.

## Veröffentlichen

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

Wenn du einen stabilen Versions-Tag im Format `vJJJJ.MM.TT` pushst, baut der Workflow automatisch Pakete für Windows x64 und Linux x64, erstellt ein GitHub Release mit generierten Release-Notes und hängt die Windows- und Linux-ZIP sowie ein Fedora-RPM an. Er lädt dafür die jeweilige native BASS-Bibliothek direkt aus dem offiziellen Un4seen-Paket herunter; sie wird nicht im Git-Repository gespeichert.

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
