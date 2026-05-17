using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PizzaOven;

public static class Setup
{
    public static string GetMD5Checksum(string filename)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filename);
        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
    }
    
    public static async Task<bool> GameSetupAsync(Window parentWindow)
    {
        string defaultPath = string.Empty;

        // Try Steam registry on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                // Dynamic registry read so project builds cross-platform
                var key = Microsoft.Win32.Registry.LocalMachine
                    .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2231450");
                if (key?.GetValue("InstallLocation") is string loc && !string.IsNullOrEmpty(loc))
                    defaultPath = Path.Combine(loc, "PizzaTower.exe");
            }
            catch { }
        }

        // Try Linux Steam path
        if (!File.Exists(defaultPath) && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var linuxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local/share/Steam/steamapps/common/Pizza Tower/PizzaTower.exe");
            if (File.Exists(linuxPath)) defaultPath = linuxPath;
        }

        if (!File.Exists(defaultPath))
        {
            Global.logger.WriteLine(
                "Couldn't find install path in registry, select path to exe instead",
                LoggerType.Warning);

            var files = await parentWindow.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Select PizzaTower.exe from your Steam install folder",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Executable")
                        {
                            Patterns = ["PizzaTower.exe"],
                            MimeTypes = ["application/octet-stream"]
                        }
                    ]
                });

            if (files.Count == 1)
            {
                var chosen = files[0].TryGetLocalPath() ?? string.Empty;
                if (Path.GetFileName(chosen).Equals("PizzaTower.exe",
                        StringComparison.OrdinalIgnoreCase))
                    defaultPath = chosen;
                else
                {
                    Global.logger.WriteLine("PizzaTower.exe not found", LoggerType.Error);
                    if (Global.ronnietutorial)
                        PLUSTutorial.RonnieVariables.SetupSucessful = 0;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        Global.config.ModsFolder = Path.GetDirectoryName(defaultPath);
        Global.config.Launcher   = defaultPath;
        Global.UpdateConfig();
        Global.logger.WriteLine("Setup completed for Pizza Tower!", LoggerType.Info);
        if (Global.ronnietutorial)
            PLUSTutorial.RonnieVariables.SetupSucessful = 1;
        return true;
    }
}
