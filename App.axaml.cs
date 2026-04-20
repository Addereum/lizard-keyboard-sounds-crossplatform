using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Text.Json;

namespace LizardCrossPlatform
{
    public partial class App : Application
    {
        private LizardHookManager? _hookManager;
        private AudioEngine? _audioEngine;
        private SettingsWindow? _settingsWindow;
        private string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // We don't want a main window to show up on start
                desktop.MainWindow = null;

                // Load Settings
                var settings = LoadSettings();

                // Initialize Engine & Hook
                var soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets/sound.mp3");
                if (!File.Exists(soundPath))
                    soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../assets/sound.mp3");

                _audioEngine = new AudioEngine(soundPath) { Volume = settings.Volume };
                
                _hookManager = new LizardHookManager();
                _hookManager.KeyPressed += () => _audioEngine.Play();
                _hookManager.Start();

                // Setup Settings Window
                _settingsWindow = new SettingsWindow();
                _settingsWindow.SetVolume(settings.Volume);
                _settingsWindow.VolumeChanged += (s, v) => {
                    if (_audioEngine != null) _audioEngine.Volume = v;
                    SaveSettings(new AppSettings { Volume = v });
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnSettingsClick(object? sender, EventArgs e)
        {
            _settingsWindow?.Show();
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            _hookManager?.Dispose();
            _audioEngine?.Dispose();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private AppSettings LoadSettings()
        {
            try {
                if (File.Exists(_settingsPath)) {
                    string json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            } catch { }
            return new AppSettings();
        }

        private void SaveSettings(AppSettings settings)
        {
            try {
                string json = JsonSerializer.Serialize(settings);
                File.WriteAllText(_settingsPath, json);
            } catch { }
        }
    }

    public class AppSettings {
        public float Volume { get; set; } = 1.0f;
    }
}