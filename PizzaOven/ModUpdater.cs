using Avalonia.Threading;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PizzaOven.UI;
using SharpCompress.Archives;

namespace PizzaOven;

public static class ModUpdater
{
    private static ProgressBox? _progressBox;
    private static int _updateCounter;

    public static async Task CheckForUpdatesAsync(string path, MainWindow main)
    {
        _updateCounter = 0;

        void Enable() => Dispatcher.UIThread.Post(() =>
        {
            main.ModGrid.IsEnabled             = true;
            main.ConfigButton.IsEnabled        = true;
            main.LaunchButton.IsEnabled        = true;
            main.ClearButton.IsEnabled         = true;
            main.UpdateButton.IsEnabled        = true;
            main.ModGridSearchButton.IsEnabled = true;
        });

        if (!Directory.Exists(path)) { Enable(); return; }

        var cts         = new CancellationTokenSource();
        var requestUrls = new Dictionary<string, List<string>>();
        var modList     = new Dictionary<string, List<string>>();
        var urlCounts   = new Dictionary<string, int>();

        foreach (var mod in Directory.GetDirectories(path).Where(x => File.Exists($"{x}/mod.json")))
        {
            Metadata? metadata;
            try { metadata = JsonSerializer.Deserialize<Metadata>(File.ReadAllText($"{mod}{Global.s}mod.json")); }
            catch (Exception e) { Global.logger.WriteLine($"Error reading metadata for {mod} ({e.Message})", LoggerType.Error); continue; }

            if (metadata?.homepage == null) continue;
            var url = CreateUri(metadata.homepage.ToString());
            if (url == null) continue;

            var modType = (char.ToUpper(url.Segments[1][0]) + url.Segments[1][1..^1]).TrimEnd('s');
            var modId   = url.Segments[2];

            if (!urlCounts.ContainsKey(modType)) urlCounts[modType] = 0;
            int idx = urlCounts[modType];

            modList.TryAdd(modType, new());
            modList[modType].Add(mod);

            requestUrls.TryAdd(modType, new() { $"https://gamebanana.com/apiv6/{modType}/Multi?_csvProperties=_sName,_aSubmitter,_aCategory,_aSuperCategory,_sProfileUrl,_sDescription,_bHasUpdates,_aLatestUpdates,_aFiles,_aPreviewMedia,_aAlternateFileSources,_tsDateUpdated&_csvRowIds=" });
            if (requestUrls[modType].Count == idx)
                requestUrls[modType].Add($"https://gamebanana.com/apiv6/{modType}/Multi?_csvProperties=_sName,_aSubmitter,_aCategory,_aSuperCategory,_sProfileUrl,_sDescription,_bHasUpdates,_aLatestUpdates,_aFiles,_aPreviewMedia,_aAlternateFileSources,_tsDateUpdated&_csvRowIds=");
            requestUrls[modType][idx] += $"{modId},";
            if (requestUrls[modType][idx].Length > 1990) urlCounts[modType]++;
        }

        // Trim trailing commas
        foreach (var key in requestUrls.Keys)
            for (int i = 0; i < requestUrls[key].Count; i++)
                if (requestUrls[key][i].EndsWith(','))
                    requestUrls[key][i] = requestUrls[key][i][..^1];

        if (requestUrls.Count == 0) { Global.logger.WriteLine("No updates available.", LoggerType.Info); Enable(); return; }

        var responses = new List<GameBananaAPIV4>();
        using (var http = new HttpClient())
        {
            foreach (var type in requestUrls)
                foreach (var reqUrl in type.Value)
                {
                    try
                    {
                        var json    = await http.GetStringAsync(reqUrl);
                        var partial = JsonSerializer.Deserialize<List<GameBananaAPIV4>>(json);
                        if (partial != null) responses.AddRange(partial);
                    }
                    catch (Exception e)
                    {
                        Global.logger.WriteLine($"{reqUrl} {e.Message}", LoggerType.Error);
                        Enable(); return;
                    }
                }
        }

        var flatMods = modList.SelectMany(t => t.Value).ToList();
        for (int i = 0; i < flatMods.Count; i++)
        {
            Metadata? meta;
            try { meta = JsonSerializer.Deserialize<Metadata>(File.ReadAllText($"{flatMods[i]}{Global.s}mod.json")); }
            catch (Exception e) { Global.logger.WriteLine($"Error reading metadata ({e.Message})", LoggerType.Error); continue; }
            if (meta == null) continue;
            await ModUpdateAsync(responses[i], flatMods[i], meta,
                new Progress<DownloadProgress>(ReportProgress),
                CancellationTokenSource.CreateLinkedTokenSource(cts.Token), main);
        }

        Global.logger.WriteLine(_updateCounter == 0 ? "No updates available." : "Done checking for updates!", LoggerType.Info);
        Enable();
        Dispatcher.UIThread.Post(() => main.Activate());
    }

    private static void ReportProgress(DownloadProgress p)
    {
        if (_progressBox == null) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (p.Percentage == 1) _progressBox.finished = true;
            _progressBox.Progress.Value    = p.Percentage * 100;
            _progressBox.StatusText.Text   = $"{Math.Round(p.Percentage * 100, 2)}% ({StringConverters.FormatSize(p.DownloadedBytes)} of {StringConverters.FormatSize(p.TotalBytes)})";
        });
    }

    private static async Task ModUpdateAsync(GameBananaAPIV4 item, string mod, Metadata metadata,
        Progress<DownloadProgress> progress, CancellationTokenSource cts, MainWindow main)
    {
        if (metadata.lastupdate == null)
        {
            metadata.lastupdate = item.HasUpdates == true ? item.Updates?[0].DateAdded : new DateTime(1970, 1, 1);
            File.WriteAllText($"{mod}{Global.s}mod.json",
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        if (item.HasUpdates != true || item.Updates == null || item.Updates.Length == 0) return;

        var update = item.Updates[0];
        if (DateTime.Compare((DateTime)metadata.lastupdate, update.DateAdded) >= 0) return;

        _updateCounter++;
        Global.logger.WriteLine($"An update is available for {Path.GetFileName(mod)}!", LoggerType.Info);

        bool doUpdate = false, skip = false;
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var box = new ChangelogBox(update, Path.GetFileName(mod),
                $"A new update is available for {Path.GetFileName(mod)}", item.Image, true);
            await box.ShowDialog(main);
            doUpdate = box.YesNo;
            skip     = box.Skip;
        });

        if (skip)
        {
            metadata.lastupdate = update.DateAdded;
            File.WriteAllText($"{mod}{Global.s}mod.json",
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
            Global.logger.WriteLine($"Skipped update for {Path.GetFileName(mod)}.", LoggerType.Info);
            return;
        }
        if (!doUpdate) { Global.logger.WriteLine($"Declined update for {Path.GetFileName(mod)}.", LoggerType.Info); return; }

        var files = item.Files ?? new();
        string? downloadUrl = null, fileName = null;

        if (files.Count > 1)
        {
            UpdateFileBox? fileBox = null;
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                fileBox = new UpdateFileBox(files, Path.GetFileName(mod));
                await fileBox.ShowDialog(main);
            });
            downloadUrl = fileBox?.chosenFileUrl;
            fileName    = fileBox?.chosenFileName;
            if (fileBox?.chosenFileDescription != null) { metadata.filedescription = fileBox.chosenFileDescription; SaveMeta(mod, metadata); }
        }
        else if (files.Count == 1)
        {
            downloadUrl = files[0].DownloadUrl;
            fileName    = files[0].FileName;
            metadata.filedescription = files[0].Description;
            SaveMeta(mod, metadata);
        }

        if (item.AlternateFileSources != null)
        {
            bool useAlt = false;
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // Simple yes/no dialog
                var dlg = BuildYesNoDialog(main,
                    $"Alternate file sources found for {Path.GetFileName(mod)}. Manually update?");
                await dlg.ShowDialog(main);
                useAlt = (bool?)dlg.Tag == true;
            });
            if (useAlt)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var altWin = new AltLinkWindow(item.AlternateFileSources, Path.GetFileName(mod),
                        "Pizza Tower", metadata.homepage?.AbsoluteUri ?? "", true);
                    await altWin.ShowDialog(main);
                });
                return;
            }
        }

        if (downloadUrl != null && fileName != null)
            await DownloadFileAsync(downloadUrl, fileName, mod, item, progress, cts);
        else
            Global.logger.WriteLine($"Cancelled update for {Path.GetFileName(mod)}", LoggerType.Info);
    }

    private static void SaveMeta(string mod, Metadata m) =>
        File.WriteAllText($"{mod}{Global.s}mod.json",
            JsonSerializer.Serialize(m, new JsonSerializerOptions { WriteIndented = true }));

    private static Avalonia.Controls.Window BuildYesNoDialog(MainWindow parent, string msg)
    {
        var win = new Avalonia.Controls.Window
        {
            Title = "Pizza Oven", Width = 400, Height = 130,
            Background = Avalonia.Media.Brushes.Black
        };
        var yes = new Avalonia.Controls.Button { Content = "Yes", Margin = new Avalonia.Thickness(0,0,8,0) };
        var no  = new Avalonia.Controls.Button { Content = "No" };
        yes.Click += (_, _) => { win.Tag = true;  win.Close(); };
        no.Click  += (_, _) => { win.Tag = false; win.Close(); };
        win.Content = new Avalonia.Controls.StackPanel
        {
            Margin = new Avalonia.Thickness(16), Spacing = 12,
            Children =
            {
                new Avalonia.Controls.TextBlock
                    { Text = msg, Foreground = Avalonia.Media.Brushes.White,
                      TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new Avalonia.Controls.StackPanel
                    { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { yes, no } }
            }
        };
        return win;
    }

    private static async Task DownloadFileAsync(string uri, string fileName, string mod,
        GameBananaAPIV4 item, Progress<DownloadProgress> progress, CancellationTokenSource cts)
    {
        var dlDir = Path.Combine(Global.assemblyLocation, "Downloads");
        var dest  = Path.Combine(dlDir, fileName);
        Directory.CreateDirectory(dlDir);

        try
        {
            if (File.Exists(dest)) File.Delete(dest);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _progressBox = new ProgressBox(cts);
                _progressBox.Progress.Value  = 0;
                _progressBox.finished        = false;
                _progressBox.Title           = "Download Progress";
                _progressBox.Show();
            });

            using var fs  = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None);
            using var http = new HttpClient();
            await http.DownloadAsync(uri, fs, fileName, progress, cts.Token);

            await Dispatcher.UIThread.InvokeAsync(() => _progressBox?.Close());

            ClearDirectory(mod);
            await ExtractFileAsync(fileName, mod, item);
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(dest)) File.Delete(dest);
            await Dispatcher.UIThread.InvokeAsync(() => { if (_progressBox != null) { _progressBox.finished = true; _progressBox.Close(); } });
        }
        catch (Exception e)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { if (_progressBox != null) { _progressBox.finished = true; _progressBox.Close(); } });
            Global.logger.WriteLine($"Error whilst downloading {fileName} ({e.Message})", LoggerType.Error);
        }
    }

    private static void ClearDirectory(string path)
    {
        var dir = new DirectoryInfo(path);
        foreach (var f in dir.GetFiles())
            if (f.Name != "mod.json") f.Delete();
        foreach (var d in dir.GetDirectories()) { ClearDirectory(d.FullName); d.Delete(); }
    }

    private static async Task ExtractFileAsync(string fileName, string output, GameBananaAPIV4 item)
    {
        await Task.Run(() =>
        {
            var src = Path.Combine(Global.assemblyLocation, "Downloads", fileName);
            if (!File.Exists(src)) return;
            try
            {
                using var archive = ArchiveFactory.OpenArchive(src);
                foreach (var entry in archive.Entries)
                    if (!entry.IsDirectory)
                        entry.WriteToDirectory(output,
                            new ExtractionOptions { ExtractFullPath = true, Overwrite = true });

                var metaPath = Path.Combine(output, "mod.json");
                if (File.Exists(metaPath))
                {
                    var meta = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(metaPath)) ?? new();
                    meta.submitter  = item.Owner.Name;
                    meta.description = item.Description;
                    meta.preview    = item.Image;
                    meta.homepage   = item.Link;
                    meta.avi        = item.Owner.Avatar;
                    meta.upic       = item.Owner.Upic;
                    meta.cat        = item.CategoryName;
                    meta.caticon    = item.Category.Icon;
                    meta.lastupdate = item.DateUpdated;
                    File.WriteAllText(metaPath,
                        JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (Exception e) { Global.logger.WriteLine($"Couldn't extract {fileName} ({e.Message})", LoggerType.Error); }
            File.Delete(src);
        });
    }

    private static Uri? CreateUri(string url)
    {
        if ((Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
             Uri.TryCreate("http://" + url, UriKind.Absolute, out uri)) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            uri.Segments.Length == 3 &&
            (uri.Host == "gamebanana.com" || uri.Host == "www.gamebanana.com"))
            return uri;
        return null;
    }
}
