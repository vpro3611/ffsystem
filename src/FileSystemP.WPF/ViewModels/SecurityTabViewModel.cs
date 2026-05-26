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
    public List<PermissionEntryRecord> Records { get; } = new();

    public void LoadMetadata(SecurityMetadataRecord metadata)
    {
        Owner = metadata.Owner ?? "Unknown";
        Group = metadata.Group ?? "Unknown";
        Permissions.Clear();
        Records.Clear();
        Records.AddRange(metadata.Permissions);
        
        // Group by identity to show combined summary in the main view
        var groups = metadata.Permissions.GroupBy(p => p.Identity);
        foreach (var group in groups)
        {
            var aggregatedRights = (FileSystemRights)0;
            foreach (var rule in group) aggregatedRights |= rule.Rights;
            
            // Fix: Don't filter out if it's a combination of flags.
            // Only filter if the string representation is purely numeric, 
            // which usually means 'Synchronize' or other raw mask bits we want to hide if they are ALONE.
            // However, it's safer to just show everything for now to ensure the grid isn't empty.
            
            var isAnyInherited = group.Any(r => r.IsInherited);
            var isAnyExplicit = group.Any(r => !r.IsInherited);
            
            Permissions.Add(new PermissionEntryViewModel(new PermissionEntryRecord(
                group.Key, aggregatedRights, AccessControlType.Allow, isAnyInherited && !isAnyExplicit)));
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
        WindowsRights = GetFriendlyRightsString(record.Rights);
        LinuxRights = MapToLinuxRights(record.Rights);
        IsInherited = record.IsInherited;
    }

    private static string GetFriendlyRightsString(FileSystemRights rights)
    {
        if (rights.HasFlag(FileSystemRights.FullControl)) return "Full Control";
        
        var list = new List<string>();
        if ((rights & FileSystemRights.Modify) == FileSystemRights.Modify) list.Add("Modify");
        else
        {
            if (rights.HasFlag(FileSystemRights.ReadAndExecute)) list.Add("Read & Execute");
            else if (rights.HasFlag(FileSystemRights.Read)) list.Add("Read");
            
            if (rights.HasFlag(FileSystemRights.Write)) list.Add("Write");
        }

        if (list.Count == 0)
        {
            // Fallback for special or complex bits
            string s = rights.ToString();
            return int.TryParse(s, out _) ? "Special" : s;
        }

        return string.Join(", ", list);
    }

    private static string MapToLinuxRights(FileSystemRights rights)
    {
        string r = rights.HasFlag(FileSystemRights.Read) ? "r" : "-";
        string w = rights.HasFlag(FileSystemRights.Write) ? "w" : "-";
        string x = rights.HasFlag(FileSystemRights.ExecuteFile) ? "x" : "-";
        return $"{r}{w}{x}";
    }
}
