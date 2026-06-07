namespace FileSystemP.Core.CommandService;

public sealed record FileContentResult(byte[] Content);

public sealed record FileLinesResult(IReadOnlyList<string> Lines);
