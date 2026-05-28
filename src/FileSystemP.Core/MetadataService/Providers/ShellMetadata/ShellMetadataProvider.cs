using FileSystemP.Core.MetadataService.DTO;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace FileSystemP.Core.MetadataService.Providers.ShellMetadata;

public class ShellMetadataProvider : IShellMetadataProviderInterface
{
    private const string ClassName = nameof(ShellMetadataProvider);

    public ShellMetadataRecord GetShellMetadata(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            throw new AppException($"File or directory not found: {path}", $"{ClassName}.{nameof(GetShellMetadata)}()");
        }

        var propertyRecords = new List<ShellPropertyRecord>();

        try
        {
            using (var shellObject = ShellObject.FromParsingName(path))
            {
                // We use DefaultPropertyCollection but we could also iterate through specific ones 
                // if we wanted to be more "detailed related to each file type".
                // However, DefaultPropertyCollection usually contains the most relevant ones for the specific file.
                
                foreach (var property in shellObject.Properties.DefaultPropertyCollection)
                {
                    if (string.IsNullOrEmpty(property.CanonicalName)) continue;

                    string displayName = property.Description?.DisplayName ?? property.CanonicalName;
                    object? value = null;

                    try
                    {
                        value = property.ValueAsObject;
                    }
                    catch
                    {
                        // Some properties might fail to load, skip them or set to null
                    }

                    propertyRecords.Add(new ShellPropertyRecord(property.CanonicalName, displayName, value));
                }
            }
        }
        catch (Exception ex)
        {
            throw new AppException(
                $"Failed to retrieve shell metadata for: {path}",
                $"{ClassName}.{nameof(GetShellMetadata)}()",
                innerException: ex
            );
        }

        return new ShellMetadataRecord(propertyRecords);
    }
}
