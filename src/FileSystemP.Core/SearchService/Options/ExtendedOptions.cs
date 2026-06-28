namespace FileSystemP.Core.SearchService.Options;

public enum SearchTargetType
{
    Files,
    Directories,
    Both
}

public enum NameSearchMode
{
    Contains,
    Exact,
    Pattern
}

public record ExtendedOptions(
    SearchOption Option,
    SearchTargetType TargetType,
    string? Pattern,  
    NameSearchMode NameMode,
    List<string>? Extensions = null,
    List<FileAttributes>? Attributes = null,
    long? AboveSize = null,
    long? ExactSize = null,
    long? BelowSize = null,
    DateTime? CreatedFromDate = null,
    DateTime? CreatedExactDate = null,
    DateTime? CreatedToDate = null,
    DateTime? ModifiedFromDate = null,
    DateTime? ModifiedExactDate = null,
    DateTime? ModifiedToDate = null,
    DateTime? AccessedFromDate = null,
    DateTime? AccessedExactDate = null,
    DateTime? AccessedToDate = null
);
