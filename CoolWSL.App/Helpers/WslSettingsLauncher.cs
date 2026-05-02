using System.ComponentModel;
using System.Diagnostics;

namespace CoolWSL.App.Helpers;

internal static class WslSettingsLauncher
{
    private const string RegisteredShellTarget = @"shell:AppsFolder\{6D809377-6AF0-444B-8957-A3773F02200E}\WSL\wslsettings\wslsettings.exe";
    private static readonly string[] ProtocolTargets = ["wslsettings:", "wslsettings.exe"];

    public static void Open()
    {
        Exception? lastException = null;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = RegisteredShellTarget,
                UseShellExecute = true,
            });

            return;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            lastException = ex;
        }

        foreach (var target in ProtocolTargets)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true,
                });

                return;
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                lastException = ex;
            }
        }

        throw new InvalidOperationException("The official WSL Settings app could not be opened.", lastException);
    }
}