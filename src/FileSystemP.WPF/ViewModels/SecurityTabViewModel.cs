using CommunityToolkit.Mvvm.ComponentModel;
using FileSystemP.Core.MetadataService.DTO;
using System.Collections.ObjectModel;
using System.Security.AccessControl;

namespace FileSystemP.WPF.ViewModels;

public partial class SecurityTabViewModel : ObservableObject
{
    [ObservableProperty] private string _owner = string.Empty;
    [ObservableProperty] private string _group = string.Empty;
    public ObservableCollection<PermissionEntryViewModel> Permissions { get; } = new();

    public void LoadMetadata(SecurityMetadataRecord metadata)
    {
        Owner = metadata.Owner ?? "Unknown";
        Group = metadata.Group ?? "Unknown";
        Permissions.Clear();
        foreach (var entry in metadata.Permissions)
        {
            if (int.TryParse(entry.Rights.ToString(), out _))
            {
                continue;
            }
            Permissions.Add(new PermissionEntryViewModel(entry));
        }
    }
}

public partial class PermissionEntryViewModel : ObservableObject
{
    public string Identity { get; }
    public string WindowsRights { get; }
    public string LinuxRights { get; }
    public bool IsInherited { get; }

    public PermissionEntryViewModel(PermissionEntryRecord record)
    {
        Identity = record.Identity;
        WindowsRights = record.Rights.ToString();
        LinuxRights = MapToLinuxRights(record.Rights);
        IsInherited = record.IsInherited;
    }

    private static string MapToLinuxRights(FileSystemRights rights)
    {
        string r = rights.HasFlag(FileSystemRights.Read) ? "r" : "-";
        string w = rights.HasFlag(FileSystemRights.Write) ? "w" : "-";
        string x = rights.HasFlag(FileSystemRights.ExecuteFile) ? "x" : "-";
        return $"{r}{w}{x}";
    }
}
