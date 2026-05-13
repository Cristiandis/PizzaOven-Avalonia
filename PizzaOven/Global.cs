using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace PizzaOven;

public static class Global
{
    public static Config config = new();
    public static Logger logger = null!;
    public static char s = Path.DirectorySeparatorChar;
    public static string assemblyLocation = GetUserDataPath();
    public static string appLocation = AppDomain.CurrentDomain.BaseDirectory
    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    public static string customassetsfolder = Path.Combine(assemblyLocation, "CustomAssets");
    public static bool ronnietutorial = false;
    public static ObservableCollection<Mod> ModList = new();

    private static string GetUserDataPath()
    {
        if (OperatingSystem.IsWindows())
        {
            return AppDomain.CurrentDomain.BaseDirectory
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        else
        {
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var baseDir = string.IsNullOrEmpty(xdgDataHome)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "share")
                : xdgDataHome;
            var appDir = Path.Combine(baseDir, "pizzaoven");
            Directory.CreateDirectory(appDir);
            return appDir;
        }
    }

    public static void UpdateConfig()
    {
        string configString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        try
        {
            File.WriteAllText($@"{assemblyLocation}{s}Config.json", configString);
        }
        catch (Exception e)
        {
            logger.WriteLine($"Couldn't write Config.json ({e.Message})", LoggerType.Error);
        }
    }
}