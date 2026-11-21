using Avalonia;
using System;
using System.Linq;
using Velopack;

namespace LizardCrossPlatform;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (OperatingSystem.IsWindows())
        {
            VelopackApp.Build()
                .OnAfterInstallFastCallback(_ => WindowsUpdaterStartup.SetEnabled(enabled: true))
                .OnBeforeUninstallFastCallback(_ => WindowsUpdaterStartup.SetEnabled(enabled: false))
                .Run();

            if (args.Contains("--update-checker", StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    UpdateStartupChecker.RunAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                return;
            }
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
