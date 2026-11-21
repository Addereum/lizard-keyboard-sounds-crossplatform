using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace LizardCrossPlatform;

internal static class UpdateStartupChecker
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LizardKeyboardSounds",
        "settings.json");

    public static async Task RunAsync()
    {
        var settings = LoadSettings();
        if (!settings.CheckUpdatesAtStartup)
        {
            return;
        }

        var channel = settings.UpdateChannel == "prerelease" ? "win-alpha" : "win";
        var source = new GithubSource("https://github.com/Addereum/lizard-keyboard-sounds-crossplatform", null, prerelease: true);
        var manager = new UpdateManager(source, new UpdateOptions
        {
            ExplicitChannel = channel,
            AllowVersionDowngrade = true
        });
        if (!manager.IsInstalled)
        {
            return;
        }

        var update = await manager.CheckForUpdatesAsync();
        if (update is null || IsMainAppAlreadyRunning())
        {
            return;
        }

        var appRoot = VelopackLocator.Current.RootAppDir;
        if (string.IsNullOrWhiteSpace(appRoot))
        {
            return;
        }

        var appPath = Path.Combine(appRoot, "LizardCrossPlatform.exe");
        if (File.Exists(appPath))
        {
            Process.Start(new ProcessStartInfo(appPath) { UseShellExecute = true });
        }
    }

    private static AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }

        return new AppSettings();
    }

    private static bool IsMainAppAlreadyRunning()
    {
        var processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var processes = Process.GetProcessesByName(processName);
        try
        {
            return processes.Any(process => process.Id != Environment.ProcessId);
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }
}
