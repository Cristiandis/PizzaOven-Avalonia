using Avalonia;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PizzaOven;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var srcThemes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
        var dstThemes = Path.Combine(Global.assemblyLocation, "Themes");
        if (Directory.Exists(srcThemes) && !Directory.Exists(dstThemes))
        {
            Directory.CreateDirectory(dstThemes);
            foreach (var f in Directory.GetFiles(srcThemes, "*.potheme"))
                File.Copy(f, Path.Combine(dstThemes, Path.GetFileName(f)));
        }
        
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
        {
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
                  .UsePlatformDetect()
                  .WithInterFont()
                  .LogToTrace();
}
