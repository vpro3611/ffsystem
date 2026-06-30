using System.Windows.Media;
using FileSystemP.Core.Services;
using FileSystemP.WPF.ViewModels;

namespace FileSystemP.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void TryExecuteKeyBinding_UsesConfiguredSearchBinding()
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), "FileSystemP_KeyBindingTests_" + Guid.NewGuid().ToString("N") + ".json");
        var settingsService = new KeyBindingSettingsService(settingsPath);
        settingsService.EnsureSettingsFileIsValid();
        settingsService.SetBinding("search", "Ctrl+Shift+F", overwrite: true);

        try
        {
            var vm = new MainWindowViewModel(settingsService);

            bool executed = vm.TryExecuteKeyBinding("Ctrl+Shift+F");

            Assert.True(executed);
            Assert.Equal(1, vm.SelectedSidebarSectionIndex);
        }
        finally
        {
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }
        }
    }

    [Fact]
    public void OpenDetachedTerminal_SetsDetachedState()
    {
        var vm = new MainWindowViewModel();

        vm.OpenDetachedTerminal();

        Assert.True(vm.IsTerminalDetached);
        Assert.False(vm.Palette.IsVisible);
    }

    [Fact]
    public void RestoreEmbeddedTerminalAfterDetachedClose_ClearsPaletteAndShowsEmbeddedHost()
    {
        var vm = new MainWindowViewModel();
        vm.Palette.Input = "help";
        vm.Palette.OutputHistory.Add(new TerminalLine("old output", Brushes.White));

        vm.OpenDetachedTerminal();
        vm.RestoreEmbeddedTerminalAfterDetachedClose();

        Assert.False(vm.IsTerminalDetached);
        Assert.True(vm.Palette.IsVisible);
        Assert.Empty(vm.Palette.OutputHistory);
        Assert.Equal(string.Empty, vm.Palette.Input);
    }

    [Fact]
    public void OpenDetachedTerminal_WhenAlreadyDetached_DoesNotResetPaletteState()
    {
        var vm = new MainWindowViewModel();
        vm.OpenDetachedTerminal();
        vm.Palette.Input = "still here";

        vm.OpenDetachedTerminal();

        Assert.True(vm.IsTerminalDetached);
        Assert.Equal("still here", vm.Palette.Input);
    }
}
