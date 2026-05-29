using FileSystemP.Core.SearchService.DTO;
using FileSystemP.Core.SearchService.Options;

namespace FileSystemP.Core.SearchService;

public class SearchService : ISearchService
{
    public async Task<ExtendedSearchResult> SearchAsync(string path, ExtendedOptions options, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var results = new List<FileSystemInfo>();
            SearchRecursive(path, options, results, cancellationToken);
            return new ExtendedSearchResult(results);
        }, cancellationToken);
    }

    private void SearchRecursive(string path, ExtendedOptions options, List<FileSystemInfo> results, CancellationToken cancellationToken)
    {
        // Check for cancellation at the start of each directory
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var directory = new DirectoryInfo(path);
            
            // Get entries in current directory
            var entries = directory.EnumerateFileSystemInfos();

            foreach (var info in entries)
            {
                // Check for cancellation during long file lists
                cancellationToken.ThrowIfCancellationRequested();

                if (IsMatch(info, options))
                {
                    results.Add(info);
                }

                // Recurse if needed and it's a directory
                if (options.Option == SearchOption.AllDirectories && info is DirectoryInfo subDir)
                {
                    SearchRecursive(subDir.FullName, options, results, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation so the caller knows it was stopped
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }
        catch (Exception)
        {
            // Skip other IO errors (e.g. path too long, device not ready)
        }
    }

    private bool IsMatch(FileSystemInfo info, ExtendedOptions options)
    {
        bool isDir = info is DirectoryInfo;

        // Target Type Filter
        if (options.TargetType == SearchTargetType.Files && isDir) return false;
        if (options.TargetType == SearchTargetType.Directories && !isDir) return false;

        // Pattern Match
        if (options.Pattern is not null && !info.Name.Contains(options.Pattern, StringComparison.OrdinalIgnoreCase))
            return false;

        // Extension Match (Files only)
        if (options.Extensions is not null)
        {
            if (isDir) return false;
            if (!options.Extensions.Contains(info.Extension.TrimStart('.'), StringComparer.OrdinalIgnoreCase))
                return false;
        }

        // Attributes Match
        if (options.Attributes is not null && !options.Attributes.All(attr => info.Attributes.HasFlag(attr)))
            return false;

        // Size Matches (Files only)
        if (info is FileInfo file)
        {
            if (options.AboveSize is not null && file.Length <= options.AboveSize.Value) return false;
            if (options.ExactSize is not null && file.Length != options.ExactSize.Value) return false;
            if (options.BelowSize is not null && file.Length >= options.BelowSize.Value) return false;
        }
        else if (options.AboveSize is not null || options.ExactSize is not null || options.BelowSize is not null)
        {
            return false;
        }

        // Date Matches
        if (options.CreatedFromDate is not null && info.CreationTime <= options.CreatedFromDate.Value) return false;
        if (options.CreatedExactDate is not null && info.CreationTime != options.CreatedExactDate.Value) return false;
        if (options.CreatedToDate is not null && info.CreationTime >= options.CreatedToDate.Value) return false;

        if (options.ModifiedFromDate is not null && info.LastWriteTime <= options.ModifiedFromDate.Value) return false;
        if (options.ModifiedExactDate is not null && info.LastWriteTime != options.ModifiedExactDate.Value) return false;
        if (options.ModifiedToDate is not null && info.LastWriteTime >= options.ModifiedToDate.Value) return false;

        if (options.AccessedFromDate is not null && info.LastAccessTime <= options.AccessedFromDate.Value) return false;
        if (options.AccessedExactDate is not null && info.LastAccessTime != options.AccessedExactDate.Value) return false;
        if (options.AccessedToDate is not null && info.LastAccessTime >= options.AccessedToDate.Value) return false;

        return true;
    }
}
