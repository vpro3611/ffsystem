using FileSystemP.WPF.ViewModels;
using System.Windows;
using Tulpep.ActiveDirectoryObjectPicker;

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
        var picker = new DirectoryObjectPickerDialog()
        {
            AllowedObjectTypes = ObjectTypes.Users | ObjectTypes.Groups | ObjectTypes.BuiltInGroups,
            AllowedLocations = Locations.LocalComputer | Locations.JoinedDomain | Locations.Workgroup,
            DefaultLocations = Locations.LocalComputer,
            MultiSelect = true
        };

        try
        {
            using (picker)
            {
                if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var selected in picker.SelectedObjects)
                    {
                        _viewModel.NewIdentityName = selected.Name;
                        _viewModel.AddIdentityCommand.Execute(null);
                    }
                }
            }
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            MessageBox.Show($"Failed to open user picker: {ex.Message}\n\nPlease ensure your computer is connected to the network if using a domain account.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
