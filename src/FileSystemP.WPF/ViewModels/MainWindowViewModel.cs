using CommunityToolkit.Mvvm.ComponentModel;
using FileSystemP.WPF.ViewModels;

namespace FileSystemP.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public FileTreeViewModel Tree { get; }
    public FilePanelViewModel Panel { get; }

    [ObservableProperty]
    private string _currentPath = string.Empty;

    public MainWindowViewModel()
    {
        Action<string> navigate = path => CurrentPath = path;
        Tree = new FileTreeViewModel(navigate);
        Panel = new FilePanelViewModel(navigate);
    }

    partial void OnCurrentPathChanged(string value)
    {
        _ = Panel.LoadEntries(value);
    }
}
