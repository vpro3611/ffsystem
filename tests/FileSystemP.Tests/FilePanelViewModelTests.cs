using FileSystemP.Core.MetadataService.Providers.Ntfs;
using FileSystemP.Core.Services;
using FileSystemP.WPF.ViewModels;

namespace FileSystemP.Tests;

public class FilePanelViewModelTests
{
    [Fact]
    public async Task MoveEntryToDirectory_MovesFileIntoDestinationDirectory()
    {
        var visitedPaths = new List<string>();
        var launchedFiles = new List<string>();
        var testDir = Path.Combine(Path.GetTempPath(), "FileSystemP_FilePanelTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            var sourcePath = Path.Combine(testDir, "source.txt");
            var destinationDirectory = Path.Combine(testDir, "destination");
            var finalPath = Path.Combine(destinationDirectory, "source.txt");
            File.WriteAllText(sourcePath, "content");
            Directory.CreateDirectory(destinationDirectory);

            var vm = new FilePanelViewModel(
                visitedPaths.Add,
                new NtfsMetadataProvider(),
                new UndoService(),
                launchedFiles.Add);

            await vm.MoveEntryToDirectory(sourcePath, destinationDirectory);

            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(finalPath));
            Assert.Equal("content", File.ReadAllText(finalPath));
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    [Fact]
    public async Task MoveEntryToDirectory_MovesDirectoryIntoDestinationDirectory()
    {
        var visitedPaths = new List<string>();
        var launchedFiles = new List<string>();
        var testDir = Path.Combine(Path.GetTempPath(), "FileSystemP_FilePanelTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            var sourceDirectory = Path.Combine(testDir, "sourceDir");
            var destinationDirectory = Path.Combine(testDir, "destination");
            var movedDirectory = Path.Combine(destinationDirectory, "sourceDir");
            Directory.CreateDirectory(sourceDirectory);
            Directory.CreateDirectory(destinationDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "nested.txt"), "nested content");

            var vm = new FilePanelViewModel(
                visitedPaths.Add,
                new NtfsMetadataProvider(),
                new UndoService(),
                launchedFiles.Add);

            await vm.MoveEntryToDirectory(sourceDirectory, destinationDirectory);

            Assert.False(Directory.Exists(sourceDirectory));
            Assert.True(Directory.Exists(movedDirectory));
            Assert.Equal("nested content", File.ReadAllText(Path.Combine(movedDirectory, "nested.txt")));
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    [Fact]
    public void OpenPath_WithDirectory_NavigatesToDirectory()
    {
        var visitedPaths = new List<string>();
        var launchedFiles = new List<string>();
        var testDir = Path.Combine(Path.GetTempPath(), "FileSystemP_FilePanelTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            var vm = new FilePanelViewModel(
                visitedPaths.Add,
                new NtfsMetadataProvider(),
                new UndoService(),
                launchedFiles.Add);

            vm.OpenPath(testDir);

            Assert.Equal([testDir], visitedPaths);
            Assert.Empty(launchedFiles);
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    [Fact]
    public void OpenPath_WithFile_UsesInjectedFileLauncher()
    {
        var visitedPaths = new List<string>();
        var launchedFiles = new List<string>();
        var testDir = Path.Combine(Path.GetTempPath(), "FileSystemP_FilePanelTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            var filePath = Path.Combine(testDir, "document.txt");
            File.WriteAllText(filePath, "content");

            var vm = new FilePanelViewModel(
                visitedPaths.Add,
                new NtfsMetadataProvider(),
                new UndoService(),
                launchedFiles.Add);

            vm.OpenPath(filePath);

            Assert.Empty(visitedPaths);
            Assert.Equal([filePath], launchedFiles);
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }
}
