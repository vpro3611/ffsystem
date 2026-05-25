using CommunityToolkit.Mvvm.ComponentModel;
using FileSystemP.Core.AttributeService;
using FileSystemP.Core.MetadataService.DTO;
using FileSystemP.Core.MetadataService.Providers.Ntfs;
using FileSystemP.Core.MetadataService.Providers.SecurityAndACL;
using FileSystemP.WPF.Helpers;
using System.IO;
using System.Windows.Media;

namespace FileSystemP.WPF.ViewModels;

public partial class PropertiesViewModel : ObservableObject
{
    private readonly INtfsMetadataProvider _metadataProvider;
    private readonly ReadonlyAttributeService _readonlyAttributeService;
    private readonly HiddenAttributeService _hiddenAttributeService;
    private readonly ArchiveAttributeService _archiveAttributeService;
    private readonly NotContentIndexedAttributeService _notContentIndexedAttributeService;
    private readonly CompressAttributeService _compressAttributeService;
    private readonly ISecurityMetadataProvider _securityMetadataProvider;
    private readonly Action? _onChangesApplied;

    private bool _originalIsReadOnly;
    private bool _originalIsHidden;
    private bool _originalIsArchive;
    private bool _originalAllowsContentIndexing;
    private bool _originalIsCompressed;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _type = string.Empty;
    [ObservableProperty] private ImageSource? _icon;
    [ObservableProperty] private string _location = string.Empty;
    [ObservableProperty] private string _targetPath = string.Empty;
    [ObservableProperty] private string _size = string.Empty;
    [ObservableProperty] private string _createdAt = string.Empty;
    [ObservableProperty] private string _modifiedAt = string.Empty;
    [ObservableProperty] private string _accessedAt = string.Empty;
    [ObservableProperty] private string? _parentPath;
    [ObservableProperty] private string? _rootPath;
    [ObservableProperty] private bool _isDirectory;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private SecurityTabViewModel _security = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    private bool _isReadOnly;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    private bool _isHidden;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    private bool _isArchive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    private bool _allowsContentIndexing = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    private bool _isCompressed;

    [ObservableProperty] private bool _applyChangesRecursively = true;

    public PropertiesViewModel(object metadata)
        : this(
            metadata,
            new NtfsMetadataProvider(),
            new ReadonlyAttributeService(),
            new HiddenAttributeService(),
            new ArchiveAttributeService(),
            new NotContentIndexedAttributeService(),
            new CompressAttributeService(),
            onChangesApplied: null)
    {
    }

    public PropertiesViewModel(object metadata, Action? onChangesApplied)
        : this(
            metadata,
            new NtfsMetadataProvider(),
            new ReadonlyAttributeService(),
            new HiddenAttributeService(),
            new ArchiveAttributeService(),
            new NotContentIndexedAttributeService(),
            new CompressAttributeService(),
            onChangesApplied)
    {
    }

    public PropertiesViewModel(
        object metadata,
        INtfsMetadataProvider metadataProvider,
        ReadonlyAttributeService readonlyAttributeService,
        HiddenAttributeService hiddenAttributeService,
        ArchiveAttributeService archiveAttributeService,
        NotContentIndexedAttributeService notContentIndexedAttributeService,
        CompressAttributeService compressAttributeService,
        Action? onChangesApplied,
        ISecurityMetadataProvider? securityMetadataProvider = null)
    {
        _metadataProvider = metadataProvider;
        _readonlyAttributeService = readonlyAttributeService;
        _hiddenAttributeService = hiddenAttributeService;
        _archiveAttributeService = archiveAttributeService;
        _notContentIndexedAttributeService = notContentIndexedAttributeService;
        _compressAttributeService = compressAttributeService;
        _securityMetadataProvider = (SecurityMetadataProvider?)securityMetadataProvider ?? new SecurityMetadataProvider();
        _onChangesApplied = onChangesApplied;

        LoadFromMetadata(metadata);
    }

    public bool HasChanges =>
        IsReadOnly != _originalIsReadOnly ||
        IsHidden != _originalIsHidden ||
        IsArchive != _originalIsArchive ||
        AllowsContentIndexing != _originalAllowsContentIndexing ||
        IsCompressed != _originalIsCompressed;

    public bool SupportsRecursiveAttributeChanges => IsDirectory;

    public bool SupportsCompression => IsCompressionSupportedForPath(TargetPath);

    public bool IsApplyingCompression => IsBusy && IsCompressed != _originalIsCompressed;

    public bool CanCloseDialog => !IsBusy;

    public bool CanApplyChanges => HasChanges && !IsBusy;

    public string ReadOnlyLabel => IsDirectory
        ? "Read-only (Only applies to files in folder)"
        : "Read-only";

    public string ArchiveLabel => IsDirectory
        ? "Folder is ready for archiving"
        : "File is ready for archiving";

    public string ContentIndexingLabel => IsDirectory
        ? "Allow files in this folder to have contents indexed in addition to file properties"
        : "Allow this file to have contents indexed in addition to file properties";

    public string BusyStatusText => IsApplyingCompression
        ? "Applying compression. This can take a while for large folders."
        : "Applying attribute changes...";

    public bool ShowDirectoryCompressionWarning => IsDirectory && IsCompressed;

    public string DirectoryCompressionWarningText => ApplyChangesRecursively
        ? "Compression will touch this folder, its subfolders, and its files. Large trees can take noticeable time."
        : "Compression will be applied only to this folder. Existing children will not be changed.";

    public async Task SaveChangesAsync()
    {
        if (!HasChanges)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Run(ApplyPendingChanges);
            ReloadMetadata();
            _onChangesApplied?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyPendingChanges()
    {
        ApplyReadonlyBeforeOtherOperationsIfNeeded();
        ApplyHiddenChanges();
        ApplyArchiveChanges();
        ApplyContentIndexingChanges();
        ApplyCompressionChanges();
        ApplyReadonlyAfterOtherOperationsIfNeeded();
    }

    private void ApplyReadonlyBeforeOtherOperationsIfNeeded()
    {
        if (IsReadOnly || IsReadOnly == _originalIsReadOnly)
        {
            return;
        }

        if (IsDirectory)
        {
            _readonlyAttributeService.UnsetReadonlyDir(TargetPath, ApplyChangesRecursively);
            return;
        }

        _readonlyAttributeService.UnsetReadonlyFile(TargetPath);
    }

    private void ApplyReadonlyAfterOtherOperationsIfNeeded()
    {
        if (!IsReadOnly || IsReadOnly == _originalIsReadOnly)
        {
            return;
        }

        if (IsDirectory)
        {
            _readonlyAttributeService.SetReadonlyDir(TargetPath, ApplyChangesRecursively);
            return;
        }

        _readonlyAttributeService.SetReadonlyFile(TargetPath);
    }

    private void ApplyHiddenChanges()
    {
        if (IsHidden == _originalIsHidden)
        {
            return;
        }

        if (IsHidden)
        {
            _hiddenAttributeService.SetHidden(TargetPath);
        }
        else
        {
            _hiddenAttributeService.UnsetHidden(TargetPath);
        }
    }

    private void ApplyArchiveChanges()
    {
        if (IsArchive == _originalIsArchive)
        {
            return;
        }

        if (IsArchive)
        {
            _archiveAttributeService.SetArchive(TargetPath);
        }
        else
        {
            _archiveAttributeService.UnsetArchive(TargetPath);
        }
    }

    private void ApplyContentIndexingChanges()
    {
        if (AllowsContentIndexing == _originalAllowsContentIndexing)
        {
            return;
        }

        if (AllowsContentIndexing)
        {
            _notContentIndexedAttributeService.UnsetNotContentIndexed(TargetPath);
        }
        else
        {
            _notContentIndexedAttributeService.SetNotContentIndexed(TargetPath);
        }
    }

    private void ApplyCompressionChanges()
    {
        if (IsCompressed == _originalIsCompressed)
        {
            return;
        }

        if (IsDirectory)
        {
            if (IsCompressed)
            {
                _compressAttributeService.CompressDirectory(TargetPath, ApplyChangesRecursively);
            }
            else
            {
                _compressAttributeService.DecompressDirectory(TargetPath, ApplyChangesRecursively);
            }

            return;
        }

        if (IsCompressed)
        {
            _compressAttributeService.CompressFile(TargetPath);
        }
        else
        {
            _compressAttributeService.DecompressFile(TargetPath);
        }
    }

    private void ReloadMetadata()
    {
        object metadata = IsDirectory
            ? _metadataProvider.GetDirectoryMetadata(TargetPath)
            : _metadataProvider.GetFileMetadata(TargetPath);

        LoadFromMetadata(metadata);
    }

    private void LoadFromMetadata(object metadata)
    {
        if (metadata is NtfsMetadataRecord file)
        {
            Name = file.Name;
            Type = string.IsNullOrEmpty(file.Extension) ? "File" : $"{file.Extension.TrimStart('.').ToUpperInvariant()} File";
            Icon = ShellIconHelper.GetIcon(file.FullPath);
            Location = file.Directory;
            TargetPath = file.FullPath;
            Size = FormatSize(file.Size);
            CreatedAt = file.CreatedAt.ToString("f");
            ModifiedAt = file.ModifiedAt.ToString("f");
            AccessedAt = file.AccessedAt.ToString("f");
            ParentPath = file.Directory;
            RootPath = Path.GetPathRoot(file.FullPath);
            IsDirectory = false;
            ApplyChangesRecursively = false;

            UpdateEditableAttributes(file.Attributes);
            try { Security.LoadMetadata(_securityMetadataProvider.GetSecurityMetadata(TargetPath)); } catch { }
            return;
        }

        if (metadata is DirectoryNtfsMetadataRecord dir)
        {
            Name = dir.Name;
            Type = "File Folder";
            Icon = ShellIconHelper.GetIcon(dir.FullPath, isDirectory: true);
            Location = dir.Parent;
            TargetPath = dir.FullPath;
            Size = FormatSize(dir.Size);
            CreatedAt = dir.CreatedAt.ToString("f");
            ModifiedAt = dir.ModifiedAt.ToString("f");
            AccessedAt = dir.AccessedAt.ToString("f");
            ParentPath = dir.Parent;
            RootPath = dir.Root;
            IsDirectory = true;
            ApplyChangesRecursively = true;

            UpdateEditableAttributes(dir.Attributes);
            try { Security.LoadMetadata(_securityMetadataProvider.GetSecurityMetadata(TargetPath)); } catch { }
            return;
        }

        throw new ArgumentException("Unsupported metadata type.", nameof(metadata));
    }

    private void UpdateEditableAttributes(FileAttributes attributes)
    {
        IsReadOnly = attributes.HasFlag(FileAttributes.ReadOnly);
        IsHidden = attributes.HasFlag(FileAttributes.Hidden);
        IsArchive = attributes.HasFlag(FileAttributes.Archive);
        AllowsContentIndexing = !attributes.HasFlag(FileAttributes.NotContentIndexed);
        IsCompressed = attributes.HasFlag(FileAttributes.Compressed);

        _originalIsReadOnly = IsReadOnly;
        _originalIsHidden = IsHidden;
        _originalIsArchive = IsArchive;
        _originalAllowsContentIndexing = AllowsContentIndexing;
        _originalIsCompressed = IsCompressed;

        NotifyDerivedStateChanged();
        OnPropertyChanged(nameof(HasChanges));
    }

    private void NotifyDerivedStateChanged()
    {
        OnPropertyChanged(nameof(SupportsRecursiveAttributeChanges));
        OnPropertyChanged(nameof(SupportsCompression));
        OnPropertyChanged(nameof(IsApplyingCompression));
        OnPropertyChanged(nameof(CanCloseDialog));
        OnPropertyChanged(nameof(CanApplyChanges));
        OnPropertyChanged(nameof(ReadOnlyLabel));
        OnPropertyChanged(nameof(ArchiveLabel));
        OnPropertyChanged(nameof(ContentIndexingLabel));
        OnPropertyChanged(nameof(BusyStatusText));
        OnPropertyChanged(nameof(ShowDirectoryCompressionWarning));
        OnPropertyChanged(nameof(DirectoryCompressionWarningText));
    }

    private static bool IsCompressionSupportedForPath(string path)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            string fullPath = Path.GetFullPath(path);
            string? root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root))
            {
                return false;
            }

            var drive = new DriveInfo(root);
            return drive.IsReady && string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]} ({bytes:n0} bytes)";
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsApplyingCompression));
        OnPropertyChanged(nameof(BusyStatusText));
        OnPropertyChanged(nameof(CanCloseDialog));
        OnPropertyChanged(nameof(CanApplyChanges));
    }

    partial void OnIsCompressedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsApplyingCompression));
        OnPropertyChanged(nameof(BusyStatusText));
        OnPropertyChanged(nameof(ShowDirectoryCompressionWarning));
        OnPropertyChanged(nameof(CanApplyChanges));
    }

    partial void OnApplyChangesRecursivelyChanged(bool value)
    {
        OnPropertyChanged(nameof(DirectoryCompressionWarningText));
        OnPropertyChanged(nameof(CanApplyChanges));
    }

    partial void OnIsDirectoryChanged(bool value)
    {
        NotifyDerivedStateChanged();
    }

    partial void OnTargetPathChanged(string value)
    {
        OnPropertyChanged(nameof(SupportsCompression));
    }

    partial void OnIsReadOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanApplyChanges));
    }

    partial void OnIsHiddenChanged(bool value)
    {
        OnPropertyChanged(nameof(CanApplyChanges));
    }

    partial void OnIsArchiveChanged(bool value)
    {
        OnPropertyChanged(nameof(CanApplyChanges));
    }

    partial void OnAllowsContentIndexingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanApplyChanges));
    }
}
