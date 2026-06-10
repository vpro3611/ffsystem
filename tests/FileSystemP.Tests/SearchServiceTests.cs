using System.IO;
using FileSystemP.Core.SearchService;
using FileSystemP.Core.SearchService.Options;
using Xunit;

namespace FileSystemP.Tests;

public class SearchServiceTests : IDisposable
{
    private readonly string _testRoot;
    private readonly SearchService _service;

    public SearchServiceTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "FileSystemP_SearchTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
        _service = new SearchService();
        
        SetupTestData();
    }

    private void SetupTestData()
    {
        // Root: 
        // - file1.txt
        // - doc1.pdf
        // - [FolderA]
        //   - file2.txt
        //   - [SubFolderB]
        //     - file3.log
        
        File.WriteAllText(Path.Combine(_testRoot, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(_testRoot, "doc1.pdf"), "content");
        
        var folderA = Path.Combine(_testRoot, "FolderA");
        Directory.CreateDirectory(folderA);
        File.WriteAllText(Path.Combine(folderA, "file2.txt"), "content");
        
        var subFolderB = Path.Combine(folderA, "SubFolderB");
        Directory.CreateDirectory(subFolderB);
        File.WriteAllText(Path.Combine(subFolderB, "file3.log"), "content");
    }

    [Fact]
    public async Task Search_TopDirectoryOnly_ReturnsOnlyTopFiles()
    {
        var options = CreateDefaultOptions();
        options = options with { Option = SearchOption.TopDirectoryOnly, TargetType = SearchTargetType.Files };

        var result = await _service.SearchAsync(_testRoot, options);

        Assert.Equal(2, result.FoundEntries.Count);
        Assert.Contains(result.FoundEntries, e => e.Name == "file1.txt");
        Assert.Contains(result.FoundEntries, e => e.Name == "doc1.pdf");
    }

    [Fact]
    public async Task Search_AllDirectories_ReturnsNestedFiles()
    {
        var options = CreateDefaultOptions();
        options = options with { Option = SearchOption.AllDirectories, TargetType = SearchTargetType.Files };

        var result = await _service.SearchAsync(_testRoot, options);

        Assert.Equal(4, result.FoundEntries.Count);
        Assert.Contains(result.FoundEntries, e => e.Name == "file3.log");
    }

    [Fact]
    public async Task Search_TargetTypeDirectories_ReturnsOnlyFolders()
    {
        var options = CreateDefaultOptions();
        options = options with { Option = SearchOption.AllDirectories, TargetType = SearchTargetType.Directories };

        var result = await _service.SearchAsync(_testRoot, options);

        Assert.Equal(2, result.FoundEntries.Count);
        Assert.All(result.FoundEntries, e => Assert.True(e is DirectoryInfo));
    }

    [Fact]
    public async Task Search_WithPattern_FiltersResults()
    {
        var options = CreateDefaultOptions();
        options = options with { Pattern = "file", Option = SearchOption.AllDirectories };

        var result = await _service.SearchAsync(_testRoot, options);

        Assert.Equal(3, result.FoundEntries.Count);
        Assert.All(result.FoundEntries, e => Assert.Contains("file", e.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_WithExtension_FiltersFiles()
    {
        var options = CreateDefaultOptions();
        options = options with { Extensions = new List<string> { "pdf" }, Option = SearchOption.AllDirectories };

        var result = await _service.SearchAsync(_testRoot, options);

        Assert.Single(result.FoundEntries);
        Assert.Equal("doc1.pdf", result.FoundEntries[0].Name);
    }

    [Fact]
    public async Task Search_Cancellation_AbortsSearch()
    {
        // We create a deep structure to ensure it takes at least some time
        var cts = new CancellationTokenSource();
        var options = CreateDefaultOptions() with { Option = SearchOption.AllDirectories };

        // Cancel immediately
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => 
            await _service.SearchAsync(_testRoot, options, cts.Token));
    }

    private ExtendedOptions CreateDefaultOptions()
    {
        return new ExtendedOptions(
            Option: SearchOption.TopDirectoryOnly,
            TargetType: SearchTargetType.Both,
            Pattern: null,
            NameMode: NameSearchMode.Contains,
            Extensions: null,
            Attributes: null,
            AboveSize: null,
            ExactSize: null,
            BelowSize: null,
            CreatedFromDate: null,
            CreatedExactDate: null,
            CreatedToDate: null,
            ModifiedFromDate: null,
            ModifiedExactDate: null,
            ModifiedToDate: null,
            AccessedFromDate: null,
            AccessedExactDate: null,
            AccessedToDate: null
        );
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }
        catch { /* Ignore cleanup errors */ }
    }
}
