using FileSystemP.Core;
using FileSystemP.Core.MetadataService.Providers.Ntfs;
using FileSystemP.Core.MetadataService.DTO;
using System.IO;

namespace FileSystemP.Tests;

public class NtfsMetadataProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly NtfsMetadataProvider _provider;

    public NtfsMetadataProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _provider = new NtfsMetadataProvider();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            // Unset read-only attributes to allow deletion
            var files = Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string At(string name) => Path.Combine(_tempDir, name);

    [Fact]
    public void GetFileMetadata_ExistingFile_ReturnsCorrectMetadata()
    {
        // Arrange
        var filePath = At("test.txt");
        File.WriteAllText(filePath, "test content");
        var expectedInfo = new FileInfo(filePath);

        // Act
        var metadata = _provider.GetFileMetadata(filePath);

        // Assert
        Assert.Equal(expectedInfo.Name, metadata.Name);
        Assert.Equal(expectedInfo.FullName, metadata.FullPath);
        Assert.Equal(expectedInfo.DirectoryName, metadata.Directory);
        Assert.Equal(".txt", metadata.Extension);
        Assert.Equal(12, metadata.Size);
        // We compare times with a small tolerance due to file system precision or rounding
        Assert.True((expectedInfo.CreationTime - metadata.CreatedAt).Duration() < TimeSpan.FromSeconds(1));
        Assert.True((expectedInfo.LastWriteTime - metadata.ModifiedAt).Duration() < TimeSpan.FromSeconds(1));
        Assert.True((expectedInfo.LastAccessTime - metadata.AccessedAt).Duration() < TimeSpan.FromSeconds(1));
        Assert.Equal(expectedInfo.Attributes, metadata.Attributes);
    }

    [Fact]
    public void GetFileMetadata_NonExistentFile_ThrowsAppException()
    {
        // Arrange
        var filePath = At("missing.txt");

        // Act & Assert
        var ex = Assert.Throws<AppException>(() => _provider.GetFileMetadata(filePath));
        Assert.Contains("File not found", ex.Message);
        Assert.Equal("NtfsMetadataProvider.GetFileMetadata()", ex.ClassRootCauseName);
    }

    [Fact]
    public void GetDirectoryMetadata_ExistingDirectory_ReturnsCorrectMetadata()
    {
        // Arrange
        var dirPath = At("testdir");
        Directory.CreateDirectory(dirPath);
        var expectedInfo = new DirectoryInfo(dirPath);

        // Act
        var metadata = _provider.GetDirectoryMetadata(dirPath);

        // Assert
        Assert.Equal(expectedInfo.Name, metadata.Name);
        Assert.Equal(expectedInfo.FullName, metadata.FullPath);
        Assert.Equal(expectedInfo.Root.FullName, metadata.Root);
        Assert.Equal(expectedInfo.Parent?.FullName ?? expectedInfo.Root.FullName, metadata.Parent);
        Assert.True((expectedInfo.CreationTime - metadata.CreatedAt).Duration() < TimeSpan.FromSeconds(1));
        Assert.True((expectedInfo.LastWriteTime - metadata.ModifiedAt).Duration() < TimeSpan.FromSeconds(1));
        Assert.True((expectedInfo.LastAccessTime - metadata.AccessedAt).Duration() < TimeSpan.FromSeconds(1));
        Assert.Equal(expectedInfo.Attributes, metadata.Attributes);
    }

    [Fact]
    public void GetDirectoryMetadata_NonExistentDirectory_ThrowsAppException()
    {
        // Arrange
        var dirPath = At("missing_dir");

        // Act & Assert
        var ex = Assert.Throws<AppException>(() => _provider.GetDirectoryMetadata(dirPath));
        Assert.Contains("Directory not found", ex.Message);
        Assert.Equal("NtfsMetadataProvider.GetDirectoryMetadata()", ex.ClassRootCauseName);
    }

    [Fact]
    public void GetFileMetadata_NoExtension_ReturnsEmptyExtension()
    {
        // Arrange
        var filePath = At("noextension");
        File.WriteAllText(filePath, "content");
        
        // Act
        var metadata = _provider.GetFileMetadata(filePath);

        // Assert
        Assert.Equal("", metadata.Extension);
    }

    [Fact]
    public void GetFileMetadata_ReadOnlyFile_ReturnsCorrectAttributes()
    {
        // Arrange
        var filePath = At("readonly.txt");
        File.WriteAllText(filePath, "content");
        File.SetAttributes(filePath, FileAttributes.ReadOnly);
        
        // Act
        var metadata = _provider.GetFileMetadata(filePath);

        // Assert
        Assert.True(metadata.Attributes.HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void GetFileMetadata_HiddenFile_ReturnsCorrectAttributes()
    {
        // Arrange
        var filePath = At("hidden.txt");
        File.WriteAllText(filePath, "content");
        File.SetAttributes(filePath, FileAttributes.Hidden);
        
        // Act
        var metadata = _provider.GetFileMetadata(filePath);

        // Assert
        Assert.True(metadata.Attributes.HasFlag(FileAttributes.Hidden));
    }

    [Fact]
    public void GetDirectoryMetadata_NestedDirectory_ReturnsCorrectParent()
    {
        // Arrange
        var parentPath = At("ParentDir");
        var childPath = Path.Combine(parentPath, "ChildDir");
        Directory.CreateDirectory(childPath);

        // Act
        var metadata = _provider.GetDirectoryMetadata(childPath);

        // Assert
        Assert.Equal("ChildDir", metadata.Name);
        Assert.Equal(childPath, metadata.FullPath);
        Assert.Equal(parentPath, metadata.Parent);
    }

    [Fact]
    public void GetDirectoryMetadata_AtRoot_ReturnsRootAsParent()
    {
        // This is tricky because we don't always have a drive root in temp.
        // But we can check the behavior for the temp dir itself.
        var dirInfo = new DirectoryInfo(_tempDir);
        var expectedParent = dirInfo.Parent?.FullName ?? dirInfo.Root.FullName;

        var metadata = _provider.GetDirectoryMetadata(_tempDir);

        Assert.Equal(expectedParent, metadata.Parent);
        Assert.Equal(dirInfo.Root.FullName, metadata.Root);
    }
}
