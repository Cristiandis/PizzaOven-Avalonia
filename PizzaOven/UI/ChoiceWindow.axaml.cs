using Avalonia.Controls;
using System.Collections.Generic;

namespace PizzaOven.UI;

public partial class ChoiceWindow : Window
{
    public int? choice = null;

    public ChoiceWindow(List<Choice> choices, string? title = null)
    {
        InitializeComponent();
        ChoiceList.ItemsSource = choices;
        if (title != null) Title = title;
    }

    private void SelectButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Choice item })
        {
            choice = item.Index;
            Close();
        }
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
