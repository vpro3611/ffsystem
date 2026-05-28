using FileSystemP.Core;
using FileSystemP.Core.MetadataService.DTO;
using FileSystemP.Core.MetadataService.Providers.ShellMetadata;
using System.IO;

namespace FileSystemP.Tests;

public class ShellMetadataProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ShellMetadataProvider _provider;

    public ShellMetadataProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _provider = new ShellMetadataProvider();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string At(string name) => Path.Combine(_tempDir, name);

    [Fact]
    public void GetShellMetadata_ExistingFile_ReturnsMetadata()
    {
        // Arrange
        var filePath = At("test.txt");
        File.WriteAllText(filePath, "test content");

        // Act
        var metadata = _provider.GetShellMetadata(filePath);

        // Assert
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.Properties);
        
        // At least some properties should be present
        Assert.NotEmpty(metadata.Properties);
        
        // Common property check
        var nameProp = metadata.Properties.FirstOrDefault(p => p.CanonicalName == "System.ItemNameDisplay");
        Assert.NotNull(nameProp);
        Assert.Equal("test.txt", nameProp.Value);
        Assert.False(string.IsNullOrEmpty(nameProp.DisplayName));
    }

    [Fact]
    public void GetShellMetadata_NonExistentFile_ThrowsAppException()
    {
        // Arrange
        var filePath = At("missing.txt");

        // Act & Assert
        var ex = Assert.Throws<AppException>(() => _provider.GetShellMetadata(filePath));
        Assert.Contains("File or directory not found", ex.Message);
        Assert.Equal("ShellMetadataProvider.GetShellMetadata()", ex.ClassRootCauseName);
    }
}
