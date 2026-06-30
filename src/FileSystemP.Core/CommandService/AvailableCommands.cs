namespace FileSystemP.Core.CommandService;

public enum AvailableCommands
{
    Cd,
    Rename,
    Delete,
    CreateDirectory,
    CreateFile,
    CreateFileWithContent,
    Copy,
    Move,
    ReadFileContents,
    Exit,
    Help,
    HelpForFlags,
    Explain,
    ExplainAll,
    Ls,
    OpenProperties,
    Back,
    Forward,
    Home,
    Undo,
    Search,
    Hidden,
    Find,
    OpenFile
}

public enum FlagsForCommands
{
    NoRecursive,
    Recursive,
    NoOverwrite,
    Overwrite,
    LsSize,
    LsModTime,
    LsAccessedTime,
    LsCreatedTime,
    LsOnlyHidden,
    LsNoHidden,
    LsNone,
    FindExact,
    FindPattern,
    FindNone
}
