using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.IO;
using System.Linq;

namespace PizzaOven.UI;

public partial class EditWindow : Window
{
    public string? _name;
    public bool    _folder;
    public string? directory;
    public string? newName;

    public EditWindow(string? name, bool folder)
    {
        InitializeComponent();
        _folder = folder;
        if (!string.IsNullOrEmpty(name))
        {
            _name = name;
            NameBox.Text = name;
            Title = $"Edit {name}";
        }
        else
            Title = _folder ? "Create New Mod" : "Create New Loadout";
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void ConfirmButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_folder)
        {
            if (_name != null) EditFolderName();
            else               CreateName();
        }
    }

    private void CreateName()
    {
        var newDir = Path.Combine(Global.assemblyLocation, "Mods", NameBox.Text ?? "");
        if (!Directory.Exists(newDir)) { directory = newDir; Close(); }
        else Global.logger.WriteLine($"{newDir} already exists", LoggerType.Error);
    }

    private void EditFolderName()
    {
        var text = NameBox.Text ?? "";
        if (string.Equals(text, _name, StringComparison.OrdinalIgnoreCase)) return;

        var oldDir = Path.Combine(Global.assemblyLocation, "Mods", _name!);
        var newDir = Path.Combine(Global.assemblyLocation, "Mods", text);
        if (Directory.Exists(newDir))
        { Global.logger.WriteLine($"{newDir} already exists", LoggerType.Error); return; }

        try
        {
            Directory.Move(oldDir, newDir);
            var idx = Global.config.ModList!.IndexOf(Global.config.ModList!
                [Global.config.ModList!.ToArray()
                    .Select((m, i) => (m, i)).First(x => x.m.name == _name).i]);
            Global.config.ModList![idx].name = text;
            Global.ModList = Global.config.ModList;
            Close();
        }
        catch (Exception ex)
        {
            Global.logger.WriteLine($"Couldn't rename ({ex.Message})", LoggerType.Error);
        }
    }

    private void NameBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && _folder)
        {
            if (_name != null) EditFolderName();
            else               CreateName();
        }
    }
}
