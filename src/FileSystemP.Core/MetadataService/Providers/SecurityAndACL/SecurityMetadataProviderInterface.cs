using FileSystemP.Core.MetadataService.DTO;

namespace FileSystemP.Core.MetadataService.Providers.SecurityAndACL;

public interface ISecurityMetadataProvider
{
    SecurityMetadataRecord GetSecurityMetadata(string path);
}