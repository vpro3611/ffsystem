using CommunityToolkit.Mvvm.ComponentModel;
using FileSystemP.Core.Services;
using FileSystemP.WPF.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace FileSystemP.WPF.ViewModels;

public partial class FileTreeViewModel : ObservableObject
{
    private readonly Action<string> _navigate;

    [ObservableProperty]
    private ObservableCollection<FileTreeNode> _roots = new();

    [ObservableProperty]
    private FileTreeNode? _selectedNode;

    public FileTreeViewModel(Action<string> navigate)
    {
        _navigate = navigate;
        foreach (var drive in DriveService.GetDrives())
        {
            ImageSource? icon = ShellIconHelper.GetIcon(drive.Name, isDrive: true);
            Roots.Add(new FileTreeNode(drive.Name, drive.Name, icon));
        }
    }

    partial void OnSelectedNodeChanged(FileTreeNode? value)
    {
        if (value is not null)
            _navigate(value.FullPath);
    }
}
