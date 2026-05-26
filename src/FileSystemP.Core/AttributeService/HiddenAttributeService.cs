namespace FileSystemP.Core.AttributeService;

public class HiddenAttributeService
{
    
    private const string _className = nameof(HiddenAttributeService);
    
    private void CheckForExistence(string path, string method)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) throw new AppException($"Path not found: {path}", $"{_className}.{method}()");
    }
    
    public bool IsHidden(string path)
    {
        FileInfo file = new FileInfo(path);
        FileAttributes attributes = file.Attributes;
        
        return attributes.HasFlag(FileAttributes.Hidden);
    }


    public void SetHidden(string path)
    {
        CheckForExistence(path, nameof(SetHidden));
        
        FileInfo file = new FileInfo(path);
        FileAttributes attributes = file.Attributes;

        if (!IsHidden(path))
        {
            file.Attributes = attributes | FileAttributes.Hidden;
        }
    }

    public void UnsetHidden(string path)
    {
        CheckForExistence(path, nameof(UnsetHidden));
        
        FileInfo file = new FileInfo(path);
        FileAttributes attributes = file.Attributes;
        
        if (IsHidden(path))
        {
            file.Attributes = attributes & ~FileAttributes.Hidden;
        }
    }
}
