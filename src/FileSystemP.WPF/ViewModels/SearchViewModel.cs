using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSystemP.Core.SearchService;
using FileSystemP.Core.SearchService.Options;
using FileSystemP.WPF.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    [ObservableProperty] private NameSearchMode _nameMode = NameSearchMode.Contains;
    [ObservableProperty] private SearchTargetType _targetType = SearchTargetType.Both;
    [ObservableProperty] private bool _recursive = true;
    [ObservableProperty] private string _extensions = string.Empty;

    [ObservableProperty] private bool _attributeReadOnly;
    [ObservableProperty] private bool _attributeHidden;
    [ObservableProperty] private bool _attributeArchive;
    [ObservableProperty] private bool _attributeSystem;

    [ObservableProperty] private long? _aboveSize;
    [ObservableProperty] private long? _exactSize;
    [ObservableProperty] private long? _belowSize;

    [ObservableProperty] private DateTime? _createdFrom;
    [ObservableProperty] private DateTime? _createdOn;
    [ObservableProperty] private DateTime? _createdTo;
    [ObservableProperty] private DateTime? _modifiedFrom;
    [ObservableProperty] private DateTime? _modifiedOn;
    [ObservableProperty] private DateTime? _modifiedTo;
    [ObservableProperty] private DateTime? _accessedFrom;
    [ObservableProperty] private DateTime? _accessedOn;
    [ObservableProperty] private DateTime? _accessedTo;

    [ObservableProperty] private bool _isSearching;

    public bool IsSizeRangeEnabled => !ExactSize.HasValue;
    public bool IsSizeExactEnabled => !AboveSize.HasValue && !BelowSize.HasValue;
    public bool IsCreatedRangeEnabled => !CreatedOn.HasValue;
    public bool IsCreatedExactEnabled => !CreatedFrom.HasValue && !CreatedTo.HasValue;
    public bool IsModifiedRangeEnabled => !ModifiedOn.HasValue;
    public bool IsModifiedExactEnabled => !ModifiedFrom.HasValue && !ModifiedTo.HasValue;
    public bool IsAccessedRangeEnabled => !AccessedOn.HasValue;
    public bool IsAccessedExactEnabled => !AccessedFrom.HasValue && !AccessedTo.HasValue;

    public SearchViewModel(ISearchService searchService, FilePanelViewModel panel, Action<string> navigateTo)
    {
        _searchService = searchService;
        _panel = panel;
        _navigateTo = navigateTo;
    }

    [RelayCommand(CanExecute = nameof(CanStartSearch))]
    private async Task StartSearch(string? currentPath)
    {
        if (string.IsNullOrEmpty(currentPath))
        {
            _panel.ErrorMessage = "Please select a directory to search in.";
            return;
        }

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
            NameMode: NameMode,
            Extensions: ParseExtensions(),
            Attributes: GetSelectedAttributes(),
            AboveSize: AboveSize.HasValue ? AboveSize.Value * 1024 * 1024 : null, // Convert MB to Bytes
            ExactSize: ExactSize.HasValue ? ExactSize.Value * 1024 * 1024 : null, // Convert MB to Bytes
            BelowSize: BelowSize.HasValue ? BelowSize.Value * 1024 * 1024 : null, // Convert MB to Bytes
            CreatedFromDate: CreatedFrom,
            CreatedExactDate: CreatedOn,
            CreatedToDate: CreatedTo,
            ModifiedFromDate: ModifiedFrom,
            ModifiedExactDate: ModifiedOn,
            ModifiedToDate: ModifiedTo,
            AccessedFromDate: AccessedFrom,
            AccessedExactDate: AccessedOn,
            AccessedToDate: AccessedTo
        );

        try
        {
            var searchResult = await _searchService.SearchAsync(currentPath, options, _cts.Token);
            _panel.ErrorMessage = null;
            
            // Map results to FileEntry objects
            var entries = searchResult.FoundEntries.Select(MapToFileEntry).ToList();
            
            foreach (var entry in entries)
            {
                _panel.Entries.Add(entry);
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
    private void ClearFilters()
    {
        Pattern = string.Empty;
        TargetType = SearchTargetType.Both;
        Recursive = true;
        Extensions = string.Empty;

        AttributeReadOnly = false;
        AttributeHidden = false;
        AttributeArchive = false;
        AttributeSystem = false;

        AboveSize = null;
        ExactSize = null;
        BelowSize = null;

        CreatedFrom = null;
        CreatedOn = null;
        CreatedTo = null;
        ModifiedFrom = null;
        ModifiedOn = null;
        ModifiedTo = null;
        AccessedFrom = null;
        AccessedOn = null;
        AccessedTo = null;
    }

    private List<string>? ParseExtensions()
    {
        var items = Extensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ext => ext.Trim().TrimStart('.'))
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return items.Count == 0 ? null : items;
    }

    private List<FileAttributes>? GetSelectedAttributes()
    {
        var attributes = new List<FileAttributes>();

        if (AttributeReadOnly) attributes.Add(FileAttributes.ReadOnly);
        if (AttributeHidden) attributes.Add(FileAttributes.Hidden);
        if (AttributeArchive) attributes.Add(FileAttributes.Archive);
        if (AttributeSystem) attributes.Add(FileAttributes.System);

        return attributes.Count == 0 ? null : attributes;
    }

    partial void OnAboveSizeChanged(long? value)
    {
        if (value.HasValue && ExactSize.HasValue)
            ExactSize = null;

        NotifySizeStateChanged();
    }

    partial void OnExactSizeChanged(long? value)
    {
        if (value.HasValue)
        {
            if (AboveSize.HasValue) AboveSize = null;
            if (BelowSize.HasValue) BelowSize = null;
        }

        NotifySizeStateChanged();
    }

    partial void OnBelowSizeChanged(long? value)
    {
        if (value.HasValue && ExactSize.HasValue)
            ExactSize = null;

        NotifySizeStateChanged();
    }

    partial void OnCreatedFromChanged(DateTime? value)
    {
        if (value.HasValue && CreatedOn.HasValue)
            CreatedOn = null;

        NotifyCreatedDateStateChanged();
    }

    partial void OnCreatedOnChanged(DateTime? value)
    {
        if (value.HasValue)
        {
            if (CreatedFrom.HasValue) CreatedFrom = null;
            if (CreatedTo.HasValue) CreatedTo = null;
        }

        NotifyCreatedDateStateChanged();
    }

    partial void OnCreatedToChanged(DateTime? value)
    {
        if (value.HasValue && CreatedOn.HasValue)
            CreatedOn = null;

        NotifyCreatedDateStateChanged();
    }

    partial void OnModifiedFromChanged(DateTime? value)
    {
        if (value.HasValue && ModifiedOn.HasValue)
            ModifiedOn = null;

        NotifyModifiedDateStateChanged();
    }

    partial void OnModifiedOnChanged(DateTime? value)
    {
        if (value.HasValue)
        {
            if (ModifiedFrom.HasValue) ModifiedFrom = null;
            if (ModifiedTo.HasValue) ModifiedTo = null;
        }

        NotifyModifiedDateStateChanged();
    }

    partial void OnModifiedToChanged(DateTime? value)
    {
        if (value.HasValue && ModifiedOn.HasValue)
            ModifiedOn = null;

        NotifyModifiedDateStateChanged();
    }

    partial void OnAccessedFromChanged(DateTime? value)
    {
        if (value.HasValue && AccessedOn.HasValue)
            AccessedOn = null;

        NotifyAccessedDateStateChanged();
    }

    partial void OnAccessedOnChanged(DateTime? value)
    {
        if (value.HasValue)
        {
            if (AccessedFrom.HasValue) AccessedFrom = null;
            if (AccessedTo.HasValue) AccessedTo = null;
        }

        NotifyAccessedDateStateChanged();
    }

    partial void OnAccessedToChanged(DateTime? value)
    {
        if (value.HasValue && AccessedOn.HasValue)
            AccessedOn = null;

        NotifyAccessedDateStateChanged();
    }

    private void NotifyCreatedDateStateChanged()
    {
        OnPropertyChanged(nameof(IsCreatedRangeEnabled));
        OnPropertyChanged(nameof(IsCreatedExactEnabled));
    }

    private void NotifySizeStateChanged()
    {
        OnPropertyChanged(nameof(IsSizeRangeEnabled));
        OnPropertyChanged(nameof(IsSizeExactEnabled));
    }

    private void NotifyModifiedDateStateChanged()
    {
        OnPropertyChanged(nameof(IsModifiedRangeEnabled));
        OnPropertyChanged(nameof(IsModifiedExactEnabled));
    }

    private void NotifyAccessedDateStateChanged()
    {
        OnPropertyChanged(nameof(IsAccessedRangeEnabled));
        OnPropertyChanged(nameof(IsAccessedExactEnabled));
    }

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
            Size = isDir ? "" : FormatSize(((FileInfo)info).Length),
            Icon = Helpers.ShellIconHelper.GetIcon(info.FullName, isDirectory: isDir)
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
