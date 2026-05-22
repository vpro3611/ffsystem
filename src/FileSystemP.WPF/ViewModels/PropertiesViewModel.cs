using CommunityToolkit.Mvvm.ComponentModel;
using FileSystemP.Core.MetadataService.DTO;
using FileSystemP.WPF.Helpers;
using System.IO;
using System.Windows.Media;

namespace FileSystemP.WPF.ViewModels;

public partial class PropertiesViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _type = string.Empty;
    [ObservableProperty] private ImageSource? _icon;
    [ObservableProperty] private string _location = string.Empty;
    [ObservableProperty] private string _targetPath = string.Empty;
    [ObservableProperty] private string _size = string.Empty;
    [ObservableProperty] private string _createdAt = string.Empty;
    [ObservableProperty] private string _modifiedAt = string.Empty;
    [ObservableProperty] private string _accessedAt = string.Empty;
    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private bool _isHidden;
    [ObservableProperty] private string? _parentPath;
    [ObservableProperty] private string? _rootPath;
    [ObservableProperty] private bool _isDirectory;

    public PropertiesViewModel(object metadata)
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
            IsReadOnly = file.Attributes.HasFlag(FileAttributes.ReadOnly);
            IsHidden = file.Attributes.HasFlag(FileAttributes.Hidden);
            ParentPath = file.Directory;
            RootPath = Path.GetPathRoot(file.FullPath);
            IsDirectory = false;
        }
        else if (metadata is DirectoryNtfsMetadataRecord dir)
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
            IsReadOnly = dir.Attributes.HasFlag(FileAttributes.ReadOnly);
            IsHidden = dir.Attributes.HasFlag(FileAttributes.Hidden);
            ParentPath = dir.Parent;
            RootPath = dir.Root;
            IsDirectory = true;
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
}
