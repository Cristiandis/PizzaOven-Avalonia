using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace PizzaOven;

public partial class MainWindow
{
    private static readonly string[] themebrushes = { "Primary", "Secondary", "Inner", "Loading", "Text" };
    private static readonly string[] themeimageExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };
    private static readonly string[] transparentboxes = { "Logger", "ModDescription", "ModGrid" };
    private readonly Dictionary<string, string> defaultBrushHexes = new();

    private static string CustomAssetsFolder =>
        Path.Combine(Global.assemblyLocation, "CustomAssets");

    private void HandlePLUStoggle(string section, string key, bool defaultValue, string toggleName)
    {
        var enabled = PLUSSavesystem.toggle_ini_bool(section, key, defaultValue);
        InitPLUSToggle(toggleName, enabled);
    }

    public void InitPLUSToggle(string name, bool enabled)
    {
        try
        {
            Button? button = null;
            var OnText = "";
            var OffText = "";

            switch (name)
            {
                case "Mute":
                    PLUSMUSIC.MuteEnabled = enabled;
                    PLUSMUSIC.ApplyCurrentVolume(true);
                    button = MuteButton;
                    OnText = "Disable Mute? [IT'S ON]";
                    OffText = "Enable Mute? [IT'S OFF]";
                    break;
                case "UnfocusedMute":
                    PLUSMUSIC.UnfocusedMuteEnabled = enabled;
                    PLUSMUSIC.ApplyCurrentVolume(true);
                    button = UnfocusedMuteButton;
                    OnText = "Disable Unfocused Mute? [IT'S ON]";
                    OffText = "Enable Unfocused Mute? [IT'S OFF]";
                    break;
                case "RPC":
                    try
                    {
                        if (enabled) POPRESENCE.Initialize();
                        else POPRESENCE.Shutdown();
                    }
                    catch { }
                    button = RPCtoggle;
                    OnText = "Enable RPC? [IT'S ON]";
                    OffText = "Disable RPC? [IT'S OFF]";
                    break;
                case "Debug":
                    button = DebugToggle;
                    OnText = "Enable Debug? [IT'S ON]";
                    OffText = "Disable Debug? [IT'S OFF]";
                    break;
                case "ModUpdater":
                    button = MODUPDATERtoggle;
                    OnText = "Disable Check for Mod Updates? [IT'S ON]";
                    OffText = "Enable Check for Mod Updates? [IT'S OFF]";
                    break;
                case "POLanguage":
                    if (!enabled && Global.config.ModsFolder != null)
                    {
                        var langPath = Path.Combine(Global.config.ModsFolder, "lang");
                        var extensions = new[] { ".po", ".custompo", ".downgradepo" };
                        if (Directory.Exists(langPath))
                            foreach (var file in Directory.GetFiles(langPath, "*.*", SearchOption.AllDirectories)
                                .Where(f => extensions.Contains(Path.GetExtension(f))))
                                try { File.Delete(file); } catch { }
                    }
                    button = POLanguage;
                    OnText = "Do not Apply to Language Files? [IT'S ON]";
                    OffText = "Do Apply to Language Files? [IT'S OFF]";
                    break;
                case "Startup":
                    button = StartupToggle;
                    OnText = "Do not open on Startup? [IT'S ON]";
                    OffText = "Do open on Startup? [IT'S OFF]";
                    break;
            }

            if (button != null) button.Content = enabled ? OnText : OffText;
        }
        catch { }
    }

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
            "OpenSuggestForm" => "https://docs.google.com/forms/d/e/1FAIpQLScI-8L6-ATpE6_ip3gzESXAWi4B_0pwHiHI5g83fb3SlLTM_A/viewform",
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
        _settingsPanels["NavCustomizaton"] = PanelCustomization;
        _settingsPanels["NavCredits"] = PanelCredits;
    }

    private void SettingsNav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        foreach (var panel in _settingsPanels.Values)
            panel.IsVisible = false;
        if (_settingsPanels.TryGetValue(btn.Name ?? "", out var target))
            target.IsVisible = true;
    }

    public void InitToggles()
    {
        InitPLUSToggle("RPC", PLUSSavesystem.read_ini_bool("Discord", "RPC", true));
        InitPLUSToggle("ModUpdater", PLUSSavesystem.read_ini_bool("LowEnd", "ModUpdate", true));
        InitPLUSToggle("Debug", PLUSSavesystem.read_ini_bool("Launch", "Debug", false));
        InitPLUSToggle("POLanguage", PLUSSavesystem.read_ini_bool("Files", "POLanguage", true));
        InitPLUSToggle("Mute", PLUSMUSIC.MuteEnabled);
        InitPLUSToggle("UnfocusedMute", PLUSMUSIC.UnfocusedMuteEnabled);

        if (double.TryParse(PLUSSavesystem.read_ini("Audio", "SoundVolume", "100"), out var vol))
            if (SoundVolume != null) SoundVolume.Value = vol;

        if (StartupToggle != null)
            StartupToggle.Content = AutostartManager.IsEnabled()
                ? "Do not open on Startup? [IT'S ON]"
                : "Do open on Startup? [IT'S OFF]";
    }

    #endregion

    #region App Settings

    private void StartupToggle_Click(object sender, RoutedEventArgs e)
    {
        var enabled = !AutostartManager.IsEnabled();
        AutostartManager.SetAutostart(enabled);
        StartupToggle.Content = enabled ? "Do not open on Startup? [IT'S ON]" : "Do open on Startup? [IT'S OFF]";
    }

    private void RPCToggle_Click(object sender, RoutedEventArgs e) => HandlePLUStoggle("Discord", "RPC", true, "RPC");
    private void ModUpdaterToggle_Click(object sender, RoutedEventArgs e) => HandlePLUStoggle("LowEnd", "ModUpdate", true, "ModUpdater");

    #endregion

    #region Launch Settings

    private void DowngradeDownload_Click(object sender, RoutedEventArgs e) => PLUSDepotDownloader.DowngradeDownload(this);

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
                new FilePickerFileType("PizzaTower.exe") { Patterns = new[] { "PizzaTower.exe" } }
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

    private void DebugToggle_Click(object sender, RoutedEventArgs e) => HandlePLUStoggle("Launch", "Debug", false, "Debug");

    #endregion

    #region Mod Settings

    private void CleanPOFiles_Click(object sender, RoutedEventArgs e)
    {
        if (Global.config.ModsFolder == null) return;
        foreach (var file in Directory.GetFiles(Global.config.ModsFolder, "*.po", SearchOption.AllDirectories))
            try { File.Delete(file); } catch { }
        Global.logger.WriteLine("Cleaned all .po files from game folder.", LoggerType.Info);
    }

    private void POLanguage_Click(object sender, RoutedEventArgs e) => HandlePLUStoggle("Files", "POLanguage", true, "POLanguage");

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
        var gmLoaderFolder = Path.Combine(Global.appLocation, "GMLOADER-windows");
        var gmLoaderExe = Path.Combine(gmLoaderFolder, "GMLoader.exe");

        if (!File.Exists(gmLoaderExe))
        {
            Global.logger.WriteLine("GMLoader runtime not found. Cannot convert.", LoggerType.Error);
            return;
        }

        foreach (var proc in Process.GetProcessesByName("GMLoader"))
            try { proc.Kill(); proc.WaitForExit(); } catch { }

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

        Global.logger.WriteLine("Please select the base data.win, then the modded file (.xdelta or .win).", LoggerType.Info);

        var sourceFiles = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select base data.win",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("data.win") { Patterns = new[] { "*.win" } } }
        });
        if (sourceFiles.Count == 0) { Global.logger.WriteLine("No source file selected.", LoggerType.Error); return; }
        var source = sourceFiles[0].Path.LocalPath;

        var moddedFiles = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select modded file (.xdelta or .win)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Modded") { Patterns = new[] { "*.xdelta", "*.win" } } }
        });
        if (moddedFiles.Count == 0) { Global.logger.WriteLine("No modded file selected.", LoggerType.Error); return; }
        var modded = moddedFiles[0].Path.LocalPath;

        var vanillaWin = Path.Combine(gmLoaderFolder, "vanilla.win");
        var moddedDest = Path.Combine(gmLoaderFolder, $"modded{Path.GetExtension(modded)}");
        File.Copy(source, vanillaWin, true);
        File.Copy(modded, moddedDest, true);

        if (Path.GetExtension(modded).Equals(".xdelta", StringComparison.OrdinalIgnoreCase))
        {
            var xdelta = OperatingSystem.IsWindows()
                ? Path.Combine(Global.appLocation, "Dependencies", "xdelta.exe")
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
                        try { process.Kill(); } catch { }
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

    #region App Customization

    private void InitThemes()
    {
        RestoreFolderFromResource("CustomAssets", Global.assemblyLocation);
        foreach (var name in themebrushes)
        {
            if (!defaultBrushHexes.ContainsKey(name))
                defaultBrushHexes[name] = PLUSThemes.Get_BrushColorAsHex($"{name}Brush");
            Theme_Update(name, true);
        }
        ApplyTransparentBoxes(true);
        LoadThemePresets();
        ApplyBackgroundImage();
    }

    private void LoadThemePresets()
    {
        var themesPath = Path.Combine(Global.assemblyLocation, "Themes");
        if (!Directory.Exists(themesPath)) return;

        ThemePresetsCombo.Items.Clear();
        foreach (var file in Directory.GetFiles(themesPath))
            if (Path.GetExtension(file).Equals(".potheme", StringComparison.OrdinalIgnoreCase))
                ThemePresetsCombo.Items.Add(Path.GetFileNameWithoutExtension(file));

        if (ThemePresetsCombo.Items.Count > 0)
            ThemePresetsCombo.SelectedIndex = 0;
    }

    public void Theme_Update(string brushname, bool skippicker = false)
    {
        if (skippicker)
        {
            var saved = PLUSSavesystem.read_ini("Themes", brushname);
            if (!string.IsNullOrEmpty(saved) && PLUSThemes.validhex(saved))
                PLUSThemes.Set_BrushColor($"{brushname}Brush", saved);
            return;
        }
        Themes_GrabColor(brushname);
    }

    private async void Themes_GrabColor(string brushname)
    {
        var current = PLUSSavesystem.read_ini("Themes", brushname,
            defaultBrushHexes.GetValueOrDefault(brushname, "#131313"));
        var hex = await ShowColorPickerDialog(current);
        if (hex != null && PLUSThemes.validhex(hex))
        {
            PLUSSavesystem.write_ini("Themes", brushname, hex);
            PLUSThemes.Set_BrushColor($"{brushname}Brush", hex);
            PLUSrefresh();
        }
    }

    private async Task<string?> ShowColorPickerDialog(string current)
    {
        if (!PLUSThemes.validhex(current)) current = "#000000";
        var initialColor = Color.TryParse(current, out var c) ? c : Colors.Black;
        var colorView = new ColorView
        {
            Color = initialColor,
            IsAlphaEnabled = false,
            IsAlphaVisible = false
        };
        var dialog = new Window
        {
            Title = "Pick a Color",
            Width = 400,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Children =
                {
                    colorView,
                    new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(8)
                    }
                }
            }
        };
        string? result = null;
        var okButton = ((StackPanel)dialog.Content).Children.OfType<Button>().First();
        okButton.Click += (_, _) =>
        {
            var col = colorView.Color;
            result = PLUSThemes.rgb_to_hex(col.R, col.G, col.B);
            dialog.Close();
        };
        await dialog.ShowDialog(this);
        return result;
    }

    public void Themes_Reset(string brushname)
    {
        PLUSSavesystem.delete_ini_value("Themes", brushname);
        PLUSThemes.Set_BrushColor($"{brushname}Brush", defaultBrushHexes.GetValueOrDefault(brushname, "#131313"));
    }

    private void ApplyTransparentBoxes(bool init = false)
    {
        foreach (var key in transparentboxes)
        {
            var value = PLUSSavesystem.read_ini("Themes", $"Transparency_{key}", "100");
            var slider = this.FindControl<Slider>($"Transparency_{key}");
            if (slider == null) continue;
            var parsed = double.TryParse(value, out var p) ? p : 100;
            if (init) slider.Value = parsed;
        }
        if (init) return;
        if (ConsoleWindow != null) ConsoleWindow.Opacity = GetTransparency("Logger");
        if (DescriptionWindow != null) DescriptionWindow.Opacity = GetTransparency("ModDescription");
        if (ModGridBorder != null) ModGridBorder.Opacity = GetTransparency("ModGrid");
    }

    private double GetTransparency(string key)
    {
        var value = PLUSSavesystem.read_ini("Themes", $"Transparency_{key}", "100");
        return double.TryParse(value, out var p) ? p / 100.0 : 1.0;
    }

    private async void ApplyBackgroundImage()
    {
        if (!Directory.Exists(CustomAssetsFolder)) return;
        var bgPath = Directory.GetFiles(CustomAssetsFolder)
            .FirstOrDefault(f =>
                Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase)
                && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(bgPath) || !File.Exists(bgPath))
        {
            if (this.TryFindResource("PrimaryBrush", out var res) && res is IBrush fallback)
                MainGrid.Background = fallback;
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(bgPath);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            MainGrid.Background = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
        }
        catch { }
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var key = btn.Name?.Replace("Themes", "").Replace("Reset", "") ?? "";
        Theme_Update(key);
    }

    private void ThemeReset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var key = btn.Name?.Replace("Themes", "").Replace("Reset", "") ?? "";
        Themes_Reset(key);
        PLUSrefresh();
    }

    private void Transparent_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is Slider slider)
        {
            PLUSSavesystem.write_ini("Themes", slider.Name, ((int)slider.Value).ToString());
            ApplyTransparentBoxes();
        }
    }

    private async void ThemesSave_Click(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Theme",
            DefaultExtension = "potheme",
            FileTypeChoices = new[] { new FilePickerFileType("PO Theme") { Patterns = new[] { "*.potheme" } } }
        });
        if (file == null) return;
        ThemesSaveFile(file.Path.LocalPath);
    }

    private async void ThemesLoad_Click(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Theme",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("PO Theme") { Patterns = new[] { "*.potheme" } } }
        });
        if (files.Count == 0) return;
        ThemesFileLoad(files[0].Path.LocalPath);
    }

    private void ThemesResetAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var brush in themebrushes)
            Themes_Reset(brush);

        if (Directory.Exists(CustomAssetsFolder))
        {
            var bgPath = Directory.GetFiles(CustomAssetsFolder)
                .FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase)
                    && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(bgPath) && File.Exists(bgPath))
                File.Delete(bgPath);
        }

        if (this.TryFindResource("PrimaryBrush", out var res) && res is IBrush fallback)
            MainGrid.Background = fallback;

        foreach (var t in transparentboxes)
            PLUSSavesystem.write_ini("Themes", $"Transparency_{t}", "100");
        ApplyTransparentBoxes(true);
        PLUSrefresh();
    }

    private void ThemePresetsApply_Click(object sender, RoutedEventArgs e)
    {
        var theme = ThemePresetsCombo.SelectedItem as string;
        if (string.IsNullOrEmpty(theme)) return;
        var filepath = Path.Combine(Global.assemblyLocation, "Themes", $"{theme}.potheme");
        if (File.Exists(filepath))
        {
            ThemesFileLoad(filepath);
            Global.logger.WriteLine($"Applied theme: {theme}", LoggerType.Info);
        }
        else
        {
            Global.logger.WriteLine($"Theme file missing at: {filepath}", LoggerType.Error);
        }
    }

    private async void ThemesBackgroundUpload_Click(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Background Image",
            AllowMultiple = false,
            FileTypeFilter = new[]
                { new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } } }
        });
        if (files.Count == 0) return;
        var src = files[0].Path.LocalPath;
        Directory.CreateDirectory(CustomAssetsFolder);
        File.Copy(src, Path.Combine(CustomAssetsFolder, $"background{Path.GetExtension(src)}"), true);
        PLUSrefresh();
        ApplyBackgroundImage();
    }

    private void ThemesBackgroundReset_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(CustomAssetsFolder))
        {
            var bgPath = Directory.GetFiles(CustomAssetsFolder)
                .FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase)
                    && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(bgPath) && File.Exists(bgPath))
                File.Delete(bgPath);
        }

        if (this.TryFindResource("PrimaryBrush", out var res) && res is IBrush fallback)
            MainGrid.Background = fallback;
        PLUSrefresh();
    }

    private void AssetsFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(CustomAssetsFolder);
        Process.Start(new ProcessStartInfo { FileName = CustomAssetsFolder, UseShellExecute = true });
    }

    private void ThemesSaveFile(string path)
    {
        var theme = new Dictionary<string, string>
        {
            ["saveversion"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0"
        };
        foreach (var brush in themebrushes)
            theme[brush] = PLUSSavesystem.read_ini("Themes", brush, defaultBrushHexes.GetValueOrDefault(brush, ""));

        theme["background"] = "";
        var bgPath = Directory.Exists(CustomAssetsFolder)
            ? Directory.GetFiles(CustomAssetsFolder)
                .FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase)
                    && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            : null;
        if (!string.IsNullOrEmpty(bgPath) && File.Exists(bgPath))
            theme["background"] = $"{Path.GetExtension(bgPath).TrimStart('.')};{PLUSThemes.Base64_SaveFile(bgPath)}";

        foreach (var t in transparentboxes)
            theme[$"Transparency_{t}"] = PLUSSavesystem.read_ini("Themes", $"Transparency_{t}", "100");

        File.WriteAllText(path, JsonSerializer.Serialize(theme, new JsonSerializerOptions { WriteIndented = true }));
        LoadThemePresets();
    }

    private void ThemesFileLoad(string path)
    {
        try
        {
            var theme = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
            if (theme == null) return;

            foreach (var brush in themebrushes)
                if (theme.TryGetValue(brush, out var val))
                {
                    PLUSSavesystem.write_ini("Themes", brush, val);
                    Theme_Update(brush, true);
                }

            var bgPath = Directory.Exists(CustomAssetsFolder)
                ? Directory.GetFiles(CustomAssetsFolder)
                    .FirstOrDefault(f =>
                        Path.GetFileNameWithoutExtension(f).Equals("background", StringComparison.OrdinalIgnoreCase)
                        && themeimageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                : null;
            if (!string.IsNullOrEmpty(bgPath) && File.Exists(bgPath))
                File.Delete(bgPath);

            if (theme.TryGetValue("background", out var bg) && bg.Contains(";"))
            {
                var parts = bg.Split(";");
                if (parts.Length == 2 && PLUSThemes.IsBase64String(parts[1]))
                    PLUSThemes.Base64_LoadFile(parts[1], Path.Combine(CustomAssetsFolder, $"background.{parts[0]}"));
            }

            foreach (var t in transparentboxes)
            {
                var val = theme.TryGetValue($"Transparency_{t}", out var tv) ? tv : "100";
                PLUSSavesystem.write_ini("Themes", $"Transparency_{t}", val);
            }

            ApplyTransparentBoxes(true);
            ApplyBackgroundImage();
            PLUSrefresh();
        }
        catch (Exception ex)
        {
            Global.logger.WriteLine($"Failed to load theme: {ex.Message}", LoggerType.Error);
        }
    }

    #endregion

    #region Assets & Audio

    private void RestoreMissingAssets_Click(object sender, RoutedEventArgs e)
    {
        Global.logger.WriteLine("Restoring assets...", LoggerType.Info);
        try
        {
            RestoreFolderFromResource("CustomAssets", Global.assemblyLocation);

            var srcThemes = Path.Combine(Global.appLocation, "Themes");
            var dstThemes = Path.Combine(Global.assemblyLocation, "Themes");
            if (Directory.Exists(srcThemes))
            {
                Directory.CreateDirectory(dstThemes);
                foreach (var f in Directory.GetFiles(srcThemes, "*.potheme"))
                {
                    var dst = Path.Combine(dstThemes, Path.GetFileName(f));
                    if (!File.Exists(dst)) File.Copy(f, dst);
                }
            }

            LoadThemePresets();
            ApplyBackgroundImage();
            Task.Run(async () => await PLUSMUSIC.InitializeAsync());
            Global.logger.WriteLine("Restoration complete.", LoggerType.Info);
        }
        catch (Exception ex)
        {
            Global.logger.WriteLine($"Restoration failed: {ex.Message}", LoggerType.Error);
        }
    }

    private void RestoreFolderFromResource(string folderName, string basePath)
    {
        string targetPath = Path.Combine(basePath, folderName);
        Directory.CreateDirectory(targetPath);

        var assembly = Assembly.GetExecutingAssembly();
        string prefix = $"PizzaOven.{folderName}.";

        foreach (var resource in assembly.GetManifestResourceNames().Where(r => r.StartsWith(prefix)))
        {
            string relativePath = resource.Substring(prefix.Length)
                .Replace('.', Path.DirectorySeparatorChar);

            int lastSep = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastSep != -1)
                relativePath = relativePath[..lastSep] + "." + relativePath[(lastSep + 1)..];

            string outputPath = Path.Combine(targetPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            if (File.Exists(outputPath)) continue;

            using var input = assembly.GetManifestResourceStream(resource)!;
            using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            input.CopyTo(output);
        }
    }

    private void RestoreALLAssets_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(Global.customassetsfolder))
        {
            foreach (var file in Directory.GetFiles(Global.customassetsfolder, "*", SearchOption.AllDirectories))
                try { File.Delete(file); } catch { }
            foreach (var dir in Directory.GetDirectories(Global.customassetsfolder, "*", SearchOption.AllDirectories))
                try
                {
                    if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                        Directory.Delete(dir);
                }
                catch { }
        }

        var dstThemes = Path.Combine(Global.assemblyLocation, "Themes");
        if (Directory.Exists(dstThemes))
            foreach (var file in Directory.GetFiles(dstThemes, "*.potheme"))
                try { File.Delete(file); } catch { }

        RestoreMissingAssets_Click(sender, e);
    }

    private void SoundVolume_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        PLUSSavesystem.write_ini("Audio", "SoundVolume", ((int)e.NewValue).ToString());
        PLUSMUSIC.ApplyCurrentVolume(true);
    }

    private void Mute_Click(object sender, RoutedEventArgs e) => HandlePLUStoggle("Audio", "Mute", false, "Mute");
    private void UnfocusedMute_Click(object sender, RoutedEventArgs e) => HandlePLUStoggle("Audio", "UnfocusedMute", true, "UnfocusedMute");

    #endregion
}