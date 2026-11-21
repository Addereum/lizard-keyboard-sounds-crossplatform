using System.Collections.Generic;
using System;

namespace LizardCrossPlatform;

public sealed class AppSettings
{
    public float Volume { get; set; } = 0.7f;
    public string? DefaultSoundPath { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool OnlyPlayOnceWhileHeld { get; set; } = true;
    public string AppLanguage { get; set; } = "de";
    public string KeyboardLayout { get; set; } = "de-DE";
    public Dictionary<string, string> KeySounds { get; set; } = new();
    public HashSet<string> MutedKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public AppSettings Copy()
    {
        return new AppSettings
        {
            Volume = Volume,
            DefaultSoundPath = DefaultSoundPath,
            IsEnabled = IsEnabled,
            OnlyPlayOnceWhileHeld = OnlyPlayOnceWhileHeld,
            AppLanguage = AppLanguage,
            KeyboardLayout = KeyboardLayout,
            KeySounds = new Dictionary<string, string>(KeySounds ?? new Dictionary<string, string>()),
            MutedKeys = new HashSet<string>(MutedKeys ?? new HashSet<string>(), StringComparer.OrdinalIgnoreCase)
        };
    }
}
