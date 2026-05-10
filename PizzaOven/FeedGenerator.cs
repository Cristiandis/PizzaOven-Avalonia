using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PizzaOven;

public enum FeedFilter  { Featured, Recent, Popular, None }
public enum TypeFilter  { Mods, WiPs, Sounds }

public static class FeedGenerator
{
    private static Dictionary<string, GameBananaModList> _feed = new();
    public static bool error;
    public static Exception? exception;
    public static GameBananaModList? CurrentFeed;

    public static double GetHeader(this HttpResponseMessage response, string key)
    {
        if (!response.Headers.TryGetValues(key, out var vals)) return -1;
        return double.TryParse(vals.First(), out var d) ? d : -1;
    }

    public static void ClearCache() => _feed.Clear();

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
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();
            CurrentFeed.Records = new();
            foreach (var el in list)
            {
                var record = JsonSerializer.Deserialize<GameBananaRecord>(el.GetRawText(), options);
                if (record != null) CurrentFeed.Records.Add(record);
            }
            var numRecords = response.GetHeader("X-GbApi-Metadata_nRecordCount");
            if (numRecords != -1)
            {
                var totalPages = Math.Ceiling(numRecords / (double)perPage);
                CurrentFeed.TotalPages = totalPages == 0 ? 1 : totalPages;
            }
        }
        catch (Exception e)
        {
            error     = true;
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
            TypeFilter.WiPs   => "Wip/",
            _                 => "Mod/"
        };

        if (search != null)
            url += $"ByName?_sName=*{search}*&_idGameRow=7692&";
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
            FeedFilter.Recent   => "&_sOrderBy=_tsDateUpdated,DESC",
            FeedFilter.Featured => "&_aArgs[]=_sbWasFeatured = true& _sOrderBy=_tsDateAdded,DESC",
            FeedFilter.Popular  => "&_sOrderBy=_nDownloadCount,DESC",
            _                   => ""
        };

        if (subcategory?.ID != null)      url += $"&_aCategoryRowIds[]={subcategory.ID}";
        else if (category?.ID != null)    url += $"&_aCategoryRowIds[]={category.ID}";

        url += $"&_nPage={page}";
        return url;
    }
}
