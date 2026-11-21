using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Collections.Generic;
using SharpHook.Data;
using System;
using System.IO;
using System.Text.Json;

namespace LizardCrossPlatform;

public partial class App : Application
{
    private LizardHookManager? _hookManager;
    private AudioEngine? _audioEngine;
    private MainWindow? _mainWindow;
    private AppSettings _settings = new();
    private readonly HashSet<KeyCode> _pressedKeys = new();
    private readonly object _pressedKeysLock = new();
    private string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LizardKeyboardSounds",
        "settings.json");

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _settings = LoadSettings();
            _audioEngine = new AudioEngine();
            _hookManager = new LizardHookManager();
            _mainWindow = new MainWindow(_settings, _audioEngine);

            _mainWindow.SettingsChanged += settings =>
            {
                _settings = settings;
                SaveSettings(settings);
                ApplyTrayLanguage(settings.AppLanguage);
            };
            _mainWindow.CaptureRequested += () => _hookManager.CaptureNextKey();
            _hookManager.KeyPressed += OnKeyPressed;
            _hookManager.KeyReleased += OnKeyReleased;
            _hookManager.KeyCaptured += key =>
            {
                lock (_pressedKeysLock)
                {
                    _pressedKeys.Add(key);
                }

                Dispatcher.UIThread.Post(async () =>
                {
                    if (_mainWindow is not null)
                    {
                        await _mainWindow.AssignSoundToKeyAsync(key);
                    }
                });
            };
            _hookManager.CaptureCancelled += () => Dispatcher.UIThread.Post(() => _mainWindow?.ShowCaptureCancelled());

            _mainWindow.Closing += (_, e) =>
            {
                if (!_mainWindow.AllowClose)
                {
                    e.Cancel = true;
                    _mainWindow.Hide();
                }
            };

            desktop.MainWindow = _mainWindow;
            ApplyTrayLanguage(_settings.AppLanguage);
            _hookManager.Start();
            _mainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnKeyPressed(KeyCode keyCode)
    {
        var settings = _settings;
        bool isFirstPress;
        lock (_pressedKeysLock)
        {
            isFirstPress = _pressedKeys.Add(keyCode);
        }

        if (settings.OnlyPlayOnceWhileHeld && !isFirstPress)
        {
            return;
        }

        if (!settings.IsEnabled)
        {
            return;
        }

        var keyName = keyCode.ToString();
        if (settings.MutedKeys?.Contains(keyName) == true)
        {
            return;
        }

        var soundPath = settings.KeySounds.TryGetValue(keyName, out var mappedPath)
            ? mappedPath
            : settings.DefaultSoundPath;

        if (!string.IsNullOrWhiteSpace(soundPath))
        {
            _audioEngine?.Play(soundPath, settings.Volume);
        }
    }

    private void OnKeyReleased(KeyCode keyCode)
    {
        lock (_pressedKeysLock)
        {
            _pressedKeys.Remove(keyCode);
        }
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        _hookManager?.Dispose();
        _audioEngine?.Dispose();
        if (_mainWindow is not null)
        {
            _mainWindow.AllowClose = true;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void ApplyTrayLanguage(string language)
    {
        Resources["TraySettingsText"] = UiText.Get(language, "traySettings");
        Resources["TrayExitText"] = UiText.Get(language, "trayExit");
    }

    private AppSettings LoadSettings()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path))
            {
                var legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                path = File.Exists(legacyPath) ? legacyPath : path;
            }

            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));
                if (loaded is not null)
                {
                    loaded.Volume = Math.Clamp(loaded.Volume, 0f, 1f);
                    loaded.KeySounds ??= new();
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }

        var defaults = new AppSettings();
        var legacySound = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "sound.mp3");
        if (File.Exists(legacySound))
        {
            defaults.DefaultSoundPath = legacySound;
        }

        return defaults;
    }

    private void SaveSettings(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
