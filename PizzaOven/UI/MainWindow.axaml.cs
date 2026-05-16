using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using PizzaOven.UI;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace PizzaOven;

public partial class MainWindow : Window
{
    private static bool selected;
    private static readonly Dictionary<TypeFilter, List<GameBananaCategory>> cats = new();

    private static readonly List<GameBananaCategory> All = new[]
    {
        new GameBananaCategory { Name = "All", ID = null }
    }.ToList();

    private static readonly List<GameBananaCategory> None = new[]
    {
        new GameBananaCategory { Name = "- - -", ID = null }
    }.ToList();

    private static int page = 1;

    private static bool filterSelect;
    private static bool searched;

    private static readonly List<string> FilterBoxList = new[] { "Featured", "Recent", "Popular" }.ToList();
    public static string PizzaTowerVersion = "1.1.280";

    private static readonly List<string> FilterBoxListWhenSearched =
        new[] { "Featured", "Recent", "Popular", "- - -" }.ToList();

    private readonly string defaultText =
        "No mod is currently selected. Pressing launch will start a vanilla Pizza Tower. " +
        "Start downloading and using mods in the Browse Mods tab on top. Only one mod can be selected at a time.";

    private readonly FileSystemWatcher ModsWatcher;

    private bool _isLoaded;
    private bool _updatingPageBox;

    public List<string> exes;
    private int imageCount;

    private int imageCounter;
    public string version;

    public MainWindow()
    {
        InitializeComponent();

        PLUSMUSIC.InitializeEngine();

        Activated += (s, e) => PLUSMUSIC.ApplyCurrentVolume(true);
        Deactivated += (s, e) => PLUSMUSIC.ApplyCurrentVolume(false);

        ModGrid.AddHandler(DragDrop.DragOverEvent, Add_Enter);
        var spinTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        double spinAngle = 0;
        spinTimer.Tick += (_, _) =>
        {
            spinAngle = (spinAngle + 4) % 360;
            if (LoadingBar.RenderTransform is RotateTransform rt)
                rt.Angle = spinAngle;
        };
        spinTimer.Start();
        Global.logger = new Logger(ConsoleWindow);
        Global.config = new Config();

        var PizzaOvenVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        version = PizzaOvenVersion.Substring(0, PizzaOvenVersion.LastIndexOf('.'));

        Global.logger.WriteLine($"Launched PizzaOven Mod Manager v{version}!", LoggerType.Info);

        if (File.Exists($@"{Global.assemblyLocation}{Global.s}Config.json"))
            try
            {
                var configString = File.ReadAllText($@"{Global.assemblyLocation}{Global.s}Config.json");
                Global.config = JsonSerializer.Deserialize<Config>(configString);
            }
            catch (Exception e)
            {
                Global.logger.WriteLine(e.Message, LoggerType.Error);
            }

        if (Global.config.Height != null && Global.config.Height >= MinHeight)
            Height = (double)Global.config.Height;
        if (Global.config.Width != null && Global.config.Width >= MinWidth)
            Width = (double)Global.config.Width;
        if (Global.config.Maximized)
            WindowState = WindowState.Maximized;
        if (Global.config.TopGridHeight != null)
            MainGrid.RowDefinitions[1].Height = new GridLength((double)Global.config.TopGridHeight, GridUnitType.Star);
        if (Global.config.BottomGridHeight != null)
            MainGrid.RowDefinitions[3].Height =
                new GridLength((double)Global.config.BottomGridHeight, GridUnitType.Star);
        if (Global.config.LeftGridWidth != null)
            MiddleGrid.ColumnDefinitions[0].Width =
                new GridLength((double)Global.config.LeftGridWidth, GridUnitType.Star);
        if (Global.config.RightGridWidth != null)
            MiddleGrid.ColumnDefinitions[2].Width =
                new GridLength((double)Global.config.RightGridWidth, GridUnitType.Star);

        if (Global.config.ModList == null)
            Global.config.ModList = new ObservableCollection<Mod>();
        Global.ModList = Global.config.ModList;

        Directory.CreateDirectory($@"{Global.assemblyLocation}{Global.s}Mods");

        ModsWatcher = new FileSystemWatcher($@"{Global.assemblyLocation}{Global.s}Mods");
        ModsWatcher.Created += OnModified;
        ModsWatcher.Deleted += OnModified;
        ModsWatcher.Renamed += OnModified;

        Refresh();
        SelectItem();

        ModsWatcher.EnableRaisingEvents = true;

        DescriptionWindow.Text = defaultText;

        var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://PizzaOven/Assets/PizzaOvenPreview.png")));
        Preview.Source = bitmap;
        PreviewBG.Source = null;

        Global.logger.WriteLine("Checking for updates...", LoggerType.Info);
        ModGrid.IsEnabled = false;
        ConfigButton.IsEnabled = false;
        LaunchButton.IsEnabled = false;
        ClearButton.IsEnabled = false;
        UpdateButton.IsEnabled = false;
        ModGridSearchButton.IsEnabled = false;

        _ = PLUSSavesystem.read_ini_bool("LowEnd", "ModUpdate", true)
            ? ModUpdater.CheckForUpdatesAsync($"{Global.assemblyLocation}{Global.s}Mods", this)
            : Task.CompletedTask;

        if (Global.config.ModsFolder == null)
            Opened += async (_, _) =>
            {
                if (await Setup.GameSetupAsync(this))
                {
                    LaunchButton.IsEnabled = true;
                }
                else
                {
                    LaunchButton.IsEnabled = false;
                    Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
                }
            };
        if (PLUSSavesystem.read_ini_bool("Discord", "RPC", true))
            POPRESENCE.Initialize();
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        InitSettingsPanels();
        InitThemes();
        InitToggles();
        PLUSRefreshFolders();

        Task.Run(() =>
        {
            try
            {
                var parse = PLUSSavesystem.read_ini("Announcement", "lastshown");
                PLUSAnnouncementWindow.PLUSAnnouncement? ann = null;
                try { ann = PLUSAnnouncementWindow.GetLatestAnnouncementAsync().GetAwaiter().GetResult(); }
                catch { return; }

                if (ann == null || !ann.enabled) return;
                if (parse != "")
                    if (DateTimeOffset.TryParse(parse, out var parsed) &&
                        parsed > ann.date.ToUniversalTime())
                        return;

                Dispatcher.UIThread.Post(() =>
                {
                    var announcementWindow = new PLUSAnnouncementWindow(ann);
                    announcementWindow.Show(this);
                });
            }
            catch { }
        });

        Task.Run(async () =>
        {
            try
            {
                if (!Directory.Exists(Global.customassetsfolder))
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        RestoreMissingAssets_Click(null, null));

                await PLUSMUSIC.InitializeAsync();
                PLUSMUSIC.StartMusicWatcher();
            }
            catch { }
        });
    }

    private void OnModified(object sender, FileSystemEventArgs e)
    {
        Refresh();
        Global.UpdateConfig();
        Dispatcher.UIThread.Post(() => Activate());
    }

    private async void SelectItem()
    {
        await Task.Run(() =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                var index = Global.ModList.ToList().FindIndex(mod => mod.enabled);
                if (index != -1)
                {
                    ModGrid.SelectedItem = ModGrid.Items[index];
                    ModGrid.ScrollIntoView(ModGrid.Items[index]);
                }
                else
                {
                    ModGrid.SelectedIndex = -1;
                    ShowMetadata(null);
                }
            });
        });
    }

    private async void Refresh()
    {
        var currentModDirectory = $@"{Global.assemblyLocation}{Global.s}Mods";
        var currentFolder = ModFolderCombo?.SelectedItem as string ?? "All";
        foreach (var mod in Directory.GetDirectories(currentModDirectory))
            if (Global.ModList.ToList().Where(x => x.name == Path.GetFileName(mod)).Count() == 0)
            {
                if (currentFolder != "All")
                {
                    var modFolder = PLUSSavesystem.read_ini("Folder", Path.GetFileName(mod), "All");
                    if (modFolder != currentFolder) continue;
                }
                var m = new Mod();
                m.name = Path.GetFileName(mod);
                m.enabled = false;
                Thread.Sleep(1000);
                if (File.Exists($"{mod}{Global.s}mod.json"))
                {
                    var metadataString = File.ReadAllText($"{mod}{Global.s}mod.json");
                    var metadata = JsonSerializer.Deserialize<Metadata>(metadataString);
                    m.preview = metadata.preview;
                }
                else
                {
                    m.preview = new Uri("avares://PizzaOven/Assets/PizzaOvenLogo.png");
                }

                Dispatcher.UIThread.Invoke(() => Global.ModList.Add(m));
                Global.logger.WriteLine($"Added {Path.GetFileName(mod)}", LoggerType.Info);
            }

        foreach (var mod in Global.ModList.ToList())
            if (!Directory.GetDirectories(currentModDirectory).ToList().Select(x => Path.GetFileName(x))
                    .Contains(mod.name))
            {
                Dispatcher.UIThread.Invoke(() => Global.ModList.Remove(mod));
                Global.logger.WriteLine($"Deleted {mod.name}", LoggerType.Info);
            }

        await Task.Run(() =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                ModGrid.ItemsSource = Global.ModList;
                if (ModGrid.Items.Count == 0)
                    DropBox.IsVisible = true;
                else
                    DropBox.IsVisible = false;
                Stats.Text =
                    $"{Global.ModList.Count} mods • {Directory.GetFiles($@"{Global.assemblyLocation}{Global.s}Mods", "*", SearchOption.AllDirectories).Length.ToString("N0")} files • " +
                    $"{StringConverters.FormatSize(new DirectoryInfo($@"{Global.assemblyLocation}{Global.s}Mods").GetDirectorySize())} • v{version}";
            });
        });
        Global.config.ModList = Global.ModList;
        Global.logger.WriteLine("Refreshed!", LoggerType.Info);
    }

    private async void Setup_Click(object sender, RoutedEventArgs e)
    {
        if (await Setup.GameSetupAsync(this))
            LaunchButton.IsEnabled = true;
    }

    private async void Launch_Click(object sender, RoutedEventArgs e)
    {
        if (Global.config.ModsFolder != null)
        {
            ModGrid.IsEnabled = false;
            ConfigButton.IsEnabled = false;
            LaunchButton.IsEnabled = false;
            ClearButton.IsEnabled = false;
            UpdateButton.IsEnabled = false;
            ModGridSearchButton.IsEnabled = false;
            Directory.CreateDirectory(Global.config.ModsFolder);
            Global.logger.WriteLine("Cooking mods for Pizza Tower", LoggerType.Info);
            if (!await Build(Global.config.ModsFolder))
            {
                Global.logger.WriteLine("Pizza Oven failed to cook the selected mod and will not launch the game",
                    LoggerType.Error);
                ModGrid.IsEnabled = true;
                ConfigButton.IsEnabled = true;
                LaunchButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
                UpdateButton.IsEnabled = true;
                ModGridSearchButton.IsEnabled = true;
                return;
            }

            ModGrid.IsEnabled = true;
            ConfigButton.IsEnabled = true;
            LaunchButton.IsEnabled = true;
            ClearButton.IsEnabled = true;
            UpdateButton.IsEnabled = true;
            ModGridSearchButton.IsEnabled = true;
        }
        else
        {
            Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
            return;
        }

        if (Global.config.Launcher != null && File.Exists(Global.config.Launcher))
        {
            var path = Global.config.Launcher;
            try
            {
                Global.UpdateConfig();
                Global.logger.WriteLine($"Launching {path}", LoggerType.Info);
                ProcessStartInfo ps;
                if (OperatingSystem.IsLinux())
                    ps = new ProcessStartInfo("steam")
                    {
                        Arguments = "steam://rungameid/2231450",
                        UseShellExecute = true
                    };
                else
                    ps = new ProcessStartInfo(path)
                    {
                        WorkingDirectory = Path.GetDirectoryName(Global.config.Launcher),
                        UseShellExecute = true,
                        Verb = "open",
                        Arguments = PLUSSavesystem.read_ini_bool("Launch", "Debug", false) ? "--debug" : ""
                    };
                Process.Start(ps);
            }       
            catch (Exception ex)
            {
                Global.logger.WriteLine($"Couldn't launch {path} ({ex.Message})", LoggerType.Error);
            }
        }
        else
        {
            Global.logger.WriteLine("Please click Setup before starting!", LoggerType.Warning);
        }
    }

    private void GameBanana_Click(object sender, PointerPressedEventArgs e)
    {
        var id = "7692";
        try
        {
            Process.Start(new ProcessStartInfo($"https://gamebanana.com/games/{id}") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Global.logger.WriteLine($"Couldn't open up GameBanana ({ex.Message})", LoggerType.Error);
        }
    }

    private void Window_Closing(object sender, WindowClosingEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            Global.config.Maximized = true;
        }
        else
        {
            Global.config.Height = Height;
            Global.config.Width = Width;
            Global.config.Maximized = false;
        }

        Global.config.TopGridHeight = MainGrid.RowDefinitions[1].Height.Value;
        Global.config.BottomGridHeight = MainGrid.RowDefinitions[3].Height.Value;
        Global.config.LeftGridWidth = MiddleGrid.ColumnDefinitions[0].Width.Value;
        Global.config.RightGridWidth = MiddleGrid.ColumnDefinitions[2].Width.Value;
        Global.UpdateConfig();
        POPRESENCE.Shutdown();
        PLUSMUSIC.Shutdown();
    }

    private void OnResize(object sender, SizeChangedEventArgs e)
    {
        BigScreenshot.MaxHeight = Bounds.Height - 240;
    }

    private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
    }

    private void OpenItem_Click(object sender, RoutedEventArgs e)
    {
        var selectedMods = ModGrid.SelectedItems;
        foreach (var row in selectedMods.OfType<Mod>())
        {
            var folderName = $@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{row.name}";
            if (Directory.Exists(folderName))
                try
                {
                    Process.Start(new ProcessStartInfo(folderName) { UseShellExecute = true });
                    Global.logger.WriteLine($@"Opened {folderName}.", LoggerType.Info);
                }
                catch (Exception ex)
                {
                    Global.logger.WriteLine($@"Couldn't open {folderName}. ({ex.Message})", LoggerType.Error);
                }
        }
    }

    private void EditItem_Click(object sender, RoutedEventArgs e)
    {
        var selectedMods = ModGrid.SelectedItems.OfType<Mod>().ToArray();
        ModsWatcher.EnableRaisingEvents = false;
        foreach (var row in selectedMods)
        {
            var ew = new EditWindow(row.name, true);
            ew.ShowDialog(this);
        }

        ModsWatcher.EnableRaisingEvents = true;
        Global.UpdateConfig();
        ModGrid.ItemsSource = null;
        ModGrid.ItemsSource = Global.ModList;
    }

    private void FetchItem_Click(object sender, RoutedEventArgs e)
    {
        var selectedMods = ModGrid.SelectedItems.OfType<Mod>().ToArray();
        ModsWatcher.EnableRaisingEvents = false;
        foreach (var row in selectedMods)
        {
            var fw = new FetchWindow(row);
            fw.ShowDialog(this);
            if (fw.success)
            {
                ShowMetadata(row.name);
                ModGrid.ItemsSource = null;
                ModGrid.ItemsSource = Global.ModList;
            }
        }

        ModsWatcher.EnableRaisingEvents = true;
    }

    private async void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        var selectedMods = ModGrid.SelectedItems.OfType<Mod>().ToArray();
        foreach (var row in selectedMods)
        {
            var confirmed =
                await ShowConfirmDialog($"Are you sure you want to delete {row.name}?\nThis cannot be undone.");
            if (confirmed)
                try
                {
                    await Task.Run(() =>
                        Directory.Delete($@"{Global.assemblyLocation}{Global.s}Mods{Global.s}{row.name}", true));
                    Global.logger.WriteLine($@"Deleting {row.name}.", LoggerType.Info);
                    ShowMetadata(null);
                }
                catch (Exception ex)
                {
                    Global.logger.WriteLine($@"Couldn't delete {row.name} ({ex.Message})", LoggerType.Error);
                }
        }
    }

    private async Task<bool> ShowConfirmDialog(string message)
    {
        var choices = new List<Choice>
        {
            new() { OptionText = "Yes", Index = 0 },
            new() { OptionText = "No", Index = 1 }
        };
        var dialog = new ChoiceWindow(choices, message);
        await dialog.ShowDialog(this);
        return dialog.choice == 0;
    }

    private async Task<bool> Build(string path)
    {
        return await Task.Run(async () =>
        {
            if (!ModLoader.Restart()) return false;
            var downgradeName = await Dispatcher.UIThread.InvokeAsync(() => DowngradeCombo.SelectedItem as string);
            if (!string.IsNullOrEmpty(downgradeName) && downgradeName != PizzaTowerVersion)
            {
                var patchPath = Path.Combine(Global.appLocation, "Downgrades", downgradeName + ".xdelta");
                if (!File.Exists(patchPath))
                {
                    Global.logger.WriteLine($"Downgrade patch not found: {patchPath}", LoggerType.Error);
                    return false;
                }

                if (!ModLoader.Downgrade(patchPath))
                    return false;
            }

            var mods = Global.config.ModList.Where(x => x.enabled).ToList();
            if (mods.Count == 0) return true;

            var modPaths = mods.Select(m =>
                $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{m.name}").ToArray();
            var gmloaderMods = modPaths.Where(ModLoader.IsGMLoaderMod).ToArray();
            var afomMods = modPaths.Where(ModLoader.IsAFOMMod).ToArray();
            var classicMods = modPaths.Where(p => !ModLoader.IsGMLoaderMod(p) && !ModLoader.IsAFOMMod(p)).ToArray();

            if (gmloaderMods.Length > 0 && classicMods.Length > 0)
            {
                Global.logger.WriteLine("Cannot mix GMLoader mods with other mod types.", LoggerType.Error);
                return false;
            }

            if (afomMods.Length > 1)
            {
                Global.logger.WriteLine("Only one AFOM level pack can be selected at a time.", LoggerType.Error);
                return false;
            }

            if (afomMods.Length == 1)
            {
                var afomMod = afomMods[0];
                var afomResult = await ModLoader.BuildAFOM(afomMod, async msg =>
                {
                    var result = false;
                    await Dispatcher.UIThread.InvokeAsync(async () => { result = await ShowConfirmDialog(msg); });
                    return result;
                });
                if (!afomResult) return false;
            }

            foreach (var mod in classicMods)
                if (!ModLoader.Build(mod))
                    return false;

            if (gmloaderMods.Length == 1)
                return ModLoader.BuildGMLoader(gmloaderMods[0]);
            if (gmloaderMods.Length > 1)
                return await ModLoader.BuildGMLoaderMultiple(gmloaderMods);

            return true;
        });
    }

    private void Add_Enter(object sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.Handled = true;
            e.DragEffects = DragDropEffects.Move;
            DropBox.IsVisible = true;
        }
    }

    private void Add_Leave(object sender, DragEventArgs e)
    {
        e.Handled = true;
        DropBox.IsVisible = false;
    }

    private async void Add_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        var ModsFolder = $"{Global.assemblyLocation}{Global.s}Mods";
        Directory.CreateDirectory(ModsFolder);
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            var fileList = files?.Select(f => f.Path.LocalPath).ToArray() ?? Array.Empty<string>();
            await Task.Run(() => ExtractPackages(fileList));
        }

        DropBox.IsVisible = false;
    }

    private void ExtractPackages(string[] fileList)
    {
        var temp = $"{Global.assemblyLocation}{Global.s}temp";
        var ModsFolder = $"{Global.assemblyLocation}{Global.s}Mods";
        foreach (var file in fileList)
        {
            Directory.CreateDirectory(temp);
            if (Directory.Exists(file))
            {
                var path = $@"{temp}{Global.s}{Path.GetFileName(file)}";
                var index = 2;
                while (Directory.Exists(path))
                {
                    path = $@"{temp}{Global.s}{Path.GetFileName(file)} ({index})";
                    index += 1;
                }

                MoveDirectory(file, path);
            }
            else if (Path.GetExtension(file).ToLower() == ".7z" || Path.GetExtension(file).ToLower() == ".rar" ||
                     Path.GetExtension(file).ToLower() == ".zip")
            {
                var _ArchiveSource = file;
                if (File.Exists(_ArchiveSource))
                {
                    try
                    {
                        using (var archive = ArchiveFactory.OpenArchive(_ArchiveSource))
                        {
                            Directory.CreateDirectory($"{temp}{Global.s}{Path.GetFileNameWithoutExtension(file)}");
                            foreach (var entry in archive.Entries)
                                if (!entry.IsDirectory)
                                    entry.WriteToDirectory(
                                        $"{temp}{Global.s}{Path.GetFileNameWithoutExtension(file)}",
                                        new ExtractionOptions
                                        {
                                            ExtractFullPath = true,
                                            Overwrite = true
                                        });
                        }
                    }
                    catch (Exception e)
                    {
                        Global.logger.WriteLine($"Couldn't extract {file}: {e.Message}", LoggerType.Error);
                    }

                    File.Delete(_ArchiveSource);
                }
            }

            foreach (var folder in Directory.GetDirectories(temp, "*", SearchOption.TopDirectoryOnly))
            {
                var path = $@"{ModsFolder}{Global.s}{Path.GetFileName(folder)}";
                var index = 2;
                while (Directory.Exists(path))
                {
                    path = $@"{ModsFolder}{Global.s}{Path.GetFileName(folder)} ({index})";
                    index += 1;
                }

                MoveDirectory(folder, path);
            }

            if (Directory.Exists(temp))
                Directory.Delete(temp, true);
        }
    }

    private static void MoveDirectory(string sourcePath, string targetPath)
    {
        foreach (var path in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            var newPath = path.Replace(sourcePath, targetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(path, newPath, true);
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        var temp = Global.ModList.ToList();
        temp.ForEach(mod => mod.enabled = false);
        Global.ModList = new ObservableCollection<Mod>(temp);
        ShowMetadata(null);
        Global.UpdateConfig();
        ModGrid.SelectedIndex = -1;
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        Global.logger.WriteLine("Checking for updates...", LoggerType.Info);
        ModGrid.IsEnabled = false;
        ConfigButton.IsEnabled = false;
        LaunchButton.IsEnabled = false;
        ClearButton.IsEnabled = false;
        UpdateButton.IsEnabled = false;
        ModGridSearchButton.IsEnabled = false;
        Dispatcher.UIThread.Invoke(async () =>
        {
            await ModUpdater.CheckForUpdatesAsync($"{Global.assemblyLocation}{Global.s}Mods", this);
        });
    }

    private async void ShowMetadata(string mod)
    {
        if (mod == null || !File.Exists($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mod}{Global.s}mod.json"))
        {
            DescriptionWindow.Text = defaultText;
            var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://PizzaOven/Assets/PizzaOvenPreview.png")));
            Preview.Source = bitmap;
            PreviewBG.Source = null;
        }
        else
        {
            var metadataString =
                File.ReadAllText($"{Global.assemblyLocation}{Global.s}Mods{Global.s}{mod}{Global.s}mod.json");
            var metadata = JsonSerializer.Deserialize<Metadata>(metadataString);

            var descText = "";
            if (metadata.submitter != null)
                descText += $"Submitter: {metadata.submitter}\n";
            descText += $"Category: {metadata.cat}\n";
            if (!string.IsNullOrEmpty(metadata.description))
                descText += $"Description: {metadata.description}\n\n";
            if (!string.IsNullOrEmpty(metadata.filedescription))
                descText += $"File Description: {metadata.filedescription}\n\n";
            if (metadata.homepage != null && metadata.homepage.ToString().Length > 0)
                descText += $"Home Page: {metadata.homepage}";

            DescriptionWindow.Text = descText;

            if (metadata.preview != null)
            {
                try
                {
                    Bitmap bitmap;
                    if (metadata.preview.IsFile)
                    {
                        bitmap = new Bitmap(metadata.preview.LocalPath);
                    }
                    else
                    {
                        using var http = new HttpClient();
                        var bytes = await http.GetByteArrayAsync(metadata.preview);
                        using var ms = new MemoryStream(bytes);
                        bitmap = new Bitmap(ms);
                    }

                    Preview.Source = bitmap;
                    PreviewBG.Source = bitmap;
                }
                catch
                {
                    var bitmap =
                        new Bitmap(AssetLoader.Open(new Uri("avares://PizzaOven/Assets/PizzaOvenPreview.png")));
                    Preview.Source = bitmap;
                    PreviewBG.Source = null;
                }
            }
            else
            {
                var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://PizzaOven/Assets/PizzaOvenPreview.png")));
                Preview.Source = bitmap;
                PreviewBG.Source = null;
            }
        }
    }

    private void ModGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var temp = Global.ModList.ToList();
        temp.ForEach(m => m.enabled = false);

        var selectedMods = ModGrid.SelectedItems.OfType<Mod>().ToList();
        foreach (var m in selectedMods)
        {
            var match = temp.FirstOrDefault(x => x.name == m.name);
            if (match != null)
                match.enabled = true;
        }

        Global.ModList = new ObservableCollection<Mod>(temp);
        Global.config.ModList = Global.ModList;

        if (selectedMods.Count > 0 && temp.Any(x => x.enabled))
            ShowMetadata(selectedMods.Last().name);
        else
            ShowMetadata(null);

        Global.UpdateConfig();
    }

    private void Download_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var item = button.DataContext as GameBananaRecord;
        new ModDownloader().BrowserDownload("Pizza Tower", item);
    }

    private void AltDownload_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var item = button.DataContext as GameBananaRecord;
        new AltLinkWindow(item.AlternateFileSources, item.Title, "Pizza Tower", item.Link.AbsoluteUri).ShowDialog(this);
    }

    private void Homepage_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var item = button.DataContext as GameBananaRecord;
        try
        {
            Process.Start(new ProcessStartInfo(item.Link.ToString()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Global.logger.WriteLine($"Couldn't open up {item.Link} ({ex.Message})", LoggerType.Error);
        }
    }

    private void MoreInfo_Click(object sender, RoutedEventArgs e)
    {
        HomepageButton.Content =
            $"{(TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim()?.TrimEnd('s')} Page";
        var button = sender as Button;
        var item = button.DataContext as GameBananaRecord;

        DownloadButton.IsVisible = item.Compatible;
        AltButton.IsVisible = item.HasAltLinks;

        DescPanel.DataContext = button.DataContext;
        MediaPanel.DataContext = button.DataContext;

        DescText.Text = item.ConvertedText;

        ImageLeft.IsEnabled = true;
        ImageRight.IsEnabled = true;
        BigImageLeft.IsEnabled = true;
        BigImageRight.IsEnabled = true;

        imageCount = item.Media.Where(x => x.Type == "image").ToList().Count;
        imageCounter = 0;

        if (imageCount > 0)
        {
            Grid.SetColumnSpan(DescTextScroller, 1);
            ImagePanel.IsVisible = true;
            LoadImage(item, imageCounter);
        }
        else
        {
            Grid.SetColumnSpan(DescTextScroller, 2);
            ImagePanel.IsVisible = false;
        }

        if (imageCount == 1)
        {
            ImageLeft.IsEnabled = false;
            ImageRight.IsEnabled = false;
            BigImageLeft.IsEnabled = false;
            BigImageRight.IsEnabled = false;
        }

        DescPanel.IsVisible = true;
    }

    private async void LoadImage(GameBananaRecord item, int idx)
    {
        try
        {
            var uri = new Uri($"{item.Media[idx].Base}/{item.Media[idx].File}");
            CaptionText.Text = item.Media[idx].Caption;
            BigCaptionText.Text = item.Media[idx].Caption;
            CaptionText.IsVisible = !string.IsNullOrEmpty(CaptionText.Text);
            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(uri);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            Screenshot.Source = bitmap;
            BigScreenshot.Source = bitmap;
            BigCaptionText.IsVisible = !string.IsNullOrEmpty(BigCaptionText.Text);
        }
        catch
        {
        }
    }

    private void CloseDesc_Click(object sender, RoutedEventArgs e)
    {
        DescPanel.IsVisible = false;
    }

    private void CloseMedia_Click(object sender, RoutedEventArgs e)
    {
        MediaPanel.IsVisible = false;
    }

    private void Image_Click(object sender, PointerPressedEventArgs e)
    {
        MediaPanel.IsVisible = true;
    }

    private void ImageLeft_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var item = button.DataContext as GameBananaRecord;
        if (--imageCounter == -1) imageCounter = imageCount - 1;
        LoadImage(item, imageCounter);
    }

    private void ImageRight_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var item = button.DataContext as GameBananaRecord;
        if (++imageCounter == imageCount) imageCounter = 0;
        LoadImage(item, imageCounter);
    }

    private async void InitializeBrowser()
    {
        ErrorPanel.IsVisible = false;
        LoadingBar.IsVisible = true;

        try
        {
            await Task.Run(async () =>
            {
                using var httpClient = new HttpClient();
                var gameID = "7692";
                var types = new[] { "Mod", "Wip", "Sound" };
                var counter = 0;

                foreach (var type in types)
                {
                    try
                    {
                        var requestUrl =
                            $"https://gamebanana.com/apiv4/{type}Category/ByGame?_aGameRowIds[]={gameID}&_sRecordSchema=Custom&_csvProperties=_idRow,_sName,_sProfileUrl,_sIconUrl,_idParentCategoryRow&_nPerpage=50";

                        var responseMessage = await httpClient.GetAsync(requestUrl);
                        var responseString = await responseMessage.Content.ReadAsStringAsync();

                        responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                        var response = JsonSerializer.Deserialize<List<GameBananaCategory>>(responseString);

                        if (response != null) cats[(TypeFilter)counter] = response;
                    }
                    catch (Exception ex)
                    {
                        Global.logger.WriteLine($"Failed to load {type} categories: {ex.Message}", LoggerType.Error);
                    }

                    counter++;
                }
            });

            filterSelect = true;
            FilterBox.ItemsSource = FilterBoxList;

            if (cats.ContainsKey((TypeFilter)TypeBox.SelectedIndex))
                CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex]
                    .Where(x => x.RootID == 0).OrderBy(y => y.ID));
            else
                CatBox.ItemsSource = None;

            SubCatBox.ItemsSource = None;
            CatBox.SelectedIndex = 0;
            SubCatBox.SelectedIndex = 0;
            FilterBox.SelectedIndex = 1;
            filterSelect = false;

            RefreshFilter();
            selected = true;
        }
        catch (Exception ex)
        {
            LoadingBar.IsVisible = false;
            ErrorPanel.IsVisible = true;
            BrowserRefreshButton.IsVisible = true;
            BrowserMessage.Text = ex.Message;
        }
        finally
        {
            LoadingBar.IsVisible = false;
        }
    }

    private static string GetHttpErrorMessage(string message)
    {
        return Regex.Match(message, @"\d+").Value switch
        {
            "443" => "Your internet connection is down.",
            "500" or "503" or "504" => "GameBanana's servers are down.",
            _ => message
        };
    }

    private void DecrementPage(object sender, RoutedEventArgs e)
    {
        --page;
        RefreshFilter();
    }

    private void IncrementPage(object sender, RoutedEventArgs e)
    {
        ++page;
        RefreshFilter();
    }

    private void BrowserRefresh(object sender, RoutedEventArgs e)
    {
        if (!selected) InitializeBrowser();
        else RefreshFilter();
    }

    private async void RefreshFilter()
    {
        NSFWCheckbox.IsEnabled = false;
        SearchBar.IsEnabled = false;
        SearchButton.IsEnabled = false;
        FilterBox.IsEnabled = false;
        TypeBox.IsEnabled = false;
        CatBox.IsEnabled = false;
        SubCatBox.IsEnabled = false;
        PageLeft.IsEnabled = false;
        PageRight.IsEnabled = false;
        PageBox.IsEnabled = false;
        PerPageBox.IsEnabled = false;
        ClearCacheButton.IsEnabled = false;
        ErrorPanel.IsVisible = false;
        filterSelect = true;
        PageBox.SelectedItem = page;
        filterSelect = false;
        Page.Text = $"Page {page}";
        LoadingBar.IsVisible = true;
        FeedBox.IsVisible = false;

        var search = searched ? SearchBar.Text : null;
        await FeedGenerator.GetFeedAsync(page, (TypeFilter)TypeBox.SelectedIndex, (FeedFilter)FilterBox.SelectedIndex,
            (GameBananaCategory)CatBox.SelectedItem, (GameBananaCategory)SubCatBox.SelectedItem,
            PerPageBox.SelectedIndex < 0 ? 10 : (PerPageBox.SelectedIndex + 1) * 10, (bool)NSFWCheckbox.IsChecked,
            search);

        FeedBox.ItemsSource = FeedGenerator.CurrentFeed.Records;

        if (FeedGenerator.error)
        {
            LoadingBar.IsVisible = false;
            ErrorPanel.IsVisible = true;
            BrowserRefreshButton.IsVisible = true;
            if (FeedGenerator.exception.Message.Contains("JSON tokens"))
            {
                BrowserMessage.Text = "Uh oh! Pizza Oven failed to deserialize the GameBanana feed.";
                return;
            }

            BrowserMessage.Text = GetHttpErrorMessage(FeedGenerator.exception.Message);
            return;
        }

        PageRight.IsEnabled = page < FeedGenerator.CurrentFeed.TotalPages;
        PageLeft.IsEnabled = page != 1;

        if (FeedGenerator.CurrentFeed.Records?.Count > 0)
        {
            LoadingBar.IsVisible = false;
            FeedBox.ScrollIntoView(FeedBox.Items[0]);
            FeedBox.IsVisible = true;
        }
        else
        {
            ErrorPanel.IsVisible = true;
            BrowserRefreshButton.IsVisible = false;
            BrowserMessage.IsVisible = true;
            BrowserMessage.Text = "Pizza Oven couldn't find any mods.";
        }

        _updatingPageBox = true;
        PageBox.ItemsSource = Enumerable.Range(1, (int)FeedGenerator.CurrentFeed.TotalPages);
        PageBox.SelectedItem = page;
        _updatingPageBox = false;
        LoadingBar.IsVisible = false;

        CatBox.IsEnabled = true;
        SubCatBox.IsEnabled = true;
        TypeBox.IsEnabled = true;
        FilterBox.IsEnabled = true;
        PageBox.IsEnabled = true;
        PerPageBox.IsEnabled = true;
        SearchBar.IsEnabled = true;
        SearchButton.IsEnabled = true;
        NSFWCheckbox.IsEnabled = true;
        ClearCacheButton.IsEnabled = true;
    }

    private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded() && !filterSelect)
        {
            if (!searched || FilterBox.SelectedIndex != 3)
            {
                filterSelect = true;
                var temp = FilterBox.SelectedIndex;
                FilterBox.ItemsSource = FilterBoxList;
                FilterBox.SelectedIndex = temp;
                filterSelect = false;
            }

            SearchBar.Clear();
            searched = false;
            page = 1;
            RefreshFilter();
        }
    }

    private void PerPageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded() && !filterSelect)
        {
            page = 1;
            RefreshFilter();
        }
    }

    private void TypeFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeBox.SelectedIndex < 0 || !cats.ContainsKey((TypeFilter)TypeBox.SelectedIndex)) return;
        if (IsLoaded() && !filterSelect)
        {
            SearchBar.Clear();
            searched = false;
            filterSelect = true;
            if (!searched)
            {
                FilterBox.ItemsSource = FilterBoxList;
                FilterBox.SelectedIndex = 1;
            }

            if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0)
                    .OrderBy(y => y.ID));
            else
                CatBox.ItemsSource = None;
            CatBox.SelectedIndex = 0;
            var cat = (GameBananaCategory)CatBox.SelectedValue;
            if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex]
                    .Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
            else
                SubCatBox.ItemsSource = None;
            SubCatBox.SelectedIndex = 0;
            filterSelect = false;
            page = 1;
            RefreshFilter();
        }
    }

    private void MainFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded() && !filterSelect)
        {
            SearchBar.Clear();
            searched = false;
            filterSelect = true;
            if (!searched)
            {
                FilterBox.ItemsSource = FilterBoxList;
                FilterBox.SelectedIndex = 1;
            }

            var cat = (GameBananaCategory)CatBox.SelectedValue;
            if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex]
                    .Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
            else
                SubCatBox.ItemsSource = None;
            SubCatBox.SelectedIndex = 0;
            filterSelect = false;
            page = 1;
            RefreshFilter();
        }
    }

    private void SubFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!filterSelect && IsLoaded())
        {
            SearchBar.Clear();
            searched = false;
            page = 1;
            RefreshFilter();
        }
    }

    private void PageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!filterSelect && IsLoaded())
        {
            if (_updatingPageBox || PageBox.SelectedItem == null) return;
            page = (int)PageBox.SelectedItem;
            RefreshFilter();
        }
    }

    private void NSFWCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        if (!filterSelect && IsLoaded())
        {
            if (searched)
            {
                filterSelect = true;
                FilterBox.ItemsSource = FilterBoxList;
                FilterBox.SelectedIndex = 1;
                filterSelect = false;
            }

            SearchBar.Clear();
            searched = false;
            page = 1;
            RefreshFilter();
        }
    }

    private void ClearCache(object sender, RoutedEventArgs e)
    {
        searched = false;
        SearchBar.Clear();
        FeedGenerator.ClearCache();
        RefreshFilter();
    }

    private void Search()
    {
        if (!filterSelect && IsLoaded() && !string.IsNullOrWhiteSpace(SearchBar.Text))
        {
            filterSelect = true;
            FilterBox.ItemsSource = FilterBoxListWhenSearched;
            FilterBox.SelectedIndex = 3;
            NSFWCheckbox.IsChecked = true;
            if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                CatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0)
                    .OrderBy(y => y.ID));
            else
                CatBox.ItemsSource = None;
            CatBox.SelectedIndex = 0;
            var cat = (GameBananaCategory)CatBox.SelectedValue;
            if (cats[(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                SubCatBox.ItemsSource = All.Concat(cats[(TypeFilter)TypeBox.SelectedIndex]
                    .Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
            else
                SubCatBox.ItemsSource = None;
            SubCatBox.SelectedIndex = 0;
            filterSelect = false;
            searched = true;
            page = 1;
            RefreshFilter();
        }
    }

    private void SearchBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Search();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        Search();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (IsLoaded() && ModGridSearchButton.IsEnabled)
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                switch (e.Key)
                {
                    case Key.F:
                        ModGrid_SearchBar.Focus();
                        break;
                }
    }

    private void ModGrid_SearchBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ModGridSearch();
    }

    private void ModGridSearch()
    {
        if (!string.IsNullOrEmpty(ModGrid_SearchBar.Text) && ModGridSearchButton.IsEnabled && Global.ModList.Count > 0)
        {
            var text = ModGrid_SearchBar.Text;
            Global.ModList = new ObservableCollection<Mod>(
                Global.ModList.Where(mod => mod.name.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                    .Concat(Global.ModList.Where(mod =>
                        !mod.name.Contains(text, StringComparison.InvariantCultureIgnoreCase))));
            Refresh();
            ModGrid.ScrollIntoView(ModGrid.Items[0]);
        }
    }

    private void ModGridSearchButton_Click(object sender, RoutedEventArgs e)
    {
        ModGridSearch();
    }

    private void Clear_PreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
    {
        ModGrid_SearchBar.Clear();
    }

    private bool IsLoaded()
    {
        return _isLoaded;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _isLoaded = true;
        PLUSrefresh();
    }

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] == ModBrowser)
            if (!selected)
                InitializeBrowser();
        if (e.AddedItems.Count > 0 && e.AddedItems[0] == PatchNotes)
            if (!selected)
                OnPatchNotesSelected();
    }

    private void PLUSrefresh()
    {
        var downgradePath = Path.Combine(Global.appLocation, "Downgrades");
        if (Directory.Exists(downgradePath))
        {
            var saved = DowngradeCombo.SelectedItem as string;
            var items = Directory.GetFiles(downgradePath)
                .Where(f => Path.GetFileName(f).ToLower().Contains("xdelta"))
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToList();
            items.Add(PizzaTowerVersion);
            DowngradeCombo.ItemsSource = items;
            DowngradeCombo.SelectedItem = items.FirstOrDefault(i =>
                string.Equals(i, saved, StringComparison.OrdinalIgnoreCase)) ?? PizzaTowerVersion;
        }

        var jsonPath = Path.Combine(Global.appLocation, "Dependencies", "ptversions.json");
        if (File.Exists(jsonPath))
            try
            {
                var versions = JsonSerializer.Deserialize<List<PTversion>>(File.ReadAllText(jsonPath));
                if (versions != null)
                {
                    DowngradeDownloadCombo.ItemsSource = versions.Select(v => v.version).ToList();
                    if (DowngradeDownloadCombo.SelectedIndex < 0)
                        DowngradeDownloadCombo.SelectedIndex = 0;
                }
            }
            catch
            {
            }

        LoadThemePresets();
        ApplyBackgroundImage();
        ApplyTransparentBoxes();

        Dispatcher.UIThread.Post(() =>
        {
            ModGrid.ItemsSource = Global.ModList;
            if (ModGrid.Items.Count > 0)
                ModGrid.ScrollIntoView(ModGrid.Items[0]);
        });
    }

    #region PatchNotes

    private void OnPatchNotesSelected()
    {
        CreatePatchNotes();
    }

    public async void CheckLauncherUpdates_Click(object sender, RoutedEventArgs e)
    {
        var cts = new CancellationTokenSource();
        await AutoUpdater.CheckForPizzaOvenUpdate(cts);
    }

    public void AddPatchNotes(string version, string[] topNotes, string[] notes, string[] catnotes, bool warnupdate,
        string timeago)
    {
        var localVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        var versionText = new TextBlock
        {
            Text = version,
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 10, 0, 5)
        };
        versionText.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextBrush"));

        var titlerowPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 2)
        };

        var timeagolabel = new Label
        {
            FontSize = 15,
            Background = new SolidColorBrush(Color.FromRgb(60, 64, 68)),
            Padding = new Thickness(5, 2, 5, 2),
            Margin = new Thickness(5, 2, 5, 2),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = Brushes.White
        };
        var timeagolabelcontentBinding = new Binding
        {
            Source = timeago,
            Mode = BindingMode.OneWay
        };
        timeagolabel.Bind(ContentProperty, timeagolabelcontentBinding);

        titlerowPanel.Children.Add(versionText);
        titlerowPanel.Children.Add(timeagolabel);

        if (warnupdate)
        {
            var warnlabel = new Label
            {
                FontSize = 15,
                Background = new SolidColorBrush(Color.FromRgb(60, 64, 68)),
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(5, 2, 5, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Brushes.Red
            };
            var warncontentBinding = new Binding
            {
                Source = $"v{localVersion.Substring(0, localVersion.LastIndexOf('.'))} is OUTDATED",
                Mode = BindingMode.OneWay
            };
            warnlabel.Bind(ContentProperty, warncontentBinding);
            titlerowPanel.Children.Add(warnlabel);
        }

        PatchNotesPanel.Children.Add(titlerowPanel);

        if (topNotes != null)
            foreach (var topNote in topNotes)
            {
                if (string.IsNullOrWhiteSpace(topNote))
                    continue;

                var topNoteText = new TextBlock
                {
                    Text = topNote,
                    FontSize = 15,
                    FontStyle = FontStyle.Italic,
                    Margin = new Thickness(5, 0, 0, 6)
                };
                topNoteText.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextBrush"));


                PatchNotesPanel.Children.Add(topNoteText);
            }

        if (notes != null)
            for (var i = 0; i < notes.Length; i++)
            {
                var noteText = new TextBlock
                {
                    Text = "• " + notes[i],
                    FontSize = 14,
                    Margin = new Thickness(10, 2, 0, 2)
                };
                noteText.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextBrush"));

                var catLabel = new Label
                {
                    FontSize = 15,
                    Background = new SolidColorBrush(Color.FromRgb(60, 64, 68)),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(5, 2, 5, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                var contentBinding = new Binding
                {
                    Source = catnotes[i],
                    Mode = BindingMode.OneWay
                };
                catLabel.Bind(ContentProperty, contentBinding);

                try
                {
                    var converter = new CategoryColorConverter();

                    var category = catnotes[i];
                    var brush = (SolidColorBrush)converter.Convert(category, typeof(SolidColorBrush), null,
                        CultureInfo.InvariantCulture);

                    catLabel.Foreground = brush;
                }
                catch
                {
                    catLabel.Foreground = Brushes.White;
                }

                var rowPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                rowPanel.Children.Add(noteText);
                rowPanel.Children.Add(catLabel);

                PatchNotesPanel.Children.Add(rowPanel);
            }
    }

    public async void CreatePatchNotes()
    {
        try
        {
            PatchNotesPanel.Children.Clear();
            var url =
                "https://api.gamebanana.com/Core/Item/Data?itemtype=Tool&itemid=22718&fields=Updates().bSubmissionHasUpdates(),Updates().aGetLatestUpdates()&return_keys=1";

            using var client = new HttpClient();
            var jsonResponse = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            if (!root.TryGetProperty("Updates().aGetLatestUpdates()", out var updatesArray))
                return;

            if (updatesArray.GetArrayLength() == 0)
            {
                AddPatchNotes("No updates available", new[] { "" }, new string[0], new string[0], false, "");
                return;
            }

            var latest = updatesArray[0];

            var versionTitle = latest.GetProperty("_sTitle").GetString() ?? "";


            var ts = latest.GetProperty("_tsDateAdded").GetInt64();

            var target = DateTimeOffset.FromUnixTimeSeconds(ts);
            var diff = DateTimeOffset.UtcNow - target;


            var timeago = StringConverters.FormatTimeAgo(diff);

            var changelog = latest.GetProperty("_aChangeLog");
            var notes = new string[changelog.GetArrayLength()];
            var catnotes = new string[changelog.GetArrayLength()];
            var i = 0;
            foreach (var entry in changelog.EnumerateArray())
            {
                var text = entry.GetProperty("text").GetString() ?? "";
                var cat = entry.GetProperty("cat").GetString() ?? "";
                catnotes[i] = cat;
                notes[i++] = text;
            }

            var versionNumber = latest.GetProperty("_sVersion").GetString() ?? "";
            var warnupdate = false;
            var localVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var onlineVersionMatch = Regex.Match(versionTitle, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]");
            string onlineVersion = null;

            if (onlineVersionMatch.Success)
            {
                onlineVersion = onlineVersionMatch.Value;
                warnupdate = AutoUpdater.UpdateAvailable(onlineVersion, localVersion);
            }


            var topNotes = new[] { "" };

            AddPatchNotes(versionTitle, topNotes, notes, catnotes, warnupdate, timeago);
        }
        catch
        {
            AddPatchNotes("Failed to load", new[] { "" },
                new[] { "Maybe Check your internet", "Maybe Gamebanana Servers are down" },
                new[] { "Addition", "Addition" }, false, "Failed to load");
        }
    }

    #endregion
}