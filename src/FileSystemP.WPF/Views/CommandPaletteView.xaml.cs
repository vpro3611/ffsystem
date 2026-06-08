using System.Windows.Controls;
using System.Windows.Input;
using FileSystemP.WPF.ViewModels;
using System.Collections.Specialized;

namespace FileSystemP.WPF.Views;

public partial class CommandPaletteView : UserControl
{
    public CommandPaletteView()
    {
        InitializeComponent();
        
        IsVisibleChanged += (s, e) =>
        {
            if ((bool)e.NewValue)
            {
                InputBox.Focus();
            }
        };

        Loaded += (s, e) =>
        {
            if (DataContext is CommandPaletteViewModel vm)
            {
                vm.OutputHistory.CollectionChanged += (sender, args) =>
                {
                    HistoryScroll.ScrollToEnd();
                };
            }
        };
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not CommandPaletteViewModel vm) return;

        if (e.Key == Key.Up)
        {
            vm.NavigateHistoryCommand.Execute("Up");
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            vm.NavigateHistoryCommand.Execute("Down");
            e.Handled = true;
        }
    }
}
