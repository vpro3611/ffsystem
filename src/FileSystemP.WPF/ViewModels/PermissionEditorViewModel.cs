using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSystemP.Core.MetadataService.DTO;
using FileSystemP.Core.MetadataService.Providers.SecurityAndACL;
using System.Collections.ObjectModel;
using System.Security.AccessControl;

namespace FileSystemP.WPF.ViewModels;

public partial class PermissionEditorViewModel : ObservableObject
{
    private readonly string _path;
    private readonly ISecurityModifierService _securityModifierService;
    private readonly List<PermissionEntryRecord> _originalPermissions = new();

    [ObservableProperty] private string _newIdentityName = string.Empty;
    [ObservableProperty] private IdentityViewModel? _selectedIdentity;
    [ObservableProperty] private bool _isInheritanceProtected;

    public ObservableCollection<IdentityViewModel> Identities { get; } = new();

    public PermissionEditorViewModel(string path, ISecurityModifierService securityModifierService)
    {
        _path = path;
        _securityModifierService = securityModifierService;
    }

    public void LoadPermissions(IEnumerable<PermissionEntryRecord> permissions)
    {
        _originalPermissions.Clear();
        _originalPermissions.AddRange(permissions);
        
        Identities.Clear();
        var groups = permissions.GroupBy(p => p.Identity);
        foreach (var group in groups)
        {
            var identity = new IdentityViewModel(group.Key);
            foreach (var rule in group)
            {
                identity.UpdateRights(rule.Rights);
            }
            Identities.Add(identity);
        }
    }

    [RelayCommand]
    private void AddIdentity()
    {
        if (_securityModifierService.ValidateIdentity(NewIdentityName))
        {
            if (!Identities.Any(i => i.Name == NewIdentityName))
            {
                Identities.Add(new IdentityViewModel(NewIdentityName));
            }
            NewIdentityName = string.Empty;
        }
    }

    public SecurityTransaction GenerateTransaction()
    {
        var changes = new List<PermissionChange>();
        
        foreach (var identity in Identities)
        {
            var currentRights = identity.GetRights();
            var originalRules = _originalPermissions.Where(p => p.Identity == identity.Name).ToList();
            
            // Simplified: if rights changed, we remove old rules and add one new one
            // In a real system, we'd handle Allow/Deny separately, but here we focus on one "Allow" block
            var originalRights = originalRules.Aggregate((FileSystemRights)0, (acc, r) => acc | r.Rights);
            
            if (currentRights != originalRights)
            {
                // Remove all original rules for this identity
                foreach (var old in originalRules)
                {
                    changes.Add(new PermissionChange(old, null));
                }
                
                // Add new aggregated rule
                if (currentRights != 0)
                {
                    changes.Add(new PermissionChange(null, new PermissionEntryRecord(
                        identity.Name, currentRights, AccessControlType.Allow, false)));
                }
            }
        }
        
        // Handle removals
        var remainingIdentities = Identities.Select(i => i.Name).ToHashSet();
        foreach (var old in _originalPermissions)
        {
            if (!remainingIdentities.Contains(old.Identity))
            {
                changes.Add(new PermissionChange(old, null));
            }
        }

        return new SecurityTransaction(null, false, changes);
    }

    // Properties for selected identity
    public bool IsReadSelected
    {
        get => SelectedIdentity?.IsRead ?? false;
        set { if (SelectedIdentity != null) { SelectedIdentity.IsRead = value; OnPropertyChanged(); } }
    }

    public bool IsWriteSelected
    {
        get => SelectedIdentity?.IsWrite ?? false;
        set { if (SelectedIdentity != null) { SelectedIdentity.IsWrite = value; OnPropertyChanged(); } }
    }

    public bool IsExecuteSelected
    {
        get => SelectedIdentity?.IsExecute ?? false;
        set { if (SelectedIdentity != null) { SelectedIdentity.IsExecute = value; OnPropertyChanged(); } }
    }

    public bool IsFullControlSelected
    {
        get => SelectedIdentity?.IsFullControl ?? false;
        set { if (SelectedIdentity != null) { SelectedIdentity.IsFullControl = value; OnPropertyChanged(); } }
    }

    partial void OnSelectedIdentityChanged(IdentityViewModel? value)
    {
        OnPropertyChanged(nameof(IsReadSelected));
        OnPropertyChanged(nameof(IsWriteSelected));
        OnPropertyChanged(nameof(IsExecuteSelected));
        OnPropertyChanged(nameof(IsFullControlSelected));
    }
}

public partial class IdentityViewModel : ObservableObject
{
    public string Name { get; }
    [ObservableProperty] private bool _isRead;
    [ObservableProperty] private bool _isWrite;
    [ObservableProperty] private bool _isExecute;
    [ObservableProperty] private bool _isFullControl;

    public IdentityViewModel(string name) => Name = name;

    public void UpdateRights(FileSystemRights rights)
    {
        if (rights.HasFlag(FileSystemRights.FullControl)) IsFullControl = true;
        if (rights.HasFlag(FileSystemRights.Read)) IsRead = true;
        if (rights.HasFlag(FileSystemRights.Write)) IsWrite = true;
        if (rights.HasFlag(FileSystemRights.ExecuteFile)) IsExecute = true;
    }

    public FileSystemRights GetRights()
    {
        if (IsFullControl) return FileSystemRights.FullControl;
        var rights = (FileSystemRights)0;
        if (IsRead) rights |= FileSystemRights.Read;
        if (IsWrite) rights |= FileSystemRights.Write;
        if (IsExecute) rights |= FileSystemRights.ExecuteFile;
        return rights;
    }
}
