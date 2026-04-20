# 🦎 Lizard Keyboard Sounds (Cross-Platform)

Die ultimative Mechanical Keyboard Sound App für **Windows** und **Linux**. Geschrieben in C# mit Avalonia UI.

![Lizard Logo](icon.png)

## ✨ Features
- **Cross-Platform**: Läuft nativ auf Windows und Ubuntu/Linux.
- **System Tray**: Läuft dezent im Hintergrund.
- **GUI Settings**: Grafische Lautstärkeregelung direkt in der App.
- **SharpHook**: Hochperformantes, globales Abfangen von Tastenanschlägen.
- **Persistent**: Speichert deine Einstellungen automatisch in `settings.json`.

---

## 🚀 Installation & Setup

### 1. Voraussetzungen
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- **Linux Nutzer**: Ein X11-basiertes Desktop-Environment (siehe Troubleshooting).

### 2. Sound-Datei hinzufügen
Die App benötigt eine Datei namens `sound.mp3`.
- Erstelle einen Ordner `assets` im Projektverzeichnis.
- Speichere deinen Sound als **`sound.mp3`** darin ab.
- **Empfehlung**: [Lizzard Effect auf MyInstants](https://www.myinstants.com/de/instant/lizzard-1-94606/)

### 3. Starten
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
Die fertige Datei findest du dann unter `bin/Release/net8.0/linux-x64/publish/`.

---

## 🛠️ Troubleshooting

### 🔇 Kein Ton unter Linux?
Die Audio-Engine (`NetCoreAudio`) nutzt System-Player. Falls kein Ton kommt, installiere einen MP3-Player:
```bash
sudo apt update
sudo apt install mpg123
```
Alternativ wird auch `aplay` oder `mplayer` unterstützt.

### ⌨️ Tasten werden nicht erkannt (Linux)?
Unter Linux gibt es zwei große Grafik-Systeme: **Wayland** und **X11 (Xorg)**.
- **Problem**: Wayland verbietet aus Sicherheitsgründen das Abfangen globaler Tastenanschläge.
- **Lösung**: Melde dich beim Ubuntu-Login ab. Klicke auf das Zahnrad unten rechts und wähle **"Ubuntu on Xorg"** aus. Damit funktionieren Hooks wieder einwandfrei.

### 🛡️ Antivirus-Warnung (Windows)
Da die App Tastenanschläge abfängt (Hooks), stufen manche Virenscanner sie als "Keylogger" ein. Das ist bei dieser Art von Software technisch bedingt normal. Du kannst den Ordner als Ausnahme hinzufügen.

---
*Viel Spaß beim Tippen! 🦎*
