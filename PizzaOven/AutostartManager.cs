using System;
using System.IO;
using Microsoft.Win32;

namespace PizzaOven
{
    public static class AutostartManager
    {
        private const string AppName = "pizzaoven";
        private const string DisplayName = "Pizza Oven";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void SetAutostart(bool enable)
        {
            if (OperatingSystem.IsLinux()) SetLinuxAutostart(enable);
            else if (OperatingSystem.IsWindows()) SetWindowsAutostart(enable);
        }

        public static bool IsEnabled()
        {
            if (OperatingSystem.IsLinux()) return File.Exists(GetLinuxPath());
            if (OperatingSystem.IsWindows()) return IsWindowsEnabled();
            return false;
        }

        #region Linux
        private static string GetLinuxPath() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "autostart", $"{AppName}.desktop");

        private static void SetLinuxAutostart(bool enable)
        {
            string filePath = GetLinuxPath();
            if (enable)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllText(filePath, $"""
                    [Desktop Entry]
                    Type=Application
                    Name={DisplayName}
                    Comment=Mod manager for Pizza Tower
                    Exec=/usr/bin/{AppName}
                    Icon={AppName}
                    Terminal=false
                    X-GNOME-Autostart-enabled=true
                    """);
            }
            else if (File.Exists(filePath))
                File.Delete(filePath);
        }
        #endregion

        #region Windows
        private static void SetWindowsAutostart(bool enable)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;
            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(DisplayName, $"\"{exePath}\"");
            }
            else
                key.DeleteValue(DisplayName, false);
        }

        private static bool IsWindowsEnabled()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(DisplayName) != null;
        }
        #endregion
    }
}