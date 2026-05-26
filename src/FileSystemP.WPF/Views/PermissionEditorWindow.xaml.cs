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
            AllowedLocations = Locations.All,
            MultiSelect = true
        };

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

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedIdentity != null)
        {
            _viewModel.Identities.Remove(_viewModel.SelectedIdentity);
        }
    }
}
