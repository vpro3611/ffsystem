using System.Windows;
using System.Windows.Controls;
using FileSystemP.WPF.ViewModels;

namespace FileSystemP.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainWindowViewModel vm && e.NewValue is FileTreeNode node)
            vm.Tree.SelectedNode = node;
    }

    private void FileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.Panel.OpenCommand.CanExecute(null))
            vm.Panel.OpenCommand.Execute(null);
    }

    private void FileList_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DependencyObject? dep = e.OriginalSource as DependencyObject;
        while (dep != null && dep is not ListViewItem)
            dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
        if (dep is ListViewItem item)
            FileList.SelectedItem = item.DataContext;
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (DataContext is MainWindowViewModel vm)
        {
            // Detect F12
            if (e.Key == System.Windows.Input.Key.F12)
            {
                vm.Palette.ToggleVisibilityCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape && vm.Palette.IsVisible)
            {
                vm.Palette.IsVisible = false;
                e.Handled = true;
            }
        }
    }
}
