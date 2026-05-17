using System;
using System.IO;
using System.Net.Http;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PizzaOven;

public partial class DownloadWindow : Window
{
    public bool YesNo;

    public DownloadWindow(GameBananaAPIV4 record)
    {
        InitializeComponent();
        DownloadText.Text = $"{record.Title}\nSubmitted by {record.Owner.Name}";
        SetPreview(record.Image);
    }

    public DownloadWindow(GameBananaRecord record)
    {
        InitializeComponent();
        DownloadText.Text = $"{record.Title}\nSubmitted by {record.Owner.Name}";
        SetPreview(record.Image);
    }

    private async void SetPreview(Uri? uri)
    {
        if (uri == null) return;
        try
        {
            if (uri.Scheme == "avares")
            {
                var assets = AssetLoader.Open(uri);
                Preview.Source = new Bitmap(assets);
            }
            else
            {
                using var http = new HttpClient();
                var bytes = await http.GetByteArrayAsync(uri);
                using var ms = new MemoryStream(bytes);
                Preview.Source = new Bitmap(ms);
            }
        }
        catch
        {
        }
    }

    private void Yes_Click(object? sender, RoutedEventArgs e)
    {
        YesNo = true;
        Close();
    }

    private void No_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}