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
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace LizardCrossPlatform;

public partial class App : Application
{
    private LizardHookManager? _hookManager;
    private AudioEngine? _audioEngine;
    private MainWindow? _mainWindow;
    private UpdateManager? _updateManager;
    private bool _updateCheckInProgress;
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
            if (OperatingSystem.IsWindows())
            {
                _updateManager = CreateUpdateManager(_settings.UpdateChannel);
            }
            _mainWindow = new MainWindow(_settings, _audioEngine);

            _mainWindow.SettingsChanged += settings =>
            {
                var previousSettings = _settings;
                _settings = settings;
                SaveSettings(settings);
                ApplyTrayLanguage(settings.AppLanguage);
                if (_updateManager?.IsInstalled == true)
                {
                    if (previousSettings.UpdateChannel != settings.UpdateChannel)
                    {
                        _updateManager = CreateUpdateManager(settings.UpdateChannel);
                    }

                    if (previousSettings.CheckUpdatesAtStartup != settings.CheckUpdatesAtStartup)
                    {
                        WindowsUpdaterStartup.SetEnabled(settings.CheckUpdatesAtStartup);
                    }
                }
            };
            _mainWindow.CaptureRequested += () => _hookManager.CaptureNextKey();
            _hookManager.KeyPressed += OnKeyPressed;
            _hookManager.KeyReleased += OnKeyReleased;
            _hookManager.HookFailed += ex => Dispatcher.UIThread.Post(() => _mainWindow?.ShowHookError(ex));
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

            if (_updateManager?.IsInstalled == true)
            {
                _mainWindow.SetUpdaterAvailable();
                _mainWindow.UpdateCheckRequested += CheckForUpdatesAsync;
                _mainWindow.Opened += (_, _) => _ = CheckForUpdatesAsync(showNoUpdates: false);
                WindowsUpdaterStartup.SetEnabled(_settings.CheckUpdatesAtStartup);
            }

            desktop.MainWindow = _mainWindow;
            ApplyTrayLanguage(_settings.AppLanguage);
            _hookManager.Start();
            _mainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static UpdateManager CreateUpdateManager(string updateChannel)
    {
        var channel = updateChannel == "prerelease" ? "win-alpha" : "win";
        var source = new GithubSource("https://github.com/Addereum/lizard-keyboard-sounds-crossplatform", null, prerelease: true);
        return new UpdateManager(source, new UpdateOptions
        {
            ExplicitChannel = channel,
            AllowVersionDowngrade = true
        });
    }

    private async Task CheckForUpdatesAsync(bool showNoUpdates)
    {
        if (_updateCheckInProgress || _updateManager?.IsInstalled != true || _mainWindow is null)
        {
            return;
        }

        _updateCheckInProgress = true;
        try
        {
            if (showNoUpdates)
            {
                _mainWindow.ShowUpdateMessage(UiText.Get(_settings.AppLanguage, "updatesChecking"));
            }

            var update = await _updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                if (showNoUpdates)
                {
                    _mainWindow.ShowUpdateMessage(UiText.Get(_settings.AppLanguage, "upToDate"));
                }

                return;
            }

            var targetVersion = update.TargetFullRelease.Version.ToString();
            if (!await _mainWindow.ConfirmUpdateAsync(targetVersion))
            {
                return;
            }

            _mainWindow.ShowUpdateMessage(UiText.Get(_settings.AppLanguage, "updateDownloading"));
            await _updateManager.DownloadUpdatesAsync(update);
            _updateManager.ApplyUpdatesAndRestart(update);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            if (showNoUpdates)
            {
                _mainWindow.ShowUpdateMessage(string.Format(
                    UiText.Get(_settings.AppLanguage, "updateFailed"), ex.Message), isWarning: true);
            }
        }
        finally
        {
            _updateCheckInProgress = false;
        }
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
