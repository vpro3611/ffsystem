namespace FileSystemP.Core.CommandService;

public interface IParser
{
     Task<CommandResult> ExecuteAllParsed(List<string> command);
}
