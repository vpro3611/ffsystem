using System.Security.AccessControl;

namespace FileSystemP.Core.MetadataService.DTO;

public record PermissionEntryRecord(
    string Identity,
    FileSystemRights Rights,
    AccessControlType Type,
    bool IsInherited
);

public record SecurityTransaction(
    bool? IsInheritanceProtected,
    bool PreserveInheritanceOnProtect,
    IReadOnlyList<PermissionChange> Changes
);

public record PermissionChange(
    PermissionEntryRecord? OldEntry,
    PermissionEntryRecord? NewEntry
);
