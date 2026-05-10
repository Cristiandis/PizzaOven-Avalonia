using System;
using System.Collections.ObjectModel;

namespace PizzaOven;

public class Mod
{
    public string name { get; set; } = string.Empty;
    public bool enabled { get; set; }
    public Uri? preview { get; set; }
}

public class Metadata
{
    public string? title { get; set; }
    public Uri? preview { get; set; }
    public string? submitter { get; set; }
    public Uri? avi { get; set; }
    public Uri? upic { get; set; }
    public Uri? caticon { get; set; }
    public string? cat { get; set; }
    public string? description { get; set; }
    public string? filedescription { get; set; }
    public Uri? homepage { get; set; }
    public DateTime? lastupdate { get; set; }
}

public class Config
{
    public string? Launcher { get; set; }
    public bool FirstOpen { get; set; }
    public string? ModsFolder { get; set; }
    public ObservableCollection<Mod>? ModList { get; set; }
    public double? LeftGridWidth { get; set; }
    public double? RightGridWidth { get; set; }
    public double? TopGridHeight { get; set; }
    public double? BottomGridHeight { get; set; }
    public double? Height { get; set; }
    public double? Width { get; set; }
    public bool Maximized { get; set; }
}

public class Choice
{
    public string OptionText { get; set; } = string.Empty;
    public string OptionSubText { get; set; } = string.Empty;
    public int Index { get; set; }
}
