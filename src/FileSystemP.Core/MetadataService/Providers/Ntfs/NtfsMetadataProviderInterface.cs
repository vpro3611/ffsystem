using FileSystemP.Core.MetadataService.DTO;

namespace FileSystemP.Core.MetadataService.Providers.Ntfs;

public interface INtfsMetadataProvider
{
    NtfsMetadataRecord GetFileMetadata(string path);
    DirectoryNtfsMetadataRecord GetDirectoryMetadata(string path);
}