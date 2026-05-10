using Avalonia.Threading;
using Onova;
using Onova.Models;
using Onova.Services;
using PizzaOven.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PizzaOven;

public class AutoUpdater
{
    private static ProgressBox? _progressBox;
    private static readonly HttpClient _client = new();

    public static async Task<bool> CheckForPizzaOvenUpdate(CancellationTokenSource cts)
    {
        var localVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        try
        {
            const string requestUrl =
                "https://api.gamebanana.com/Core/Item/Data?itemtype=Tool&itemid=12625" +
                "&fields=Updates().bSubmissionHasUpdates(),Updates().aGetLatestUpdates(),Files().aFiles()&return_keys=1";

            var response = JsonSerializer.Deserialize<GameBananaItem>(await _client.GetStringAsync(requestUrl));
            if (response?.HasUpdates != true) return false;

            var updates = response.Updates;
            if (updates == null || updates.Length == 0) return false;

            var match = Regex.Match(updates[0].Title, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]");
            if (!match.Success) return false;
            var onlineVersion = match.Value.Trim();

            if (!UpdateAvailable(onlineVersion, localVersion)) return false;

            bool doUpdate = false;
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var box = new ChangelogBox(updates[0], "Pizza Oven",
                    $"A new version of Pizza Oven is available (v{onlineVersion})!", null);
                await box.ShowDialog(GetMainWindow());
                doUpdate = box.YesNo;
            });

            if (!doUpdate) return false;

            var files = response.Files;
            if (files == null || files.Count == 0) return false;

            var downloadUrl = files.First().Value.DownloadUrl;
            var fileName    = files.First().Value.FileName;

            await DownloadPizzaOvenAsync(downloadUrl, fileName, onlineVersion,
                new Progress<DownloadProgress>(ReportProgress), cts);

            await ShowInfoAsync($"Finished downloading {fileName}!\nPizza Oven will now restart.");

            if (!Version.TryParse(onlineVersion, out var version))
            {
                await ShowInfoAsync($"Error parsing {onlineVersion}!\nCancelling update.");
                return false;
            }

            var updateManager = new UpdateManager(
                AssemblyMetadata.FromAssembly(Assembly.GetEntryAssembly()!,
                    Process.GetCurrentProcess().MainModule!.FileName!),
                new LocalPackageResolver(Path.Combine(Global.assemblyLocation, "Downloads", "PizzaOvenUpdate")),
                new ZipPackageExtractor());

            await updateManager.PrepareUpdateAsync(version);
            updateManager.LaunchUpdater(version);
            return true;
        }
        catch (Exception e)
        {
            Global.logger.WriteLine($"Unable to check for update... ({e.Message})", LoggerType.Error);
        }
        return false;
    }

    private static async Task DownloadPizzaOvenAsync(string uri, string fileName, string version,
        Progress<DownloadProgress> progress, CancellationTokenSource cts)
    {
        var updateDir = Path.Combine(Global.assemblyLocation, "Downloads", "PizzaOvenUpdate");
        Directory.CreateDirectory(updateDir);
        var dest = Path.Combine(updateDir, fileName);

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _progressBox = new ProgressBox(cts);
                _progressBox.Progress.Value = 0;
                _progressBox.StatusText.Text = $"Downloading {fileName}";
                _progressBox.Title = "Pizza Oven Update Progress";
                _progressBox.finished = false;
                _progressBox.Show();
            });

            using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None);
            await _client.DownloadAsync(uri, fs, fileName, progress, cts.Token);

            // Rename to versioned zip
            var renamedDest = Path.Combine(updateDir, $"{version}.zip");
            File.Move(dest, renamedDest, true);

            await Dispatcher.UIThread.InvokeAsync(() => _progressBox?.Close());
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(dest)) File.Delete(dest);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_progressBox != null) { _progressBox.finished = true; _progressBox.Close(); }
            });
        }
        catch (Exception e)
        {
            Global.logger.WriteLine($"Error downloading {fileName}: {e.Message}", LoggerType.Error);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_progressBox != null) { _progressBox.finished = true; _progressBox.Close(); }
            });
        }
    }

    private static void ReportProgress(DownloadProgress p)
    {
        if (_progressBox == null) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (p.Percentage == 1) _progressBox.finished = true;
            _progressBox.Progress.Value  = p.Percentage * 100;
            _progressBox.StatusText.Text = $"{Math.Round(p.Percentage * 100, 2)}% " +
                $"({StringConverters.FormatSize(p.DownloadedBytes)} of {StringConverters.FormatSize(p.TotalBytes)})";
        });
    }

    private static bool UpdateAvailable(string? online, string? local)
    {
        if (online == null || local == null) return false;
        var o = online.Split('.');
        var l = local.Split('.');
        int len = Math.Max(o.Length, l.Length);
        Array.Resize(ref o, len);
        Array.Resize(ref l, len);
        for (int i = 0; i < len; i++)
        {
            int ov = int.TryParse(o[i], out var x) ? x : 0;
            int lv = int.TryParse(l[i], out var y) ? y : 0;
            if (ov > lv) return true;
            if (ov < lv) return false;
        }
        return false;
    }

    private static Avalonia.Controls.Window? GetMainWindow() =>
        (Avalonia.Application.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    private static async Task ShowInfoAsync(string msg)
    {
        var win = GetMainWindow();
        if (win == null) return;
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dlg = new Avalonia.Controls.Window
            {
                Title = "Pizza Oven", Width = 400, Height = 130,
                Background = Avalonia.Media.Brushes.Black
            };
            var btn = new Avalonia.Controls.Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            btn.Click += (_, _) => dlg.Close();
            dlg.Content = new Avalonia.Controls.StackPanel
            {
                Margin = new Avalonia.Thickness(16), Spacing = 12,
                Children =
                {
                    new Avalonia.Controls.TextBlock { Text = msg, Foreground = Avalonia.Media.Brushes.White, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    btn
                }
            };
            await dlg.ShowDialog(win);
        });
    }
}
