using CommunityToolkit.Mvvm.ComponentModel;

namespace FileSystemP.WPF.ViewModels;

public partial class AdvancedAttributesViewModel : ObservableObject
{
    [ObservableProperty] private bool _isArchive;
    [ObservableProperty] private bool _allowsContentIndexing;
    [ObservableProperty] private bool _isCompressed;
    [ObservableProperty] private bool _applyChangesRecursively;

    public AdvancedAttributesViewModel(PropertiesViewModel properties)
    {
        IsArchive = properties.IsArchive;
        AllowsContentIndexing = properties.AllowsContentIndexing;
        IsCompressed = properties.IsCompressed;
        ApplyChangesRecursively = properties.ApplyChangesRecursively;

        IsDirectory = properties.IsDirectory;
        SupportsCompression = properties.SupportsCompression;
        ArchiveLabel = properties.ArchiveLabel;
        ContentIndexingLabel = properties.ContentIndexingLabel;
    }

    public bool IsDirectory { get; }

    public bool SupportsCompression { get; }

    public string ArchiveLabel { get; }

    public string ContentIndexingLabel { get; }

    public bool ShowDirectoryCompressionWarning => IsDirectory && IsCompressed;

    public string DirectoryCompressionWarningText => ApplyChangesRecursively
        ? "Warning: compressing a folder recursively can take noticeable time and will process all files and subfolders."
        : "Only this folder will be marked for compression. Existing children will not be changed.";

    public void ApplyTo(PropertiesViewModel properties)
    {
        properties.IsArchive = IsArchive;
        properties.AllowsContentIndexing = AllowsContentIndexing;
        properties.IsCompressed = IsCompressed;
        properties.ApplyChangesRecursively = ApplyChangesRecursively;
    }

    partial void OnIsCompressedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowDirectoryCompressionWarning));
    }

    partial void OnApplyChangesRecursivelyChanged(bool value)
    {
        OnPropertyChanged(nameof(DirectoryCompressionWarningText));
    }
}
