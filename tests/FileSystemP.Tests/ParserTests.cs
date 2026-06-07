using System.Collections;
using System.Text;
using FileSystemP.Core;
using FileSystemP.Core.CommandService;

namespace FileSystemP.Tests;

public sealed class ParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Parser _parser;

    public ParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _parser = Parser.CreateParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string At(string name) => Path.Combine(_tempDir, name);

    [Fact]
    public void CreateParser_ReturnsParserInstance()
    {
        var parser = Parser.CreateParser();

        Assert.NotNull(parser);
        Assert.IsType<Parser>(parser);
    }

    [Fact]
    public async Task ExecuteAllParsed_EmptyCommand_ReturnsNoOpResult()
    {
        var result = await _parser.ExecuteAllParsed([]);

        Assert.True(result.Success);
        Assert.False(result.ShouldExit);
        Assert.Null(result.Payload);
        Assert.Equal("No command provided.", result.Message);
    }

    [Fact]
    public async Task ExecuteAllParsed_UnknownCommand_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["unknown"]));

        Assert.Contains("Command not found: unknown", exception.Message);
        Assert.Equal("Parser.IndentifyCommand()", exception.ClassRootCauseName);
    }

    [Fact]
    public async Task Cd_WithValidPath_ReturnsTopLevelEntries()
    {
        File.WriteAllText(At("file.txt"), "a");
        Directory.CreateDirectory(At("subdir"));
        File.WriteAllText(Path.Combine(At("subdir"), "nested.txt"), "b");

        var result = await _parser.ExecuteAllParsed(["cd", _tempDir]);

        var entries = Assert.IsAssignableFrom<IEnumerable<FileSystemInfo>>(result.Payload);
        Assert.Equal("Validated path `" + _tempDir + "`.", result.Message);
        Assert.Contains(entries, entry => entry.Name == "file.txt");
        Assert.Contains(entries, entry => entry.Name == "subdir");
        Assert.DoesNotContain(entries, entry => entry.Name == "nested.txt");
    }

    [Fact]
    public async Task Cd_WithoutPath_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["cd"]));

        Assert.Contains("requires at least 2 argument", exception.Message);
    }

    [Fact]
    public async Task Rename_WithValidArguments_RenamesFile()
    {
        var originalPath = At("old.txt");
        File.WriteAllText(originalPath, "content");

        var result = await _parser.ExecuteAllParsed(["rename", originalPath, "new.txt"]);

        Assert.True(result.Success);
        Assert.False(File.Exists(originalPath));
        Assert.True(File.Exists(At("new.txt")));
        Assert.Equal("Renamed `" + originalPath + "` to `new.txt`.", result.Message);
    }

    [Fact]
    public async Task Rename_WithoutNewName_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["rename", At("old.txt")]));

        Assert.Contains("requires at least 3 arguments", exception.Message);
    }

    [Theory]
    [InlineData("-r")]
    [InlineData("--recursive")]
    public async Task Delete_WithRecursiveFlags_DeletesDirectory(string flag)
    {
        var directory = At("dir");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "file.txt"), "content");

        var result = await _parser.ExecuteAllParsed(["del", directory, flag]);

        Assert.True(result.Success);
        Assert.False(Directory.Exists(directory));
        Assert.Equal("Deleted `" + directory + "`.", result.Message);
    }

    [Theory]
    [InlineData("-nr")]
    [InlineData("--no-recursive")]
    public async Task Delete_WithNoRecursiveFlags_DeletesEmptyDirectory(string flag)
    {
        var directory = At("dir");
        Directory.CreateDirectory(directory);

        var result = await _parser.ExecuteAllParsed(["del", directory, flag]);

        Assert.True(result.Success);
        Assert.False(Directory.Exists(directory));
        Assert.Equal("Deleted `" + directory + "`.", result.Message);
    }

    [Fact]
    public async Task Delete_WithUnknownFlag_ThrowsAppException()
    {
        var filePath = At("file.txt");
        File.WriteAllText(filePath, "content");

        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["del", filePath, "--bogus"]));

        Assert.Contains("Flag `--bogus`not found for command `del`!", exception.Message);
        Assert.Equal("Parser.ParseFlags()", exception.ClassRootCauseName);
    }

    [Fact]
    public async Task Delete_WithoutFlag_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["del", At("file.txt")]));

        Assert.Contains("requires at least 3 arguments", exception.Message);
    }

    [Fact]
    public async Task CreateDirectory_WithValidPath_CreatesDirectory()
    {
        var path = At("newdir");

        var result = await _parser.ExecuteAllParsed(["mkdir", path]);

        Assert.True(Directory.Exists(path));
        Assert.Equal("Created directory `" + path + "`.", result.Message);
    }

    [Fact]
    public async Task CreateDirectory_WithoutPath_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["mkdir"]));

        Assert.Contains("requires at least 2 argument", exception.Message);
    }

    [Fact]
    public async Task CreateFile_WithValidPath_CreatesFile()
    {
        var path = At("new.txt");

        var result = await _parser.ExecuteAllParsed(["mkfile", path]);

        Assert.True(File.Exists(path));
        Assert.Equal("Created file `" + path + "`.", result.Message);
    }

    [Fact]
    public async Task CreateFile_WithoutPath_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["mkfile"]));

        Assert.Contains("requires at least 2 argument", exception.Message);
    }

    [Fact]
    public async Task CreateFileWithContent_WithValidArguments_WritesContent()
    {
        var path = At("content.txt");

        var result = await _parser.ExecuteAllParsed(["mkfilewith", path, "hello world"]);

        Assert.Equal("hello world", File.ReadAllText(path));
        Assert.Equal("Created file `" + path + "` with content.", result.Message);
    }

    [Fact]
    public async Task CreateFileWithContent_ThenReadFileContents_RoundTripsWrittenBytes()
    {
        var path = At("roundtrip.txt");

        await _parser.ExecuteAllParsed(["mkfilewith", path, "roundtrip content"]);
        var result = await _parser.ExecuteAllParsed(["rfilecont", path]);

        var payload = Assert.IsType<FileContentResult>(result.Payload);
        Assert.Equal("roundtrip content", Encoding.UTF8.GetString(payload.Content));
    }

    [Fact]
    public async Task CreateFileWithContent_WithoutContent_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["mkfilewith", At("content.txt")]));

        Assert.Contains("requires at least 3 arguments", exception.Message);
    }

    [Theory]
    [InlineData("-o")]
    [InlineData("--overwrite")]
    public async Task Copy_WithOverwriteFlags_ReplacesExistingDestination(string flag)
    {
        var source = At("source.txt");
        var destination = At("destination.txt");
        File.WriteAllText(source, "new");
        File.WriteAllText(destination, "old");

        var result = await _parser.ExecuteAllParsed(["cp", source, destination, flag]);

        Assert.Equal("new", File.ReadAllText(destination));
        Assert.Equal("Copied `" + source + "` to `" + destination + "` with overwrite flag being set to `True`.", result.Message);
    }

    [Theory]
    [InlineData("-no")]
    [InlineData("--no-overwrite")]
    public async Task Copy_WithNoOverwriteFlags_CopiesWhenDestinationDoesNotExist(string flag)
    {
        var source = At("source.txt");
        var destination = At("destination.txt");
        File.WriteAllText(source, "data");

        var result = await _parser.ExecuteAllParsed(["cp", source, destination, flag]);

        Assert.Equal("data", File.ReadAllText(destination));
        Assert.Equal("Copied `" + source + "` to `" + destination + "` with overwrite flag being set to `False`.", result.Message);
    }

    [Theory]
    [InlineData("-no")]
    [InlineData("--no-overwrite")]
    public async Task Copy_WithNoOverwriteFlags_ThrowsWhenDestinationExists(string flag)
    {
        var source = At("source.txt");
        var destination = At("destination.txt");
        File.WriteAllText(source, "data");
        File.WriteAllText(destination, "existing");

        await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["cp", source, destination, flag]));
    }

    [Fact]
    public async Task Rename_ThenCopyWithOverwrite_PreservesRenamedFileContent()
    {
        var originalPath = At("draft.txt");
        var renamedPath = At("final.txt");
        var copyPath = At("final-copy.txt");
        File.WriteAllText(originalPath, "important text");
        File.WriteAllText(copyPath, "stale");

        await _parser.ExecuteAllParsed(["rename", originalPath, "final.txt"]);
        await _parser.ExecuteAllParsed(["cp", renamedPath, copyPath, "--overwrite"]);

        Assert.False(File.Exists(originalPath));
        Assert.True(File.Exists(renamedPath));
        Assert.Equal("important text", File.ReadAllText(renamedPath));
        Assert.Equal("important text", File.ReadAllText(copyPath));
    }

    [Fact]
    public async Task Copy_WithUnknownFlag_ThrowsAppException()
    {
        var source = At("source.txt");
        var destination = At("destination.txt");
        File.WriteAllText(source, "data");

        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["cp", source, destination, "--bogus"]));

        Assert.Contains("Flag `--bogus`not found for command `cp`!", exception.Message);
        Assert.Equal("Parser.ParseFlags()", exception.ClassRootCauseName);
    }

    [Fact]
    public async Task Copy_WithoutRequiredArguments_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["cp", At("source.txt"), At("destination.txt")]));

        Assert.Contains("requires at least 4 arguments", exception.Message);
    }

    [Fact]
    public async Task ReadFileContents_WithoutLineCount_ReturnsBytes()
    {
        var path = At("file.txt");
        var expected = Encoding.UTF8.GetBytes("hello");
        await File.WriteAllBytesAsync(path, expected);

        var result = await _parser.ExecuteAllParsed(["rfilecont", path]);

        var payload = Assert.IsType<FileContentResult>(result.Payload);
        Assert.Equal(expected, payload.Content);
        Assert.Equal("Read file `" + path + "`.", result.Message);
    }

    [Fact]
    public async Task ReadFileContents_WithPositiveLineCount_ReturnsFirstLines()
    {
        var path = At("file.txt");
        await File.WriteAllLinesAsync(path, ["one", "two", "three"]);

        var result = await _parser.ExecuteAllParsed(["rfilecont", path, "2"]);

        var payload = Assert.IsType<FileLinesResult>(result.Payload);
        Assert.Equal(["one", "two"], payload.Lines);
        Assert.Equal("Read 2 lines from `" + path + "`.", result.Message);
    }

    [Fact]
    public async Task ReadFileContents_WithNegativeLineCount_ReturnsLastLines()
    {
        var path = At("file.txt");
        await File.WriteAllLinesAsync(path, ["one", "two", "three"]);

        var result = await _parser.ExecuteAllParsed(["rfilecont", path, "-2"]);

        var payload = Assert.IsType<FileLinesResult>(result.Payload);
        Assert.Equal(["two", "three"], payload.Lines);
        Assert.Equal("Read 2 lines from `" + path + "`.", result.Message);
    }

    [Fact]
    public async Task ReadFileContents_WithZeroLineCount_ReturnsEmptyLines()
    {
        var path = At("file.txt");
        await File.WriteAllLinesAsync(path, ["one", "two"]);

        var result = await _parser.ExecuteAllParsed(["rfilecont", path, "0"]);

        var payload = Assert.IsType<FileLinesResult>(result.Payload);
        Assert.Empty(payload.Lines);
        Assert.Equal("Read 0 lines from `" + path + "`.", result.Message);
    }

    [Fact]
    public async Task ReadFileContents_WithInvalidLineCount_ThrowsFormatException()
    {
        var path = At("file.txt");
        await File.WriteAllTextAsync(path, "content");

        await Assert.ThrowsAsync<FormatException>(() => _parser.ExecuteAllParsed(["rfilecont", path, "NaN"]));
    }

    [Fact]
    public async Task ReadFileContents_WithoutPath_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["rfilecont"]));

        Assert.Contains("requires at least 2 arguments", exception.Message);
    }

    [Fact]
    public async Task CreateDirectory_CreateNestedFile_CdAndDeleteRecursive_WorksAsOneFlow()
    {
        var directory = At("workspace");
        var nestedFile = Path.Combine(directory, "note.txt");

        await _parser.ExecuteAllParsed(["mkdir", directory]);
        await _parser.ExecuteAllParsed(["mkfilewith", nestedFile, "hello"]);
        var cdResult = await _parser.ExecuteAllParsed(["cd", directory]);
        await _parser.ExecuteAllParsed(["del", directory, "-r"]);

        var entries = Assert.IsAssignableFrom<IEnumerable<FileSystemInfo>>(cdResult.Payload);
        Assert.Contains(entries, entry => entry.Name == "note.txt");
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task Exit_ReturnsExitResult()
    {
        var result = await _parser.ExecuteAllParsed(["exit"]);

        Assert.True(result.Success);
        Assert.True(result.ShouldExit);
        Assert.Equal("Exiting the application. Goodbye!", result.Message);
    }

    [Fact]
    public async Task Exit_WithExtraArguments_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["exit", "now"]));

        Assert.Contains("requires no arguments", exception.Message);
    }

    [Fact]
    public async Task Help_WithoutArguments_ReturnsAvailableCommandNames()
    {
        var result = await _parser.ExecuteAllParsed(["help"]);

        var payload = Assert.IsAssignableFrom<IEnumerable>(result.Payload);
        var values = payload.Cast<object>().Select(x => x.ToString()).ToList();

        Assert.Contains("cd", values);
        Assert.Contains("rename", values);
        Assert.Contains("del", values);
        Assert.Contains("mkdir", values);
        Assert.Contains("mkfile", values);
        Assert.Contains("mkfilewith", values);
        Assert.Contains("cp", values);
        Assert.Contains("rfilecont", values);
        Assert.Contains("exit", values);
        Assert.Contains("help", values);
        Assert.Equal("List of all available commands.", result.Message);
    }

    [Fact]
    public async Task Help_WithKnownCommand_ReturnsTypedCommandName()
    {
        var result = await _parser.ExecuteAllParsed(["help", "cp"]);

        var payload = Assert.IsType<List<string>>(result.Payload);
        Assert.Equal(["Copy"], payload);
        Assert.Equal("Help for command `Copy`.", result.Message);
    }

    [Fact]
    public async Task Help_WithUnknownCommand_FallsBackToAvailableCommandNames()
    {
        var result = await _parser.ExecuteAllParsed(["help", "missing"]);

        var payload = Assert.IsAssignableFrom<IEnumerable>(result.Payload);
        var values = payload.Cast<object>().Select(x => x.ToString()).ToList();

        Assert.Contains("cd", values);
        Assert.Contains("help", values);
        Assert.Equal("List of all available commands.", result.Message);
    }

    [Fact]
    public async Task Help_WithTooManyArguments_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["help", "cp", "extra"]));

        Assert.Contains("requires at least 1 argument", exception.Message);
    }

    [Fact]
    public async Task HelpFlags_WithoutArguments_ReturnsAvailableFlags()
    {
        var result = await _parser.ExecuteAllParsed(["helpflags"]);

        var payload = Assert.IsAssignableFrom<IEnumerable>(result.Payload);
        var values = payload.Cast<object>().Select(x => x.ToString()).ToList();

        Assert.Contains("-o", values);
        Assert.Contains("--overwrite", values);
        Assert.Contains("-r", values);
        Assert.Contains("--recursive", values);
        Assert.Contains("-nr", values);
        Assert.Contains("--no-recursive", values);
        Assert.Contains("-no", values);
        Assert.Contains("--no-overwrite", values);
        Assert.Equal("List of all available flags.", result.Message);
    }

    [Fact]
    public async Task HelpFlags_WithKnownFlag_ReturnsTypedFlagName()
    {
        var result = await _parser.ExecuteAllParsed(["helpflags", "--overwrite"]);

        var payload = Assert.IsType<List<string>>(result.Payload);
        Assert.Equal(["Overwrite"], payload);
        Assert.Equal("Help for flag `Overwrite`.", result.Message);
    }

    [Fact]
    public async Task Explain_WithKnownAndUnknownTargets_ReturnsDescriptions()
    {
        var result = await _parser.ExecuteAllParsed(["explain", "cp", "--recursive", "missing"]);

        var payload = Assert.IsType<Dictionary<string, string>>(result.Payload);

        Assert.Contains("Copies a file to a new destination.", payload["cp"]);
        Assert.Contains("Recursive flag.", payload["--recursive"]);
        Assert.Equal("No description available. This `missing` does not exist.", payload["missing"]);
        Assert.Equal("Explanation of the commands and flags provided.", result.Message);
    }

    [Fact]
    public async Task Explain_WithoutTargets_ThrowsAppException()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() => _parser.ExecuteAllParsed(["explain"]));

        Assert.Contains("requires at least 2 arguments", exception.Message);
    }
}
