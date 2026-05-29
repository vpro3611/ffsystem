using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSystemP.Core.SearchService;
using FileSystemP.Core.SearchService.Options;
using FileSystemP.WPF.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileSystemP.WPF.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly FilePanelViewModel _panel;
    private readonly Action<string> _navigateTo;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _pattern = string.Empty;
    [ObservableProperty] private SearchTargetType _targetType = SearchTargetType.Both;
    [ObservableProperty] private bool _recursive = true;
    
    [ObservableProperty] private long? _aboveSize;
    [ObservableProperty] private long? _belowSize;
    
    [ObservableProperty] private DateTime? _createdFrom;
    [ObservableProperty] private DateTime? _createdTo;

    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _isVisible;

    public SearchViewModel(ISearchService searchService, FilePanelViewModel panel, Action<string> navigateTo)
    {
        _searchService = searchService;
        _panel = panel;
        _navigateTo = navigateTo;
    }

    [RelayCommand(CanExecute = nameof(CanStartSearch))]
    private async Task StartSearch(string currentPath)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        IsSearching = true;
        _panel.Entries.Clear();
        _panel.IsEmpty = false;
        _panel.ErrorMessage = "Searching...";
        StartSearchCommand.NotifyCanExecuteChanged();

        var options = new ExtendedOptions(
            Option: Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly,
            TargetType: TargetType,
            Pattern: string.IsNullOrWhiteSpace(Pattern) ? null : Pattern,
            Extensions: null,
            Attributes: null,
            AboveSize: AboveSize.HasValue ? AboveSize.Value * 1024 * 1024 : null, // Convert MB to Bytes
            ExactSize: null,
            BelowSize: BelowSize.HasValue ? BelowSize.Value * 1024 * 1024 : null, // Convert MB to Bytes
            CreatedFromDate: CreatedFrom,
            CreatedExactDate: null,
            CreatedToDate: CreatedTo,
            ModifiedFromDate: null,
            ModifiedExactDate: null,
            ModifiedToDate: null,
            AccessedFromDate: null,
            AccessedExactDate: null,
            AccessedToDate: null
        );

        try
        {
            var searchResult = await _searchService.SearchAsync(currentPath, options, _cts.Token);
            _panel.ErrorMessage = null;
            foreach (var entry in searchResult.FoundEntries)
            {
                _panel.Entries.Add(MapToFileEntry(entry));
            }
            _panel.IsEmpty = _panel.Entries.Count == 0;
            if (_panel.IsEmpty) _panel.ErrorMessage = "No results found.";
        }
        catch (OperationCanceledException) 
        {
            _panel.ErrorMessage = "Search canceled.";
        }
        catch (Exception ex)
        {
            _panel.ErrorMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
            StartSearchCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanStartSearch() => !IsSearching;

    [RelayCommand]
    private void CancelSearch() => _cts?.Cancel();

    [RelayCommand]
    private void ToggleVisibility() => IsVisible = !IsVisible;

    private static FileEntry MapToFileEntry(FileSystemInfo info)
    {
        bool isDir = info is DirectoryInfo;
        return new FileEntry
        {
            FilePath = info.FullName,
            Name = info.Name,
            IsDirectory = isDir,
            DateModified = info.LastWriteTime,
            Type = isDir ? "Folder" : GetFileType(info.Name),
            Size = isDir ? "" : FormatSize(((FileInfo)info).Length)
        };
    }

    private static string GetFileType(string name)
    {
        var ext = Path.GetExtension(name).ToUpperInvariant();
        return string.IsNullOrEmpty(ext) ? "File" : $"{ext.TrimStart('.')} File";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}
