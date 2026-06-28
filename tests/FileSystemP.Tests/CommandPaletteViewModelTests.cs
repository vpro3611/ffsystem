using FileSystemP.WPF.ViewModels;
using FileSystemP.Core.Services;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Windows.Media;

namespace FileSystemP.Tests;

public class CommandPaletteViewModelTests
{
    [Fact]
    public async Task ExecuteCommand_Help_AddsToHistory()
    {
        var mainVm = new MainWindowViewModel();
        var vm = new CommandPaletteViewModel(p => {}, mainVm, new UndoService());
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
            var vm = new CommandPaletteViewModel(p => {}, mainVm, new UndoService());
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

    [Fact]
    public void Reset_ClearsInputOutputAndCommandHistory()
    {
        var mainVm = new MainWindowViewModel();
        var vm = mainVm.Palette;

        vm.Input = "help";
        vm.OutputHistory.Add(new TerminalLine("old output", Brushes.White));
        vm.CommandHistory.Add("help");
        vm.NavigateHistoryCommand.Execute("Up");

        Assert.Equal("help", vm.Input);

        vm.Reset();

        Assert.Equal(string.Empty, vm.Input);
        Assert.Empty(vm.OutputHistory);
        Assert.Empty(vm.CommandHistory);
        Assert.NotEmpty(vm.Prompt);
    }

    [Fact]
    public async Task ExecuteCommand_MkFileWith_BacktickWrappedContent_TreatsContentAsSingleArgument()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "FileSystemP_ViewModelTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            var mainVm = new MainWindowViewModel();
            var vm = new CommandPaletteViewModel(p => { }, mainVm, new UndoService());
            vm.UpdatePrompt(testDir);

            var filePath = Path.Combine(testDir, "new.txt");
            vm.Input = "mkfilewith new.txt `content new new new`";

            await vm.ExecuteCommand();

            Assert.True(File.Exists(filePath));
            Assert.Equal("content new new new", File.ReadAllText(filePath));
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
    public async Task ExecuteCommand_WithUnmatchedBacktick_ShowsFriendlyError()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "FileSystemP_ViewModelTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            var mainVm = new MainWindowViewModel();
            var vm = new CommandPaletteViewModel(p => { }, mainVm, new UndoService());
            vm.UpdatePrompt(testDir);

            vm.Input = "mkfilewith new.txt `content new new new";

            await vm.ExecuteCommand();

            var errorLine = Assert.Single(vm.OutputHistory, line => line.Text.StartsWith("Error:"));
            Assert.Equal("Error: Unmatched quote: missing closing `", errorLine.Text);
            Assert.False(File.Exists(Path.Combine(testDir, "new.txt")));
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
