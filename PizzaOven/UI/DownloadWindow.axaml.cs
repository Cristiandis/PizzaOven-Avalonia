using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;

namespace PizzaOven;

public partial class DownloadWindow : Window
{
    public bool YesNo = false;

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
            using var http = new System.Net.Http.HttpClient();
            var bytes = await http.GetByteArrayAsync(uri);
            using var ms = new System.IO.MemoryStream(bytes);
            Preview.Source = new Bitmap(ms);
        }
        catch { }
    }

    private void Yes_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        YesNo = true;
        Close();
    }

    private void No_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
