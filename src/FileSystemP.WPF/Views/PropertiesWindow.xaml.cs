using System.Windows;
using FileSystemP.WPF.ViewModels;

namespace FileSystemP.WPF.Views;

public partial class PropertiesWindow : Window
{
    private PropertiesWindow(object metadata)
    {
        InitializeComponent();
        DataContext = new PropertiesViewModel(metadata);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    public static void ShowFor(object metadata, Window? owner)
    {
        var dialog = new PropertiesWindow(metadata)
        {
            Owner = owner
        };

        dialog.ShowDialog();
    }
}
