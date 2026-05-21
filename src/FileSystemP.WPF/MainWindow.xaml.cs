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
}
