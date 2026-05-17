using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace PizzaOven;

public enum FeedFilter
{
    Featured,
    Recent,
    Popular,
    None
}

public enum TypeFilter
{
    Mods,
    WiPs,
    Sounds
}

public static class FeedGenerator
{
    private static readonly Dictionary<string, GameBananaModList> _feed = new();
    public static bool error;
    public static Exception? exception;
    public static GameBananaModList? CurrentFeed;
    private static HttpListener? _listener;
    private static string? _currentTempPath;

    public static double GetHeader(this HttpResponseMessage response, string key)
    {
        if (!response.Headers.TryGetValues(key, out var vals)) return -1;
        return double.TryParse(vals.First(), out var d) ? d : -1;
    }

    public static void ClearCache()
    {
        _feed.Clear();
    }

    public static async Task GetFeedAsync(int page, TypeFilter type, FeedFilter filter,
        GameBananaCategory? category, GameBananaCategory? subcategory,
        int perPage, bool nsfw, string? search)
    {
        error = false;

        if (_feed.Count > 15)
            _feed.Remove(_feed.Aggregate((l, r) =>
                l.Value.TimeFetched < r.Value.TimeFetched ? l : r).Key);

        using var http = new HttpClient();
        var url = GenerateUrl(page, type, filter, category, subcategory, perPage, nsfw, search);

        if (_feed.TryGetValue(url, out var cached) && cached.IsValid)
        {
            CurrentFeed = cached;
            return;
        }

        CurrentFeed = new GameBananaModList();
        try
        {
            var response = await http.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]" || json.Trim() == "")
            {
                CurrentFeed.Records = new ObservableCollection<GameBananaRecord>();
                CurrentFeed.TotalPages = 1;
                return;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new List<JsonElement>();
            CurrentFeed.Records = new ObservableCollection<GameBananaRecord>();
            foreach (var el in list)
            {
                var record = JsonSerializer.Deserialize<GameBananaRecord>(el.GetRawText(), options);
                if (record != null) CurrentFeed.Records.Add(record);
            }

            var numRecords = response.GetHeader("X-GbApi-Metadata_nRecordCount");
            if (numRecords != -1)
            {
                var totalPages = Math.Ceiling(numRecords / perPage);
                CurrentFeed.TotalPages = totalPages == 0 ? 1 : totalPages;
            }
        }
        catch (Exception e)
        {
            error = true;
            exception = e;
            return;
        }

        _feed[url] = CurrentFeed;
    }

    private static string GenerateUrl(int page, TypeFilter type, FeedFilter filter,
        GameBananaCategory? category, GameBananaCategory? subcategory,
        int perPage, bool nsfw, string? search)
    {
        var url = "https://gamebanana.com/apiv6/";
        url += type switch
        {
            TypeFilter.Sounds => "Sound/",
            TypeFilter.WiPs => "Wip/",
            _ => "Mod/"
        };

        if (search != null)
            url += $"ByName?_sName=*{Uri.EscapeDataString(search)}*&_idGameRow=7692&";
        else if (category?.ID != null)
            url += "ByCategory?";
        else
            url += "ByGame?_aGameRowIds[]=7692&";

        url += $"_csvProperties=_sName,_sModelName,_sProfileUrl,_aSubmitter,_tsDateUpdated,_tsDateAdded," +
               $"_aPreviewMedia,_sText,_sDescription,_aCategory,_aRootCategory,_aGame,_nViewCount," +
               $"_nLikeCount,_nDownloadCount,_aFiles,_aModManagerIntegrations,_bIsNsfw,_aAlternateFileSources" +
               $"&_nPerpage={perPage}";

        if (!nsfw) url += "&_aArgs[]=_sbIsNsfw = false";

        url += filter switch
        {
            FeedFilter.Recent => "&_sOrderBy=_tsDateUpdated,DESC",
            FeedFilter.Featured => "&_aArgs[]=_sbWasFeatured = true& _sOrderBy=_tsDateAdded,DESC",
            FeedFilter.Popular => "&_sOrderBy=_nDownloadCount,DESC",
            _ => ""
        };

        if (subcategory?.ID != null) url += $"&_aCategoryRowIds[]={subcategory.ID}";
        else if (category?.ID != null) url += $"&_aCategoryRowIds[]={category.ID}";

        url += $"&_nPage={page}";
        return url;
    }

    private static async Task<string> MakeRonnieMod()
    {
        if (_listener?.IsListening == true)
            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch
            {
            }

        if (_currentTempPath != null && File.Exists(_currentTempPath))
            try
            {
                File.Delete(_currentTempPath);
            }
            catch
            {
            }

        var avaresUri = "avares://PizzaOven/TutorialMod/RonnieMod.zip";

        try
        {
            using (var assetStream = AssetLoader.Open(new Uri(avaresUri)))
            {
                _currentTempPath = Path.Combine(Path.GetTempPath(), "RonnieMod.zip");

                using (var fileStream = new FileStream(_currentTempPath, FileMode.Create, FileAccess.Write))
                {
                    assetStream.CopyTo(fileStream);
                }
            }
        }
        catch
        {
            return null;
        }

        var url = "http://localhost:5000/RonnieMod.zip";
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:5000/");
        _listener.Start();

        _ = Task.Run(async () =>
        {
            while (_listener.IsListening)
                try
                {
                    var context = await _listener.GetContextAsync();
                    var fileBytes = File.ReadAllBytes(_currentTempPath);
                    context.Response.ContentType = "application/zip";
                    context.Response.ContentLength64 = fileBytes.Length;
                    await context.Response.OutputStream.WriteAsync(fileBytes);
                    context.Response.Close();
                }
                catch
                {
                    break;
                }
        });

        return url;
    }

    public static async Task GetFakeFeed()
    {
        var fakeRecord = new GameBananaRecord
        {
            Title = "Ronnie Oven Mod",
            Description = "Our Favorite Oven",
            Text =
                "<h1>Ronnie Mod</h1>This never before seen mod is made for my favorite superhero Ronnie the Oven!<br><br>if you don't know who Ronnie is, what the hell man, he's talking to you RIGHT NOW!<br><br>oh yeah! you Get to play as Ronnie the Oven!! wow!! Moveset: you can double jump, you break if you run into a wall and your groundpound initiates a nuke!!<br><br>Man I sure hope this mod works very well I put a lot of effort into it I also hope Ronnie sees this he's my superstar",
            Likes = -5,
            Downloads = 1,
            DateAddedLong = DateTimeOffset.UtcNow.AddDays(-5).ToUnixTimeSeconds(),
            DateUpdatedLong = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
            IsNsfw = false,
            Owner = new GameBananaMember
            {
                Name = "SurfyCrescent97",
                Avatar = new Uri("avares://PizzaOven/TutorialMod/profile.png"),
                Upic = new Uri("avares://PizzaOven/TutorialMod/upic.gif")
            },
            Category = new GameBananaCategory
            {
                Name = "",
                Icon = new Uri("avares://PizzaOven/TutorialMod/category.jpg")
            },
            RootCategory = new GameBananaCategory
            {
                Name = "Full Game Edit",
                Icon = new Uri("avares://PizzaOven/TutorialMod/category.jpg")
            },
            AllFiles = new List<GameBananaItemFile>
            {
                new()
                {
                    Id = "file1",
                    FileName = "RonnieMod.zip",
                    Filesize = 1024 * 932,
                    DownloadUrl = await MakeRonnieMod(),
                    Description = "Main mod file",
                    ContainsExe = false,
                    Downloads = 0,
                    DateAddedLong = DateTimeOffset.UtcNow.AddDays(-5).ToUnixTimeSeconds()
                }
            },
            Media = new List<GameBananaImage>
            {
                new()
                {
                    Type = "image",
                    Base = new Uri("avares://PizzaOven/TutorialMod"),
                    File = "mod.png",
                    Caption = "Our Oven Ronnie!"
                }
            },
            AlternateFileSources = new List<GameBananaAlternateFileSource>()
        };

        CurrentFeed = new GameBananaModList
        {
            Records = new ObservableCollection<GameBananaRecord> { fakeRecord },
            TotalPages = 1,
            TimeFetched = DateTime.UtcNow
        };
    }
}