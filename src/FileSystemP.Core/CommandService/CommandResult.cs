namespace FileSystemP.Core.CommandService;

public sealed record CommandResult(bool Success, object? Payload = null, string? Message = null, bool ShouldExit = false, bool ShouldOpenProperties = false,
    bool ShouldGoBack = false, bool ShouldGoForward = false, bool ShouldUndo = false, bool ShouldOpenSearch = false, bool ShouldToggleHidden = false, string? GoHomePath = null, bool ShouldOpenFile = false, string? PathToOpen = null)
{
    public static CommandResult Ok(object? payload = null, string? message = null) => new(true, payload, message);

    public static CommandResult NoOp(string? message = null) => new(true, null, message);
    
    public static CommandResult Exit(string? message = null) => new(true, null, message, true);

    public static CommandResult OpenProperties(object? payload = null, string? message = null) => new(true, payload, message, false, true);
    
    public static CommandResult Back(string? message = null) =>
        new(true, null, message, false, false, true);

    public static CommandResult Forward(string? message = null) =>
        new(true, Message: message, ShouldGoForward: true);
    
    public static CommandResult Home(string homePath, string? message = null) =>
        new(true, Message: message, GoHomePath: homePath);
    
    public static CommandResult Undo(string? message = null) => 
        new(true, Message: message, ShouldUndo: true);

    public static CommandResult OpenSearch(string? message = null) =>
        new(true, Message: message, ShouldOpenSearch: true);
    
    public static CommandResult ToggleHidden(string? message = null) => 
        new(true, Message: message, ShouldToggleHidden: true);
    
    public static CommandResult OpenFile(string pathToOpen, string? message = null) => 
        new(true, Message: message, ShouldOpenFile: true, PathToOpen: pathToOpen);
}
