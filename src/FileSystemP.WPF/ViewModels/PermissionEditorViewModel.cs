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

    public void LoadPermissionsFromDisk()
    {
        var provider = new SecurityMetadataProvider();
        var metadata = provider.GetSecurityMetadata(_path);
        LoadPermissions(metadata.Permissions);
        IsInheritanceProtected = false; 
    }

    public void ApplyTransaction(SecurityTransaction transaction)
    {
        _securityModifierService.ApplySecurityChanges(_path, transaction);
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
                identity.UpdateRights(rule.Rights, rule.IsInherited);
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
            var currentExplicitRights = identity.GetExplicitRights();
            var originalExplicitRules = _originalPermissions.Where(p => p.Identity == identity.Name && !p.IsInherited).ToList();
            var originalExplicitRights = originalExplicitRules.Aggregate((FileSystemRights)0, (acc, r) => acc | r.Rights);
            
            if (currentExplicitRights != originalExplicitRights)
            {
                // Remove all original EXPLICIT rules for this identity
                foreach (var old in originalExplicitRules)
                {
                    changes.Add(new PermissionChange(old, null));
                }
                
                // Add new aggregated explicit rule
                if (currentExplicitRights != 0)
                {
                    changes.Add(new PermissionChange(null, new PermissionEntryRecord(
                        identity.Name, currentExplicitRights, AccessControlType.Allow, false, 
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit)));
                }
            }
        }
        
        // Handle removals of explicit rules
        var remainingIdentities = Identities.Select(i => i.Name).ToHashSet();
        foreach (var old in _originalPermissions)
        {
            if (!old.IsInherited && !remainingIdentities.Contains(old.Identity))
            {
                changes.Add(new PermissionChange(old, null));
            }
        }

        return new SecurityTransaction(null, false, changes);
    }

    public bool IsReadSelected
    {
        get => SelectedIdentity?.IsRead ?? false;
        set { if (SelectedIdentity != null) { SelectedIdentity.ExplicitRead = value; OnPropertyChanged(); } }
    }

    public bool IsWriteSelected
    {
        get => SelectedIdentity?.IsWrite ?? false;
        set { if (SelectedIdentity != null) { SelectedIdentity.ExplicitWrite = value; OnPropertyChanged(); } }
    }

    public bool IsExecuteSelected
    {
        get => SelectedIdentity?.IsExecute ?? false;
        set { if (SelectedIdentity != null) { SelectedIdentity.ExplicitExecute = value; OnPropertyChanged(); } }
    }

    public bool IsFullControlSelected
    {
        get => SelectedIdentity?.IsFullControl ?? false;
        set { if (SelectedIdentity != null) { SelectedIdentity.ExplicitFullControl = value; OnPropertyChanged(); } }
    }

    public bool IsReadEnabled => SelectedIdentity?.IsReadEnabled ?? false;
    public bool IsWriteEnabled => SelectedIdentity?.IsWriteEnabled ?? false;
    public bool IsExecuteEnabled => SelectedIdentity?.IsExecuteEnabled ?? false;
    public bool IsFullControlEnabled => SelectedIdentity?.IsFullControlEnabled ?? false;
    public bool CanRemoveSelected => SelectedIdentity?.CanRemove ?? false;

    partial void OnSelectedIdentityChanged(IdentityViewModel? value)
    {
        OnPropertyChanged(nameof(IsReadSelected));
        OnPropertyChanged(nameof(IsWriteSelected));
        OnPropertyChanged(nameof(IsExecuteSelected));
        OnPropertyChanged(nameof(IsFullControlSelected));
        OnPropertyChanged(nameof(IsReadEnabled));
        OnPropertyChanged(nameof(IsWriteEnabled));
        OnPropertyChanged(nameof(IsExecuteEnabled));
        OnPropertyChanged(nameof(IsFullControlEnabled));
        OnPropertyChanged(nameof(CanRemoveSelected));
    }
}

public partial class IdentityViewModel : ObservableObject
{
    public string Name { get; }
    
    // Explicit rights
    [ObservableProperty] private bool _explicitRead;
    [ObservableProperty] private bool _explicitWrite;
    [ObservableProperty] private bool _explicitExecute;
    [ObservableProperty] private bool _explicitFullControl;

    // Inherited rights
    [ObservableProperty] private bool _inheritedRead;
    [ObservableProperty] private bool _inheritedWrite;
    [ObservableProperty] private bool _inheritedExecute;
    [ObservableProperty] private bool _inheritedFullControl;

    // UI View Properties
    public bool IsRead => ExplicitRead || InheritedRead;
    public bool IsWrite => ExplicitWrite || InheritedWrite;
    public bool IsExecute => ExplicitExecute || InheritedExecute;
    public bool IsFullControl => ExplicitFullControl || InheritedFullControl;

    public bool IsReadEnabled => !InheritedRead;
    public bool IsWriteEnabled => !InheritedWrite;
    public bool IsExecuteEnabled => !InheritedExecute;
    public bool IsFullControlEnabled => !InheritedFullControl;

    public bool CanRemove => !InheritedRead && !InheritedWrite && !InheritedExecute && !InheritedFullControl;

    public IdentityViewModel(string name) => Name = name;

    public void UpdateRights(FileSystemRights rights, bool isInherited)
    {
        if (isInherited)
        {
            if (rights.HasFlag(FileSystemRights.FullControl)) InheritedFullControl = true;
            if (rights.HasFlag(FileSystemRights.Read)) InheritedRead = true;
            if (rights.HasFlag(FileSystemRights.Write)) InheritedWrite = true;
            if (rights.HasFlag(FileSystemRights.ExecuteFile)) InheritedExecute = true;
        }
        else
        {
            if (rights.HasFlag(FileSystemRights.FullControl)) ExplicitFullControl = true;
            if (rights.HasFlag(FileSystemRights.Read)) ExplicitRead = true;
            if (rights.HasFlag(FileSystemRights.Write)) ExplicitWrite = true;
            if (rights.HasFlag(FileSystemRights.ExecuteFile)) ExplicitExecute = true;
        }
    }

    public FileSystemRights GetExplicitRights()
    {
        if (ExplicitFullControl) return FileSystemRights.FullControl;
        var rights = (FileSystemRights)0;
        if (ExplicitRead) rights |= FileSystemRights.Read;
        if (ExplicitWrite) rights |= FileSystemRights.Write;
        if (ExplicitExecute) rights |= FileSystemRights.ExecuteFile;
        return rights;
    }

    partial void OnExplicitReadChanged(bool value) => OnPropertyChanged(nameof(IsRead));
    partial void OnExplicitWriteChanged(bool value) => OnPropertyChanged(nameof(IsWrite));
    partial void OnExplicitExecuteChanged(bool value) => OnPropertyChanged(nameof(IsExecute));
    partial void OnExplicitFullControlChanged(bool value) => OnPropertyChanged(nameof(IsFullControl));
}
