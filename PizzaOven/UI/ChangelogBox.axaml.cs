using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PizzaOven.UI;

public partial class ChangelogBox : Window
{
    public bool YesNo = false;
    public bool Skip  = false;

    public ChangelogBox(GameBananaItemUpdate update, string packageName,
                        string text, Uri? preview, bool skip = false)
    {
        InitializeComponent();

        if (preview != null)
        {
            _ = LoadImageAsync(preview);
            PreviewImage.IsVisible = true;
        }

        ChangesGrid.ItemsSource = update.Changes;
        Title          = $"{packageName} Changelog";
        VersionLabel.Text = $"Update: {update.Title} {update.Version}";
        Text.Text      = text;

        update.Text    = update.Text.Replace("<br>", "\n").Replace("&nbsp;", " ");
        var cleaned    = Regex.Replace(update.Text, "<.*?>", string.Empty);
        UpdateText.Text = cleaned;
        if (cleaned.Length == 0) UpdateText.IsVisible = false;

        if (skip) SkipButton.IsVisible = true;
    }

    private async Task LoadImageAsync(Uri uri)
    {
        try
        {
            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(uri);
            using var ms = new System.IO.MemoryStream(bytes);
            PreviewImage.Source = new Bitmap(ms);
        }
        catch { }
    }

    private void NoButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)  => Close();
    private void YesButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) { YesNo = true; Close(); }
    private void Skip_Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) { Skip = true; Close(); }
}
