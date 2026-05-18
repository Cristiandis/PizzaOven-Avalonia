using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PizzaOven;

public static class RegistryConfig
{
    public static bool InstallGBHandler()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                RegisterWindows($"{Global.assemblyLocation}{Global.s}PizzaOven.exe", "pizzaovenplus");
                return true;
            }
            catch
            {
                return false;
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                RegisterLinux();
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static void RegisterWindows(string appPath, string protocolName)
    {
#if WINDOWS
        var reg = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\PizzaOven");
        reg.SetValue("", $"URL:{protocolName}");
        reg.SetValue("URL Protocol", "");
        reg = reg.CreateSubKey(@"shell\open\command");
        reg.SetValue("", $"\"{appPath}\" -download \"%1\"");
        reg.Close();
#else
        _ = appPath;
        _ = protocolName;
#endif
    }

    private static void RegisterLinux()
    {
        var applicationsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "applications");

        Directory.CreateDirectory(applicationsDir);
        
        bool isFlatpak = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FLATPAK_ID"));
        string exec = isFlatpak
            ? "/app/bin/pizzaoven -download %u"
            : $"{Global.appLocation}{Global.s}pizzaoven -download %u";

        var handlerDesktop = Path.Combine(applicationsDir, "pizzaoven-handler.desktop");
        File.WriteAllText(handlerDesktop,
            "[Desktop Entry]\n" +
            "Name=Pizza Oven+\n" +
            $"Exec={exec}\n" +
            "Type=Application\n" +
            "NoDisplay=true\n" +
            "MimeType=x-scheme-handler/pizzaovenplus;\n");

        RunSilent("xdg-mime", "default pizzaoven-handler.desktop x-scheme-handler/pizzaovenplus");
        RunSilent("update-desktop-database", applicationsDir);
    }

    private static void RunSilent(string fileName, string arguments)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            proc?.WaitForExit(3000);
        }
        catch
        {
        }
    }
}