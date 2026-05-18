using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Win32;
using MsBox.Avalonia;

namespace PizzaOven;

public class PTversion
{
    public string manifestID { get; set; }
    public string version { get; set; }
    public string type { get; set; }
}

public class PLUSDepotDownloader
{
    public static string GetSteamUsername()
    {
        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (key?.GetValue("AutoLoginUser") is string value && !string.IsNullOrEmpty(value))
                return value;
        }
        else
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share",
                "Steam", "config", "loginusers.vdf");
            if (File.Exists(path))
                try
                {
                    var currentAccount = "";
                    var firstFoundUser = "";
                    var isMostRecent = false;

                    foreach (var line in File.ReadLines(path))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;

                        var tokens = trimmed.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t))
                            .ToList();

                        if (tokens.Count < 2) continue;

                        var key = tokens[0];
                        var val = tokens[1];

                        if (key.Equals("AccountName", StringComparison.OrdinalIgnoreCase))
                        {
                            currentAccount = val;
                            if (string.IsNullOrEmpty(firstFoundUser))
                                firstFoundUser = val;

                            if (isMostRecent) return currentAccount;
                        }
                        else if (key.Equals("MostRecent", StringComparison.OrdinalIgnoreCase) && val == "1")
                        {
                            isMostRecent = true;
                            if (!string.IsNullOrEmpty(currentAccount)) return currentAccount;
                        }


                        if (trimmed == "}")
                        {
                            currentAccount = "";
                            isMostRecent = false;
                        }
                    }


                    if (!string.IsNullOrEmpty(firstFoundUser))
                        return firstFoundUser;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PizzaOven] Error parsing loginusers.vdf: {ex.Message}");
                }
        }

        return "";
    }

    public static async Task DowngradeDownload(MainWindow mainWindow)
    {
        var ogWinFile = "";

        var topLevel = TopLevel.GetTopLevel(mainWindow);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Source File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Source (*.win)") { Patterns = new[] { "*.win" } }
            }
        });

        if (files != null && files.Count > 0)
        {
            ogWinFile = files[0].Path.LocalPath;
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "No file selected.");
            await box.ShowWindowDialogAsync(mainWindow);
            return;
        }

        if (string.IsNullOrEmpty(ogWinFile))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select a .win file first.");
            await box.ShowWindowDialogAsync(mainWindow);
            return;
        }

        var ptVersions =
            JsonSerializer.Deserialize<List<PTversion>>(
                File.ReadAllText(Path.Combine(Global.appLocation, "Dependencies", "ptversions.json")));

        var downloadCombo = mainWindow.FindControl<ComboBox>("DowngradeDownloadCombo");

        var selectedVersion = downloadCombo?.SelectedItem as string;

        if (string.IsNullOrEmpty(selectedVersion))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select a version.");
            await box.ShowWindowDialogAsync(mainWindow);
            return;
        }

        var versionsDir = Path.Combine(Global.assemblyLocation, "Downgrades");
        var tempDir = Path.Combine(versionsDir, "temp");
        Directory.CreateDirectory(versionsDir);
        Directory.CreateDirectory(tempDir);

        var steamUser = GetSteamUsername();
        foreach (var v in ptVersions)
        {
            if (v.version != selectedVersion)
                continue;

            if (v.type == "depot")
            {
                var success = await DownloadDowngradeAsync(mainWindow, "2231450", "2231451", v.manifestID, steamUser,
                    tempDir, ogWinFile, v.version);

                if (!success)
                {
                    Console.WriteLine($"Failed to process version {v.version}");
                    continue;
                }

                try
                {
                    var sourceFile = Path.Combine(tempDir, "data.win");
                    var destFile = Path.Combine(versionsDir, $"{v.version}.win");
                    if (File.Exists(sourceFile))
                        File.Move(sourceFile, destFile, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error moving file for version {v.version}: {ex.Message}");
                }

                Console.WriteLine($"Version {v.version} processed successfully.");
                break;
            }
        }

        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
        catch
        {
        }
    }

    public static void CreatePatch(string sourceFile, string targetFile, string patchFile, string xdelta)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = xdelta,
            Arguments = $@"-e -s ""{sourceFile}"" ""{targetFile}"" ""{patchFile}""",
            WorkingDirectory = Path.GetDirectoryName(xdelta),
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();
    }

    public static async Task<bool> DownloadDowngradeAsync(Window parentWindow, string appID, string depotID,
        string manifestID, string username, string outputDir, string ogWinFile, string version)
    {
        string xdelta;
        string depotDownloaderPath;

        if (OperatingSystem.IsWindows())
        {
            xdelta = Path.Combine(Global.appLocation, "Dependencies", "xdelta.exe");
            depotDownloaderPath = Path.Combine(Global.appLocation, "Dependencies", "DepotDownloader-windows",
                "DepotDownloader.exe");
        }
        else
        {
            xdelta = Path.Combine(Global.appLocation, "Dependencies", "xdelta3");
            depotDownloaderPath = Path.Combine(Global.appLocation, "Dependencies", "DepotDownloader-linux",
                "DepotDownloader");

            try
            {
                if (File.Exists(depotDownloaderPath))
                    File.SetUnixFileMode(depotDownloaderPath,
                        UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch
            {
            }
        }

        if (OperatingSystem.IsWindows() && !File.Exists(depotDownloaderPath))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"{depotDownloaderPath} not found.");
            await box.ShowWindowDialogAsync(parentWindow);
            return false;
        }

        var password = "";
        var isConfirmed = false;

        var inputWindow = new Window
        {
            Title = "Steam Authentication",
            Width = 350,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SizeToContent = SizeToContent.WidthAndHeight
        };

        var lbl = new TextBlock
            { Text = $"Enter Steam password for '{username}':", Margin = new Thickness(0, 0, 0, 8) };
        var txt = new TextBox { PasswordChar = '*', Width = 300, HorizontalAlignment = HorizontalAlignment.Center };

        var btnOk = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
        var btnCancel = new Button { Content = "Cancel", Width = 80 };

        btnOk.Click += (_, _) =>
        {
            isConfirmed = true;
            inputWindow.Close();
        };
        btnCancel.Click += (_, _) =>
        {
            isConfirmed = false;
            inputWindow.Close();
        };

        var btnStack = new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        btnStack.Children.Add(btnOk);
        btnStack.Children.Add(btnCancel);

        var mainStack = new StackPanel { Margin = new Thickness(20) };
        mainStack.Children.Add(lbl);
        mainStack.Children.Add(txt);
        mainStack.Children.Add(btnStack);

        inputWindow.Content = mainStack;

        await inputWindow.ShowDialog(parentWindow);

        if (isConfirmed)
            password = txt.Text ?? "";
        else
            return false;

        if (string.IsNullOrEmpty(password))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Password cannot be empty.");
            await box.ShowWindowDialogAsync(parentWindow);
            return false;
        }

        var args =
            $@"-app {appID} -depot {depotID} -manifest {manifestID} -username ""{username}"" -password ""{password}"" -remember-password -dir ""{outputDir}""";

        var startInfo = new ProcessStartInfo
        {
            FileName = depotDownloaderPath,
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(depotDownloaderPath),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };

            var consoleWindow = parentWindow.FindControl<TextBox>("ConsoleWindow");

            if (consoleWindow != null) Dispatcher.UIThread.Post(() => consoleWindow.Text = "");

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data) && consoleWindow != null)
                    Dispatcher.UIThread.Post(() =>
                    {
                        consoleWindow.Text += e.Data + Environment.NewLine;
                        consoleWindow.CaretIndex = consoleWindow.Text.Length;
                    });
                if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data) && consoleWindow != null)
                    Dispatcher.UIThread.Post(() =>
                    {
                        consoleWindow.Text += $"[ERROR] {e.Data}" + Environment.NewLine;
                        consoleWindow.CaretIndex = consoleWindow.Text.Length;
                    });
                if (!string.IsNullOrEmpty(e.Data)) Console.Error.WriteLine(e.Data);
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not download depot {manifestID}");
                await box.ShowWindowDialogAsync(parentWindow);
                return false;
            }

            var tempSource = Path.Combine(outputDir, "source.win");
            var tempTarget = Path.Combine(outputDir, "data.win");
            var patchFile = Path.Combine(Global.assemblyLocation, "Downgrades", $"{version}.xdelta");

            File.Copy(ogWinFile, tempSource, true);

            if (File.Exists(tempTarget))
            {
                CreatePatch(tempSource, tempTarget, patchFile, xdelta);
                File.Delete(tempTarget);
            }
            else
            {
                Console.WriteLine($"Warning: {tempTarget} not found.");
            }

            try
            {
                var tempDepotDir = Path.Combine(outputDir, ".DepotDownloader");
                if (Directory.Exists(tempDepotDir))
                    Directory.Delete(tempDepotDir, true);
            }
            catch
            {
            }

            return true;
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Error running DepotDownloader:\n{ex.Message}");
            await box.ShowWindowDialogAsync(parentWindow);
            return false;
        }
    }
}