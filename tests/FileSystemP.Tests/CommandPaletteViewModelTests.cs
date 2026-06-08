using FileSystemP.WPF.ViewModels;
using Xunit;
using System.Threading.Tasks;

namespace FileSystemP.Tests;

public class CommandPaletteViewModelTests
{
    [Fact]
    public async Task ExecuteCommand_Help_AddsToHistory()
    {
        var vm = new CommandPaletteViewModel(p => {});
        vm.Input = "help";
        await vm.ExecuteCommand();
        Assert.NotEmpty(vm.OutputHistory);
    }
}
