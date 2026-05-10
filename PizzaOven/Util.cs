using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PizzaOven;

public class NaturalSort : IComparer<string>
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, EntryPoint = "StrCmpLogicalW")]
    private static extern int StrCmpLogicalW(string x, string y);

    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try { return StrCmpLogicalW(x, y); }
            catch { /* P/Invoke not available */ }
        }

        return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
