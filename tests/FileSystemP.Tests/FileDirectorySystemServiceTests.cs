using FileSystemP.Core;
using FileSystemP.Core.Services;

namespace FileSystemP.Tests;

public class FileDirectorySystemServiceTests : IDisposable
{
    private readonly string _tempDir;

    public FileDirectorySystemServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string At(string name) => Path.Combine(_tempDir, name);

    // --- GetEntries ---

    [Fact]
    public void GetEntries_ReturnsOnlyTopLevelItems()
    {
        File.WriteAllText(At("file.txt"), "");
        Directory.CreateDirectory(At("subdir"));
        File.WriteAllText(Path.Combine(At("subdir"), "nested.txt"), "");

        var entries = FileDirectorySystemService.GetEntries(_tempDir).ToList();

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void GetEntries_ReturnsBothFilesAndDirectories()
    {
        File.WriteAllText(At("file.txt"), "");
        Directory.CreateDirectory(At("subdir"));

        var entries = FileDirectorySystemService.GetEntries(_tempDir).ToList();

        Assert.Contains(entries, e => e is FileInfo && e.Name == "file.txt");
        Assert.Contains(entries, e => e is DirectoryInfo && e.Name == "subdir");
    }

    // --- Rename ---

    [Fact]
    public void Rename_File_OldPathGoneAndNewPathExists()
    {
        var oldPath = At("old.txt");
        File.WriteAllText(oldPath, "content");

        FileDirectorySystemService.Rename(oldPath, "new.txt");

        Assert.False(File.Exists(oldPath));
        Assert.True(File.Exists(At("new.txt")));
    }

    [Fact]
    public void Rename_File_PreservesContent()
    {
        var oldPath = At("old.txt");
        File.WriteAllText(oldPath, "hello");

        FileDirectorySystemService.Rename(oldPath, "new.txt");

        Assert.Equal("hello", File.ReadAllText(At("new.txt")));
    }

    [Fact]
    public void Rename_Directory_OldPathGoneAndNewPathExists()
    {
        var oldPath = At("OldDir");
        Directory.CreateDirectory(oldPath);

        FileDirectorySystemService.Rename(oldPath, "NewDir");

        Assert.False(Directory.Exists(oldPath));
        Assert.True(Directory.Exists(At("NewDir")));
    }

    // --- Delete ---

    [Fact]
    public void Delete_File_RemovesFile()
    {
        var path = At("file.txt");
        File.WriteAllText(path, "");

        FileDirectorySystemService.Delete(path);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Delete_Directory_RemovesDirectoryAndItsContents()
    {
        var dir = At("dir");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "file.txt"), "");

        FileDirectorySystemService.Delete(dir);

        Assert.False(Directory.Exists(dir));
    }

    // --- CreateDirectory ---

    [Fact]
    public void CreateDirectory_CreatesDirectory()
    {
        var path = At("newdir");

        FileDirectorySystemService.CreateDirectory(path);

        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public void CreateDirectory_DoesNotThrowIfAlreadyExists()
    {
        var path = At("existingdir");
        Directory.CreateDirectory(path);

        var ex = Record.Exception(() => FileDirectorySystemService.CreateDirectory(path));

        Assert.Null(ex);
    }

    // --- CreateFile ---

    [Fact]
    public void CreateFile_CreatesEmptyFile()
    {
        var path = At("new.txt");

        FileDirectorySystemService.CreateFile(path);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void CreateFile_DoesNotOverwriteExistingFile()
    {
        var path = At("existing.txt");
        File.WriteAllText(path, "original");

        FileDirectorySystemService.CreateFile(path);

        Assert.Equal("original", File.ReadAllText(path));
    }

    // --- CreateFileWithContent ---

    [Fact]
    public async Task CreateFileWithContent_CreatesFileWithContent()
    {
        var path = At("content.txt");

        await FileDirectorySystemService.CreateFileWithContent(path, "hello world");

        Assert.Equal("hello world", File.ReadAllText(path));
    }

    [Fact]
    public async Task CreateFileWithContent_OverwritesExistingContent()
    {
        var path = At("content.txt");
        File.WriteAllText(path, "old content");

        await FileDirectorySystemService.CreateFileWithContent(path, "new content");

        Assert.Equal("new content", File.ReadAllText(path));
    }

    // --- Copy ---

    [Fact]
    public void Copy_CopiesFileToDestination()
    {
        var src = At("source.txt");
        var dst = At("dest.txt");
        File.WriteAllText(src, "data");

        FileDirectorySystemService.Copy(src, dst);

        Assert.True(File.Exists(dst));
        Assert.Equal("data", File.ReadAllText(dst));
    }

    [Fact]
    public void Copy_ThrowsAppExceptionIfSourceDoesNotExist()
    {
        Assert.Throws<AppException>(() =>
            FileDirectorySystemService.Copy(At("nonexistent.txt"), At("dest.txt")));
    }

    [Fact]
    public void Copy_ThrowsAppExceptionIfDestinationAlreadyExists()
    {
        var src = At("source.txt");
        var dst = At("dest.txt");
        File.WriteAllText(src, "data");
        File.WriteAllText(dst, "existing");

        Assert.Throws<AppException>(() =>
            FileDirectorySystemService.Copy(src, dst));
    }

    // --- Edge cases ---

    [Fact]
    public void GetEntries_ThrowsAppExceptionForNonexistentPath()
    {
        var path = At("doesnotexist");

        Assert.Throws<AppException>(() =>
            FileDirectorySystemService.GetEntries(path).ToList());
    }

    [Fact]
    public void Rename_ThrowsAppExceptionWhenDestinationAlreadyExists()
    {
        var src = At("original.txt");
        var existing = At("existing.txt");
        File.WriteAllText(src, "a");
        File.WriteAllText(existing, "b");

        Assert.Throws<AppException>(() =>
            FileDirectorySystemService.Rename(src, "existing.txt"));
    }

    [Fact]
    public void Delete_ThrowsAppExceptionWhenDeletingNonEmptyDirectoryNonRecursively()
    {
        var dir = At("dir");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "file.txt"), "");

        Assert.Throws<AppException>(() =>
            FileDirectorySystemService.Delete(dir, recursive: false));
    }

    // --- ReadFileContent ---

    [Fact]
    public async Task ReadFileContent_ReturnsBytesOfFile()
    {
        var path = At("file.txt");
        var bytes = System.Text.Encoding.UTF8.GetBytes("hello");
        File.WriteAllBytes(path, bytes);

        var result = await FileDirectorySystemService.ReadFileContent(path);

        Assert.Equal(bytes, result);
    }

    [Fact]
    public async Task ReadFileContent_ThrowsAppExceptionIfFileDoesNotExist()
    {
        await Assert.ThrowsAsync<AppException>(() =>
            FileDirectorySystemService.ReadFileContent(At("missing.txt")));
    }

    // --- Directory Copy ---

    [Fact]
    public void Copy_Directory_CopiesRecursively()
    {
        var src = At("srcDir");
        var dst = At("dstDir");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "file1.txt"), "data1");
        var sub = Directory.CreateDirectory(Path.Combine(src, "sub"));
        File.WriteAllText(Path.Combine(sub.FullName, "file2.txt"), "data2");

        FileDirectorySystemService.Copy(src, dst);

        Assert.True(Directory.Exists(dst));
        Assert.Equal("data1", File.ReadAllText(Path.Combine(dst, "file1.txt")));
        Assert.True(Directory.Exists(Path.Combine(dst, "sub")));
        Assert.Equal("data2", File.ReadAllText(Path.Combine(dst, "sub", "file2.txt")));
    }

    [Fact]
    public void Copy_Directory_ThrowsAppExceptionOnInfiniteRecursion()
    {
        var src = At("outer");
        var dst = Path.Combine(src, "inner");
        Directory.CreateDirectory(src);

        var ex = Assert.Throws<AppException>(() =>
            FileDirectorySystemService.Copy(src, dst));
        Assert.Contains("infinite recursion", ex.Message.ToLower());
    }
}
