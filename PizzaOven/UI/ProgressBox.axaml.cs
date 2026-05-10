using Avalonia.Controls;
using System.Threading;

namespace PizzaOven;

public partial class ProgressBox : Window
{
    private readonly CancellationTokenSource _cts;
    public bool finished = false;

    public ProgressBox(CancellationTokenSource cts)
    {
        InitializeComponent();
        _cts = cts;
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        _cts.Cancel();
    }
}
