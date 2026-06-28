using System;
using System.Windows;

namespace FileSystemP.WPF.Views;

public partial class TerminalWindow : Window
{
    private readonly Action _onClosed;

    public TerminalWindow(Action onClosed)
    {
        InitializeComponent();
        _onClosed = onClosed;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _onClosed();
    }
}
