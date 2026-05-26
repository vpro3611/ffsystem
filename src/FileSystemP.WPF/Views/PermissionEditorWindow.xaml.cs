using FileSystemP.WPF.ViewModels;
using System.Windows;

namespace FileSystemP.WPF.Views;

public partial class PermissionEditorWindow : Window
{
    private readonly PermissionEditorViewModel _viewModel;

    public PermissionEditorWindow(PermissionEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Apply();
        DialogResult = true;
        Close();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        Apply();
    }

    private void Apply()
    {
        var transaction = _viewModel.GenerateTransaction();
        _viewModel.ApplyTransaction(transaction);
        _viewModel.LoadPermissionsFromDisk();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        string? result = InputDialog.Show("Enter the object name to select:", "Everyone");
        if (result != null)
        {
            _viewModel.NewIdentityName = result;
            _viewModel.AddIdentityCommand.Execute(null);
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedIdentity != null)
        {
            _viewModel.Identities.Remove(_viewModel.SelectedIdentity);
        }
    }
}
