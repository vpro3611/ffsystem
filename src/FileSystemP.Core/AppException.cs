namespace FileSystemP.Core;

public class AppException : Exception
{
    public string? ClassRootCauseName { get; }

    public AppException(
        string message,
        string? classRootCauseName = null,
        string? source = null,
        Exception? innerException = null
    ) : base(message, innerException)
    {
        ClassRootCauseName = classRootCauseName;
        Source = source;
    }
}