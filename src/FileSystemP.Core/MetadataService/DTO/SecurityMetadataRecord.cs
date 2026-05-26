namespace FileSystemP.Core.MetadataService.DTO;

public record SecurityMetadataRecord(
    string? Owner,
    string? Group,
    IReadOnlyList<PermissionEntryRecord> Permissions
);