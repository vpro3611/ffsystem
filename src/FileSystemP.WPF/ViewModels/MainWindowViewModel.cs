using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSystemP.Core.MetadataService.Providers.Ntfs;
using FileSystemP.Core.Services;
using FileSystemP.WPF.Models;
using FileSystemP.WPF.Views;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace FileSystemP.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public FileTreeViewModel Tree { get; }
    public FilePanelViewModel Panel { get; }
    public SearchViewModel Search { get; }
    public CommandPaletteViewModel Palette { get; }

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private IEnumerable<PathSegment> _pathSegments = Enumerable.Empty<PathSegment>();

    [ObservableProperty]
    private int _selectedSidebarSectionIndex;

    [ObservableProperty]
    private bool _isTerminalDetached;

    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();
    private TerminalWindow? _terminalWindow;

    public MainWindowViewModel()
    {
        var undoService = new UndoService();
        var metadataProvider = new NtfsMetadataProvider();
        var searchService = new FileSystemP.Core.SearchService.SearchService();

        Tree = new FileTreeViewModel(NavigateTo);
        Panel = new FilePanelViewModel(NavigateTo, metadataProvider, undoService);
        Search = new SearchViewModel(searchService, Panel, NavigateTo);
        Palette = new CommandPaletteViewModel(NavigateTo, this, undoService);
    }

    partial void OnCurrentPathChanged(string value)
    {
        if (Directory.Exists(value))
        {
            Directory.SetCurrentDirectory(value);
        }
        
        _ = Panel.LoadEntries(value);
        UpdatePathSegments(value);
        Palette?.UpdatePrompt(value);
    }

    private void UpdatePathSegments(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            PathSegments = Enumerable.Empty<PathSegment>();
            return;
        }

        var segments = new List<PathSegment>();
        var fullPath = string.Empty;

        // Split by backslash, keeping the root (e.g., C:\) correctly
        var parts = path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        
        // Handle drive root (e.g., C:\)
        var driveMatch = System.Text.RegularExpressions.Regex.Match(path, @"^[a-zA-Z]:\\");
        if (driveMatch.Success)
        {
            var root = driveMatch.Value;
            segments.Add(new PathSegment(root, root));
            fullPath = root;
        }

        foreach (var part in parts)
        {
            // Skip the drive letter if we already added it as root
            if (fullPath.StartsWith(part, StringComparison.OrdinalIgnoreCase) && fullPath.Length <= 3)
                continue;

            fullPath = Path.Combine(fullPath, part);
            segments.Add(new PathSegment(part, fullPath));
        }

        PathSegments = segments;
    }

    [RelayCommand]
    private void NavigateTo(string path)
    {
        if (string.IsNullOrEmpty(path) || path == CurrentPath)
            return;

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

    [RelayCommand]
    private void ShowSearchMenu()
    {
        SelectedSidebarSectionIndex = 1;
    }

    public void OpenDetachedTerminal()
    {
        IsTerminalDetached = true;
        Palette.IsVisible = false;
    }

    public void RestoreEmbeddedTerminalAfterDetachedClose()
    {
        Palette.Reset();
        IsTerminalDetached = false;
        Palette.IsVisible = true;
    }

    [RelayCommand]
    private void ShowDetachedTerminalWindow()
    {
        if (_terminalWindow is not null)
        {
            if (_terminalWindow.WindowState == WindowState.Minimized)
            {
                _terminalWindow.WindowState = WindowState.Normal;
            }

            _terminalWindow.Activate();
            return;
        }

        bool wasVisible = Palette.IsVisible;
        OpenDetachedTerminal();

        try
        {
            var window = new TerminalWindow(() =>
            {
                _terminalWindow = null;
                RestoreEmbeddedTerminalAfterDetachedClose();
            })
            {
                DataContext = Palette,
                Owner = Application.Current?.MainWindow
            };

            _terminalWindow = window;
            window.Show();
            window.Activate();
        }
        catch
        {
            _terminalWindow = null;
            IsTerminalDetached = false;
            Palette.IsVisible = wasVisible;
            throw;
        }
    }
}
