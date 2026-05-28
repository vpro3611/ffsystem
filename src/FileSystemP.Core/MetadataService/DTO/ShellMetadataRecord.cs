namespace FileSystemP.Core.MetadataService.DTO;

public record ShellPropertyRecord(
    string CanonicalName,
    string DisplayName,
    object? Value
);

public record ShellMetadataRecord(
    List<ShellPropertyRecord> Properties
);
