using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PizzaOven;

public partial class FetchWindow : Window
{
    public bool success;
    private readonly Mod _mod;

    public FetchWindow(Mod mod)
    {
        InitializeComponent();
        _mod  = mod;
        Title = $"Fetch Metadata for {mod.name}";
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void ConfirmButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        _ = FetchAsync();

    private void UrlBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) _ = FetchAsync();
    }

    private Uri? ParseGameBananaUrl(string raw)
    {
        if ((Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
             Uri.TryCreate("http://" + raw, UriKind.Absolute, out uri)) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            uri.Segments.Length == 3 &&
            (uri.Host == "gamebanana.com" || uri.Host == "www.gamebanana.com"))
            return uri;
        return null;
    }

    private async Task FetchAsync()
    {
        var url = ParseGameBananaUrl(UrlBox.Text ?? "");
        if (url == null)
        {
            Global.logger.WriteLine(
                $"{UrlBox.Text} is invalid. Expected: https://gamebanana.com/<Type>/<ID>",
                LoggerType.Error);
            return;
        }

        try
        {
            var modType = char.ToUpper(url.Segments[1][0]) + url.Segments[1][1..^1];
            var modId   = url.Segments[2];
            using var client = new HttpClient();
            var requestUrl = $"https://gamebanana.com/apiv6/{modType}/{modId}" +
                "?_csvProperties=_aSubmitter,_sDescription,_aPreviewMedia,_sProfileUrl,_sName," +
                "_aSuperCategory,_aCategory,_tsDateUpdated";

            var json   = await client.GetStringAsync(requestUrl);
            var record = JsonSerializer.Deserialize<GameBananaAPIV4>(json)!;

            var metadata = new Metadata
            {
                submitter   = record.Owner.Name,
                description = record.Description,
                preview     = record.Image,
                homepage    = record.Link,
                avi         = record.Owner.Avatar,
                upic        = record.Owner.Upic,
                cat         = record.CategoryName,
                caticon     = record.Category.Icon,
                lastupdate  = record.DateUpdated
            };

            var dest = Path.Combine(Global.assemblyLocation, "Mods", _mod.name, "mod.json");
            File.WriteAllText(dest,
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

            if (!_mod.name.Equals(record.Title, StringComparison.OrdinalIgnoreCase))
            {
                var oldDir = Path.Combine(Global.assemblyLocation, "Mods", _mod.name);
                var newDir = Path.Combine(Global.assemblyLocation, "Mods", record.Title);
                if (!Directory.Exists(newDir))
                {
                    try { Directory.Move(oldDir, newDir); }
                    catch (Exception ex)
                    {
                        Global.logger.WriteLine($"Couldn't rename ({ex.Message})", LoggerType.Error);
                    }
                }
                else
                    Global.logger.WriteLine($"{newDir} already exists", LoggerType.Error);
            }

            var idx = -1;
            for (int i = 0; i < Global.config.ModList!.Count; i++)
                if (Global.config.ModList[i].name == _mod.name) { idx = i; break; }
            if (idx >= 0)
            {
                Global.config.ModList[idx].preview = record.Image;
                Global.config.ModList[idx].name    = record.Title;
                Global.ModList = Global.config.ModList;
            }

            success = true;
            Close();
        }
        catch (Exception ex)
        {
            Global.logger.WriteLine(ex.Message, LoggerType.Error);
        }
    }
}
