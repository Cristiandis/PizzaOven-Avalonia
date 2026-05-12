using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace PizzaOven;

public partial class MainWindow
{
    #region Tutorial

    private void ReplayTutorial_Click(object sender, RoutedEventArgs e)
    {
        Global.logger.WriteLine("Tutorial not yet implemented.", LoggerType.Warning);
    }

    #endregion

    #region Links

    private void OpenLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var url = btn.Name switch
        {
            "OpenSuggestForm" =>
                "https://docs.google.com/forms/d/e/1FAIpQLScI-8L6-ATpE6_ip3gzESXAWi4B_0pwHiHI5g83fb3SlLTM_A/viewform",
            "OpenEmail" => "https://mail.google.com/mail/u/0/#inbox",
            "OpenTwitterX" => "https://x.com/SurfyCrescent97",
            "OpenDiscord" => "https://discord.gg/snv7CrRQzx",
            _ => null
        };
        if (url != null)
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    #endregion

    #region Settings Navigation

    private readonly Dictionary<string, StackPanel> _settingsPanels = new();

    private void InitSettingsPanels()
    {
        _settingsPanels["NavTutorial"] = PanelTutorial;
        _settingsPanels["NavLinks"] = PanelLinks;
        _settingsPanels["NavAppSettings"] = PanelAppSettings;
        _settingsPanels["NavLaunchSettings"] = PanelLaunchSettings;
        _settingsPanels["NavModSettings"] = PanelModSettings;
        _settingsPanels["NavGMLoader"] = PanelGMLoader;
        _settingsPanels["NavCredits"] = PanelCredits;
    }

    private void SettingsNav_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsPanels.Count == 0) InitSettingsPanels();
        if (sender is not Button btn) return;
        foreach (var panel in _settingsPanels.Values)
            panel.IsVisible = false;
        if (_settingsPanels.TryGetValue(btn.Name ?? "", out var target))
            target.IsVisible = true;
    }

    private void UpdateToggle(Button btn, bool state, string onText, string offText)
    {
        btn.Content = state ? onText : offText;
    }

    public void InitToggles()
    {
        UpdateToggle(RPCtoggle,
            PLUSSavesystem.read_ini_bool("Discord", "RPC", true),
            "Enable RPC? [IT'S ON]", "Enable RPC? [IT'S OFF]");
        UpdateToggle(MODUPDATERtoggle,
            PLUSSavesystem.read_ini_bool("LowEnd", "ModUpdate", true),
            "Disable Check for Mod Updates? [IT'S ON]", "Disable Check for Mod Updates? [IT'S OFF]");
        UpdateToggle(DebugToggle,
            PLUSSavesystem.read_ini_bool("Launch", "Debug", false),
            "Enable Debug? [IT'S ON]", "Enable Debug? [IT'S OFF]");
        UpdateToggle(POLanguage,
            PLUSSavesystem.read_ini_bool("Files", "POLanguage", true),
            "Do not Apply to Language Files? [IT'S ON]", "Do not Apply to Language Files? [IT'S OFF]");
        StartupToggle.Content = "Do open on Startup? [IT'S OFF]";
    }

    #endregion

    #region App Settings

    private void StartupToggle_Click(object sender, RoutedEventArgs e)
    {
        Global.logger.WriteLine("Launch on startup is not supported on this platform.", LoggerType.Warning);
    }

    private void RPCToggle_Click(object sender, RoutedEventArgs e)
    {
        var enabled = PLUSSavesystem.toggle_ini_bool("Discord", "RPC", true);
        UpdateToggle(RPCtoggle, enabled, "Enable RPC? [IT'S ON]", "Enable RPC? [IT'S OFF]");
        try
        {
            if (enabled) POPRESENCE.Initialize();
            else POPRESENCE.Shutdown();
        }
        catch
        {
        }
    }

    private void ModUpdaterToggle_Click(object sender, RoutedEventArgs e)
    {
        var enabled = PLUSSavesystem.toggle_ini_bool("LowEnd", "ModUpdate", true);
        UpdateToggle(MODUPDATERtoggle, enabled,
            "Disable Check for Mod Updates? [IT'S ON]", "Disable Check for Mod Updates? [IT'S OFF]");
    }

    #endregion

    #region Launch Settings

    private void DowngradeDownload_Click(object sender, RoutedEventArgs e)
    {
        Global.logger.WriteLine("Downgrade downloader not yet implemented.", LoggerType.Warning);
    }

    private void OpenPTFolder_Click(object sender, RoutedEventArgs e)
    {
        if (Global.config.ModsFolder != null && Directory.Exists(Global.config.ModsFolder))
            Process.Start(new ProcessStartInfo { FileName = Global.config.ModsFolder, UseShellExecute = true });
        else
            Global.logger.WriteLine("Game folder not set. Please run Setup first.", LoggerType.Warning);
    }

    private async void ChooseNewPTFolder_Click(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select PizzaTower.exe from your Steam install folder",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PizzaTower.exe")
                {
                    Patterns = new[] { "PizzaTower.exe" }
                }
            }
        });
        if (files.Count == 0) return;
        var path = files[0].Path.LocalPath;
        if (!Path.GetFileName(path).Equals("PizzaTower.exe", StringComparison.OrdinalIgnoreCase))
        {
            Global.logger.WriteLine("PizzaTower.exe not found at selected path.", LoggerType.Error);
            return;
        }

        Global.config.ModsFolder = Path.GetDirectoryName(path);
        Global.config.Launcher = path;
        Global.UpdateConfig();
        Global.logger.WriteLine($"Pizza Tower folder set to: {Global.config.ModsFolder}", LoggerType.Info);
    }

    private void DebugToggle_Click(object sender, RoutedEventArgs e)
    {
        var enabled = PLUSSavesystem.toggle_ini_bool("Launch", "Debug", false);
        UpdateToggle(DebugToggle, enabled, "Enable Debug? [IT'S ON]", "Enable Debug? [IT'S OFF]");
    }

    #endregion

    #region Mod Settings

    private void CleanPOFiles_Click(object sender, RoutedEventArgs e)
    {
        if (Global.config.ModsFolder == null) return;
        foreach (var file in Directory.GetFiles(Global.config.ModsFolder, "*.po", SearchOption.AllDirectories))
            try
            {
                File.Delete(file);
            }
            catch
            {
            }

        Global.logger.WriteLine("Cleaned all .po files from game folder.", LoggerType.Info);
    }

    private void POLanguage_Click(object sender, RoutedEventArgs e)
    {
        var enabled = PLUSSavesystem.toggle_ini_bool("Files", "POLanguage", true);
        UpdateToggle(POLanguage, enabled,
            "Do not Apply to Language Files? [IT'S ON]", "Do not Apply to Language Files? [IT'S OFF]");
        if (!enabled && Global.config.ModsFolder != null)
        {
            var langPath = Path.Combine(Global.config.ModsFolder, "lang");
            if (Directory.Exists(langPath))
                foreach (var f in Directory.GetFiles(langPath, "*.*", SearchOption.AllDirectories)
                             .Where(f => new[] { ".po", ".custompo", ".downgradepo" }.Contains(Path.GetExtension(f))))
                    try
                    {
                        File.Delete(f);
                    }
                    catch
                    {
                    }
        }
    }

    private async void MakeDataWinPO_Click(object sender, RoutedEventArgs e)
    {
        if (Global.config.ModsFolder == null) return;
        var dataWin = Path.Combine(Global.config.ModsFolder, "data.win");
        var dataWinPO = Path.Combine(Global.config.ModsFolder, "data.win.po");
        if (!File.Exists(dataWin))
        {
            Global.logger.WriteLine("data.win not found in game folder.", LoggerType.Warning);
            return;
        }

        if (File.Exists(dataWinPO))
        {
            var overwrite = await ShowConfirmDialog("data.win.po already exists. Overwrite it?");
            if (!overwrite) return;
        }

        File.Copy(dataWin, dataWinPO, true);
        Global.logger.WriteLine("Created/overwritten data.win.po from current data.win.", LoggerType.Info);
    }

    #endregion

    #region GMLoader Settings

    private async void ConvertToGMLoader_Click(object sender, RoutedEventArgs e)
    {
        string gmLoaderFolder = Path.Combine(Global.appLocation, 
            OperatingSystem.IsWindows() ? "GMLOADER-windows" : "GMLOADER-linux");
        string gmLoaderExe = Path.Combine(gmLoaderFolder, 
            OperatingSystem.IsWindows() ? "GMLoader.exe" : "GMLoader.bin");

        if (!File.Exists(gmLoaderExe))
        {
            Global.logger.WriteLine("GMLoader runtime not found. Cannot convert.", LoggerType.Error);
            return;
        }

        foreach (var proc in Process.GetProcessesByName("GMLoader"))
            try
            {
                proc.Kill();
                proc.WaitForExit();
            }
            catch
            {
            }

        string[] toDelete =
        {
            Path.Combine(gmLoaderFolder, "Export"),
            Path.Combine(gmLoaderFolder, "vanilla_export"),
            Path.Combine(gmLoaderFolder, "modded_export"),
            Path.Combine(gmLoaderFolder, "converted_output"),
            Path.Combine(gmLoaderFolder, "vanilla.win"),
            Path.Combine(gmLoaderFolder, "modded.win"),
            Path.Combine(gmLoaderFolder, "modded.xdelta")
        };
        foreach (var path in toDelete)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            if (File.Exists(path)) File.Delete(path);
        }

        Global.logger.WriteLine("Please select the base data.win, then the modded file (.xdelta or .win).",
            LoggerType.Info);

        var sourceFiles = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select base data.win",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("data.win") { Patterns = new[] { "*.win" } } }
        });
        if (sourceFiles.Count == 0)
        {
            Global.logger.WriteLine("No source file selected.", LoggerType.Error);
            return;
        }

        var source = sourceFiles[0].Path.LocalPath;

        var moddedFiles = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select modded file (.xdelta or .win)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Modded") { Patterns = new[] { "*.xdelta", "*.win" } } }
        });
        if (moddedFiles.Count == 0)
        {
            Global.logger.WriteLine("No modded file selected.", LoggerType.Error);
            return;
        }

        var modded = moddedFiles[0].Path.LocalPath;

        var vanillaWin = Path.Combine(gmLoaderFolder, "vanilla.win");
        var moddedDest = Path.Combine(gmLoaderFolder, $"modded{Path.GetExtension(modded)}");
        File.Copy(source, vanillaWin, true);
        File.Copy(modded, moddedDest, true);

        if (Path.GetExtension(modded).Equals(".xdelta", StringComparison.OrdinalIgnoreCase))
        {
            var xdelta = OperatingSystem.IsWindows()
                ? Path.Combine(Global.assemblyLocation, "Dependencies", "xdelta.exe")
                : "xdelta3";
            var moddedWin = Path.Combine(gmLoaderFolder, "modded.win");
            try
            {
                Global.logger.WriteLine("Patching xdelta to produce modded.win...", LoggerType.Info);
                ModLoader.PathFixPatch(vanillaWin, moddedDest, moddedWin + ".temp", xdelta);
                File.Move(moddedWin + ".temp", moddedWin, true);
                File.Delete(moddedDest);
            }
            catch
            {
                Global.logger.WriteLine("Failed to apply xdelta patch.", LoggerType.Error);
                File.Delete(vanillaWin);
                File.Delete(moddedDest);
                return;
            }
        }

        Global.logger.WriteLine("Running GMLoader -convert... this may take a while.", LoggerType.Info);

        var convertedOutput = Path.Combine(gmLoaderFolder, "converted_output");
        string[] gmLoaderFolders = { "audio", "code", "lib", "csx", "room", "shader", "texture", "xdelta" };

        var startInfo = new ProcessStartInfo
        {
            FileName = gmLoaderExe,
            Arguments = "-convert",
            WorkingDirectory = gmLoaderFolder,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        var success = await Task.Run(() =>
        {
            using var process = Process.Start(startInfo)!;
            while (!process.HasExited)
            {
                if (Directory.Exists(convertedOutput))
                {
                    var hasFolders = gmLoaderFolders.Any(f => Directory.Exists(Path.Combine(convertedOutput, f)));
                    var nonEmpty = Directory.GetDirectories(convertedOutput)
                        .All(d => Directory.EnumerateFileSystemEntries(d).Any());
                    if (hasFolders && nonEmpty)
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                        }

                        return true;
                    }
                }

                Thread.Sleep(1000);
            }

            return false;
        });

        if (!success)
        {
            Global.logger.WriteLine("GMLoader exited before producing output.", LoggerType.Error);
            return;
        }

        var modName = Path.GetFileNameWithoutExtension(modded);
        var basePath = Path.Combine(Global.assemblyLocation, "Mods", $"{modName} - GMLoader");
        var finalPath = basePath;
        var i = 1;
        while (Directory.Exists(finalPath))
            finalPath = $"{basePath} ({i++})";
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ModsWatcher.EnableRaisingEvents = false;
            Directory.Move(convertedOutput, finalPath);
            ModsWatcher.EnableRaisingEvents = true;
            Refresh();
        });
        

        foreach (var path in toDelete)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            if (File.Exists(path)) File.Delete(path);
        }

        Global.logger.WriteLine($"Conversion complete! Mod saved to: {finalPath}", LoggerType.Info);
    }

    private void KillGMLoader_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("GMLoader"))
            {
                proc.Kill();
                Global.logger.WriteLine($"Killed GMLoader process (PID {proc.Id})", LoggerType.Info);
            }
        }
        catch (Exception ex)
        {
            Global.logger.WriteLine($"Failed to kill GMLoader: {ex.Message}", LoggerType.Error);
        }
    }

    #endregion
}