using FileSystemP.Core.AttributeService;
using FileSystemP.Core.Services;

namespace FileSystemP.Core.CommandService;


public class Parser : IParser
{
    
    private static readonly Dictionary<string, AvailableCommands> CommandMap = new()
    {
        ["cd"] = AvailableCommands.Cd,
        ["rename"] = AvailableCommands.Rename,
        ["del"] = AvailableCommands.Delete,
        ["mkdir"] = AvailableCommands.CreateDirectory,
        ["mkfile"] = AvailableCommands.CreateFile,
        ["mkfilewith"] = AvailableCommands.CreateFileWithContent,
        ["cp"] = AvailableCommands.Copy,
        ["rfilecont"] = AvailableCommands.ReadFileContents,
        ["exit"] = AvailableCommands.Exit,
        ["help"] = AvailableCommands.Help,
        ["helpflags"] = AvailableCommands.HelpForFlags,
        ["explain"] = AvailableCommands.Explain,
        ["explainall"] = AvailableCommands.ExplainAll,
        ["ls"] = AvailableCommands.Ls
    };

    private static readonly Dictionary<string, FlagsForCommands> FlagMap = new()
    {
        ["-o"] = FlagsForCommands.Overwrite,
        ["--overwrite"] = FlagsForCommands.Overwrite,
        ["-r"] = FlagsForCommands.Recursive,
        ["--recursive"] = FlagsForCommands.Recursive,
        ["-nr"] = FlagsForCommands.NoRecursive,
        ["--no-recursive"] = FlagsForCommands.NoRecursive,
        ["-no"] = FlagsForCommands.NoOverwrite,
        ["--no-overwrite"] = FlagsForCommands.NoOverwrite,
        ["-s"] = FlagsForCommands.LsSize,
        ["--size"] = FlagsForCommands.LsSize,
        ["-mt"] = FlagsForCommands.LsModTime,
        ["--mod-time"] = FlagsForCommands.LsModTime,
        ["-at"] = FlagsForCommands.LsAccessedTime,
        ["--accessed-time"] = FlagsForCommands.LsAccessedTime,
        ["-ct"] = FlagsForCommands.LsCreatedTime,
        ["--created-time"] = FlagsForCommands.LsCreatedTime,
        ["-oh"] = FlagsForCommands.LsOnlyHidden,
        ["--only-hidden"] = FlagsForCommands.LsOnlyHidden,
        ["-nh"] = FlagsForCommands.LsNoHidden,
        ["--no-hidden"] = FlagsForCommands.LsNoHidden,
    };

    private static readonly Dictionary<string, string> CommandAndFlagDescriptions = new()
    {
        ["cd"] = "Changes the current location context to the target path by validating that the path can be opened. Flags: none. Example: cd C:\\Temp",
        ["rename"] = "Renames a file or directory. Usage: rename <path> <new-name>. Flags: none. Example: rename C:\\Temp\\old.txt new.txt",
        ["del"] = "Deletes a file or directory. Usage: del <path> <flag>. Flags: -r/--recursive deletes directories recursively, -nr/--no-recursive deletes a directory only when it is empty. Example: del C:\\Temp\\Logs -r",
        ["mkdir"] = "Creates a directory if it does not already exist. Usage: mkdir <path>. Flags: none. Example: mkdir C:\\Temp\\NewFolder",
        ["mkfile"] = "Creates an empty file if it does not already exist. Usage: mkfile <path>. Flags: none. Example: mkfile C:\\Temp\\notes.txt",
        ["mkfilewith"] = "Creates a file and writes content into it, overwriting existing content if the file already exists. Usage: mkfilewith <path> <content>. Flags: none. Example: mkfilewith C:\\Temp\\notes.txt Hello",
        ["cp"] = "Copies a file to a new destination. Usage: cp <source> <destination> <flag>. Flags: -o/--overwrite replaces the destination if it exists, -no/--no-overwrite fails when the destination already exists. Example: cp C:\\Temp\\a.txt C:\\Backup\\a.txt -o",
        ["rfilecont"] = "Reads file contents. Usage: rfilecont <path> or rfilecont <path> <line-count>. With no line count it reads the whole file as bytes; with a positive line count it reads the first N lines; with a negative line count it reads the last N lines. Flags: none. Example: rfilecont C:\\Temp\\notes.txt 10",
        ["exit"] = "Exits the command workflow or application when the outer caller handles the exit result. Flags: none. Example: exit",
        ["help"] = "Displays help information for available commands and flags. Or your can request help for specific command or flag: help <target>. Flags: none. Example: help",
        ["helpflags"] = "Displays help information for available flags. Usage: helpflags or helpflags <flag>. Flags: none. Example: helpflags --overwrite",
        ["explain"] = "Explains one or more commands or flags. Usage: explain <target> [additional-targets]. Flags: none. Example: explain cp --overwrite",
        ["explainall"] = "Explains all available commands and flags. Flags: none. Example: explainall",
        ["ls"] = "Lists the contents of the current directory or specific passed directory. Flags: none. Example: ls path",
        ["-o"] = "Overwrite flag. Used with: cp. Effect: allows the destination file to be replaced if it already exists. Example: cp C:\\Temp\\a.txt C:\\Backup\\a.txt -o",
        ["--overwrite"] = "Overwrite flag. Used with: cp. Effect: allows the destination file to be replaced if it already exists. Example: cp C:\\Temp\\a.txt C:\\Backup\\a.txt --overwrite",
        ["-r"] = "Recursive flag. Used with: del. Effect: allows deleting a directory together with all nested files and subdirectories. Example: del C:\\Temp\\Logs -r",
        ["--recursive"] = "Recursive flag. Used with: del. Effect: allows deleting a directory together with all nested files and subdirectories. Example: del C:\\Temp\\Logs --recursive",
        ["-nr"] = "No-recursive flag. Used with: del. Effect: deletes a directory only if it is empty; otherwise the operation fails. Example: del C:\\Temp\\Logs -nr",
        ["--no-recursive"] = "No-recursive flag. Used with: del. Effect: deletes a directory only if it is empty; otherwise the operation fails. Example: del C:\\Temp\\Logs --no-recursive",
        ["-no"] = "No-overwrite flag. Used with: cp. Effect: prevents replacing the destination file if it already exists. Example: cp C:\\Temp\\a.txt C:\\Backup\\a.txt -no",
        ["--no-overwrite"] = "No-overwrite flag. Used with: cp. Effect: prevents replacing the destination file if it already exists. Example: cp C:\\Temp\\a.txt C:\\Backup\\a.txt --no-overwrite",
        ["-s"] = "Size flag. Used with: ls. Effect: list files in directory sorted by size in ascending order (directories are evaluated as 0 for speed purposes). Example: ls path -s",
        ["--size"] = "Size flag. Used with: ls. Effect: list files in directory sorted by size in ascending order (directories are evaluated as 0 for speed purposes). Example: ls path --size",
        ["-mt"] = "Modified time flag. Used with: ls. Effect: list files in directory sorted by modified time in ascending order. Example: ls path -mt",
        ["--mod-time"] = "Modified time flag. Used with: ls. Effect: list files in directory sorted by modified time in ascending order. Example: ls path --mod-time",
        ["-at"] = "Accessed time flag. Used with: ls. Effect: list files in directory sorted by accessed time in ascending order. Example: ls path -at",
        ["--accessed-time"] = "Accessed time flag. Used with: ls. Effect: list files in directory sorted by accessed time in ascending order. Example: ls path --accessed-time",
        ["-ct"] = "Created time flag. Used with: ls. Effect: list files in directory sorted by created time in ascending order. Example: ls path -ct",
        ["--created-time"] = "Created time flag. Used with: ls. Effect: list files in directory sorted by created time in ascending order. Example: ls path --created-time",
        ["-oh"] = "Only hidden files flag. Used with: ls. Effect: list only hidden files in directory disregarding default files. Example: ls path -oh",
        ["--only-hidden"] = "Only hidden files flag. Used with: ls. Effect: list only hidden files in directory disregarding default files. Example: ls path --only-hidden",
        ["-nh"] = "No hidden files flag. Used with ls. Effect: list all files in directory including hidden files. Example: ls path -nh",
        ["--no-hidden"] = "No hidden files flag. Used with ls. Effect: list all files in directory including hidden files. Example: ls path --no-hidden",
    };


    public static Parser CreateParser()
    {
        return new Parser();
    }
    
    
    
    private FlagsForCommands ParseFlags(string potentialFlag, string nameOfCurrentCommand) 
    {
        if (FlagMap.TryGetValue(potentialFlag, out var typedFlag))
        {
            return typedFlag;
        }

        throw new AppException($"Flag `{potentialFlag}`not found for command `{nameOfCurrentCommand}`!", $"{nameof(Parser)}.{nameof(ParseFlags)}()");
    }
    
    private bool ValidateCommand(List<string> command)
    {
        return command.Count != 0;
    }

    private List<string> ExcludeCommandFromArray(List<string> command)
    {
        return command.Skip(1).ToList();
    }
    
    private bool CheckMinLengthForEachCommand(AvailableCommands currentCommand, List<string> command)
    {
        List<string> noCommandArray = ExcludeCommandFromArray(command);
        switch (currentCommand)
        {
            case AvailableCommands.Cd:
                return noCommandArray.Count == 1;
            case AvailableCommands.Rename:
                return noCommandArray.Count == 2;
            case AvailableCommands.Delete:
                return noCommandArray.Count == 2;
            case AvailableCommands.CreateDirectory:
                return noCommandArray.Count == 1;
            case AvailableCommands.CreateFile:
                return noCommandArray.Count == 1;
            case AvailableCommands.CreateFileWithContent:
                return noCommandArray.Count == 2;
            case AvailableCommands.Copy:
                return noCommandArray.Count == 3;
            case AvailableCommands.ReadFileContents:
                return noCommandArray.Count == 1 || noCommandArray.Count == 2;
            case AvailableCommands.Help: 
                return noCommandArray.Count == 0 || noCommandArray.Count == 1;
            case AvailableCommands.HelpForFlags:
                return noCommandArray.Count == 0 || noCommandArray.Count == 1;
            case AvailableCommands.Exit:
                return noCommandArray.Count == 0;
            case AvailableCommands.Explain: 
                return noCommandArray.Count > 0;
            case AvailableCommands.ExplainAll:
                return noCommandArray.Count == 0;
            case AvailableCommands.Ls:
                    return noCommandArray.Count >= 1 && noCommandArray.Count <= 2; // [ls] dir --flag
            default:
                return false;
        }
    }
    
    private AvailableCommands IndentifyCommand(string command)
    {
        if (CommandMap.TryGetValue(command, out var typedCommand))
        {
            return typedCommand;
        }
        throw new AppException($"Command not found: {command}\nType 'help' in order to see all available commands!",
            $"{nameof(Parser)}.{nameof(IndentifyCommand)}()");
    }

    private CommandResult ExecuteCd(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 2 argument!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }

        string pathNavigateTo = command[1];
        var dirInfo = new DirectoryInfo(pathNavigateTo);
        if (!dirInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Directory not found: {pathNavigateTo}");
        }

        var entries = FileDirectorySystemService.GetEntries(dirInfo.FullName).ToList();
        // Return both the entries and the absolute path
        return CommandResult.Ok(new CdResult(entries, dirInfo.FullName), $"Validated path `{dirInfo.FullName}`.");
    }
    
    private CommandResult ExecuteRename(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 3 arguments!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        string pathForRename = command[1];
        string newName = command[2];
        FileDirectorySystemService.Rename(pathForRename, newName);
        return CommandResult.Ok(message: $"Renamed `{pathForRename}` to `{newName}`.");
    }

    private CommandResult ExecuteDelete(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 3 arguments!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        string pathForDel = command[1];
        FlagsForCommands flag = ParseFlags(command[2], nameOfCommand);
        bool recursive = flag == FlagsForCommands.Recursive;
        FileDirectorySystemService.Delete(pathForDel, recursive);
        return CommandResult.Ok(message: $"Deleted `{pathForDel}`.");
    }

    private CommandResult ExecuteCreateDirectory(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 2 argument!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        string pathForCreateDir = command[1];
        FileDirectorySystemService.CreateDirectory(pathForCreateDir);
        return CommandResult.Ok(message: $"Created directory `{pathForCreateDir}`.");
    }

    private CommandResult ExecuteCreateFile(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 2 argument!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        string pathForCreateFile = command[1];
        FileDirectorySystemService.CreateFile(pathForCreateFile);
        return CommandResult.Ok(message: $"Created file `{pathForCreateFile}`.");
    }

    private async Task<CommandResult> ExecuteCreateFileWithContent(AvailableCommands typedCommand, string nameOfCommand,
        List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 3 arguments!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        string pathForCreateFile = command[1];
        string content = command[2];
        await FileDirectorySystemService.CreateFileWithContent(pathForCreateFile, content);
        return CommandResult.Ok(message: $"Created file `{pathForCreateFile}` with content.");
    }
    
    private CommandResult ExecuteCopy(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 4 arguments!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        string source = command[1];
        string destination = command[2]; // WARNING - check for indexes!!!
        FlagsForCommands flagCopy = ParseFlags(command[3], nameOfCommand);
                
        bool overwrites = flagCopy == FlagsForCommands.Overwrite;
        FileDirectorySystemService.Copy(source, destination, overwrites);
        return CommandResult.Ok(message: $"Copied `{source}` to `{destination}` with overwrite flag being set to `{overwrites}`.");
    }

    private async Task<CommandResult> ExecuteReadFileContents(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 2 arguments (or 3 if you want to read N lines)!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }

        string path = command[1];
        int? linesToRead = command.Count == 3 ? int.Parse(command[2]) : null;

        if (linesToRead is null)
        {
            byte[] content = await FileDirectorySystemService.ReadFileContent(path);
            return CommandResult.Ok(new FileContentResult(content), $"Read file `{path}`.");
        }

        var lines = await FileDirectorySystemService.ReadFileLineByLine(path, linesToRead.Value);
        return CommandResult.Ok(new FileLinesResult(lines), $"Read {lines.Count} lines from `{path}`.");
    }

    private CommandResult ExecuteExit(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires no arguments!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }

        return CommandResult.Exit("Exiting the application. Goodbye!");
    }
    
    private CommandResult ExecuteHelp(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 1 argument (2 in case of help for a specific command)!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        var specificCommand = command.Count == 2 ? command[1] : null;
        
        if (specificCommand is not null)
        {
            if (CommandMap.TryGetValue(specificCommand, out var typedSpecificCommand))
            {
                return CommandResult.Ok(new List<string> { typedSpecificCommand.ToString() }, $"Help for command `{typedSpecificCommand}`.");
            }
        }
        return CommandResult.Ok(CommandMap.Keys.ToList(), "List of all available commands.");
    }

    private CommandResult ExecuteHelpForFlags(AvailableCommands typedCommand, string nameOfCommand,
        List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 1 argument (2 in case of help for a specific flag)!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        var specificCommand = command.Count == 2 ? command[1] : null;
        
        if (specificCommand is not null)
        {
            if (FlagMap.TryGetValue(specificCommand, out var typedSpecificCommand))
            {
                return CommandResult.Ok(new List<string> { typedSpecificCommand.ToString() }, $"Help for flag `{typedSpecificCommand}`.");
            }
        }
        return CommandResult.Ok(FlagMap.Keys.ToList(), "List of all available flags.");
    }

    private CommandResult ExecuteExplain(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires at least 2 arguments," +
                                   $"where the 1st argument is the command `explain` and the 2nd and so on arguments are the flags or commands to explain!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        List<string> forExplanation = command.Skip(1).ToList();
        Dictionary<string, string> explanation = new();
        foreach (string element in forExplanation) 
        {
            if (CommandAndFlagDescriptions.TryGetValue(element, out var description))
            {
                explanation.Add(element, description);
            }
            else
            {
                explanation.Add(element, $"No description available. This `{element}` does not exist.");
            }
        }
        
        return CommandResult.Ok(explanation, "Explanation of the commands and flags provided.");
    }

    private CommandResult ExecuteExplainAll(AvailableCommands typedCommand, string nameOfCommand, List<string> command)
    {
        if (!CheckMinLengthForEachCommand(typedCommand, command))
        {
            throw new AppException($"Command {nameOfCommand} requires no arguments!",
                $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
        }
        Dictionary<string, string> explanation = new();
        foreach (KeyValuePair<string, string> element in CommandAndFlagDescriptions)
        {
            explanation.Add(element.Key, element.Value);
        }
        return CommandResult.Ok(explanation, "Explanation of all available commands and flags.");
    }


    private Dictionary<string, FileSystemInfo> MapperHelper(List<FileSystemInfo> entries)
    {
        int n = 1;
        Dictionary<string, FileSystemInfo> result = new();
        
        foreach (var entry in entries)
        {
            result.Add(n.ToString(), entry);
            n++;
        }

        return result;
    }

    private DirectoryInfo ValidateDirectoryInfo(string path)
{
    var dirInfo = new DirectoryInfo(path);

    if (!dirInfo.Exists)
    {
        throw new DirectoryNotFoundException(
            $"Directory not found: {path}");
    }

    return dirInfo;
}

private CommandResult ExecuteLsCore(
    string path,
    Func<FileSystemInfo, object>? orderBy = null,
    string? description = null)
{
    var dirInfo = ValidateDirectoryInfo(path);

    IEnumerable<FileSystemInfo> entries =
        FileDirectorySystemService.GetEntries(dirInfo.FullName);

    if (orderBy is not null)
    {
        entries = entries.OrderBy(orderBy);
    }

    Dictionary<string, FileSystemInfo> result =
        MapperHelper(entries.ToList());
    
    return CommandResult.Ok(
        result,
        description ?? $"List of files in directory `{path}`.");
}

private CommandResult ExecuteLsHidden(string path, bool hidden)
{
    var dirInfo = ValidateDirectoryInfo(path);
    
    IEnumerable<FileSystemInfo> entries =
        FileDirectorySystemService.GetEntries(dirInfo.FullName);

    if (hidden)
    {
        entries = entries.Where(e => e.Attributes.HasFlag(FileAttributes.Hidden));
        Dictionary<string, FileSystemInfo> result = MapperHelper(entries.ToList());
        return CommandResult.Ok(result, $"List of only hidden files in directory `{path}`.");
    }
    else
    {
        entries = entries.Where(e => !e.Attributes.HasFlag(FileAttributes.Hidden));
        Dictionary<string, FileSystemInfo> result = MapperHelper(entries.ToList());
        return CommandResult.Ok(result, $"List of all files in directory `{path}` excluding hidden files.");
    }
}

private CommandResult ExecuteLs(
    AvailableCommands typedCommand,
    string nameOfCommand,
    List<string> command,
    string currentDirectory)
{
    if (!CheckMinLengthForEachCommand(typedCommand, command))
    {
        throw new AppException(
            $"Command {nameOfCommand} requires at least 2 arguments (3 in case of ls with a specific flag)!",
            $"{nameof(Parser)}.{nameof(CheckMinLengthForEachCommand)}()");
    }

    string path =
        command[1] == "." ? currentDirectory : command[1];

    FlagsForCommands flag =
        command.Count == 3
            ? ParseFlags(command[2], nameOfCommand)
            : FlagsForCommands.LsNone;

    return flag switch
    {
        FlagsForCommands.LsSize =>
            ExecuteLsCore(
                path,
                e => e is FileInfo file ? file.Length : 0L,
                $"List of files in directory `{path}` sorted by size in ascending order."),

        FlagsForCommands.LsModTime =>
            ExecuteLsCore(
                path,
                e => e.LastWriteTime,
                $"List of files in directory `{path}` sorted by last modified time in ascending order."),

        FlagsForCommands.LsAccessedTime =>
            ExecuteLsCore(
                path,
                e => e.LastAccessTime,
                $"List of files in directory `{path}` sorted by last access time in ascending order."),

        FlagsForCommands.LsCreatedTime =>
            ExecuteLsCore(
                path,
                e => e.CreationTime,
                $"List of files in directory `{path}` sorted by creation time in ascending order."),
        
        FlagsForCommands.LsOnlyHidden => 
            ExecuteLsHidden(path, hidden: true),
        
        FlagsForCommands.LsNoHidden => 
            ExecuteLsHidden(path, hidden: false),
        
        FlagsForCommands.LsNone =>
            ExecuteLsCore(path),

        _ => CommandResult.NoOp(
            "Invalid flag provided. Type 'help' to see all available flags and 'explain <flag>' to see the description of the flag.")
    };
}
    
    private CommandResult ExecuteDefault(string nameOfCommand)
    {
        return CommandResult.NoOp(message: $"Command `{nameOfCommand}` not found. Type 'help' to see all available commands.");
    }
    
    public async Task<CommandResult> ExecuteAllParsed(List<string> command, string currentDirectory = "")
    {
        if (!ValidateCommand(command)) return CommandResult.NoOp("No command provided.");
        AvailableCommands typedCommand = IndentifyCommand(command[0]);
        string nameOfCommand = command[0];
        
        return typedCommand switch
        {
            AvailableCommands.Cd => ExecuteCd(typedCommand, nameOfCommand, command),
            AvailableCommands.Rename => ExecuteRename(typedCommand, nameOfCommand, command),
            AvailableCommands.Delete => ExecuteDelete(typedCommand, nameOfCommand, command),
            AvailableCommands.CreateDirectory => ExecuteCreateDirectory(typedCommand, nameOfCommand, command),
            AvailableCommands.CreateFile => ExecuteCreateFile(typedCommand, nameOfCommand, command),
            AvailableCommands.CreateFileWithContent => await ExecuteCreateFileWithContent(typedCommand, nameOfCommand, command),
            AvailableCommands.Copy => ExecuteCopy(typedCommand, nameOfCommand, command),
            AvailableCommands.ReadFileContents => await ExecuteReadFileContents(typedCommand, nameOfCommand, command),
            AvailableCommands.Exit => ExecuteExit(typedCommand, nameOfCommand, command),
            AvailableCommands.Help => ExecuteHelp(typedCommand, nameOfCommand, command),
            AvailableCommands.HelpForFlags => ExecuteHelpForFlags(typedCommand, nameOfCommand, command),
            AvailableCommands.Explain => ExecuteExplain(typedCommand, nameOfCommand, command),
            AvailableCommands.ExplainAll => ExecuteExplainAll(typedCommand, nameOfCommand, command),
            AvailableCommands.Ls => ExecuteLs(typedCommand, nameOfCommand, command, currentDirectory),
            _ => ExecuteDefault(nameOfCommand)
        };
    }
}
