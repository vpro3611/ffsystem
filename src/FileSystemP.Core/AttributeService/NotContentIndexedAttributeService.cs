namespace FileSystemP.Core.AttributeService;

public class NotContentIndexedAttributeService
{
    private const string _className = nameof(NotContentIndexedAttributeService);

    private void CheckForExistence(string path, string method)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
            throw new AppException($"Path not found: {path}", $"{_className}.{method}()");
    }

    public bool IsNotContentIndexed(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);
        return attributes.HasFlag(FileAttributes.NotContentIndexed);
    }

    public void SetNotContentIndexed(string path)
    {
        CheckForExistence(path, nameof(SetNotContentIndexed));

        FileAttributes attributes = File.GetAttributes(path);
        if (!attributes.HasFlag(FileAttributes.NotContentIndexed))
        {
            File.SetAttributes(path, attributes | FileAttributes.NotContentIndexed);
        }
    }

    public void UnsetNotContentIndexed(string path)
    {
        CheckForExistence(path, nameof(UnsetNotContentIndexed));

        FileAttributes attributes = File.GetAttributes(path);
        if (attributes.HasFlag(FileAttributes.NotContentIndexed))
        {
            File.SetAttributes(path, attributes & ~FileAttributes.NotContentIndexed);
        }
    }
}
