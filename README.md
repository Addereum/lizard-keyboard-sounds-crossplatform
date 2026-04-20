# 🦎 Lizard Keyboard Sounds (Cross-Platform)

Die ultimative Mechanical Keyboard Sound App für **Windows** und **Linux**. Geschrieben in C# mit Avalonia UI. 
Diese Version unterstützt **echte Polyphonie** (überlappende Sounds) durch die BASS Audio-Engine.

![Lizard Logo](icon.png)

## ✨ Features
- **System Tray App**: Läuft dezent im Hintergrund neben der Uhr.
- **Echte Polyphonie**: Dank ManagedBass überlappen sich die Töne ohne Verzögerung.
- **Globaler Hook**: Reagiert auf Tasteneingaben in jeder Anwendung (X11 unter Linux).
- **GUI Settings**: Grafische Lautstärkeregelung direkt in der App.
- **Persistenz**: Speichert deine bevorzugte Lautstärke automatisch.

---

## 🚀 Installation & Setup

### 1. Voraussetzungen
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- **Linux Nutzer**: Ein X11-basiertes Desktop-Environment (z.B. "Ubuntu on Xorg" beim Login auswählen).

### 2. Audio-Engine (WICHTIG!)
Die App nutzt **ManagedBass**. Du musst die nativen Treiber einmalig herunterladen und in den Projektordner legen:
1. Gehe zu **[un4seen.com](http://www.un4seen.com/bass.html)**.
2. Lade das Windows-Paket und/oder Linux-Paket herunter.
3. Kopiere die **x64 Version** der Library in diesen Ordner (`lizard_crossplatform`):
   - **Windows**: `bass.dll`
   - **Linux**: `libbass.so`

### 3. Sound-Datei hinzufügen
- Erstelle einen Ordner `assets` im Projektverzeichnis.
- Speichere deinen Sound als **`sound.mp3`** darin ab.
- **Empfehlung**: [Lizzard Effect auf MyInstants](https://www.myinstants.com/de/instant/lizzard-1-94606/)

### 4. Starten
```bash
cd lizard_crossplatform
dotnet run
```

---

## 📦 Building (Kompilieren)

### Für Windows (Single Executable)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Für Linux (Ubuntu)
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```
Die native `bass.dll` / `libbass.so` muss sich im selben Ordner wie die fertige `.exe` befinden.

---

## 🛠️ Troubleshooting

### 🔇 Startet nicht / Audio-Fehler?
Stelle sicher, dass die `bass.dll` (Windows) oder `libbass.so` (Linux) im Projektordner liegt. Ohne diese Dateien kann die App den Audio-Mixer nicht initialisieren.

### ⌨️ Tasten werden nicht erkannt (Linux)?
Melde dich beim Ubuntu-Login ab. Klicke auf das Zahnrad unten rechts und wähle **"Ubuntu on Xorg"** aus. Unter Wayland sind globale Hooks standardmäßig gesperrt.

---
*Viel Spaß beim "lizard" tippen! 🦎🎶*
