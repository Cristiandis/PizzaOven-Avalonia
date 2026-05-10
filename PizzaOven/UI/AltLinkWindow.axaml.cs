using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PizzaOven.UI;

public partial class AltLinkWindow : Window
{
    public AltLinkWindow(List<GameBananaAlternateFileSource> files,
                         string packageName, string game, string url, bool update = false)
    {
        InitializeComponent();
        FileList.ItemsSource = files;
        TitleBox.Text        = packageName;

        Description.Text = update
            ? $"Links from the Alternate File Sources section were found. You can select one to manually download.\n" +
              $"To update, delete previous files and extract into:\n" +
              $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{packageName}"
            : $"Links from the Alternate File Sources section were found. You can select one to manually download.\n" +
              $"To install, drag and drop the downloaded file into the mod grid, or extract into:\n" +
              $"{Global.assemblyLocation}{Global.s}Mods";

        FetchDescription.Text = update
            ? $"To fetch GameBanana metadata, right-click {packageName} > Fetch Metadata, and use:"
            : "To fetch GameBanana metadata, right-click row > Fetch Metadata, and use:";

        UrlText.Text = url;
    }

    private void SelectButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: GameBananaAlternateFileSource item })
        {
            Process.Start(new ProcessStartInfo(item.Url) { UseShellExecute = true });
            Close();
        }
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
