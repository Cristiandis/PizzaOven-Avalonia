using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace PizzaOven;

public partial class PLUSAnnouncementWindow : Window
{
    public bool IsClosed { get; private set; }
    public PLUSRonnieAnimate announcewindowanimator;

    public class PLUSAnnouncement
    {
        public DateTime date { get; set; }
        public bool enabled { get; set; }
        public string message { get; set; }
        public string expression { get; set; }
        public bool shake { get; set; }
        public string url { get; set; }
    }

    public static async Task<PLUSAnnouncement?> GetLatestAnnouncementAsync()
    {
        const string url = "https://raw.githubusercontent.com/Cristiandis/PizzaOven-Avalonia/refs/heads/PO%2B/announcements.json";
        using var client = new HttpClient();
        var json = await client.GetStringAsync(url);
        return JsonSerializer.Deserialize<PLUSAnnouncement>(json);
    }

    public PLUSAnnouncementWindow(PLUSAnnouncement ann)
    {
        InitializeComponent();
        Closed += (s, e) => IsClosed = true;
        Opened += async (s, e) => await ShowAnnouncementAsync(ann);
    }

    private async Task ShowAnnouncementAsync(PLUSAnnouncement ann)
    {
        try
        {
            announcewindowanimator = new PLUSRonnieAnimate();
            announcewindowanimator.Initialize(this, 10, 50, 1.5);


            try { announcewindowanimator.SetExpression(ann.expression); }
            catch { }

            if (ann.shake)
                announcewindowanimator.ShakeVisual(5, 5);

            try
            {
                announcewindowanimator.MakeTextbox(
                    announcewindowanimator.GetX() + 110,
                    announcewindowanimator.GetY() + 25,
                    ann.message);
                double textboxHeight = PLUSRonnieAnimate.MeasureTextBlockHeight(ann.message);
                Height = 50 + 25 + textboxHeight + 80;
            }
            catch { }

            Width = 500;

            SizeChanged += (s, e) =>
            {
                if (announcewindowanimator?._overlayCanvas == null || IsClosed) return;
                announcewindowanimator._overlayCanvas.Width = Bounds.Width;
                announcewindowanimator._overlayCanvas.Height = Bounds.Height;
            };

            PLUSSavesystem.write_ini("Announcement", "lastshown",
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
        }
        catch
        {
            Close();
        }
    }
}