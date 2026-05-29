using FileSystemP.Core.SearchService.DTO;
using FileSystemP.Core.SearchService.Options;

namespace FileSystemP.Core.SearchService;

public interface ISearchService
{
    Task<ExtendedSearchResult> SearchAsync(string path, ExtendedOptions options, CancellationToken cancellationToken = default);
}
