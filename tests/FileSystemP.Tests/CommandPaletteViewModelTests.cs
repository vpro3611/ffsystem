using FileSystemP.WPF.ViewModels;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace FileSystemP.Tests;

public class CommandPaletteViewModelTests
{
    [Fact]
    public async Task ExecuteCommand_Help_AddsToHistory()
    {
        var mainVm = new MainWindowViewModel();
        var vm = new CommandPaletteViewModel(p => {}, mainVm);
        vm.Input = "help";
        await vm.ExecuteCommand();
        Assert.NotEmpty(vm.OutputHistory);
    }

    [Fact]
    public async Task ExecuteCommand_Ls_AddsFormattedResultsToHistory()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), "FileSystemP_ViewModelTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        try
        {
            var fileName = "testfile.txt";
            var filePath = Path.Combine(testDir, fileName);
            File.WriteAllText(filePath, "hello world");
            long fileSize = new FileInfo(filePath).Length;

            var mainVm = new MainWindowViewModel();
            var vm = new CommandPaletteViewModel(p => {}, mainVm);
            vm.UpdatePrompt(testDir);
            vm.Input = $"ls {testDir}";

            // Act
            await vm.ExecuteCommand();

            // Assert
            // OutputHistory[0] is the command echo
            // OutputHistory[1] is the success message "List of files in directory..."
            // OutputHistory[2] is the first ls entry
            Assert.True(vm.OutputHistory.Count >= 3);
            var resultLine = vm.OutputHistory.Last();
            Assert.Contains(fileName, resultLine.Text);
            Assert.Contains(filePath, resultLine.Text);
            Assert.Contains($"({fileSize} bytes)", resultLine.Text);
            Assert.Contains("[1]", resultLine.Text);
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
