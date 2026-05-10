using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.Threading;

namespace PizzaOven;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Unhandled exception hook (AppDomain-level, works cross-platform)
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var ex  = e.ExceptionObject as Exception;
                var msg = $"Unhandled exception:\n{ex?.Message}\n\nInner:\n{ex?.InnerException}" +
                          $"\n\nStack Trace:\n{ex?.StackTrace}";
                _ = ShowErrorAsync(msg);
            };

            RegistryConfig.InstallGBHandler();

            bool running = AlreadyRunning();
            string[] args = desktop.Args ?? [];

            if (!running)
            {
                var mw = new MainWindow();
                desktop.MainWindow = mw;
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
                mw.Show();
                // Check for self-updates after showing the window
                mw.Opened += async (_, _) =>
                {
                    if (args.Length == 0)
                        if (await AutoUpdater.CheckForPizzaOvenUpdate(new CancellationTokenSource()))
                            mw.Close();
                };
            }

            // 1-click install support
            if (args.Length > 1 && args[0] == "-download")
            {
                if (running && desktop.MainWindow == null)
                {
                    // Need a hidden window to pump the message loop
                    desktop.MainWindow = new MainWindow { IsVisible = false };
                }
                new ModDownloader().Download(args[1], running);
            }
            else if (running)
            {
                ShowWarningAndExit("Pizza Oven is already running", desktop);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static bool AlreadyRunning()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            foreach (var p in Process.GetProcesses())
                if (p.Id != current.Id &&
                    p.ProcessName == current.ProcessName &&
                    p.MainModule?.FileName == current.MainModule?.FileName)
                    return true;
        }
        catch { }
        return false;
    }

    private static async System.Threading.Tasks.Task ShowErrorAsync(string msg)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } w })
        {
            var box = new Avalonia.Controls.Window
            {
                Title = "Error",
                Width = 600, Height = 400,
                Content = new Avalonia.Controls.TextBox
                {
                    Text = msg,
                    IsReadOnly = true,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            };
            await box.ShowDialog(w);
        }
    }

    private static void ShowWarningAndExit(string msg,
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        // Quick synchronous dialog then shutdown
        var dlg = new Avalonia.Controls.Window
        {
            Title = "Warning",
            Width = 360, Height = 120,
            Content = new Avalonia.Controls.TextBlock { Text = msg, Margin = new Avalonia.Thickness(16) }
        };
        desktop.MainWindow = dlg;
        dlg.Opened += (_, _) => dlg.Close();
        desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
    }
}
