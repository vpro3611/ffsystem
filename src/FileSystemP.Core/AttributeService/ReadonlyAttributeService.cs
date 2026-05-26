namespace FileSystemP.Core.AttributeService;

public class ReadonlyAttributeService
{
    private const string _className = nameof(ReadonlyAttributeService);
    
    public bool IsDirectory(string path)
    {
        return Directory.Exists(path);
    }

    public bool IsFile(string path)
    {
        return File.Exists(path);
    }
    
    public bool IsDirReadonly(DirectoryInfo dir)
    {
        return dir.Attributes.HasFlag(FileAttributes.ReadOnly);
    }

    public bool IsFileReadonly(FileInfo file)
    {
        return file.Attributes.HasFlag(FileAttributes.ReadOnly);
    }

    public void SetReadonlyFile(string path)
    {
        if (!IsFile(path)) throw new AppException($"Path not found or is not a file: {path}", $"{_className}.{nameof(SetReadonlyFile)}()");
        
        FileInfo file = new FileInfo(path);
        
        FileAttributes attributes = file.Attributes;
        
        if (!IsFileReadonly(file))
        {
            file.Attributes = attributes | FileAttributes.ReadOnly;
        } 
    }

    public void UnsetReadonlyFile(string path)
    {
        if (!IsFile(path)) throw new AppException($"Path not found or is not a file: {path}", $"{_className}.{nameof(UnsetReadonlyFile)}()");
        
        FileInfo file = new FileInfo(path);
        
        FileAttributes attributes = file.Attributes;
        
        if (IsFileReadonly(file))
        {
            file.Attributes = attributes & ~FileAttributes.ReadOnly;
        }
    }
    
    public void SetReadonlyDir(string path, bool recursive = false)
    {
        if (!IsDirectory(path)) throw new AppException($"Path not found or is not a directory: {path}", $"{_className}.{nameof(SetReadonlyDir)}()");
        
        DirectoryInfo dir = new DirectoryInfo(path);
        
        if (!IsDirReadonly(dir))
            dir.Attributes |= FileAttributes.ReadOnly;

        SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        if (recursive)
        {
            foreach (DirectoryInfo subdirectory in dir.GetDirectories("*", searchOption))
            {
                if (!IsDirReadonly(subdirectory))
                {
                    subdirectory.Attributes |= FileAttributes.ReadOnly;
                }
            }
        }

        foreach (FileInfo file in dir.GetFiles("*", searchOption)) 
        {
            if (!IsFileReadonly(file))
            {
                file.Attributes |= FileAttributes.ReadOnly;
            }
        }
    }

    public void UnsetReadonlyDir(string path, bool recursive = false)
    {
        if (!IsDirectory(path)) throw new AppException($"Path not found or is not a directory: {path}", $"{_className}.{nameof(UnsetReadonlyDir)}()");
        
        DirectoryInfo dir = new DirectoryInfo(path);
        
        if (IsDirReadonly(dir))
            dir.Attributes &= ~FileAttributes.ReadOnly;
        
        SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        if (recursive)
        {
            foreach (DirectoryInfo subdirectory in dir.GetDirectories("*", searchOption))
            {
                if (IsDirReadonly(subdirectory))
                {
                    subdirectory.Attributes &= ~FileAttributes.ReadOnly;
                }
            }
        }

        foreach (FileInfo file in dir.GetFiles("*", searchOption))
        {
            if (IsFileReadonly(file))
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
    }
}
