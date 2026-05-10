using System;
using System.IO;

namespace PizzaOven;

public static class StringConverters
{
    public static string FormatFileName(string filename) => Path.GetFileName(filename);

    static readonly string[] suffixes = { " Bytes", " KB", " MB", " GB", " TB", " PB" };

    public static string FormatSize(long bytes)
    {
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1000) >= 1) { number /= 1000; counter++; }
        return bytes != 0
            ? string.Format("{0:n1}{1}", number, suffixes[counter])
            : string.Format("{0:n0}{1}", number, suffixes[counter]);
    }

    public static string FormatNumber(int number)
    {
        if (number > 1_000_000) return Math.Round((double)number / 1_000_000, 1) + "M";
        if (number > 1_000)     return Math.Round((double)number / 1_000, 1) + "K";
        return number.ToString();
    }

    public static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalMinutes < 60)   return (int)ts.TotalMinutes + "min";
        if (ts.TotalHours   < 24)   return (int)ts.TotalHours   + "hr";
        if (ts.TotalDays    < 7)    return (int)ts.TotalDays     + "d";
        if (ts.TotalDays    < 30.4) return (int)(ts.TotalDays / 7)    + "wk";
        if (ts.TotalDays    < 365)  return (int)(ts.TotalDays / 30.4) + "mo";
        return (int)(ts.TotalDays / 365.25) + "yr";
    }

    public static string FormatTimeAgo(TimeSpan ts)
    {
        if (ts.TotalMinutes < 60)
        { var m = (int)ts.TotalMinutes; return m > 1 ? $"{m} minutes ago" : $"{m} minute ago"; }
        if (ts.TotalHours < 24)
        { var h = (int)ts.TotalHours; return h > 1 ? $"{h} hours ago" : $"{h} hour ago"; }
        if (ts.TotalDays < 7)
        { var d = (int)ts.TotalDays; return d > 1 ? $"{d} days ago" : $"{d} day ago"; }
        if (ts.TotalDays < 30.4)
        { var w = (int)(ts.TotalDays / 7); return w > 1 ? $"{w} weeks ago" : $"{w} week ago"; }
        if (ts.TotalDays < 365)
        { var mo = (int)(ts.TotalDays / 30.4); return mo > 1 ? $"{mo} months ago" : $"{mo} month ago"; }
        var yr = (int)(ts.TotalDays / 365.25);
        return yr > 1 ? $"{yr} years ago" : $"{yr} year ago";
    }

    public static string FormatSingular(string? rootCat, string cat)
    {
        if (rootCat == null) return cat.TrimEnd('s');
        rootCat = rootCat.Replace("User Interface", "UI");
        if (cat == "Skin Packs") return cat[..^1];
        if (rootCat[^1] == 's')
        {
            if (cat == rootCat)
            {
                rootCat = rootCat.Replace("xes", "xs").Replace("xs/", "xes/");
                return rootCat[..^1];
            }
            if (rootCat == "Clothes") return $"{cat} {rootCat}";
            return $"{cat} {rootCat[..^1]}";
        }
        return cat == rootCat ? rootCat : $"{cat} {rootCat}";
    }
}
