namespace FileSystemP.Core.MetadataService.DTO;

public record NtfsMetadataRecord(
    string Name,
    string FullPath,
    string Directory,
    string Extension,
    long Size,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    DateTime AccessedAt,
    FileAttributes Attributes
);


public record DirectoryNtfsMetadataRecord(
    string Name, 
    string FullPath,
    string Root,
    string Parent,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    DateTime AccessedAt,
    FileAttributes Attributes
);