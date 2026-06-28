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
                    RecurseSubdirectories = false,
                    AttributesToSkip = 0
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
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                    path,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }
            else
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    path,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
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
    public static void Copy(string source, string destination, bool overwrite = false, IProgress<double>? progress = null)
    {
        try
        {
            string finalDestination = destination;
            if (Directory.Exists(destination))
            {
                // If destination is a directory, copy source INTO it
                string fileName = Path.GetFileName(source);
                finalDestination = Path.Combine(destination, fileName);
            }

            if (Directory.Exists(source))
            {
                var sourceDir = new DirectoryInfo(source);
                int totalFiles = CountFiles(sourceDir);
                int copiedFiles = 0;
                CopyDirectoryRecursive(source, finalDestination, overwrite, progress, totalFiles, ref copiedFiles);
            }
            else
            {
                File.Copy(source, finalDestination, overwrite: overwrite);
                progress?.Report(1.0);
            }
        }
        catch (Exception e)
        {
            throw new AppException(e.Message, $"{_className}.{nameof(Copy)}()", e.Source, e);
        }
    }

    private static int CountFiles(DirectoryInfo dir)
    {
        int count = dir.GetFiles().Length;
        foreach (var subDir in dir.GetDirectories())
            count += CountFiles(subDir);
        return count;
    }

    private static void CopyDirectoryRecursive(string source, string destination, bool overwrite, IProgress<double>? progress, int totalFiles, ref int copiedFiles)
    {
        var sourceDir = new DirectoryInfo(source);
        if (!sourceDir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {source}");

        // Infinite recursion check
        var destFullPath = Path.GetFullPath(destination);
        var sourceFullPath = Path.GetFullPath(source);
        if (destFullPath.StartsWith(sourceFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException($"Cannot copy a directory into itself (infinite recursion detected). Source: {source}, Destination: {destination}");
        }

        if (Directory.Exists(destination) && !overwrite)
            throw new IOException($"The destination directory already exists: {destination}");

        Directory.CreateDirectory(destination);

        foreach (var file in sourceDir.GetFiles())
        {
            string targetPath = Path.Combine(destination, file.Name);
            file.CopyTo(targetPath, overwrite);
            copiedFiles++;
            if (totalFiles > 0)
                progress?.Report((double)copiedFiles / totalFiles);
        }

        foreach (var subDir in sourceDir.GetDirectories())
        {
            string targetPath = Path.Combine(destination, subDir.Name);
            CopyDirectoryRecursive(subDir.FullName, targetPath, overwrite, progress, totalFiles, ref copiedFiles);
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
