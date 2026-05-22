using FileSystemP.Core.MetadataService.DTO;
using FileSystemP.WPF.ViewModels;
using System;
using System.IO;
using Xunit;

namespace FileSystemP.Tests;

public class PropertiesViewModelTests
{
    [Fact]
    public void Constructor_WithNtfsMetadataRecord_SetsPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTime.Now.AddDays(-10);
        var modifiedAt = DateTime.Now.AddDays(-5);
        var accessedAt = DateTime.Now;
        var record = new NtfsMetadataRecord(
            Name: "test.txt",
            FullPath: @"C:\test.txt",
            Directory: @"C:\",
            Extension: ".txt",
            Size: 1024,
            CreatedAt: createdAt,
            ModifiedAt: modifiedAt,
            AccessedAt: accessedAt,
            Attributes: FileAttributes.Normal
        );

        // Act
        var viewModel = new PropertiesViewModel(record);

        // Assert
        Assert.Equal("test.txt", viewModel.Name);
        Assert.Equal("TXT File", viewModel.Type);
        Assert.Equal(@"C:\test.txt", viewModel.Location);
        Assert.Contains("1.0 KB", viewModel.Size);
        Assert.Equal(createdAt.ToString("f"), viewModel.CreatedAt);
        Assert.Equal(modifiedAt.ToString("f"), viewModel.ModifiedAt);
        Assert.Equal(accessedAt.ToString("f"), viewModel.AccessedAt);
        Assert.False(viewModel.IsReadOnly);
        Assert.False(viewModel.IsHidden);
        Assert.False(viewModel.IsDirectory);
        Assert.Equal(@"C:\", viewModel.ParentPath);
        Assert.Equal(@"C:\", viewModel.RootPath);
    }

    [Fact]
    public void Constructor_WithDirectoryNtfsMetadataRecord_SetsPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTime.Now.AddDays(-10);
        var modifiedAt = DateTime.Now.AddDays(-5);
        var accessedAt = DateTime.Now;
        var record = new DirectoryNtfsMetadataRecord(
            Name: "testdir",
            FullPath: @"C:\testdir",
            Root: @"C:\",
            Parent: @"C:\",
            CreatedAt: createdAt,
            ModifiedAt: modifiedAt,
            AccessedAt: accessedAt,
            Attributes: FileAttributes.Directory
        );

        // Act
        var viewModel = new PropertiesViewModel(record);

        // Assert
        Assert.Equal("testdir", viewModel.Name);
        Assert.Equal("File Folder", viewModel.Type);
        Assert.Equal(@"C:\testdir", viewModel.Location);
        Assert.Equal("N/A", viewModel.Size);
        Assert.Equal(createdAt.ToString("f"), viewModel.CreatedAt);
        Assert.Equal(modifiedAt.ToString("f"), viewModel.ModifiedAt);
        Assert.Equal(accessedAt.ToString("f"), viewModel.AccessedAt);
        Assert.False(viewModel.IsReadOnly);
        Assert.False(viewModel.IsHidden);
        Assert.True(viewModel.IsDirectory);
        Assert.Equal(@"C:\", viewModel.ParentPath);
        Assert.Equal(@"C:\", viewModel.RootPath);
    }

    [Theory]
    [InlineData(512, "512.0 B (512 bytes)")]
    [InlineData(1024, "1.0 KB (1,024 bytes)")]
    [InlineData(1048576, "1.0 MB (1,048,576 bytes)")]
    public void FormatSize_ReturnsExpectedString(long bytes, string expectedSubstring)
    {
        // Arrange
        var record = new NtfsMetadataRecord(
            Name: "test",
            FullPath: "test",
            Directory: "test",
            Extension: "",
            Size: bytes,
            CreatedAt: DateTime.Now,
            ModifiedAt: DateTime.Now,
            AccessedAt: DateTime.Now,
            Attributes: FileAttributes.Normal
        );

        // Act
        var viewModel = new PropertiesViewModel(record);

        // Assert
        Assert.Contains(expectedSubstring, viewModel.Size);
    }
}
