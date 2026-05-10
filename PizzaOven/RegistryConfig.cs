using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PizzaOven;

public static class RegistryConfig
{
    public static bool InstallGBHandler()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        string appPath = $"{Global.assemblyLocation}{Global.s}PizzaOven.exe";
        const string protocolName = "pizzaoven";
        try
        {
            var registryType = Type.GetType("Microsoft.Win32.Registry, Microsoft.Win32.Registry");
            if (registryType == null)
            {
                RegisterWindows(appPath, protocolName);
            }
            else
            {
                RegisterWindows(appPath, protocolName);
            }
            return true;
        }
        catch
        {
            return false;
        }
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
}
