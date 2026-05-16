using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace PizzaOven;

public partial class PLUSAnnouncementWindow : Window
{
    public PLUSAnnouncementWindow(PLUSAnnouncement ann)
    {
        Closed += (s, e) => IsClosed = true;
        Title = "Announcement";
        Height = 300;
        Width = 500;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#353535"));
        CanResize = false;

        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = ann.message,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 16
                },
                new Button
                {
                    Content = "OK",
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
        };

        var okBtn = ((StackPanel)Content).Children
            .OfType<Button>().First();
        okBtn.Click += (_, _) => Close();

        Opened += (s, e) => ShowAnnouncement(ann);
    }

    public bool IsClosed { get; private set; }

    public static PLUSAnnouncement? GetLatestAnnouncement()
    {
        const string url = "https://raw.githubusercontent.com/SurfyCrescent97/PizzaOvenPLUS/main/announcements.json";
        using var client = new HttpClient();
        var json = client.GetStringAsync(url).GetAwaiter().GetResult();
        return JsonSerializer.Deserialize<PLUSAnnouncement>(json);
    }

    private void ShowAnnouncement(PLUSAnnouncement ann)
    {
        Global.logger.WriteLine($"[Announcement] {ann.message}", LoggerType.Info);

        PLUSSavesystem.write_ini("Announcement", "lastshown",
            DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));

        if (!string.IsNullOrEmpty(ann.url))
            Process.Start(
                new ProcessStartInfo(ann.url) { UseShellExecute = true });
    }

    public class PLUSAnnouncement
    {
        public DateTime date { get; set; }
        public bool enabled { get; set; }
        public string message { get; set; }
        public string expression { get; set; }
        public bool shake { get; set; }
        public string url { get; set; }
    }
}