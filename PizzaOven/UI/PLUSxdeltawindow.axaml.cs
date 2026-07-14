using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace PizzaOven.UI;

public partial class PLUSxdeltawindow : Window
{
    private List<string> _xdeltas = new();

    public string[]? ResultXDeltas { get; private set; }

    public PLUSxdeltawindow(IEnumerable<string> xdeltas)
    {
        InitializeComponent();

        _xdeltas = xdeltas?.ToList() ?? new List<string>();

        foreach (var xdelta in _xdeltas)
        {
            XDeltaCombo.Items.Add(xdelta);
        }

        XDeltaCombo.Items.Add("Unsure (Takes Longer)");

        if (XDeltaCombo.Items.Count > 0)
            XDeltaCombo.SelectedIndex = 0;
    }

    private void ConfirmButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var selected = XDeltaCombo.SelectedItem?.ToString();

        if (selected == "Unsure (Takes Longer)")
        {
            ResultXDeltas = _xdeltas.ToArray();
        }
        else
        {
            ResultXDeltas = new[] { selected };
        }

        Close(ResultXDeltas);
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ResultXDeltas = null;
        Close(ResultXDeltas);
    }
}