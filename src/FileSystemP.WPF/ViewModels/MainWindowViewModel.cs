using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSystemP.Core.MetadataService.Providers.Ntfs;
using FileSystemP.WPF.ViewModels;
using System.Collections.Generic;

namespace FileSystemP.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public FileTreeViewModel Tree { get; }
    public FilePanelViewModel Panel { get; }

    [ObservableProperty]
    private string _currentPath = string.Empty;

    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();

    public MainWindowViewModel()
    {
        var metadataProvider = new NtfsMetadataProvider();
        Tree = new FileTreeViewModel(NavigateTo);
        Panel = new FilePanelViewModel(NavigateTo, metadataProvider);
    }

    partial void OnCurrentPathChanged(string value)
    {
        _ = Panel.LoadEntries(value);
    }

    private void NavigateTo(string path)
    {
        if (!string.IsNullOrEmpty(CurrentPath))
            _backStack.Push(CurrentPath);
        _forwardStack.Clear();
        CurrentPath = path;
        NavigateBackCommand.NotifyCanExecuteChanged();
        NavigateForwardCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void NavigateBack()
    {
        _forwardStack.Push(CurrentPath);
        CurrentPath = _backStack.Pop();
        NavigateBackCommand.NotifyCanExecuteChanged();
        NavigateForwardCommand.NotifyCanExecuteChanged();
    }
    private bool CanGoBack() => _backStack.Count > 0;

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void NavigateForward()
    {
        _backStack.Push(CurrentPath);
        CurrentPath = _forwardStack.Pop();
        NavigateBackCommand.NotifyCanExecuteChanged();
        NavigateForwardCommand.NotifyCanExecuteChanged();
    }
    private bool CanGoForward() => _forwardStack.Count > 0;

    [RelayCommand]
    private void ShowTreeNodeProperties(FileTreeNode? node)
    {
        if (node is null)
            return;

        Panel.ShowPropertiesForPath(node.FullPath, isDirectory: true);
    }
}
