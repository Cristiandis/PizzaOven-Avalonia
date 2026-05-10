using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace PizzaOven;

public enum LoggerType { Info, Warning, Error }

public class Logger
{
    private readonly TextBox _outputWindow;

    public Logger(TextBox textBox) => _outputWindow = textBox;

    public void WriteLine(string text, LoggerType type)
    {
        string header = type switch
        {
            LoggerType.Info    => "INFO",
            LoggerType.Warning => "WARNING",
            LoggerType.Error   => "ERROR",
            _                  => "INFO"
        };

        string line = $"[{DateTime.Now:HH:mm:ss}] [{header}] {text}\n";

        Dispatcher.UIThread.Post(() =>
        {
            _outputWindow.Text += line;
            _outputWindow.CaretIndex = _outputWindow.Text?.Length ?? 0;
        });
    }
}
