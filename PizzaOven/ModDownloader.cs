using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PizzaOven.UI;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace PizzaOven;

public class ModDownloader
{
    private readonly HttpClient _client = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _cancelled;
    private string? _dlId;
    private bool _downloadAll;
    private string? _fileDescription;
    private string? _fileName;
    private string? _modId;
    private string? _modType;
    private ProgressBox? _progressBox;
    private GameBananaAPIV4 _response = new();
    private string? _url;
    private string? _urlToArchive;

    private static Window? GetMainWindow()
    {
        return (Application.Current?.ApplicationLifetime as
            IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    public async void BrowserDownload(string game, GameBananaRecord record)
    {
        var doDownload = false;
        var downloadAsTower = false;

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dlg = new DownloadWindow(record);

            // Add Tower button if AFOM category
            if (record.CategoryName == "Towers/Levels CYOP/AFOM")
            {
                var towerBtn = new Button
                {
                    Content = "Yes (Download as Tower)",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(20, 5, 20, 5)
                };
                towerBtn.Click += dlg.YesTower_Click;
                dlg.DownloadGrid.RowDefinitions.Add(new RowDefinition(40, GridUnitType.Pixel));
                Grid.SetRow(towerBtn, dlg.DownloadGrid.RowDefinitions.Count - 1);
                Grid.SetColumnSpan(towerBtn, 2);
                dlg.DownloadGrid.Children.Add(towerBtn);
            }

            await dlg.ShowDialog(GetMainWindow()!);
            doDownload = dlg.YesNo;
            downloadAsTower = dlg.Tower;
        });

        if (!doDownload) return;

        string? downloadUrl = null, fileName = null;
        if (record.AllFiles?.Count == 1)
        {
            downloadUrl = record.AllFiles[0].DownloadUrl;
            fileName = record.AllFiles[0].FileName;
            _fileDescription = record.AllFiles[0].Description;
        }
        else if (record.AllFiles?.Count > 1)
        {
            UpdateFileBox? fileBox = null;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                fileBox = new UpdateFileBox(record.AllFiles!, record.Title);
                await fileBox.ShowDialog(GetMainWindow()!);
            });

            _downloadAll = fileBox?.selectedDownloadAll ?? false;
            downloadUrl = fileBox?.chosenFileUrl;
            fileName = fileBox?.chosenFileName;
            _fileDescription = fileBox?.chosenFileDescription;
        }

        var filesToProcess = _downloadAll && record.AllFiles != null
            ? record.AllFiles.Select(f => (f.DownloadUrl, f.FileName, f.Description)).ToList()
            : downloadUrl != null && fileName != null
                ? new List<(string, string, string?)> { (downloadUrl, fileName, _fileDescription) }
                : new List<(string, string, string?)>();

        foreach (var (url, name, desc) in filesToProcess)
        {
            _cancelled = false;
            _fileDescription = desc;
            await DownloadFileAsync(url, name,
                new Progress<DownloadProgress>(ReportProgress),
                CancellationTokenSource.CreateLinkedTokenSource(_cts.Token));

            if (_cancelled) continue;

            if (downloadAsTower)
                await ExtractAsTowerAsync(name, record);
            else
                await ExtractFileAsync(name, record);
        }
    }

    public async void Download(string line, bool running)
    {
        if (ParseProtocol(line) && await GetDataAsync())
        {
            var doDownload = false;
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dlg = new DownloadWindow(_response);
                await dlg.ShowDialog(GetMainWindow()!);
                doDownload = dlg.YesNo;
            });
            if (doDownload && _fileName != null && _urlToArchive != null)
            {
                await DownloadFileAsync(_urlToArchive, _fileName,
                    new Progress<DownloadProgress>(ReportProgress),
                    CancellationTokenSource.CreateLinkedTokenSource(_cts.Token));
                if (!_cancelled)
                    await ExtractFileAsync(_fileName, _response);
            }
        }

        if (running) Environment.Exit(0);
    }

    private async Task<bool> GetDataAsync()
    {
        try
        {
            var json = await _client.GetStringAsync(_url);
            _response = JsonSerializer.Deserialize<GameBananaAPIV4>(json) ?? new GameBananaAPIV4();
            var file = _response.Files?.First(x => x.Id == _dlId);
            _fileName = file?.FileName;
            _fileDescription = file?.Description;
            return true;
        }
        catch (Exception e)
        {
            Global.logger.WriteLine($"Error while fetching data: {e.Message}", LoggerType.Error);
            return false;
        }
    }

    private void ReportProgress(DownloadProgress p)
    {
        if (_progressBox == null) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (p.Percentage == 1) _progressBox.finished = true;
            _progressBox.Progress.Value = p.Percentage * 100;
            _progressBox.StatusText.Text = $"{Math.Round(p.Percentage * 100, 2)}% " +
                                           $"({StringConverters.FormatSize(p.DownloadedBytes)} of {StringConverters.FormatSize(p.TotalBytes)})";
        });
    }

    private bool ParseProtocol(string line)
    {
        try
        {
            line = line.Replace("pizzaovenplus://", "").Replace("pizzaoven:", "").TrimStart('/');
            var data = line.Split(',');
            _urlToArchive = data[0];
            _dlId = Regex.Match(_urlToArchive, @"\d*$").Value;
            _modType = data[1];
            _modId = data[2];
            _url = $"https://gamebanana.com/apiv6/{_modType}/{_modId}" +
                   "?_csvProperties=_sName,_aGame,_sProfileUrl,_aPreviewMedia,_sDescription," +
                   "_aSubmitter,_aCategory,_aSuperCategory,_aFiles,_tsDateUpdated," +
                   "_aAlternateFileSources,_bHasUpdates,_aLatestUpdates";
            return true;
        }
        catch (Exception e)
        {
            Global.logger.WriteLine($"Error while parsing {line}: {e.Message}", LoggerType.Error);
            return false;
        }
    }

    private async Task ExtractFileAsync(string fileName, GameBananaRecord record)
    {
        await ExtractCoreAsync(fileName, record.Title,
            dest => !File.Exists(Path.Combine(dest, "mod.json")) ? BuildMeta(record) : null);
    }

    private async Task ExtractFileAsync(string fileName, GameBananaAPIV4 record)
    {
        await ExtractCoreAsync(fileName, record.Title,
            dest => !File.Exists(Path.Combine(dest, "mod.json")) ? BuildMeta(record) : null);
    }

    private Metadata BuildMeta(GameBananaRecord r)
    {
        return new Metadata
        {
            title = r.Title, submitter = r.Owner.Name, description = r.Description,
            filedescription = _fileDescription, preview = r.Image, homepage = r.Link,
            avi = r.Owner.Avatar, upic = r.Owner.Upic, cat = r.CategoryName,
            caticon = r.Category.Icon, lastupdate = r.DateUpdated
        };
    }

    private Metadata BuildMeta(GameBananaAPIV4 r)
    {
        return new Metadata
        {
            title = r.Title, submitter = r.Owner.Name, description = r.Description,
            filedescription = _fileDescription, preview = r.Image, homepage = r.Link,
            avi = r.Owner.Avatar, upic = r.Owner.Upic, cat = r.CategoryName,
            caticon = r.Category.Icon, lastupdate = r.DateUpdated
        };
    }

    private async Task ExtractCoreAsync(string fileName, string title, Func<string, Metadata?> metaFactory)
    {
        await Task.Run(() =>
        {
            var src = Path.Combine(Global.assemblyLocation, "Downloads", fileName);
            var dest = Path.Combine(Global.assemblyLocation, "Mods",
                string.Concat(title.Split(Path.GetInvalidFileNameChars())));
            var n = 2;
            while (Directory.Exists(dest))
                dest = Path.Combine(Global.assemblyLocation, "Mods",
                    $"{string.Concat(title.Split(Path.GetInvalidFileNameChars()))} ({n++})");

            if (!File.Exists(src)) return;
            try
            {
                using var archive = ArchiveFactory.OpenArchive(src);
                Directory.CreateDirectory(dest);
                foreach (var entry in archive.Entries)
                    if (!entry.IsDirectory)
                        entry.WriteToDirectory(dest,
                            new ExtractionOptions { ExtractFullPath = true, Overwrite = true });

                var meta = metaFactory(dest);
                if (meta != null)
                    File.WriteAllText(Path.Combine(dest, "mod.json"),
                        JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception e)
            {
                Global.logger.WriteLine($"Couldn't extract {fileName}: {e.Message}", LoggerType.Error);
            }

            if (!Directory.Exists(dest))
                Global.logger.WriteLine($"Didn't extract {fileName} — improper format?", LoggerType.Warning);
            else
                File.Delete(src);
        });
    }

    private async Task DownloadFileAsync(string uri, string fileName,
        Progress<DownloadProgress> progress, CancellationTokenSource cts)
    {
        var dlDir = Path.Combine(Global.assemblyLocation, "Downloads");
        var dest = Path.Combine(dlDir, fileName);
        Directory.CreateDirectory(dlDir);

        try
        {
            if (File.Exists(dest)) File.Delete(dest);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _progressBox = new ProgressBox(cts);
                _progressBox.Progress.Value = 0;
                _progressBox.finished = false;
                _progressBox.Title = "Download Progress";
                _progressBox.Show();
            });

            using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None);
            await _client.DownloadAsync(uri, fs, fileName, progress, cts.Token);

            await Dispatcher.UIThread.InvokeAsync(() => _progressBox?.Close());
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(dest)) File.Delete(dest);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_progressBox != null)
                {
                    _progressBox.finished = true;
                    _progressBox.Close();
                }
            });
            _cancelled = true;
        }
        catch (Exception e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_progressBox != null)
                {
                    _progressBox.finished = true;
                    _progressBox.Close();
                }
            });
            Global.logger.WriteLine($"Error whilst downloading {fileName}: {e.Message}", LoggerType.Error);
            _cancelled = true;
        }
    }

    private async Task ExtractAsTowerAsync(string fileName, GameBananaRecord record)
    {
        await Task.Run(() =>
        {
            var src = Path.Combine(Global.assemblyLocation, "Downloads", fileName);
            var tempPath = Path.Combine(Global.assemblyLocation, "AFOMTEMP");

            if (!File.Exists(src)) return;

            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

            try
            {
                using var archive = ArchiveFactory.OpenArchive(src);
                Directory.CreateDirectory(tempPath);
                foreach (var entry in archive.Entries)
                    if (!entry.IsDirectory)
                        entry.WriteToDirectory(tempPath,
                            new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
            }
            catch (Exception e)
            {
                Global.logger.WriteLine($"Couldn't extract {fileName}: {e.Message}", LoggerType.Error);
                return;
            }

            var realFolder = tempPath;
            foreach (var dir in Directory.GetDirectories(tempPath, "*", SearchOption.AllDirectories))
                if (Directory.Exists(Path.Combine(dir, "levels")))
                {
                    realFolder = dir;
                    break;
                }

            var towersPath = Path.Combine(Global.appdata, "PizzaTower_GM2", "towers");

            if (!Directory.Exists(towersPath))
            {
                Global.logger.WriteLine(
                    $"AFOM towers folder not found at {towersPath}. Launch AFOM at least once first.",
                    LoggerType.Error);
                if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
                return;
            }

            var basePath = Path.Combine(towersPath,
                string.Concat(record.Title.Split(Path.GetInvalidFileNameChars())));
            var finalPath = basePath;
            var counter = 2;
            while (Directory.Exists(finalPath))
                finalPath = $"{basePath} ({counter++})";

            Directory.CreateDirectory(finalPath);

            foreach (var dir in Directory.GetDirectories(realFolder, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dir.Replace(realFolder, finalPath));

            foreach (var file in Directory.GetFiles(realFolder, "*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(realFolder, finalPath), true);

            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

            File.Delete(src);
            Global.logger.WriteLine($"Downloaded tower to: {finalPath}", LoggerType.Info);
        });
    }
}