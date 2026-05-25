using FileSystemP.Core;
using FileSystemP.Core.MetadataService.Providers.SecurityAndACL;
using Xunit;

namespace FileSystemP.Tests;

public class SecurityMetadataProviderTests
{
    private readonly SecurityMetadataProvider _provider = new();

    [Fact]
    public void GetSecurityMetadata_FileExists_ReturnsMetadata()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            var metadata = _provider.GetSecurityMetadata(tempFile);
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Owner);
            Assert.NotEmpty(metadata.Permissions);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetSecurityMetadata_DirectoryExists_ReturnsMetadata()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var metadata = _provider.GetSecurityMetadata(tempDir);
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Owner);
            Assert.NotEmpty(metadata.Permissions);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void GetSecurityMetadata_PathDoesNotExist_ThrowsAppExceptionWithCorrectMessage()
    {
        string nonExistentPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var exception = Assert.Throws<AppException>(() => _provider.GetSecurityMetadata(nonExistentPath));
        Assert.Contains("Path not found", exception.Message);
    }
}
