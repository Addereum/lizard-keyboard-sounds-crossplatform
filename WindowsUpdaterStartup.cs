using System;
using System.IO;
using Microsoft.Win32;
using Velopack.Locators;

namespace LizardCrossPlatform;

internal static class WindowsUpdaterStartup
{
    public static void SetEnabled(bool enabled)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            const string valueName = "LizardKeyboardSoundsUpdater";
            using var runKey = Registry.CurrentUser.CreateSubKey(runKeyPath);
            if (!enabled)
            {
                runKey?.DeleteValue(valueName, throwOnMissingValue: false);
                return;
            }

            var appRoot = VelopackLocator.Current.RootAppDir;
            if (string.IsNullOrWhiteSpace(appRoot))
            {
                return;
            }

            var appPath = Path.Combine(appRoot, "LizardCrossPlatform.exe");
            runKey?.SetValue(valueName, $"\"{appPath}\" --update-checker");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
