using System.Windows;
using FileSystemP.WPF.ViewModels;

namespace FileSystemP.WPF.Views;

public partial class AdvancedAttributesWindow : Window
{
    private AdvancedAttributesWindow(AdvancedAttributesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public static bool ShowFor(PropertiesViewModel propertiesViewModel, Window owner)
    {
        var dialogViewModel = new AdvancedAttributesViewModel(propertiesViewModel);
        var dialog = new AdvancedAttributesWindow(dialogViewModel)
        {
            Owner = owner
        };

        bool? result = dialog.ShowDialog();
        if (result == true)
        {
            dialogViewModel.ApplyTo(propertiesViewModel);
            return true;
        }

        return false;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
