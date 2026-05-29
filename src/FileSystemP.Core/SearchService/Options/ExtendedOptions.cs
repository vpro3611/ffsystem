namespace FileSystemP.Core.SearchService.Options;

public enum SearchTargetType
{
    Files,
    Directories,
    Both
}

public record ExtendedOptions(
    SearchOption Option,
    SearchTargetType TargetType,
    string? Pattern,  
    List<string>? Extensions,
    List<FileAttributes>? Attributes,
    long? AboveSize,
    long? ExactSize,
    long? BelowSize,
    DateTime? CreatedFromDate,
    DateTime? CreatedExactDate,
    DateTime? CreatedToDate,
    DateTime? ModifiedFromDate,
    DateTime? ModifiedExactDate,
    DateTime? ModifiedToDate,
    DateTime? AccessedFromDate,
    DateTime? AccessedExactDate,
    DateTime? AccessedToDate
);
