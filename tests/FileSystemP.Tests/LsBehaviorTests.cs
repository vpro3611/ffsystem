using FileSystemP.Core;
using FileSystemP.Core.CommandService;
using FileSystemP.Core.Services;
using Xunit;

namespace FileSystemP.Tests;

public class LsBehaviorTests : IDisposable
{
    private readonly string _testDir;
    private readonly Parser _parser;

    public LsBehaviorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "FileSystemP_LsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _parser = new Parser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public async Task ExecuteAllParsed_Ls_Path_ListsCorrectEntries()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, "file1.txt"), "content1");
        Directory.CreateDirectory(Path.Combine(_testDir, "dir1"));
        var command = new List<string> { "ls", _testDir };

        // Act
        var result = await _parser.ExecuteAllParsed(command);

        // Assert
        Assert.True(result.Success);
        var data = Assert.IsType<Dictionary<string, FileSystemInfo>>(result.Payload);
        Assert.Equal(2, data.Count);
        var names = data.Values.Select(v => v.Name).ToList();
        Assert.Contains("file1.txt", names);
        Assert.Contains("dir1", names);
    }

    [Fact]
    public async Task ExecuteAllParsed_Ls_Dot_UsesCurrentDirectory()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDir, "current.txt"), "here");
        var command = new List<string> { "ls", "." };

        // Act
        var result = await _parser.ExecuteAllParsed(command, _testDir);

        // Assert
        Assert.True(result.Success);
        var data = Assert.IsType<Dictionary<string, FileSystemInfo>>(result.Payload);
        Assert.Single(data);
        Assert.Equal("current.txt", data["1"].Name);
    }

    [Fact]
    public async Task ExecuteAllParsed_Ls_Size_SortsCorrectly()
    {
        // Arrange
        string smallFile = Path.Combine(_testDir, "small.txt");
        string largeFile = Path.Combine(_testDir, "large.txt");
        string directory = Path.Combine(_testDir, "adir");

        File.WriteAllText(smallFile, "small"); // 5 bytes
        File.WriteAllText(largeFile, "this is a much larger file content"); // 34 bytes
        Directory.CreateDirectory(directory); // 0 bytes in sorting

        var command = new List<string> { "ls", _testDir, "-s" };

        // Act
        var result = await _parser.ExecuteAllParsed(command);

        // Assert
        Assert.True(result.Success);
        var data = Assert.IsType<Dictionary<string, FileSystemInfo>>(result.Payload);
        var sortedList = data.Values.ToList();
        
        // Ascending order: directory (0), small (5), large (34)
        Assert.Equal("adir", sortedList[0].Name);
        Assert.Equal("small.txt", sortedList[1].Name);
        Assert.Equal("large.txt", sortedList[2].Name);
    }

    [Fact]
    public async Task ExecuteAllParsed_Ls_ModTime_SortsCorrectly()
    {
        // Arrange
        string oldFile = Path.Combine(_testDir, "old.txt");
        string newFile = Path.Combine(_testDir, "new.txt");

        File.WriteAllText(oldFile, "old");
        File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-1));
        
        File.WriteAllText(newFile, "new");
        File.SetLastWriteTime(newFile, DateTime.Now);

        var command = new List<string> { "ls", _testDir, "-mt" };

        // Act
        var result = await _parser.ExecuteAllParsed(command);

        // Assert
        Assert.True(result.Success);
        var data = Assert.IsType<Dictionary<string, FileSystemInfo>>(result.Payload);
        var sortedList = data.Values.ToList();

        Assert.Equal("old.txt", sortedList[0].Name);
        Assert.Equal("new.txt", sortedList[1].Name);
    }

    [Fact]
    public async Task ExecuteAllParsed_Ls_AccessedTime_SortsCorrectly()
    {
        // Arrange
        string oldFile = Path.Combine(_testDir, "old_acc.txt");
        string newFile = Path.Combine(_testDir, "new_acc.txt");

        File.WriteAllText(oldFile, "old");
        File.SetLastAccessTime(oldFile, DateTime.Now.AddDays(-1));
        
        File.WriteAllText(newFile, "new");
        File.SetLastAccessTime(newFile, DateTime.Now);

        var command = new List<string> { "ls", _testDir, "-at" };

        // Act
        var result = await _parser.ExecuteAllParsed(command);

        // Assert
        Assert.True(result.Success);
        var data = Assert.IsType<Dictionary<string, FileSystemInfo>>(result.Payload);
        var sortedList = data.Values.ToList();

        Assert.Equal("old_acc.txt", sortedList[0].Name);
        Assert.Equal("new_acc.txt", sortedList[1].Name);
    }

    [Fact]
    public async Task ExecuteAllParsed_Ls_CreatedTime_SortsCorrectly()
    {
        // Arrange
        string oldFile = Path.Combine(_testDir, "old_cre.txt");
        string newFile = Path.Combine(_testDir, "new_cre.txt");

        File.WriteAllText(oldFile, "old");
        File.SetCreationTime(oldFile, DateTime.Now.AddDays(-1));
        
        File.WriteAllText(newFile, "new");
        File.SetCreationTime(newFile, DateTime.Now);

        var command = new List<string> { "ls", _testDir, "-ct" };

        // Act
        var result = await _parser.ExecuteAllParsed(command);

        // Assert
        Assert.True(result.Success);
        var data = Assert.IsType<Dictionary<string, FileSystemInfo>>(result.Payload);
        var sortedList = data.Values.ToList();

        Assert.Equal("old_cre.txt", sortedList[0].Name);
        Assert.Equal("new_cre.txt", sortedList[1].Name);
    }

    [Fact]
    public async Task ExecuteAllParsed_Ls_InvalidFlag_ThrowsAppException()
    {
        // Arrange
        var command = new List<string> { "ls", _testDir, "--invalid-flag" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(command));
        Assert.Contains("Flag `--invalid-flag`not found", ex.Message);
    }

    [Fact]
    public async Task ExecuteAllParsed_Ls_NonExistentDir_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var command = new List<string> { "ls", Path.Combine(_testDir, "nonexistent") };

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => _parser.ExecuteAllParsed(command));
    }
}
