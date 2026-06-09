using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSystemP.Core.CommandService;
using FileSystemP.Core.Services;
using FileSystemP.WPF.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace FileSystemP.WPF.ViewModels;

public partial class CommandPaletteViewModel : ObservableObject
{
    private readonly Parser _parser;
    private readonly Action<string> _onNavigate;
    private readonly MainWindowViewModel _mainWindow;
    private string _currentDirectory = string.Empty;

    [ObservableProperty]
    private string _input = string.Empty;

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private bool _isVisible = false;

    [RelayCommand]
    private void ToggleVisibility()
    {
        IsVisible = !IsVisible;
        if (!IsVisible)
        {
            OutputHistory.Clear();
        }
    }

    public ObservableCollection<TerminalLine> OutputHistory { get; } = new();
    public List<string> CommandHistory { get; } = new();
    private int _historyIndex = -1;

    public CommandPaletteViewModel(Action<string> onNavigate, MainWindowViewModel mainWindow, IUndoService undoService)
    {
        _onNavigate = onNavigate;
        _mainWindow = mainWindow;
        _parser = Parser.CreateParser(undoService);
        UpdatePrompt();
    }

    public void UpdatePrompt(string? path = null)
    {
        string pc = Environment.MachineName;
        string user = Environment.UserName;
        string dir = path ?? Directory.GetCurrentDirectory();
        _currentDirectory = dir;
        Prompt = $"[{pc}]\\{user} @ {dir} >";
    }

    [RelayCommand]
    public async Task ExecuteCommand()
    {
        if (string.IsNullOrWhiteSpace(Input)) return;

        string cmdText = Input.Trim();
        CommandHistory.Add(cmdText);
        _historyIndex = CommandHistory.Count;
        
        OutputHistory.Add(new TerminalLine($"{Prompt} {cmdText}", Brushes.Green));

        if (cmdText.ToLower() == "clear")
        {
            OutputHistory.Clear();
            Input = string.Empty;
            return;
        }

        var parts = cmdText.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        
        // Handle 'cp' specifically to support progress bar
        if (parts[0].ToLower() == "cp")
        {
            await ExecuteCopyCommand(parts);
        }
        else
        {
            try 
            {
                var result = await _parser.ExecuteAllParsed(parts, _currentDirectory);
                HandleResult(result, parts);
            }
            catch (Exception ex)
            {
                OutputHistory.Add(new TerminalLine($"Error: {ex.Message}", Brushes.Red));
            }
        }

        Input = string.Empty;
    }   
    
    // TODO:
    // This command currently performs its own flag parsing.
    // Future refactor: use Parser.ParseFlags() result instead
    // so command validation remains consistent with the backend parser.
    // also, this action IS NOT getting pushed to undo stack, so we cannot undo this,
    // keep this in mind!!!!!

    private async Task ExecuteCopyCommand(List<string> parts)
    {
        try
        {
            // Simple parsing for cp: cp <source> <destination> <flag>
            if (parts.Count < 4)
            {
                OutputHistory.Add(new TerminalLine("Error: cp requires source, destination, and flag.", Brushes.Red));
                return;
            }

            string source = parts[1];
            string destination = parts[2];
            bool overwrite = parts[3].Contains("-o") || parts[3].Contains("--overwrite");

            var progressLine = new TerminalLine("Copying... 0%", Brushes.Cyan);
            OutputHistory.Add(progressLine);
            int lastIndex = OutputHistory.Count - 1;

            var progress = new Progress<double>(p =>
            {
                OutputHistory[lastIndex] = new TerminalLine($"Copying... {(int)(p * 100)}%", Brushes.Cyan);
            });

            await Task.Run(() => FileSystemP.Core.Services.FileDirectorySystemService.Copy(source, destination, overwrite, progress));
            
            OutputHistory[lastIndex] = new TerminalLine("Copy complete.", Brushes.White);
        }
        catch (Exception ex)
        {
            OutputHistory.Add(new TerminalLine($"Copy Error: {ex.Message}", Brushes.Red));
        }
    }

    private void HandleResult(CommandResult result, List<string> parts)
    {
        if (result.Success)
        {
            if (result.Message != null)
                OutputHistory.Add(new TerminalLine(result.Message, Brushes.White));
            
            if (result.Payload is IEnumerable<string> list)
            {
                foreach (var item in list)
                {
                    OutputHistory.Add(new TerminalLine($"  {item}", Brushes.White));
                }
            }
            else if (result.Payload is Dictionary<string, string> dict)
            {
                foreach (var kvp in dict)
                {
                    OutputHistory.Add(new TerminalLine($"{kvp.Key}: {kvp.Value}", Brushes.White));
                }
            }
            else if (result.Payload is Dictionary<string, FileSystemInfo> lsResult)
            {
                foreach (var kvp in lsResult)
                {
                    string size = kvp.Value is FileInfo file ? $" ({file.Length} bytes)" : "";
                    OutputHistory.Add(new TerminalLine($"[{kvp.Key}] {kvp.Value.Name} - {kvp.Value.FullName}{size}", Brushes.White));
                }
            }

            if (result.ShouldExit)
            {
                Application.Current.Shutdown();
            }

            if (result.ShouldOpenProperties && result.Payload != null)
            {
                PropertiesWindow.ShowFor(result.Payload, Application.Current.MainWindow);
            }

            if (result.ShouldGoBack)
            {
                if (_mainWindow.NavigateBackCommand.CanExecute(null))
                    _mainWindow.NavigateBackCommand.Execute(null);
                else
                    OutputHistory.Add(new TerminalLine("Cannot go back: history is empty.", Brushes.Yellow));
            }

            if (result.ShouldGoForward)
            {
                if (_mainWindow.NavigateForwardCommand.CanExecute(null))
                    _mainWindow.NavigateForwardCommand.Execute(null);
                else
                    OutputHistory.Add(new TerminalLine("Cannot go forward: history is empty.", Brushes.Yellow));
            }

            if (result.GoHomePath != null)
            {
                _onNavigate?.Invoke(result.GoHomePath);
            }

            if (result.ShouldUndo)
            {
                if (_mainWindow.Panel.UndoCommand.CanExecute(null))
                    _mainWindow.Panel.UndoCommand.Execute(null);
                else
                    OutputHistory.Add(new TerminalLine("Nothing to undo.", Brushes.Yellow));
            }

            if (result.ShouldOpenSearch)
            {
                _mainWindow.SelectedSidebarSectionIndex = 1;
            }

            if (result.ShouldToggleHidden)
            {
                _mainWindow.Panel.ShowHiddenFiles = !_mainWindow.Panel.ShowHiddenFiles;
                OutputHistory.Add(new TerminalLine($"Show Hidden Files: {_mainWindow.Panel.ShowHiddenFiles}", Brushes.White));
            }

            // Handle navigation
            if (parts[0].ToLower() == "cd" && result.Payload is CdResult cdResult)
            {
                _onNavigate?.Invoke(cdResult.FullPath);
            }
        }
    }

    [RelayCommand]
    public void NavigateHistory(string direction)
    {
        if (CommandHistory.Count == 0) return;

        if (direction == "Up")
        {
            if (_historyIndex > 0)
                _historyIndex--;
        }
        else if (direction == "Down")
        {
            if (_historyIndex < CommandHistory.Count - 1)
                _historyIndex++;
            else if (_historyIndex == CommandHistory.Count - 1)
            {
                _historyIndex = CommandHistory.Count;
                Input = string.Empty;
                return;
            }
        }

        if (_historyIndex >= 0 && _historyIndex < CommandHistory.Count)
        {
            Input = CommandHistory[_historyIndex];
        }
    }
}
