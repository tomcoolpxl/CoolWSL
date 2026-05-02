using System.ComponentModel;
using System.Diagnostics;

namespace CoolWSL.App.Helpers;

internal static class WslSettingsLauncher
{
    private static readonly string[] Targets = ["wslsettings:", "wslsettings.exe"];

    public static void Open()
    {
        Exception? lastException = null;

        foreach (var target in Targets)
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