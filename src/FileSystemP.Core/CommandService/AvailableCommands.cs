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
    ReadFileContents,
    Exit,
    Help,
    HelpForFlags,
    Explain,
    ExplainAll,
    Ls
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
    LsNone
}