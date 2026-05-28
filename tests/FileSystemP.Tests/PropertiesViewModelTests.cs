using FileSystemP.Core.AttributeService;
using FileSystemP.Core.MetadataService.DTO;
using FileSystemP.Core.MetadataService.Providers.Ntfs;
using FileSystemP.Core.MetadataService.Providers.ShellMetadata;
using FileSystemP.WPF.ViewModels;
using Moq;
using System.IO;

namespace FileSystemP.Tests;

public class PropertiesViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<IShellMetadataProviderInterface> _shellMetadataMock;

    public PropertiesViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fsp-props-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _shellMetadataMock = new Mock<IShellMetadataProviderInterface>();
        _shellMetadataMock.Setup(m => m.GetShellMetadata(It.IsAny<string>()))
            .Returns(new ShellMetadataRecord(new List<ShellPropertyRecord>()));
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempDir))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        foreach (string dir in Directory.GetDirectories(_tempDir, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            File.SetAttributes(dir, FileAttributes.Directory);
        }

        File.SetAttributes(_tempDir, FileAttributes.Directory);
        Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Constructor_WithNtfsMetadataRecord_SetsPropertiesCorrectly()
    {
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
            Attributes: FileAttributes.Archive | FileAttributes.Hidden
        );

        var viewModel = new PropertiesViewModel(
            record,
            new NtfsMetadataProvider(),
            _shellMetadataMock.Object,
            new ReadonlyAttributeService(),
            new HiddenAttributeService(),
            new ArchiveAttributeService(),
            new NotContentIndexedAttributeService(),
            new CompressAttributeService(),
            null);

        Assert.Equal("test.txt", viewModel.Name);
        Assert.Equal("TXT File", viewModel.Type);
        Assert.Equal(@"C:\", viewModel.Location);
        Assert.Equal(@"C:\test.txt", viewModel.TargetPath);
        Assert.Contains("KB", viewModel.Size);
        Assert.Contains("1", viewModel.Size);
        Assert.Equal(createdAt.ToString("f"), viewModel.CreatedAt);
        Assert.Equal(modifiedAt.ToString("f"), viewModel.ModifiedAt);
        Assert.Equal(accessedAt.ToString("f"), viewModel.AccessedAt);
        Assert.False(viewModel.IsReadOnly);
        Assert.True(viewModel.IsHidden);
        Assert.True(viewModel.IsArchive);
        Assert.True(viewModel.AllowsContentIndexing);
        Assert.False(viewModel.IsDirectory);
        Assert.Equal("Read-only", viewModel.ReadOnlyLabel);
        Assert.Equal(@"C:\", viewModel.ParentPath);
        Assert.Equal(@"C:\", viewModel.RootPath);
    }

    [Fact]
    public void Constructor_WithDirectoryNtfsMetadataRecord_SetsPropertiesCorrectly()
    {
        var createdAt = DateTime.Now.AddDays(-10);
        var modifiedAt = DateTime.Now.AddDays(-5);
        var accessedAt = DateTime.Now;
        var record = new DirectoryNtfsMetadataRecord(
            Name: "testdir",
            FullPath: @"C:\testdir",
            Root: @"C:\",
            Parent: @"C:\",
            Size: 2048,
            CreatedAt: createdAt,
            ModifiedAt: modifiedAt,
            AccessedAt: accessedAt,
            Attributes: FileAttributes.Directory | FileAttributes.ReadOnly | FileAttributes.NotContentIndexed
        );

        var viewModel = new PropertiesViewModel(
            record,
            new NtfsMetadataProvider(),
            _shellMetadataMock.Object,
            new ReadonlyAttributeService(),
            new HiddenAttributeService(),
            new ArchiveAttributeService(),
            new NotContentIndexedAttributeService(),
            new CompressAttributeService(),
            null);

        Assert.Equal("testdir", viewModel.Name);
        Assert.Equal("File Folder", viewModel.Type);
        Assert.Equal(@"C:\", viewModel.Location);
        Assert.Equal(@"C:\testdir", viewModel.TargetPath);
        Assert.Contains("KB", viewModel.Size);
        Assert.Contains("2", viewModel.Size);
        Assert.Equal(createdAt.ToString("f"), viewModel.CreatedAt);
        Assert.Equal(modifiedAt.ToString("f"), viewModel.ModifiedAt);
        Assert.Equal(accessedAt.ToString("f"), viewModel.AccessedAt);
        Assert.True(viewModel.IsReadOnly);
        Assert.False(viewModel.IsHidden);
        Assert.False(viewModel.IsArchive);
        Assert.False(viewModel.AllowsContentIndexing);
        Assert.True(viewModel.IsDirectory);
        Assert.True(viewModel.SupportsRecursiveAttributeChanges);
        Assert.True(viewModel.ApplyChangesRecursively);
        Assert.Contains("Only applies to files in folder", viewModel.ReadOnlyLabel);
        Assert.Equal(@"C:\", viewModel.ParentPath);
        Assert.Equal(@"C:\", viewModel.RootPath);
    }

    [Fact]
    public async Task SaveChanges_ForFile_PersistsSimpleAndAdvancedAttributes()
    {
        string filePath = At("attributes.txt");
        File.WriteAllText(filePath, "content");

        var metadataProvider = new NtfsMetadataProvider();
        int applyCount = 0;
        var viewModel = new PropertiesViewModel(
            metadataProvider.GetFileMetadata(filePath),
            metadataProvider,
            _shellMetadataMock.Object,
            new ReadonlyAttributeService(),
            new HiddenAttributeService(),
            new ArchiveAttributeService(),
            new NotContentIndexedAttributeService(),
            new CompressAttributeService(),
            () => applyCount++);

        viewModel.IsReadOnly = true;
        viewModel.IsHidden = true;
        viewModel.IsArchive = true;
        viewModel.AllowsContentIndexing = false;

        Assert.True(viewModel.HasChanges);

        await viewModel.SaveChangesAsync();

        FileAttributes attributes = File.GetAttributes(filePath);
        Assert.True(attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.True(attributes.HasFlag(FileAttributes.Hidden));
        Assert.True(attributes.HasFlag(FileAttributes.Archive));
        Assert.True(attributes.HasFlag(FileAttributes.NotContentIndexed));
        Assert.False(viewModel.HasChanges);
        Assert.Equal(1, applyCount);
    }

    [Fact]
    public async Task SaveChanges_ForDirectory_AppliesReadonlyRecursivelyWhenRequested()
    {
        string dirPath = At("folder");
        string childDirPath = Path.Combine(dirPath, "child");
        string childFilePath = Path.Combine(childDirPath, "note.txt");

        Directory.CreateDirectory(childDirPath);
        File.WriteAllText(childFilePath, "content");

        var metadataProvider = new NtfsMetadataProvider();
        var viewModel = new PropertiesViewModel(
            metadataProvider.GetDirectoryMetadata(dirPath),
            metadataProvider,
            _shellMetadataMock.Object,
            new ReadonlyAttributeService(),
            new HiddenAttributeService(),
            new ArchiveAttributeService(),
            new NotContentIndexedAttributeService(),
            new CompressAttributeService(),
            onChangesApplied: null);

        viewModel.IsReadOnly = true;
        viewModel.ApplyChangesRecursively = true;

        await viewModel.SaveChangesAsync();

        Assert.True(File.GetAttributes(dirPath).HasFlag(FileAttributes.ReadOnly));
        Assert.True(File.GetAttributes(childDirPath).HasFlag(FileAttributes.ReadOnly));
        Assert.True(File.GetAttributes(childFilePath).HasFlag(FileAttributes.ReadOnly));
        Assert.False(viewModel.HasChanges);
    }

    [Fact]
    public async Task SaveChanges_ForFile_CompressesWhenSupported()
    {
        if (!SupportsNtfsCompression(_tempDir))
        {
            return;
        }

        string filePath = At("compressed.txt");
        File.WriteAllText(filePath, "content");

        var metadataProvider = new NtfsMetadataProvider();
        var viewModel = new PropertiesViewModel(
            metadataProvider.GetFileMetadata(filePath),
            metadataProvider,
            _shellMetadataMock.Object,
            new ReadonlyAttributeService(),
            new HiddenAttributeService(),
            new ArchiveAttributeService(),
            new NotContentIndexedAttributeService(),
            new CompressAttributeService(),
            onChangesApplied: null);

        viewModel.IsCompressed = true;

        await viewModel.SaveChangesAsync();

        Assert.True(File.GetAttributes(filePath).HasFlag(FileAttributes.Compressed));
        Assert.False(viewModel.HasChanges);
    }

    [Theory]
    [InlineData(512, "B", "512")]
    [InlineData(1024, "KB", "1")]
    [InlineData(1048576, "MB", "1")]
    public void FormatSize_ReturnsExpectedString(long bytes, string expectedUnit, string expectedLeadingValue)
    {
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

        var viewModel = new PropertiesViewModel(
            record,
            new NtfsMetadataProvider(),
            _shellMetadataMock.Object,
            new ReadonlyAttributeService(),
            new HiddenAttributeService(),
            new ArchiveAttributeService(),
            new NotContentIndexedAttributeService(),
            new CompressAttributeService(),
            null);

        Assert.Contains(expectedUnit, viewModel.Size);
        Assert.Contains(expectedLeadingValue, viewModel.Size);
        Assert.Contains("bytes", viewModel.Size);
    }

    private string At(string name) => Path.Combine(_tempDir, name);

    private static bool SupportsNtfsCompression(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        string? root = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root))
        {
            return false;
        }

        var drive = new DriveInfo(root);
        return drive.IsReady && string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase);
    }
}
