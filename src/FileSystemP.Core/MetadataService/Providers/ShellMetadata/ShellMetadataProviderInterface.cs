using FileSystemP.Core.MetadataService.DTO;

namespace FileSystemP.Core.MetadataService.Providers.ShellMetadata;

public interface IShellMetadataProviderInterface
{
    ShellMetadataRecord GetShellMetadata(string path);
}