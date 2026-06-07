namespace FileSystemP.Core.Services;

public static class FileDirectorySystemService
{
    
    private const string _className = nameof(FileDirectorySystemService);
    
    public static IEnumerable<FileSystemInfo> GetEntries(string path)
    {
        try
        {
            return new DirectoryInfo(path).EnumerateFileSystemInfos(
                "*",
                new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = false
                });
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(GetEntries)}()", e.Source, e);
        }
    }

    public static void Rename(string path, string newName)
    {
        try
        {
            var parent = Path.GetDirectoryName(path)!;
            var destination = Path.Combine(parent, newName);

            if (Directory.Exists(path))
                Directory.Move(path, destination);
            else
                File.Move(path, destination);
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(Rename)}()", e.Source, e);
        }
    }

    public static void Delete(string path, bool recursive = true)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive);
            }
            else
            {
                File.Delete(path);
            }
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(Delete)}()", e.Source, e);
        }
    }

    public static void CreateDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(CreateDirectory)}()", e.Source, e);
        }
    }

    public static void CreateFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                using var _ = File.Create(path);
            }
        } catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(CreateFile)}()", e.Source, e);
        }
    }
    
    public static async Task CreateFileWithContent(string path, string content)
    {
        try
        {
            await File.WriteAllTextAsync(path, content);
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(CreateFileWithContent)}()", e.Source, e);
        }
    }

    public static void Copy(string source, string destination, bool overwrite = false)
    {
        try
        {
            File.Copy(source, destination, overwrite: overwrite);
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(Copy)}()", e.Source, e);
        }
    }

    public static async Task<byte[]> ReadFileContent(string path)
    {
        try
        {
            return await File.ReadAllBytesAsync(path);
        } 
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(ReadFileContent)}()", e.Source, e);
        }
    }

    public static async Task<List<string>> ReadFileLineByLine(string path, int countOfLines)
    {
        try
        {
            if (countOfLines == 0)
            {
                return [];
            }

            using var reader = new StreamReader(path);

            if (countOfLines > 0)
            {
                List<string> result = [];
                string? line;
                while (result.Count < countOfLines && (line = await reader.ReadLineAsync()) != null)
                {
                    result.Add(line);
                }
                return result;
            }
            else
            {
                int lastN = Math.Abs(countOfLines);
                Queue<string> queue = new(lastN);
                
                string? line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (queue.Count == lastN)
                    {
                        queue.Dequeue();
                    }
                    queue.Enqueue(line);
                }
                return queue.ToList();
            }
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(ReadFileLineByLine)}()", e.Source, e);
        }
    }
}
