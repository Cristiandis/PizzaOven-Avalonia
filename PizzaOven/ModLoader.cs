using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PizzaOven.UI;

namespace PizzaOven;

public static class ModLoader
{
    private static string version;

    private static Window GetMainWindow()
    {
        return (Application.Current?.ApplicationLifetime as
            IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    public static bool Restart()
    {
        RestoreDirectory(Global.config.ModsFolder);
        var banks = new List<string>(new[] { "master.bank", "master.strings.bank", "music.bank", "sfx.bank" });
        foreach (var file in Directory.GetFiles($"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop", "*",
                     SearchOption.AllDirectories))
            if (!banks.Contains(Path.GetFileName(file).ToLowerInvariant()))
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    if (e is UnauthorizedAccessException)
                        Global.logger.WriteLine(
                            $"Access denied when trying to delete {file}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven in administrator mode",
                            LoggerType.Error);
                    else
                        throw;
                    return false;
                }

        var dlls = new List<string>(new[]
        {
            "fmod.dll", "fmod-gamemaker.dll", "fmodstudio.dll", "gameframe_x64.dll", "steam_api.dll",
            "steam_api64.dll", "steamworks_x64.dll"
        });
        foreach (var file in Directory.GetFiles($"{Global.config.ModsFolder}", "*", SearchOption.TopDirectoryOnly))
            if ((Path.GetExtension(file).ToLowerInvariant() == ".dll" &&
                 !dlls.Contains(Path.GetFileName(file).ToLowerInvariant()))
                || Path.GetExtension(file).ToLowerInvariant() == ".mp4")
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    if (e is UnauthorizedAccessException)
                        Global.logger.WriteLine(
                            $"Access denied when trying to delete {file}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven in administrator mode",
                            LoggerType.Error);
                    else
                        throw;
                    return false;
                }

        foreach (var directory in Directory.GetDirectories(
                     $"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop"))
            try
            {
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                    Directory.Delete(directory, false);
            }
            catch (Exception e)
            {
                if (e is UnauthorizedAccessException)
                    Global.logger.WriteLine(
                        $"Access denied when trying to delete {directory}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven in administrator mode",
                        LoggerType.Error);
                else
                    throw;
                return false;
            }

        if (File.Exists($"{Global.config.ModsFolder}{Global.s}PizzaOven.win"))
            try
            {
                File.Delete($"{Global.config.ModsFolder}{Global.s}PizzaOven.win");
            }
            catch (Exception e)
            {
                if (e is UnauthorizedAccessException)
                    Global.logger.WriteLine(
                        $"Access denied when trying to delete {Global.config.ModsFolder}{Global.s}PizzaOven.win. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven in administrator mode",
                        LoggerType.Error);
                else
                    throw;
                return false;
            }

        CleanupGMLoader();

        return true;
    }

    // Detects whether a mod folder contains GMLoader-style structure
    public static bool IsGMLoaderMod(string modPath)
    {
        string[] gmLoaderFolders = { "audio", "code", "config", "csx", "lib", "room", "shader", "textures", "xdelta" };

        if (gmLoaderFolders.Any(f => Directory.Exists(Path.Combine(modPath, f))))
            return true;

        foreach (var subdir in Directory.GetDirectories(modPath, "*", SearchOption.AllDirectories))
            if (gmLoaderFolders.Any(f => Directory.Exists(Path.Combine(subdir, f))))
                return true;

        return false;
    }

    private static void CleanupGMLoader()
    {
        if (Global.config.ModsFolder == null) return;

        // Restore Original Files
        var pizzaTowerExe = Path.Combine(Global.config.ModsFolder, "PizzaTower.exe");
        var pizzaTowerBackup = Path.Combine(Global.config.ModsFolder, "PizzaTower_orig.exe");

        if (File.Exists(pizzaTowerBackup))
        {
            File.Move(pizzaTowerBackup, pizzaTowerExe, true);
            Global.logger.WriteLine("[GMLoader] Restored PizzaTower.exe", LoggerType.Info);
        }

        var iniPath = Path.Combine(Global.config.ModsFolder, "GMLoader.ini");
        if (File.Exists(iniPath))
        {
            var ini = File.ReadAllText(iniPath);
            ini = Regex.Replace(ini, @"GameExecutable=.*", "GameExecutable=PizzaTower.exe");
            File.WriteAllText(iniPath, ini);
        }

        var dataWin = Path.Combine(Global.config.ModsFolder, "data.win");
        var backupWin = Path.Combine(Global.config.ModsFolder, "backup.win");

        if (File.Exists(backupWin))
        {
            File.Copy(backupWin, dataWin, true);
            if (File.Exists(dataWin + ".po"))
                File.Delete(dataWin + ".po");
            Global.logger.WriteLine("[GMLoader] Restored vanilla data.win from backup.win", LoggerType.Info);
        }

        // Remove GMLoader runtime files
        var gmloaderSourceFolder = Path.Combine(Global.appLocation, "GMLOADER-windows");

        if (Directory.Exists(gmloaderSourceFolder))
            foreach (var file in Directory.GetFiles(gmloaderSourceFolder, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(gmloaderSourceFolder, file);
                var targetPath = Path.Combine(Global.config.ModsFolder, relativePath);
                try
                {
                    if (File.Exists(targetPath)) File.Delete(targetPath);
                }
                catch (Exception e)
                {
                    Global.logger.WriteLine($"[GMLoader] Failed to remove {relativePath}: {e.Message}",
                        LoggerType.Warning);
                }
            }

        // Remove the mods subfolder
        var gmModsDir = Path.Combine(Global.config.ModsFolder, "mods");
        if (Directory.Exists(gmModsDir))
            try
            {
                Directory.Delete(gmModsDir, true);
                Global.logger.WriteLine("[GMLoader] Cleaned up mods folder", LoggerType.Info);
            }
            catch (Exception e)
            {
                Global.logger.WriteLine($"[GMLoader] Failed to remove mods folder: {e.Message}", LoggerType.Warning);
            }
    }

    private static void CloneDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(sourceDir))
            CloneDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
    }

    public static bool BuildGMLoader(string mod)
    {
        var gmloaderSourceFolder = Path.Combine(Global.appLocation, "GMLOADER-windows");

        if (!Directory.Exists(gmloaderSourceFolder))
        {
            Global.logger.WriteLine($"GMLoader runtime not found at {gmloaderSourceFolder}", LoggerType.Error);
            return false;
        }

        var destinationFolder = Global.config.ModsFolder;

        try
        {
            // Copy GMLoader runtime files into game folder
            foreach (var file in Directory.GetFiles(gmloaderSourceFolder, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(gmloaderSourceFolder, file);
                var destinationPath = Path.Combine(destinationFolder, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(file, destinationPath, true);
                Global.logger.WriteLine($"[GMLoader] Copied: {relativePath}", LoggerType.Info);
            }

            string[] foldersToFind =
                { "audio", "code", "config", "csx", "lib", "room", "shader", "textures", "xdelta" };
            var currentPath = mod;
            var found = false;

            while (true)
            {
                if (foldersToFind.Any(f => Directory.Exists(Path.Combine(currentPath, f))))
                {
                    found = true;
                    break;
                }

                var subdirs = Directory.GetDirectories(currentPath);
                if (subdirs.Length == 0) break;
                currentPath = subdirs[0];
            }

            if (!found)
            {
                Global.logger.WriteLine($"No GMLoader mod folders found in {mod}", LoggerType.Error);
                return false;
            }

            // Copy mod files into game's mods subfolder
            var modsDestination = Path.Combine(destinationFolder, "mods");
            Directory.CreateDirectory(modsDestination);
            CloneDirectory(currentPath, modsDestination);

            // Swap PizzaTower.exe with GMLoader.exe so Steam launches GMLoader
            var pizzaTowerExe = Path.Combine(destinationFolder, "PizzaTower.exe");
            var gmLoaderExe = Path.Combine(destinationFolder, "GMLoader.exe");
            var pizzaTowerBackup = Path.Combine(destinationFolder, "PizzaTower_orig.exe");

            if (!File.Exists(gmLoaderExe))
            {
                Global.logger.WriteLine($"GMLoader executable not found at {gmLoaderExe}", LoggerType.Error);
                return false;
            }

            if (!File.Exists(pizzaTowerBackup))
                File.Move(pizzaTowerExe, pizzaTowerBackup);
            File.Copy(gmLoaderExe, pizzaTowerExe, true);

            // Update GMLoader.ini to launch the real exe
            var iniPath = Path.Combine(destinationFolder, "GMLoader.ini");
            if (File.Exists(iniPath))
            {
                var ini = File.ReadAllText(iniPath);
                ini = Regex.Replace(ini, @"GameExecutable=.*", "GameExecutable=PizzaTower_orig.exe");
                File.WriteAllText(iniPath, ini);
            }

            Global.logger.WriteLine("[GMLoader] Setup complete, ready for Steam launch", LoggerType.Info);
            return true;
        }
        catch (Exception ex)
        {
            Global.logger.WriteLine($"[GMLoader] Fatal error: {ex.Message}", LoggerType.Error);
            return false;
        }
    }

    public static async Task<bool> BuildGMLoaderMultiple(string[] modPaths)
    {
        var mergePath = Path.Combine(Global.appLocation, "GMLoaderMerge");
        if (Directory.Exists(mergePath))
            Directory.Delete(mergePath, true);
        Directory.CreateDirectory(mergePath);

        try
        {
            await GMLoader_MergeMods(modPaths, mergePath);
            return BuildGMLoader(mergePath);
        }
        finally
        {
            if (Directory.Exists(mergePath))
                Directory.Delete(mergePath, true);
        }
    }

    public static async Task GMLoader_MergeMods(string[] modpaths, string mergePath)
    {
        Directory.CreateDirectory(mergePath);
        string[] gmLoaderFolders = { "audio", "code", "lib", "config", "csx", "room", "shader", "texture", "xdelta" };

        for (var i = 0; i < modpaths.Length; i++)
        {
            string foundPath = null;

            if (gmLoaderFolders.Any(f => Directory.Exists(Path.Combine(modpaths[i], f))))
                foundPath = modpaths[i];
            else
                foreach (var subdir in Directory.GetDirectories(modpaths[i], "*", SearchOption.AllDirectories))
                    if (gmLoaderFolders.Any(f => Directory.Exists(Path.Combine(subdir, f))))
                    {
                        foundPath = subdir;
                        break;
                    }

            modpaths[i] = foundPath;
        }

        modpaths = modpaths.Where(p => p != null).ToArray();

        var fileMap = new Dictionary<string, List<string>>();

        foreach (var modRoot in modpaths)
        foreach (var file in Directory.GetFiles(modRoot, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(modRoot, file);
            if (!fileMap.ContainsKey(relativePath))
                fileMap[relativePath] = new List<string>();
            fileMap[relativePath].Add(file);
        }

        foreach (var kvp in fileMap)
        {
            var relativePath = kvp.Key;
            var paths = kvp.Value;

            var chosenPath = paths.Count > 1
                ? await AskConflictResolution(relativePath, paths)
                : paths[0];

            var destinationFile = Path.Combine(mergePath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(chosenPath, destinationFile, true);
        }
    }

    public static async Task<string> AskConflictResolution(string fileName, List<string> modPaths)
    {
        var selected = modPaths[0];

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var choices = modPaths.Select((p, i) => new Choice
            {
                OptionText = Path.GetFileName(Path.GetDirectoryName(p)) ?? p,
                OptionSubText = p,
                Index = i
            }).ToList();

            var dialog = new ChoiceWindow(choices, $"Conflict: {fileName}");
            await dialog.ShowDialog(GetMainWindow()!);

            if (dialog.choice.HasValue)
                selected = modPaths[dialog.choice.Value];
        });

        return selected;
    }

    public static bool Build(string mod)
    {
        var errors = 0;
        var successes = 0;
        var FilesToPatch = Directory.GetFiles($"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop").ToList();
        FilesToPatch.Insert(0, $"{Global.config.ModsFolder}{Global.s}data.win");
        FilesToPatch.Insert(1, $"{Global.config.ModsFolder}{Global.s}PizzaTower.exe");
        var xdelta = OperatingSystem.IsWindows()
            ? $"{Global.assemblyLocation}{Global.s}Dependencies{Global.s}xdelta.exe"
            : "xdelta3";

        if (OperatingSystem.IsLinux() && !File.Exists(xdelta) && !IsSystemXDelta3Available())
        {
            Global.logger.WriteLine("xdelta3 is not installed. Please install with your distro's package manager",
                LoggerType.Error);
            return false;
        }

        if (OperatingSystem.IsWindows() && !File.Exists(xdelta))
        {
            Global.logger.WriteLine($"{xdelta} is not found. Please try redownloading Pizza Oven", LoggerType.Error);
            return false;
        }

        foreach (var modFile in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
        {
            var extension = Path.GetExtension(modFile);
            try
            {
                if (extension.Equals(".xdelta", StringComparison.InvariantCultureIgnoreCase))
                {
                    WindowChecksum(modFile, xdelta);
                    var success = false;
                    var gotAccessDeniedError = false;
                    foreach (var file in FilesToPatch)
                    {
                        if (!File.Exists(file))
                        {
                            Global.logger.WriteLine($"{file} does not exist", LoggerType.Error);
                            continue;
                        }

                        try
                        {
                            Global.logger.WriteLine(
                                $"Attempting to patch {Path.GetFileName(file)} with {Path.GetFileName(modFile)}...",
                                LoggerType.Info);
                            Patch(file, modFile, $"{Path.GetDirectoryName(file)}{Global.s}temp", xdelta);
                            if (!File.Exists($"{file}.po"))
                                File.Copy(file, $"{file}.po", true);
                            File.Move($"{Path.GetDirectoryName(file)}{Global.s}temp", file, true);
                            Global.logger.WriteLine($"Applied {Path.GetFileName(modFile)} to {Path.GetFileName(file)}.",
                                LoggerType.Info);
                            successes++;
                            if (Path.GetFileName(modFile).ToLowerInvariant().Contains("yyc") &&
                                File.Exists($"{Global.config.ModsFolder}{Global.s}Steamworks_x64.dll"))
                                File.Move($"{Global.config.ModsFolder}{Global.s}Steamworks_x64.dll",
                                    $"{Global.config.ModsFolder}{Global.s}Steamworks_x64.dll.po", true);
                        }
                        catch (Exception e)
                        {
                            if (e is UnauthorizedAccessException)
                            {
                                Global.logger.WriteLine(
                                    $"Access denied when trying to patch {Path.GetFileName(file)} with {Path.GetFileName(modFile)}",
                                    LoggerType.Warning);
                                gotAccessDeniedError = true;
                                break;
                            }

                            Global.logger.WriteLine(
                                $"Unable to patch {Path.GetFileName(file)} with {Path.GetFileName(modFile)}",
                                LoggerType.Warning);
                            continue;
                        }

                        success = true;
                        break;
                    }

                    if (!success)
                    {
                        if (gotAccessDeniedError)
                            Global.logger.WriteLine(
                                $"{Path.GetFileName(modFile)} got an access denied error while patching a file. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven in administrator mode",
                                LoggerType.Error);
                        else
                            Global.logger.WriteLine(
                                $"{Path.GetFileName(modFile)} wasn't able to patch any file. Ensure that either the mod or your game version is up to date. {Path.GetFileName(modFile)} is intended for {version}. " +
                                $"If this version number matches with your current game version go to {Global.config.ModsFolder} and delete data.win.po and anything else with a .po extension then verify integrity of game files and try again.",
                                LoggerType.Error);
                        errors++;
                    }
                }
                else if (extension.Equals(".txt", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (File.ReadAllText(modFile).Contains("lang = ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        File.Copy(modFile,
                            $"{Global.config.ModsFolder}{Global.s}lang{Global.s}{Path.GetFileName(modFile)}", true);
                        Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to language folder",
                            LoggerType.Info);
                        successes++;
                    }
                }
                else if (extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (modFile.Contains("fonts", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Directory.CreateDirectory($"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts");
                        File.Copy(modFile,
                            $"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts{Global.s}{Path.GetFileName(modFile)}",
                            true);
                        Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to fonts folder",
                            LoggerType.Info);
                        successes++;
                    }
                }
                else if (extension.Equals(".win", StringComparison.InvariantCultureIgnoreCase))
                {
                    var dataWin = $"{Global.config.ModsFolder}{Global.s}data.win";
                    if (!File.Exists($"{dataWin}.po"))
                        File.Copy(dataWin, $"{dataWin}.po", true);
                    File.Copy(modFile, dataWin, true);
                    Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to use instead of data.win",
                        LoggerType.Info);
                    successes++;
                }
                else if (extension.Equals(".bank", StringComparison.InvariantCultureIgnoreCase))
                {
                    var FileToReplace =
                        $"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop{Global.s}{Path.GetFileName(modFile)}";
                    if (File.Exists(FileToReplace))
                    {
                        if (!File.Exists($"{FileToReplace}.po"))
                            File.Copy(FileToReplace, $"{FileToReplace}.po", true);
                        File.Copy(modFile, FileToReplace, true);
                        Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to use in sound folder",
                            LoggerType.Info);
                    }
                    else
                    {
                        var FileToAdd =
                            $"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop{Global.s}{Path.GetFileName(modFile)}";
                        if (!Path.GetFileName(Path.GetDirectoryName(modFile)).Equals(Path.GetFileName(mod),
                                StringComparison.InvariantCultureIgnoreCase))
                            FileToAdd =
                                $"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop{Global.s}{Path.GetFileName(Path.GetDirectoryName(modFile))}{Global.s}{Path.GetFileName(modFile)}";
                        Directory.CreateDirectory(Path.GetDirectoryName(FileToAdd)!);
                        File.Copy(modFile, FileToAdd, true);
                    }

                    successes++;
                }
                else if (extension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}{Path.GetFileName(modFile)}", true);
                    Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to game folder", LoggerType.Info);
                    successes++;
                }
                else if (extension.Equals(".mp4", StringComparison.InvariantCultureIgnoreCase))
                {
                    File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}{Path.GetFileName(modFile)}", true);
                    Global.logger.WriteLine($"Copied over {Path.GetFileName(modFile)} to game folder", LoggerType.Info);
                    successes++;
                }
            }
            catch (Exception e)
            {
                if (e is UnauthorizedAccessException)
                    Global.logger.WriteLine(
                        $"Access denied when trying to apply {Path.GetFileName(modFile)}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven in administrator mode",
                        LoggerType.Error);
                else
                    throw;
            }
        }

        if (successes == 0)
            Global.logger.WriteLine("No file was used from the current mod", LoggerType.Error);
        return errors == 0 && successes > 0;
    }

    private static void Patch(string file, string patch, string output, string xdelta)
    {
        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            FileName = xdelta,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = OperatingSystem.IsWindows() ? Path.GetDirectoryName(xdelta) : Global.assemblyLocation,
            Arguments = $@"-d -s ""{file}"" ""{patch}"" ""{output}"""
        };
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();
    }

    private static void RestoreDirectory(string path)
    {
        if (Directory.Exists(path))
            foreach (var file in Directory.GetFiles(path, "*.po", SearchOption.AllDirectories))
                try
                {
                    File.Move(file, file[..^3], true);
                }
                catch (Exception e)
                {
                    if (e is UnauthorizedAccessException)
                        Global.logger.WriteLine(
                            $"Access denied when trying to restore {Path.GetFileName(file)}. Try reinstalling Pizza Tower to a folder you have access to or running Pizza Oven in administrator mode",
                            LoggerType.Error);
                    else
                        throw;
                }
    }

    private static void WindowChecksum(string patch, string xdelta)
    {
        var vcdiffCopyWindowLength = 0;
        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            FileName = xdelta,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = OperatingSystem.IsWindows() ? Path.GetDirectoryName(xdelta) : Global.assemblyLocation,
            Arguments = $@"printhdr ""{patch}"""
        };

        using (var process = new Process { StartInfo = startInfo })
        {
            process.Start();
            string line;
            while ((line = process.StandardOutput.ReadLine()) != null)
                if (line.Contains("VCDIFF copy window length:"))
                {
                    var header = line.Split(':');
                    if (header.Length >= 2 && int.TryParse(header[1].Trim(), out var length))
                    {
                        vcdiffCopyWindowLength = length;
                        Global.logger.WriteLine($"Checksum window length for {patch}: {vcdiffCopyWindowLength}",
                            LoggerType.Info);
                    }

                    break;
                }

            process.WaitForExit();
        }

        try
        {
            string[] checksumLines = null;
            using (var stream = Assembly.GetEntryAssembly()
                       .GetManifestResourceStream("PizzaOven.Dependencies.XDelta_Common_Checksum.txt"))
            using (var reader = new StreamReader(stream))
            {
                checksumLines = EnumerateLines(reader).ToArray();
            }

            string prevLine = null;
            foreach (var checksumLine in checksumLines)
            {
                if (!string.IsNullOrEmpty(checksumLine) && checksumLine.Length >= 8)
                {
                    var checksumSubstring = checksumLine.Substring(0, 8);
                    if (int.TryParse(checksumSubstring, out var checksum) && checksum == vcdiffCopyWindowLength)
                    {
                        Global.logger.WriteLine($"Match found checksum: {vcdiffCopyWindowLength}", LoggerType.Info);
                        if (!string.IsNullOrEmpty(prevLine))
                        {
                            version = prevLine;
                            Global.logger.WriteLine($"Patch applies to Pizza Tower: {version}", LoggerType.Info);
                        }

                        return;
                    }
                }

                prevLine = checksumLine;
            }
        }
        catch (Exception ex)
        {
            Global.logger.WriteLine($"Error while checking checksum file, {ex.Message}", LoggerType.Error);
        }

        version = null;
    }

    private static IEnumerable<string> EnumerateLines(TextReader reader)
    {
        string line;
        while ((line = reader.ReadLine()) != null)
            yield return line;
    }

    private static bool IsSystemXDelta3Available()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo("xdelta3")
            {
                Arguments = "-V",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}