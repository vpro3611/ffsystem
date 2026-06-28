using FileSystemP.Core.MetadataService.Providers.Ntfs;
using FileSystemP.Core.Services;
using FileSystemP.WPF.ViewModels;

namespace FileSystemP.Tests;

public class FilePanelViewModelTests
{
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
