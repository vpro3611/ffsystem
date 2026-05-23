using System.Windows;
using System.Windows.Input;
using FileSystemP.WPF.ViewModels;

namespace FileSystemP.WPF.Views;

public partial class PropertiesWindow : Window
{
    private readonly PropertiesViewModel _viewModel;

    private PropertiesWindow(object metadata, Action? onChangesApplied)
    {
        InitializeComponent();
        _viewModel = new PropertiesViewModel(metadata, onChangesApplied: onChangesApplied);
        DataContext = _viewModel;
    }

    public bool HasSavedChanges { get; private set; }

    public static bool ShowFor(object metadata, Window? owner, Action? onChangesApplied = null)
    {
        var dialog = new PropertiesWindow(metadata, onChangesApplied)
        {
            Owner = owner
        };

        dialog.ShowDialog();
        return dialog.HasSavedChanges;
    }

    private void Advanced_Click(object sender, RoutedEventArgs e)
    {
        AdvancedAttributesWindow.ShowFor(_viewModel, this);
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        await SaveChangesAsync(closeAfterSave: false);
    }

    private async void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.HasChanges)
        {
            DialogResult = true;
            return;
        }

        await SaveChangesAsync(closeAfterSave: true);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private async Task SaveChangesAsync(bool closeAfterSave)
    {
        try
        {
            IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            await _viewModel.SaveChangesAsync();
            HasSavedChanges = true;

            if (closeAfterSave)
            {
                DialogResult = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error applying attributes",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            IsEnabled = true;
        }
    }
}
