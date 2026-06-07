namespace FileSystemP.Core.CommandService;

public sealed record CommandResult(bool Success, object? Payload = null, string? Message = null, bool ShouldExit = false)
{
    public static CommandResult Ok(object? payload = null, string? message = null) => new(true, payload, message);

    public static CommandResult NoOp(string? message = null) => new(true, null, message);


    public static CommandResult Exit(string? message = null) => new(true, null, message, true);
}
