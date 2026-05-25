using System.Security.AccessControl;

namespace FileSystemP.Core.MetadataService.DTO;

public record PermissionEntryRecord(
    string Identity,
    FileSystemRights Rights,
    AccessControlType Type,
    bool IsInherited
);