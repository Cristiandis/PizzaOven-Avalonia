using Avalonia.Controls;
using System.Collections.Generic;

namespace PizzaOven.UI;

public partial class UpdateFileBox : Window
{
    public string?  chosenFileUrl;
    public string?  chosenFileName;
    public string?  chosenFileDescription;
    public bool     selectedDownloadAll;

    public UpdateFileBox(List<GameBananaItemFile> files, string packageName)
    {
        InitializeComponent();
        selectedDownloadAll = false;
        FileList.ItemsSource = files;
        TitleBox.Text        = packageName;
    }

    private void SelectButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: GameBananaItemFile item })
        {
            chosenFileUrl         = item.DownloadUrl;
            chosenFileName        = item.FileName;
            chosenFileDescription = item.Description;
            Close();
        }
    }

    private void DownloadAll_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        selectedDownloadAll = true;
        Close();
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
