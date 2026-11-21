using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LizardCrossPlatform;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly AudioEngine _audioEngine;
    private string? _selectedKey;
    private readonly HashSet<string> _selectedKeys = new(StringComparer.Ordinal);
    private bool _controlHeld;
    private bool _needsInitialSettingsSave;
    private Exception? _hookError;

    public event Action<AppSettings>? SettingsChanged;
    public event Action? CaptureRequested;
    public event Func<bool, Task>? UpdateCheckRequested;

    public bool AllowClose { get; set; }

    public MainWindow() : this(new AppSettings(), new AudioEngine()) { }

    public MainWindow(AppSettings settings, AudioEngine audioEngine)
    {
        InitializeComponent();
        _settings = settings.Copy();
        _audioEngine = audioEngine;
        if (string.IsNullOrWhiteSpace(_settings.DefaultSoundPath))
        {
            _settings.DefaultSoundPath = GetPresetPath("Thock");
            _needsInitialSettingsSave = _settings.DefaultSoundPath is not null;
        }

        LanguageSelector.SelectedIndex = _settings.AppLanguage == "en" ? 1 : 0;
        KeyboardLayoutSelector.SelectedIndex = _settings.KeyboardLayout == "en-US" ? 1 : 0;
        VolumeSlider.Value = _settings.Volume * 100;
        OnlyPlayOnceWhileHeldCheckBox.IsChecked = _settings.OnlyPlayOnceWhileHeld;
        KeyDown += HandleSelectionShortcut;
        KeyUp += (_, e) =>
        {
            if (IsControlKey(e.Key))
                _controlHeld = false;
        };

        LanguageSelector.SelectionChanged += (_, _) =>
        {
            if (LanguageSelector.SelectedItem is ComboBoxItem item && item.Tag is string language)
            {
                _settings.AppLanguage = language;
                ApplyLocalization();
                RefreshView();
                NotifySettingsChanged();
            }
        };
        KeyboardLayoutSelector.SelectionChanged += (_, _) =>
        {
            if (KeyboardLayoutSelector.SelectedItem is ComboBoxItem item && item.Tag is string layout)
            {
                _settings.KeyboardLayout = layout;
                RefreshView();
                NotifySettingsChanged();
            }
        };
        VolumeSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == RangeBase.ValueProperty)
            {
                _settings.Volume = (float)(VolumeSlider.Value / 100d);
                VolumeValue.Text = $"{(int)VolumeSlider.Value}%";
                NotifySettingsChanged();
            }
        };
        OnlyPlayOnceWhileHeldCheckBox.Click += (_, _) =>
        {
            _settings.OnlyPlayOnceWhileHeld = OnlyPlayOnceWhileHeldCheckBox.IsChecked == true;
            NotifySettingsChanged();
        };

        ChooseDefaultButton.Click += async (_, _) => await ChooseDefaultSoundAsync();
        ThockDefaultButton.Click += (_, _) => SetDefaultPreset("Thock");
        CreamyDefaultButton.Click += (_, _) => SetDefaultPreset("Creamy");
        DefaultPreviewButton.Click += (_, _) => PreviewSound(_settings.DefaultSoundPath);
        CaptureButton.Click += (_, _) => BeginCapture();
        ImportPackButton.Click += async (_, _) => await ImportSoundPackAsync();
        PauseButton.Click += (_, _) => ToggleEnabled();
        CheckUpdatesButton.Click += async (_, _) => await ShowUpdateSettingsAsync();
        SelectedChooseButton.Click += async (_, _) => await ChooseSoundForSelectedKeyAsync();
        ThockKeyButton.Click += (_, _) => SetSelectedKeyPreset("Thock");
        CreamyKeyButton.Click += (_, _) => SetSelectedKeyPreset("Creamy");
        SelectedPreviewButton.Click += (_, _) => PreviewSelectedKey();
        SelectedRemoveButton.Click += (_, _) => RemoveSelectedKey();
        SelectedMuteCheckBox.Click += (_, _) => ToggleSelectedMute();

        ApplyLocalization();
        RefreshView();
        Opened += (_, _) =>
        {
            if (_needsInitialSettingsSave)
            {
                _needsInitialSettingsSave = false;
                NotifySettingsChanged();
            }
        };
        if (!string.IsNullOrWhiteSpace(_audioEngine.Error))
        {
            SetFeedback(LocalizedAudioError(), isWarning: true);
        }
    }

    public async Task AssignSoundToKeyAsync(KeyCode keyCode)
    {
        CaptureHint.IsVisible = false;
        CaptureButton.Content = $"⌁  {T("capture")}";
        _selectedKey = keyCode.ToString();
        _selectedKeys.Clear();
        _selectedKeys.Add(_selectedKey);

        var importedSound = await ImportSoundAsync();
        if (importedSound is null)
        {
            SetFeedback(string.Format(T("captureAborted"), FormatKey(_selectedKey)));
            RefreshView();
            return;
        }

        _settings.KeySounds[_selectedKey] = importedSound;
        NotifySettingsChanged();
        RefreshView();
        SetFeedback(string.Format(T("assigned"), FormatKey(_selectedKey), Path.GetFileName(importedSound)));
    }

    public void ShowCaptureCancelled()
    {
        CaptureHint.IsVisible = false;
        CaptureButton.Content = $"⌁  {T("capture")}";
        SetFeedback(T("captureCancelled"));
    }

    private string T(string key) => UiText.Get(_settings.AppLanguage, key);

    private void ApplyLocalization()
    {
        BrandText.Text = T("brand");
        StudioText.Text = T("studio");
        HeroEyebrow.Text = T("heroEyebrow");
        HeroTitle.Text = T("heroTitle");
        HeroSubtitle.Text = T("heroSubtitle");
        LiveMixTitle.Text = T("liveMix");
        LiveMixCaption.Text = T("liveFlow");
        DefaultSectionTitle.Text = T("defaultSection");
        DefaultDescription.Text = T("defaultDescription");
        VolumeSectionTitle.Text = T("volume");
        VolumeDescription.Text = T("volumeDescription");
        OnlyPlayOnceWhileHeldCheckBox.Content = T("onlyOnceWhileHeld");
        ChooseDefaultButton.Content = T("chooseSound");
        PresetLabel.Text = T("presets");
        KeyPresetLabel.Text = T("keyPresets");
        MutedKeysTitle.Text = T("mutedKeysTitle");
        SelectedChooseButton.Content = T("chooseSound");
        SelectedRemoveButton.Content = T("remove");
        AssignmentsSectionTitle.Text = T("keyboardSection");
        AssignmentsSectionSubtitle.Text = T("keyboardSubtitle");
        LayoutLabel.Text = T("keyboard");
        CaptureButton.Content = $"⌁  {T("capture")}";
        CaptureHint.Text = T("captureHint");
        MappedSoundsLabel.Text = T("mappedSounds");
        ImportPackButton.Content = T("importPack");
        EmptyTitle.Text = T("emptyTitle");
        EmptyDescription.Text = T("emptyDescription");
        PrivacyText.Text = T("privacy");
        CheckUpdatesButton.Content = T("updateSettings");
    }

    private void BeginCapture()
    {
        CaptureHint.IsVisible = true;
        CaptureButton.Content = T("captureRunning");
        SetFeedback(T("capturedPrompt"));
        CaptureRequested?.Invoke();
    }

    private async Task ImportSoundPackAsync()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = T("packPickTitle"),
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Mechvibes Soundpack") { Patterns = new[] { "*.zip" } }
                }
            });

            var source = files.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
            {
                return;
            }

            var pack = MechvibesPackImporter.Inspect(source);
            SetFeedback(string.Format(T("packImporting"), pack.Name));
            var importedSounds = await Task.Run(() => MechvibesPackImporter.Import(source, pack, _audioEngine));
            var importedCount = 0;
            var keptCount = 0;
            foreach (var (key, path) in importedSounds)
            {
                if (_settings.KeySounds.ContainsKey(key))
                {
                    keptCount++;
                    continue;
                }

                _settings.KeySounds[key] = path;
                importedCount++;
            }

            if (importedCount > 0)
            {
                NotifySettingsChanged();
            }

            RefreshView();
            SetFeedback(string.Format(T("packImported"), pack.Name, importedCount, keptCount));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            SetFeedback(string.Format(T("packImportFailed"), ex.Message), isWarning: true);
        }
    }

    private void ToggleEnabled()
    {
        _settings.IsEnabled = !_settings.IsEnabled;
        NotifySettingsChanged();
        RefreshView();
        SetFeedback(T(_settings.IsEnabled ? "enabled" : "disabled"));
    }

    private void SetDefaultPreset(string preset)
    {
        var path = GetPresetPath(preset);
        if (path is null)
        {
            SetFeedback(T("presetUnavailable"), isWarning: true);
            return;
        }

        _settings.DefaultSoundPath = path;
        NotifySettingsChanged();
        RefreshView();
        SetFeedback(string.Format(T("presetSelected"), preset));
    }

    private void SetSelectedKeyPreset(string preset)
    {
        if (_selectedKeys.Count == 0)
        {
            SetFeedback(T("selectKey"), isWarning: true);
            return;
        }

        var path = GetPresetPath(preset);
        if (path is null)
        {
            SetFeedback(T("presetUnavailable"), isWarning: true);
            return;
        }

        foreach (var key in _selectedKeys)
            _settings.KeySounds[key] = path;
        NotifySettingsChanged();
        RefreshView();
        SetFeedback(string.Format(T(_selectedKeys.Count == 1 ? "updated" : "updatedMany"), _selectedKeys.Count == 1 ? FormatKey(_selectedKey!) : _selectedKeys.Count));
    }

    private static string? GetPresetPath(string preset)
    {
        if (preset is not ("Thock" or "Creamy"))
        {
            return null;
        }

        var source = Path.Combine(AppContext.BaseDirectory, "Presets", $"{preset}.wav");
        if (!File.Exists(source))
        {
            return null;
        }

        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LizardKeyboardSounds", "Sounds");
        Directory.CreateDirectory(folder);
        var destination = Path.Combine(folder, $"{preset}.wav");
        if (!File.Exists(destination))
        {
            File.Copy(source, destination);
        }

        return destination;
    }

    private async Task ChooseDefaultSoundAsync()
    {
        var sound = await ImportSoundAsync();
        if (sound is null)
        {
            return;
        }

        _settings.DefaultSoundPath = sound;
        NotifySettingsChanged();
        RefreshView();
        SetFeedback(T("soundSaved"));
    }

    private async Task ChooseSoundForSelectedKeyAsync()
    {
        if (_selectedKeys.Count == 0)
        {
            SetFeedback(T("selectKey"), isWarning: true);
            return;
        }

        var sound = await ImportSoundAsync();
        if (sound is null)
        {
            return;
        }

        foreach (var key in _selectedKeys)
            _settings.KeySounds[key] = sound;
        NotifySettingsChanged();
        RefreshView();
        SetFeedback(string.Format(T(_selectedKeys.Count == 1 ? "updated" : "updatedMany"), _selectedKeys.Count == 1 ? FormatKey(_selectedKey!) : _selectedKeys.Count));
    }

    private void PreviewSelectedKey()
    {
        if (string.IsNullOrWhiteSpace(_selectedKey))
        {
            return;
        }

        var path = _settings.KeySounds.TryGetValue(_selectedKey, out var ownSound)
            ? ownSound
            : _settings.DefaultSoundPath;
        PreviewSound(path);
    }

    private void RemoveSelectedKey()
    {
        var removed = _selectedKeys.Where(key => _settings.KeySounds.Remove(key)).ToArray();
        if (removed.Length == 0)
        {
            return;
        }

        NotifySettingsChanged();
        RefreshView();
        SetFeedback(string.Format(T(removed.Length == 1 ? "removed" : "removedMany"), removed.Length == 1 ? FormatKey(removed[0]) : removed.Length));
    }

    private void SelectKey(string keyName)
    {
        _selectedKey = keyName;
        _selectedKeys.Clear();
        _selectedKeys.Add(keyName);
        RefreshView();
    }

    private void ToggleKeySelection(string keyName)
    {
        if (_selectedKeys.Contains(keyName))
            _selectedKeys.Remove(keyName);
        else
            _selectedKeys.Add(keyName);

        _selectedKey = _selectedKeys.LastOrDefault();
        RefreshView();
    }

    private void SelectAllKeys()
    {
        _selectedKeys.Clear();
        foreach (var key in KeyboardRows().Concat(NavigationRows()).Concat(NumpadRows()).SelectMany(row => row).Where(key => !string.IsNullOrEmpty(key.Code)))
            _selectedKeys.Add(key.Code);
        _selectedKey = _selectedKeys.LastOrDefault();
        RefreshView();
        SetFeedback(string.Format(T("selectedMany"), _selectedKeys.Count));
    }

    private void HandleSelectionShortcut(object? sender, KeyEventArgs e)
    {
        if (IsControlKey(e.Key))
            _controlHeld = true;

        if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SelectAllKeys();
            e.Handled = true;
        }
    }

    private static bool IsControlKey(Key key) => key.ToString() is "LeftCtrl" or "RightCtrl" or "LeftControl" or "RightControl";

    private async Task<string?> ImportSoundAsync()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = T("pickTitle"),
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(T("audioFiles")) { Patterns = new[] { "*.mp3", "*.wav", "*.ogg" } },
                    new FilePickerFileType(T("allFiles")) { Patterns = new[] { "*" } }
                }
            });

            var source = files.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
            {
                return null;
            }

            var extension = Path.GetExtension(source);
            if (!new[] { ".mp3", ".wav", ".ogg" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                SetFeedback(T("unsupported"), isWarning: true);
                return null;
            }

            var libraryFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LizardKeyboardSounds",
                "Sounds");
            Directory.CreateDirectory(libraryFolder);

            var destination = Path.Combine(libraryFolder, Path.GetFileName(source));
            if (!Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            {
                var baseName = Path.GetFileNameWithoutExtension(source);
                var number = 2;
                while (File.Exists(destination))
                {
                    destination = Path.Combine(libraryFolder, $"{baseName} ({number++}){extension}");
                }

                File.Copy(source, destination);
            }

            return destination;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            SetFeedback(T("importFailed"), isWarning: true);
            return null;
        }
    }

    private void PreviewSound(string? path)
    {
        if (!_audioEngine.Play(path, _settings.Volume))
        {
            SetFeedback(string.IsNullOrWhiteSpace(path) ? T("chooseFirst") : LocalizedAudioError(), isWarning: true);
            return;
        }

        SetFeedback(T("nowPlaying"));
    }

    private void RefreshView()
    {
        var defaultPath = _settings.DefaultSoundPath;
        DefaultSoundName.Text = string.IsNullOrWhiteSpace(defaultPath) ? T("noSound") : Path.GetFileName(defaultPath);
        DefaultSoundState.Text = string.IsNullOrWhiteSpace(defaultPath)
            ? T("formats")
            : File.Exists(defaultPath) ? T("defaultReady") : T("missingFile");
        DefaultSoundState.Foreground = string.IsNullOrWhiteSpace(defaultPath) || File.Exists(defaultPath)
            ? Brush.Parse("#92998A")
            : Brush.Parse("#E8B77A");
        DefaultPreviewButton.IsEnabled = !string.IsNullOrWhiteSpace(defaultPath) && File.Exists(defaultPath);

        PauseButton.Content = T(_settings.IsEnabled ? "pause" : "resume");
        HookStatusText.Text = T(_hookError is not null ? "hookUnavailable" : _settings.IsEnabled ? "hookActive" : "paused");
        HookStatusIndicator.Fill = Brush.Parse(_hookError is not null ? "#E8B77A" : "#C9F36B");
        ToolTip.SetTip(HookStatusText, _hookError is null ? null : $"{T("hookUnavailableHint")}\n{_hookError.Message}");
        VolumeValue.Text = $"{(int)VolumeSlider.Value}%";

        RefreshKeyboard();
        RefreshSelectedKey();

        BindingsPanel.Children.Clear();
        var mappings = _settings.KeySounds.OrderBy(pair => FormatKey(pair.Key), StringComparer.OrdinalIgnoreCase).ToArray();
        EmptyState.IsVisible = mappings.Length == 0;
        foreach (var mapping in mappings)
        {
            BindingsPanel.Children.Add(CreateBindingRow(mapping.Key, mapping.Value));
        }

        MutedKeysPanel.Children.Clear();
        var mutedKeys = _settings.MutedKeys.OrderBy(FormatKey, StringComparer.OrdinalIgnoreCase).ToArray();
        MutedKeysSection.IsVisible = mutedKeys.Length > 0;
        foreach (var key in mutedKeys)
        {
            var unmuteButton = new Button
            {
                Content = $"{FormatKey(key)}  ×",
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(10, 6),
                Background = Brush.Parse("#382421"),
                Foreground = Brush.Parse("#F0B7A8")
            };
            ToolTip.SetTip(unmuteButton, T("unmuteKey"));
            unmuteButton.Click += (_, _) => UnmuteKey(key);
            MutedKeysPanel.Children.Add(unmuteButton);
        }
    }

    private void UnmuteKey(string key)
    {
        if (!_settings.MutedKeys.Remove(key))
        {
            return;
        }

        NotifySettingsChanged();
        RefreshView();
        SetFeedback(string.Format(T("keyUnmutedFeedback"), FormatKey(key)));
    }

    public void ShowHookError(Exception error)
    {
        _hookError = error;
        HookStatusText.Text = T("hookUnavailable");
        HookStatusIndicator.Fill = Brush.Parse("#E8B77A");
        ToolTip.SetTip(HookStatusText, $"{T("hookUnavailableHint")}\n{error.Message}");
    }

    public void SetUpdaterAvailable() => CheckUpdatesButton.IsVisible = true;

    public void ShowUpdateMessage(string message, bool isWarning = false) => SetFeedback(message, isWarning);

    private async Task ShowUpdateSettingsAsync()
    {
        var channelSelector = new ComboBox { Width = 270 };
        channelSelector.Items.Add(T("updateStable"));
        channelSelector.Items.Add(T("updatePrerelease"));
        channelSelector.SelectedIndex = _settings.UpdateChannel == "prerelease" ? 1 : 0;
        var startupCheckBox = new CheckBox
        {
            Content = T("updateAtStartup"),
            IsChecked = _settings.CheckUpdatesAtStartup,
            Foreground = Brush.Parse("#DDE3D0")
        };

        var dialog = new Window
        {
            Title = T("updateSettings"),
            Width = 480,
            Height = 300,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush.Parse("#171A15"),
            Foreground = Brush.Parse("#F1F3EB")
        };
        var content = new StackPanel { Margin = new Thickness(24), Spacing = 16 };
        content.Children.Add(new TextBlock { Text = T("updateChannelLabel"), Foreground = Brush.Parse("#A5AB9B") });
        content.Children.Add(channelSelector);
        content.Children.Add(startupCheckBox);

        var buttons = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var cancelButton = new Button { Content = T("updateLater"), Padding = new Thickness(13, 8) };
        var saveButton = new Button { Content = T("save"), Padding = new Thickness(13, 8) };
        var checkButton = new Button
        {
            Content = T("checkUpdates"),
            Padding = new Thickness(13, 8),
            Background = Brush.Parse("#C9F36B"),
            Foreground = Brush.Parse("#171B12")
        };
        cancelButton.Click += (_, _) => dialog.Close(0);
        saveButton.Click += (_, _) => dialog.Close(1);
        checkButton.Click += (_, _) => dialog.Close(2);
        buttons.Children.Add(cancelButton);
        buttons.Children.Add(saveButton);
        buttons.Children.Add(checkButton);
        content.Children.Add(buttons);
        dialog.Content = content;

        var result = await dialog.ShowDialog<int>(this);
        if (result == 0)
        {
            return;
        }

        _settings.UpdateChannel = channelSelector.SelectedIndex == 1 ? "prerelease" : "stable";
        _settings.CheckUpdatesAtStartup = startupCheckBox.IsChecked == true;
        NotifySettingsChanged();
        if (result == 2 && UpdateCheckRequested is not null)
        {
            await UpdateCheckRequested(true);
        }
    }

    public async Task<bool> ConfirmUpdateAsync(string version)
    {
        var dialog = new Window
        {
            Title = T("updateTitle"),
            Width = 430,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush.Parse("#171A15"),
            Foreground = Brush.Parse("#F1F3EB")
        };
        var content = new StackPanel { Margin = new Thickness(24), Spacing = 18 };
        content.Children.Add(new TextBlock
        {
            Text = string.Format(T("updateAvailable"), version),
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap
        });
        var buttons = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };
        var laterButton = new Button { Content = T("updateLater"), Padding = new Thickness(14, 8) };
        var installButton = new Button
        {
            Content = T("updateInstall"),
            Padding = new Thickness(14, 8),
            Background = Brush.Parse("#C9F36B"),
            Foreground = Brush.Parse("#171B12")
        };
        laterButton.Click += (_, _) => dialog.Close(false);
        installButton.Click += (_, _) => dialog.Close(true);
        buttons.Children.Add(laterButton);
        buttons.Children.Add(installButton);
        content.Children.Add(buttons);
        dialog.Content = content;
        return await dialog.ShowDialog<bool>(this);
    }

    private void RefreshKeyboard()
    {
        KeyboardPanel.Children.Clear();
        KeyboardPanel.Children.Add(CreateKeyboardCluster(KeyboardRows()));
        KeyboardPanel.Children.Add(CreateKeyboardCluster(NavigationRows()));
        KeyboardPanel.Children.Add(CreateKeyboardCluster(NumpadRows()));
    }

    private Control CreateKeyboardCluster(KeyCap[][] rows)
    {
        var cluster = new StackPanel { Spacing = 6 };
        foreach (var row in rows)
        {
            var rowPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 3 };
            foreach (var key in row)
            {
                rowPanel.Children.Add(CreateKeyCap(key));
            }

            cluster.Children.Add(rowPanel);
        }

        return cluster;
    }

    private Control CreateKeyCap(KeyCap key)
    {
        if (string.IsNullOrEmpty(key.Code))
        {
            return new Border { Width = 34, Height = 38 };
        }

        var selected = _selectedKeys.Contains(key.Code);
        var assigned = _settings.KeySounds.ContainsKey(key.Code);
        var muted = _settings.MutedKeys.Contains(key.Code);
        var background = selected ? "#C9F36B" : muted ? "#3B2926" : assigned ? "#344124" : "#252A22";
        var foreground = selected ? "#171B12" : muted ? "#F0B7A8" : assigned ? "#DDF5AF" : "#D7DCCF";
        var label = _settings.KeyboardLayout == "de-DE" ? key.German : key.English;

        var button = new Button
        {
            Content = label,
            Width = key.Units * 34 + (key.Units - 1) * 3,
            Height = 38,
            Margin = new Thickness(0),
            Padding = new Thickness(2),
            Background = Brush.Parse(background),
            Foreground = Brush.Parse(foreground),
            BorderBrush = Brush.Parse(selected ? "#C9F36B" : muted ? "#A86352" : assigned ? "#78894D" : "#394034"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            FontSize = label.Length > 5 ? 8 : label.Length > 3 ? 9 : 11,
            FontWeight = FontWeight.SemiBold,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        ToolTip.SetTip(button, muted
            ? string.Format(T("keyMuted"), FormatKey(key.Code))
            : string.Format(T(assigned ? "keyHasOwn" : "keyUnassigned"), FormatKey(key.Code)));
        var controlClick = false;
        button.PointerPressed += (_, e) => controlClick = _controlHeld || e.KeyModifiers.HasFlag(KeyModifiers.Control);
        button.Click += (_, _) =>
        {
            if (controlClick || _controlHeld)
                ToggleKeySelection(key.Code);
            else
                SelectKey(key.Code);
            controlClick = false;
        };
        return button;
    }

    private KeyCap[][] KeyboardRows()
    {
        var de = _settings.KeyboardLayout == "de-DE";
        KeyCap K(string code, string deLabel, string enLabel, double width = 1) => new(code, deLabel, enLabel, width);
        KeyCap Letter(string name) => K($"Vc{name}", name, name);
        var topSymbols = de
            ? new[] { K("VcBackQuote", "^", "`~"), K("Vc1", "1", "1"), K("Vc2", "2", "2"), K("Vc3", "3", "3"), K("Vc4", "4", "4"), K("Vc5", "5", "5"), K("Vc6", "6", "6"), K("Vc7", "7", "7"), K("Vc8", "8", "8"), K("Vc9", "9", "9"), K("Vc0", "0", "0"), K("VcMinus", "ß", "-"), K("VcEquals", "´", "="), K("VcBackspace", T("keyBackspace"), T("keyBackspace"), 2) }
            : new[] { K("VcBackQuote", "`~", "`~"), K("Vc1", "1", "1"), K("Vc2", "2", "2"), K("Vc3", "3", "3"), K("Vc4", "4", "4"), K("Vc5", "5", "5"), K("Vc6", "6", "6"), K("Vc7", "7", "7"), K("Vc8", "8", "8"), K("Vc9", "9", "9"), K("Vc0", "0", "0"), K("VcMinus", "-", "-"), K("VcEquals", "=", "="), K("VcBackspace", T("keyBackspace"), T("keyBackspace"), 2) };

        var second = new[]
        {
            K("VcTab", T("keyTab"), T("keyTab"), 1.5), Letter("Q"), Letter("W"), Letter("E"), Letter("R"), Letter("T"), Letter(de ? "Z" : "Y"), Letter("U"), Letter("I"), Letter("O"), Letter("P"),
            K("VcOpenBracket", de ? "Ü" : "[", "["), K("VcCloseBracket", de ? "+" : "]", "]"), K(de ? "VcEnter" : "VcBackSlash", de ? "↵" : "\\|", "\\|", de ? 2 : 1)
        };
        var third = new[]
        {
            K("VcCapsLock", T("keyCaps"), T("keyCaps"), 1.8), Letter("A"), Letter("S"), Letter("D"), Letter("F"), Letter("G"), Letter("H"), Letter("J"), Letter("K"), Letter("L"),
            K("VcSemicolon", de ? "Ö" : ";", ";"), K("VcQuote", de ? "Ä" : "'", "'"),
            de ? K("VcBackSlash", "#", "\\|") : K("VcEnter", T("keyEnter"), T("keyEnter"), 1.8)
        };
        var fourth = de
            ? new[]
            {
                K("VcLeftShift", T("keyShift"), T("keyShift"), 2.3), K("VcIso", "< >", "< >"), Letter("Y"), Letter("X"), Letter("C"), Letter("V"), Letter("B"), Letter("N"), Letter("M"),
                K("VcComma", ",", ","), K("VcPeriod", ".", "."), K("VcSlash", "-", "/"), K("VcRightShift", T("keyShift"), T("keyShift"), 2.7)
            }
            : new[]
            {
                K("VcLeftShift", T("keyShift"), T("keyShift"), 2.3), Letter("Z"), Letter("X"), Letter("C"), Letter("V"), Letter("B"), Letter("N"), Letter("M"),
                K("VcComma", ",", ","), K("VcPeriod", ".", "."), K("VcSlash", "/", "/"), K("VcRightShift", T("keyShift"), T("keyShift"), 2.7)
            };
        var bottom = new[]
        {
            K("VcLeftControl", T("keyCtrl"), T("keyCtrl"), 1.4), K("VcLeftMeta", T("keyWin"), T("keyWin"), 1.2),
            K("VcLeftAlt", T("keyAlt"), T("keyAlt"), 1.3), K("VcSpace", T("keySpace"), T("keySpace"), 6.4),
            K("VcRightAlt", "AltGr", "AltGr", 1.3), K("VcRightMeta", T("keyWin"), T("keyWin"), 1.2), K("VcMenu", "☰", "☰", 1.2),
            K("VcRightControl", T("keyCtrl"), T("keyCtrl"), 1.4)
        };

        var function = new[]
        {
            K("VcEscape", "ESC", "ESC", 1.1), K("VcF1", "F1", "F1"), K("VcF2", "F2", "F2"), K("VcF3", "F3", "F3"), K("VcF4", "F4", "F4"),
            K("VcF5", "F5", "F5"), K("VcF6", "F6", "F6"), K("VcF7", "F7", "F7"), K("VcF8", "F8", "F8"), K("VcF9", "F9", "F9"),
            K("VcF10", "F10", "F10"), K("VcF11", "F11", "F11"), K("VcF12", "F12", "F12")
        };

        return new[] { function, topSymbols, second, third, fourth, bottom };
    }

    private KeyCap[][] NavigationRows()
    {
        KeyCap K(string code, string deLabel, string enLabel, double width = 1) => new(code, deLabel, enLabel, width);
        var spacer = new KeyCap("", "", "", 1);
        return new[]
        {
            new[] { K("VcPrintScreen", T("keyPrint"), T("keyPrint")), K("VcScrollLock", T("keyScroll"), T("keyScroll")), K("VcPause", T("keyPause"), T("keyPause")) },
            new[] { K("VcInsert", T("keyInsert"), T("keyInsert")), K("VcHome", T("keyHome"), T("keyHome")), K("VcPageUp", T("keyPageUp"), T("keyPageUp")) },
            new[] { K("VcDelete", T("keyDelete"), T("keyDelete")), K("VcEnd", T("keyEnd"), T("keyEnd")), K("VcPageDown", T("keyPageDown"), T("keyPageDown")) },
            new[] { spacer, spacer, K("VcUp", "↑", "↑") },
            new[] { K("VcLeft", "←", "←"), K("VcDown", "↓", "↓"), K("VcRight", "→", "→") }
        };
    }

    private KeyCap[][] NumpadRows()
    {
        KeyCap K(string code, string label, double width = 1) => new(code, label, label, width);
        var spacer = new KeyCap("", "", "");
        return new[]
        {
            new[] { K("VcNumLock", T("keyNumLock")), K("VcNumPadDivide", "/"), K("VcNumPadMultiply", "*"), K("VcNumPadSubtract", "−") },
            new[] { K("VcNumPad7", "7"), K("VcNumPad8", "8"), K("VcNumPad9", "9"), K("VcNumPadAdd", "+") },
            new[] { K("VcNumPad4", "4"), K("VcNumPad5", "5"), K("VcNumPad6", "6"), spacer },
            new[] { K("VcNumPad1", "1"), K("VcNumPad2", "2"), K("VcNumPad3", "3"), K("VcNumPadEnter", T("keyEnter")) },
            new[] { K("VcNumPad0", "0", 2), K("VcNumPadDecimal", ",") }
        };
    }

    private void RefreshSelectedKey()
    {
        var selected = _selectedKey;
        var selectedCount = _selectedKeys.Count;
        if (string.IsNullOrWhiteSpace(selected))
        {
            SelectedKeyTitle.Text = T("selectKey");
            SelectedKeySound.Text = T("keyboardSubtitle");
            SelectedKeyStatus.Text = "";
            SelectedChooseButton.IsEnabled = false;
            SelectedPreviewButton.IsEnabled = false;
            SelectedRemoveButton.IsEnabled = false;
            ThockKeyButton.IsEnabled = false;
            CreamyKeyButton.IsEnabled = false;
            SelectedMuteCheckBox.IsEnabled = false;
            SelectedMuteCheckBox.IsChecked = false;
            return;
        }

        if (selectedCount > 1)
        {
            SelectedKeyTitle.Text = string.Format(T("selectedMany"), selectedCount);
            SelectedKeySound.Text = T("multiSelectionHint");
            SelectedKeyStatus.Text = "";
            SelectedChooseButton.IsEnabled = true;
            SelectedPreviewButton.IsEnabled = false;
            SelectedRemoveButton.IsEnabled = _selectedKeys.Any(_settings.KeySounds.ContainsKey);
            ThockKeyButton.IsEnabled = true;
            CreamyKeyButton.IsEnabled = true;
            SelectedMuteCheckBox.IsEnabled = false;
            SelectedMuteCheckBox.IsChecked = false;
            return;
        }

        var muted = _settings.MutedKeys.Contains(selected);
        SelectedKeyTitle.Text = FormatKey(selected);
        var hasOwnSound = _settings.KeySounds.TryGetValue(selected, out var ownSound);
        var sound = hasOwnSound ? ownSound : _settings.DefaultSoundPath;
        SelectedKeySound.Text = hasOwnSound
            ? string.Format(T("keyHasOwn"), Path.GetFileName(ownSound))
            : string.IsNullOrWhiteSpace(sound) ? T("noSoundForKey") : string.Format(T("keyDefault"), Path.GetFileName(sound));
        SelectedKeyStatus.Text = muted ? T("keyMutedStatus") : hasOwnSound ? T("customSound") : T("usesDefault");
        SelectedChooseButton.IsEnabled = true;
        ThockKeyButton.IsEnabled = true;
        CreamyKeyButton.IsEnabled = true;
        SelectedPreviewButton.IsEnabled = !string.IsNullOrWhiteSpace(sound) && File.Exists(sound);
        SelectedRemoveButton.IsEnabled = hasOwnSound;
        SelectedMuteCheckBox.Content = T("muteKey");
        SelectedMuteCheckBox.IsEnabled = true;
        SelectedMuteCheckBox.IsChecked = muted;
    }

    private void ToggleSelectedMute()
    {
        if (string.IsNullOrWhiteSpace(_selectedKey))
        {
            return;
        }

        var shouldMute = SelectedMuteCheckBox.IsChecked == true;
        if (shouldMute)
        {
            _settings.MutedKeys.Add(_selectedKey);
        }
        else
        {
            _settings.MutedKeys.Remove(_selectedKey);
        }

        var key = _selectedKey;
        NotifySettingsChanged();
        RefreshView();
        SetFeedback(string.Format(T(shouldMute ? "keyMutedFeedback" : "keyUnmutedFeedback"), FormatKey(key)));
    }

    private Control CreateBindingRow(string keyName, string soundPath)
    {
        var row = new Grid { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var keyBadge = new Border
        {
            Background = Brush.Parse("#29311F"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 9),
            MinWidth = 84,
            Child = new TextBlock
            {
                Text = FormatKey(keyName),
                Foreground = Brush.Parse("#C9F36B"),
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            }
        };
        row.Children.Add(keyBadge);

        var soundName = new TextBlock
        {
            Text = Path.GetFileName(soundPath),
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse(File.Exists(soundPath) ? "#E8EBE2" : "#E8B77A"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var soundDescription = new TextBlock
        {
            Text = File.Exists(soundPath) ? T("customSound") : T("missingKeyFile"),
            FontSize = 10,
            Foreground = Brush.Parse("#92998A"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var soundInfo = new StackPanel { Spacing = 3, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(14, 0) };
        soundInfo.Children.Add(soundName);
        soundInfo.Children.Add(soundDescription);
        Grid.SetColumn(soundInfo, 1);
        row.Children.Add(soundInfo);

        var previewButton = MakeButton("▶", "#292E26", "#F1F3EB", 36);
        previewButton.Margin = new Thickness(0, 0, 8, 0);
        previewButton.IsEnabled = File.Exists(soundPath);
        previewButton.Click += (_, _) => PreviewSound(soundPath);
        Grid.SetColumn(previewButton, 2);
        row.Children.Add(previewButton);

        var changeButton = MakeButton(T("change"), "#292E26", "#E6E9DE", 74);
        changeButton.Margin = new Thickness(0, 0, 8, 0);
        changeButton.Click += async (_, _) =>
        {
            SelectKey(keyName);
            await ChooseSoundForSelectedKeyAsync();
        };
        Grid.SetColumn(changeButton, 3);
        row.Children.Add(changeButton);

        var removeButton = MakeButton(T("remove"), "#382421", "#F0B7A8", 82);
        removeButton.Click += (_, _) =>
        {
            SelectKey(keyName);
            RemoveSelectedKey();
        };
        Grid.SetColumn(removeButton, 4);
        row.Children.Add(removeButton);

        return new Border
        {
            Background = Brush.Parse("#151813"),
            BorderBrush = Brush.Parse("#2A3026"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10),
            Child = row
        };
    }

    private static Button MakeButton(string text, string background, string foreground, double minWidth)
    {
        return new Button
        {
            Content = text,
            Background = Brush.Parse(background),
            Foreground = Brush.Parse(foreground),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8),
            MinWidth = minWidth,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
    }

    private string FormatKey(string keyName)
    {
        return keyName switch
        {
            "VcSpace" => T("keySpace"),
            "VcEnter" => T("keyEnter"),
            "VcBackspace" => T("keyBackspace"),
            "VcTab" => T("keyTab"),
            "VcEscape" => "ESC",
            "VcDelete" => T("keyDelete"),
            "VcLeftShift" or "VcRightShift" => T("keyShift"),
            "VcLeftControl" or "VcRightControl" => T("keyCtrl"),
            "VcLeftAlt" or "VcRightAlt" => T("keyAlt"),
            "VcLeft" => "←",
            "VcRight" => "→",
            "VcUp" => "↑",
            "VcDown" => "↓",
            _ when keyName.StartsWith("Vc", StringComparison.Ordinal) => keyName[2..].ToUpperInvariant(),
            _ => keyName.ToUpperInvariant()
        };
    }

    private void NotifySettingsChanged() => SettingsChanged?.Invoke(_settings.Copy());

    private string LocalizedAudioError() => _audioEngine.Error?.Contains("BASS", StringComparison.OrdinalIgnoreCase) == true
        ? T("bassMissing")
        : T("audioUnavailable");

    private void SetFeedback(string message, bool isWarning = false)
    {
        FeedbackText.Text = message;
        FeedbackText.Foreground = Brush.Parse(isWarning ? "#E8B77A" : "#A5AB9B");
    }

    private sealed record KeyCap(string Code, string German, string English, double Units = 1);
}
