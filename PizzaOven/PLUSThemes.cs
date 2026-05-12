using System;
using System.IO;
using Avalonia;
using Avalonia.Media;

namespace PizzaOven;

public static class PLUSThemes
{
    public static string rgb_to_hex(byte r, byte g, byte b) => $"#{r:X2}{g:X2}{b:X2}";

    public static (byte r, byte g, byte b) hex_to_rgb(string hex)
    {
        hex = hex.Replace("#", "");
        return (Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
    }

    public static Color hex_to_color(string hex)
    {
        var (r, g, b) = hex_to_rgb(hex);
        return Color.FromRgb(r, g, b);
    }

    public static bool validhex(string hex)
    {
        if (hex.StartsWith("#")) hex = hex[1..];
        return hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out _);
    }

    public static void Set_BrushColor(string brushname, string color)
    {
        if (!validhex(color)) return;
        var c = hex_to_color(color);
        if (Application.Current?.Resources.ContainsKey(brushname) == true)
            Application.Current.Resources[brushname] = new SolidColorBrush(c);
    }

    public static string Get_BrushColorAsHex(string brushname)
    {
        if (Application.Current?.Resources.TryGetValue(brushname, out var res) == true && res is ISolidColorBrush scb)
        {
            var c = scb.Color;
            return rgb_to_hex(c.R, c.G, c.B);
        }
        return "#000000";
    }

    public static string Base64_SaveFile(string path)
    {
        return Convert.ToBase64String(File.ReadAllBytes(path));
    }

    public static void Base64_LoadFile(string base64, string path)
    {
        File.WriteAllBytes(path, Convert.FromBase64String(base64));
    }

    public static bool IsBase64String(string base64)
    {
        Span<byte> buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64, buffer, out _);
    }
}