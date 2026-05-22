using FileSystemP.Core.MetadataService.DTO;

namespace FileSystemP.Core.MetadataService.Providers.Ntfs;



public class NtfsMetadataProvider : INtfsMetadataProvider
{
    private string _className = nameof(NtfsMetadataProvider);


    public NtfsMetadataRecord GetFileMetadata(string path)
    {
        if (!File.Exists(path))
        {
            throw new AppException($"File not found: {path}", $"{_className}.{nameof(GetFileMetadata)}()");
        }
        
        FileInfo fileInfo = new FileInfo(path);
        
        return new NtfsMetadataRecord(
            fileInfo.Name,
            fileInfo.FullName,
            fileInfo.DirectoryName ?? $"Couldn't determine directory, full path: {path}",
            fileInfo.Extension,
            fileInfo.Length,
            fileInfo.CreationTime,
            fileInfo.LastWriteTime,
            fileInfo.LastAccessTime,
            fileInfo.Attributes
        );
    }

    public DirectoryNtfsMetadataRecord GetDirectoryMetadata(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new AppException($"Directory not found: {path}", $"{_className}.{nameof(GetDirectoryMetadata)}()");
        }
        
        DirectoryInfo dirInfo = new DirectoryInfo(path);

        string parentPath = dirInfo.Parent?.FullName ?? dirInfo.Root.FullName;
        
        return new DirectoryNtfsMetadataRecord(
            dirInfo.Name,
            dirInfo.FullName,
            dirInfo.Root.FullName,
            parentPath,
            dirInfo.CreationTime,
            dirInfo.LastWriteTime,
            dirInfo.LastAccessTime,
            dirInfo.Attributes
        );
    }
}