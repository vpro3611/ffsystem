namespace FileSystemP.Core.AttributeService;

public class ArchiveAttributeService
{
    private const string _className = nameof(ArchiveAttributeService);

    private void CheckForExistence(string path, string method)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
            throw new AppException($"Path not found: {path}", $"{_className}.{method}()");
    }

    public bool IsArchive(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);
        return attributes.HasFlag(FileAttributes.Archive);
    }

    public void SetArchive(string path)
    {
        CheckForExistence(path, nameof(SetArchive));

        FileAttributes attributes = File.GetAttributes(path);
        if (!attributes.HasFlag(FileAttributes.Archive))
        {
            File.SetAttributes(path, attributes | FileAttributes.Archive);
        }
    }

    public void UnsetArchive(string path)
    {
        CheckForExistence(path, nameof(UnsetArchive));

        FileAttributes attributes = File.GetAttributes(path);
        if (attributes.HasFlag(FileAttributes.Archive))
        {
            File.SetAttributes(path, attributes & ~FileAttributes.Archive);
        }
    }
}
