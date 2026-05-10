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
    public static string assemblyLocation =
        AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    public static ObservableCollection<Mod> ModList = new();

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
