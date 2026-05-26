using FileSystemP.Core;
using FileSystemP.Core.AttributeService;
using FileSystemP.Core.MetadataService.Providers.Ntfs;

namespace FileSystemP.Tests;

public class AttributeServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ArchiveAttributeService _archiveService;
    private readonly CompressAttributeService _compressService;
    private readonly HiddenAttributeService _hiddenService;
    private readonly NotContentIndexedAttributeService _notContentIndexedService;
    private readonly ReadonlyAttributeService _readonlyService;
    private readonly NtfsMetadataProvider _metadataProvider;

    public AttributeServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _archiveService = new ArchiveAttributeService();
        _compressService = new CompressAttributeService();
        _hiddenService = new HiddenAttributeService();
        _notContentIndexedService = new NotContentIndexedAttributeService();
        _readonlyService = new ReadonlyAttributeService();
        _metadataProvider = new NtfsMetadataProvider();
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempDir))
        {
            return;
        }

        foreach (var filePath in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
        }

        foreach (var directoryPath in Directory.GetDirectories(_tempDir, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            File.SetAttributes(directoryPath, FileAttributes.Directory);
        }

        File.SetAttributes(_tempDir, FileAttributes.Directory);
        Directory.Delete(_tempDir, recursive: true);
    }

    private string At(string name) => Path.Combine(_tempDir, name);

    private static bool SupportsNtfsCompression(string path)
    {
        var root = Path.GetPathRoot(Path.GetFullPath(path));
        if (string.IsNullOrEmpty(root))
        {
            return false;
        }

        var drive = new DriveInfo(root);
        return OperatingSystem.IsWindows() &&
               drive.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetArchive_File_SetsArchiveAttribute()
    {
        var filePath = At("archive-file.txt");
        File.WriteAllText(filePath, "content");
        File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.Archive);

        _archiveService.SetArchive(filePath);

        Assert.True(_archiveService.IsArchive(filePath));
        Assert.True(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.Archive));
    }

    [Fact]
    public void UnsetArchive_File_ClearsArchiveAttribute()
    {
        var filePath = At("non-archive-file.txt");
        File.WriteAllText(filePath, "content");
        File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.Archive);

        _archiveService.UnsetArchive(filePath);

        Assert.False(_archiveService.IsArchive(filePath));
        Assert.False(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.Archive));
    }

    [Fact]
    public void SetArchive_Directory_SetsArchiveAttribute()
    {
        var dirPath = At("archive-dir");
        Directory.CreateDirectory(dirPath);
        File.SetAttributes(dirPath, File.GetAttributes(dirPath) & ~FileAttributes.Archive);

        _archiveService.SetArchive(dirPath);

        Assert.True(_archiveService.IsArchive(dirPath));
        Assert.True(_metadataProvider.GetDirectoryMetadata(dirPath).Attributes.HasFlag(FileAttributes.Archive));
    }

    [Fact]
    public void UnsetArchive_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing-archive.txt");

        var ex = Assert.Throws<AppException>(() => _archiveService.UnsetArchive(missingPath));

        Assert.Contains("Path not found", ex.Message);
        Assert.Equal("ArchiveAttributeService.UnsetArchive()", ex.ClassRootCauseName);
    }

    [Fact]
    public void CompressFile_SetsCompressedAttribute()
    {
        if (!SupportsNtfsCompression(_tempDir))
        {
            return;
        }

        var filePath = At("compressed-file.txt");
        File.WriteAllText(filePath, new string('A', 4096));

        _compressService.CompressFile(filePath);

        Assert.True(_compressService.IsFileCompressed(filePath));
        Assert.True(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.Compressed));
    }

    [Fact]
    public void DecompressFile_ClearsCompressedAttribute()
    {
        if (!SupportsNtfsCompression(_tempDir))
        {
            return;
        }

        var filePath = At("decompressed-file.txt");
        File.WriteAllText(filePath, new string('B', 4096));
        _compressService.CompressFile(filePath);

        _compressService.DecompressFile(filePath);

        Assert.False(_compressService.IsFileCompressed(filePath));
        Assert.False(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.Compressed));
    }

    [Fact]
    public void CompressDirectory_Recursive_SetsCompressedAttributeForDirectorySubdirectoriesAndFiles()
    {
        if (!SupportsNtfsCompression(_tempDir))
        {
            return;
        }

        var dirPath = At("compressed-dir");
        var nestedDirPath = Path.Combine(dirPath, "nested");
        var deepDirPath = Path.Combine(nestedDirPath, "deep");
        var topLevelFilePath = Path.Combine(dirPath, "root.txt");
        var nestedFilePath = Path.Combine(nestedDirPath, "nested.txt");
        var deepFilePath = Path.Combine(deepDirPath, "deep.txt");

        Directory.CreateDirectory(deepDirPath);
        File.WriteAllText(topLevelFilePath, new string('C', 4096));
        File.WriteAllText(nestedFilePath, new string('D', 4096));
        File.WriteAllText(deepFilePath, new string('E', 4096));

        _compressService.CompressDirectory(dirPath, recursive: true);

        Assert.True(_compressService.IsDirectoryCompressed(dirPath));
        Assert.True(_compressService.IsDirectoryCompressed(nestedDirPath));
        Assert.True(_compressService.IsDirectoryCompressed(deepDirPath));
        Assert.True(_compressService.IsFileCompressed(topLevelFilePath));
        Assert.True(_compressService.IsFileCompressed(nestedFilePath));
        Assert.True(_compressService.IsFileCompressed(deepFilePath));
    }

    [Fact]
    public void CompressDirectory_NonRecursive_CompressesOnlyTargetDirectory()
    {
        if (!SupportsNtfsCompression(_tempDir))
        {
            return;
        }

        var dirPath = At("compressed-dir-non-recursive");
        var nestedDirPath = Path.Combine(dirPath, "nested");
        var topLevelFilePath = Path.Combine(dirPath, "root.txt");
        var nestedFilePath = Path.Combine(nestedDirPath, "nested.txt");

        Directory.CreateDirectory(nestedDirPath);
        File.WriteAllText(topLevelFilePath, new string('I', 4096));
        File.WriteAllText(nestedFilePath, new string('J', 4096));

        _compressService.CompressDirectory(dirPath, recursive: false);

        Assert.True(_compressService.IsDirectoryCompressed(dirPath));
        Assert.False(_compressService.IsDirectoryCompressed(nestedDirPath));
        Assert.False(_compressService.IsFileCompressed(topLevelFilePath));
        Assert.False(_compressService.IsFileCompressed(nestedFilePath));
    }

    [Fact]
    public void DecompressDirectory_Recursive_ClearsCompressedAttributeForDirectorySubdirectoriesAndFiles()
    {
        if (!SupportsNtfsCompression(_tempDir))
        {
            return;
        }

        var dirPath = At("decompressed-dir");
        var nestedDirPath = Path.Combine(dirPath, "nested");
        var deepDirPath = Path.Combine(nestedDirPath, "deep");
        var topLevelFilePath = Path.Combine(dirPath, "root.txt");
        var nestedFilePath = Path.Combine(nestedDirPath, "nested.txt");
        var deepFilePath = Path.Combine(deepDirPath, "deep.txt");

        Directory.CreateDirectory(deepDirPath);
        File.WriteAllText(topLevelFilePath, new string('F', 4096));
        File.WriteAllText(nestedFilePath, new string('G', 4096));
        File.WriteAllText(deepFilePath, new string('H', 4096));
        _compressService.CompressDirectory(dirPath, recursive: true);

        _compressService.DecompressDirectory(dirPath, recursive: true);

        Assert.False(_compressService.IsDirectoryCompressed(dirPath));
        Assert.False(_compressService.IsDirectoryCompressed(nestedDirPath));
        Assert.False(_compressService.IsDirectoryCompressed(deepDirPath));
        Assert.False(_compressService.IsFileCompressed(topLevelFilePath));
        Assert.False(_compressService.IsFileCompressed(nestedFilePath));
        Assert.False(_compressService.IsFileCompressed(deepFilePath));
    }

    [Fact]
    public void DecompressDirectory_NonRecursive_ClearsOnlyTargetDirectoryCompression()
    {
        if (!SupportsNtfsCompression(_tempDir))
        {
            return;
        }

        var dirPath = At("decompressed-dir-non-recursive");
        var nestedDirPath = Path.Combine(dirPath, "nested");
        var topLevelFilePath = Path.Combine(dirPath, "root.txt");
        var nestedFilePath = Path.Combine(nestedDirPath, "nested.txt");

        Directory.CreateDirectory(nestedDirPath);
        File.WriteAllText(topLevelFilePath, new string('K', 4096));
        File.WriteAllText(nestedFilePath, new string('L', 4096));
        _compressService.CompressDirectory(dirPath, recursive: true);

        _compressService.DecompressDirectory(dirPath, recursive: false);

        Assert.False(_compressService.IsDirectoryCompressed(dirPath));
        Assert.True(_compressService.IsDirectoryCompressed(nestedDirPath));
        Assert.True(_compressService.IsFileCompressed(topLevelFilePath));
        Assert.True(_compressService.IsFileCompressed(nestedFilePath));
    }

    [Fact]
    public void CompressFile_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing-compressed-file.txt");

        var ex = Assert.Throws<AppException>(() => _compressService.CompressFile(missingPath));

        Assert.Contains("Path not found or is not a file", ex.Message);
        Assert.Equal("CompressAttributeService.CompressFile()", ex.ClassRootCauseName);
    }

    [Fact]
    public void DecompressFile_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing-decompressed-file.txt");

        var ex = Assert.Throws<AppException>(() => _compressService.DecompressFile(missingPath));

        Assert.Contains("Path not found or is not a file", ex.Message);
        Assert.Equal("CompressAttributeService.DecompressFile()", ex.ClassRootCauseName);
    }

    [Fact]
    public void CompressDirectory_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing-compressed-dir");

        var ex = Assert.Throws<AppException>(() => _compressService.CompressDirectory(missingPath));

        Assert.Contains("Path not found or is not a directory", ex.Message);
        Assert.Equal("CompressAttributeService.CompressDirectory()", ex.ClassRootCauseName);
    }

    [Fact]
    public void DecompressDirectory_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing-decompressed-dir");

        var ex = Assert.Throws<AppException>(() => _compressService.DecompressDirectory(missingPath));

        Assert.Contains("Path not found or is not a directory", ex.Message);
        Assert.Equal("CompressAttributeService.DecompressDirectory()", ex.ClassRootCauseName);
    }

    [Fact]
    public void CompressFile_DirectoryPath_ThrowsAppException()
    {
        var dirPath = At("compress-not-a-file");
        Directory.CreateDirectory(dirPath);

        var ex = Assert.Throws<AppException>(() => _compressService.CompressFile(dirPath));

        Assert.Contains("Path not found or is not a file", ex.Message);
        Assert.Equal("CompressAttributeService.CompressFile()", ex.ClassRootCauseName);
    }

    [Fact]
    public void CompressDirectory_FilePath_ThrowsAppException()
    {
        var filePath = At("compress-not-a-dir.txt");
        File.WriteAllText(filePath, "content");

        var ex = Assert.Throws<AppException>(() => _compressService.CompressDirectory(filePath));

        Assert.Contains("Path not found or is not a directory", ex.Message);
        Assert.Equal("CompressAttributeService.CompressDirectory()", ex.ClassRootCauseName);
    }

    [Fact]
    public void SetHidden_File_SetsHiddenAttribute()
    {
        var filePath = At("hidden-file.txt");
        File.WriteAllText(filePath, "content");

        _hiddenService.SetHidden(filePath);

        Assert.True(_hiddenService.IsHidden(filePath));
        Assert.True(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.Hidden));
    }

    [Fact]
    public void UnsetHidden_File_ClearsHiddenAttribute()
    {
        var filePath = At("visible-file.txt");
        File.WriteAllText(filePath, "content");
        File.SetAttributes(filePath, FileAttributes.Hidden);

        _hiddenService.UnsetHidden(filePath);

        Assert.False(_hiddenService.IsHidden(filePath));
        Assert.False(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.Hidden));
    }

    [Fact]
    public void SetHidden_Directory_SetsHiddenAttribute()
    {
        var dirPath = At("hidden-dir");
        Directory.CreateDirectory(dirPath);

        _hiddenService.SetHidden(dirPath);

        Assert.True(_hiddenService.IsHidden(dirPath));
        Assert.True(_metadataProvider.GetDirectoryMetadata(dirPath).Attributes.HasFlag(FileAttributes.Hidden));
    }

    [Fact]
    public void UnsetHidden_Directory_ClearsHiddenAttribute()
    {
        var dirPath = At("visible-dir");
        Directory.CreateDirectory(dirPath);
        File.SetAttributes(dirPath, File.GetAttributes(dirPath) | FileAttributes.Hidden);

        _hiddenService.UnsetHidden(dirPath);

        Assert.False(_hiddenService.IsHidden(dirPath));
        Assert.False(_metadataProvider.GetDirectoryMetadata(dirPath).Attributes.HasFlag(FileAttributes.Hidden));
    }

    [Fact]
    public void SetHidden_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing.txt");

        var ex = Assert.Throws<AppException>(() => _hiddenService.SetHidden(missingPath));

        Assert.Contains("Path not found", ex.Message);
        Assert.Equal("HiddenAttributeService.SetHidden()", ex.ClassRootCauseName);
    }

    [Fact]
    public void UnsetHidden_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing.txt");

        var ex = Assert.Throws<AppException>(() => _hiddenService.UnsetHidden(missingPath));

        Assert.Contains("Path not found", ex.Message);
        Assert.Equal("HiddenAttributeService.UnsetHidden()", ex.ClassRootCauseName);
    }

    [Fact]
    public void SetNotContentIndexed_File_SetsNotContentIndexedAttribute()
    {
        var filePath = At("not-indexed-file.txt");
        File.WriteAllText(filePath, "content");

        _notContentIndexedService.SetNotContentIndexed(filePath);

        Assert.True(_notContentIndexedService.IsNotContentIndexed(filePath));
        Assert.True(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.NotContentIndexed));
    }

    [Fact]
    public void UnsetNotContentIndexed_File_ClearsNotContentIndexedAttribute()
    {
        var filePath = At("indexed-file.txt");
        File.WriteAllText(filePath, "content");
        File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.NotContentIndexed);

        _notContentIndexedService.UnsetNotContentIndexed(filePath);

        Assert.False(_notContentIndexedService.IsNotContentIndexed(filePath));
        Assert.False(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.NotContentIndexed));
    }

    [Fact]
    public void SetNotContentIndexed_Directory_SetsNotContentIndexedAttribute()
    {
        var dirPath = At("not-indexed-dir");
        Directory.CreateDirectory(dirPath);

        _notContentIndexedService.SetNotContentIndexed(dirPath);

        Assert.True(_notContentIndexedService.IsNotContentIndexed(dirPath));
        Assert.True(_metadataProvider.GetDirectoryMetadata(dirPath).Attributes.HasFlag(FileAttributes.NotContentIndexed));
    }

    [Fact]
    public void SetNotContentIndexed_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing-indexed.txt");

        var ex = Assert.Throws<AppException>(() => _notContentIndexedService.SetNotContentIndexed(missingPath));

        Assert.Contains("Path not found", ex.Message);
        Assert.Equal("NotContentIndexedAttributeService.SetNotContentIndexed()", ex.ClassRootCauseName);
    }

    [Fact]
    public void SetReadonlyFile_SetsReadonlyAttribute()
    {
        var filePath = At("readonly-file.txt");
        File.WriteAllText(filePath, "content");

        _readonlyService.SetReadonlyFile(filePath);

        Assert.True(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void UnsetReadonlyFile_ClearsReadonlyAttribute()
    {
        var filePath = At("writable-file.txt");
        File.WriteAllText(filePath, "content");
        File.SetAttributes(filePath, FileAttributes.ReadOnly);

        _readonlyService.UnsetReadonlyFile(filePath);

        Assert.False(_metadataProvider.GetFileMetadata(filePath).Attributes.HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void SetReadonlyDir_NonRecursive_SetsDirectoryAndTopLevelFilesOnly()
    {
        var dirPath = At("readonly-dir");
        var nestedDirPath = Path.Combine(dirPath, "nested");
        var topLevelFilePath = Path.Combine(dirPath, "root.txt");
        var nestedFilePath = Path.Combine(nestedDirPath, "nested.txt");

        Directory.CreateDirectory(nestedDirPath);
        File.WriteAllText(topLevelFilePath, "root");
        File.WriteAllText(nestedFilePath, "nested");

        _readonlyService.SetReadonlyDir(dirPath, recursive: false);

        Assert.True(_metadataProvider.GetDirectoryMetadata(dirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.True(_metadataProvider.GetFileMetadata(topLevelFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.False(_metadataProvider.GetDirectoryMetadata(nestedDirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.False(_metadataProvider.GetFileMetadata(nestedFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void SetReadonlyDir_Recursive_SetsDirectorySubdirectoriesAndFiles()
    {
        var dirPath = At("recursive-readonly");
        var nestedDirPath = Path.Combine(dirPath, "nested");
        var deepDirPath = Path.Combine(nestedDirPath, "deep");
        var topLevelFilePath = Path.Combine(dirPath, "root.txt");
        var nestedFilePath = Path.Combine(nestedDirPath, "nested.txt");
        var deepFilePath = Path.Combine(deepDirPath, "deep.txt");

        Directory.CreateDirectory(deepDirPath);
        File.WriteAllText(topLevelFilePath, "root");
        File.WriteAllText(nestedFilePath, "nested");
        File.WriteAllText(deepFilePath, "deep");

        _readonlyService.SetReadonlyDir(dirPath, recursive: true);

        Assert.True(_metadataProvider.GetDirectoryMetadata(dirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.True(_metadataProvider.GetDirectoryMetadata(nestedDirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.True(_metadataProvider.GetDirectoryMetadata(deepDirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.True(_metadataProvider.GetFileMetadata(topLevelFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.True(_metadataProvider.GetFileMetadata(nestedFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.True(_metadataProvider.GetFileMetadata(deepFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void UnsetReadonlyDir_NonRecursive_ClearsDirectoryAndTopLevelFilesOnly()
    {
        var dirPath = At("readonly-non-recursive-clear");
        var nestedDirPath = Path.Combine(dirPath, "nested");
        var topLevelFilePath = Path.Combine(dirPath, "root.txt");
        var nestedFilePath = Path.Combine(nestedDirPath, "nested.txt");

        Directory.CreateDirectory(nestedDirPath);
        File.WriteAllText(topLevelFilePath, "root");
        File.WriteAllText(nestedFilePath, "nested");
        _readonlyService.SetReadonlyDir(dirPath, recursive: true);

        _readonlyService.UnsetReadonlyDir(dirPath, recursive: false);

        Assert.False(_metadataProvider.GetDirectoryMetadata(dirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.False(_metadataProvider.GetFileMetadata(topLevelFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.True(_metadataProvider.GetDirectoryMetadata(nestedDirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.True(_metadataProvider.GetFileMetadata(nestedFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void UnsetReadonlyDir_Recursive_ClearsDirectorySubdirectoriesAndFiles()
    {
        var dirPath = At("recursive-writable");
        var nestedDirPath = Path.Combine(dirPath, "nested");
        var deepDirPath = Path.Combine(nestedDirPath, "deep");
        var topLevelFilePath = Path.Combine(dirPath, "root.txt");
        var nestedFilePath = Path.Combine(nestedDirPath, "nested.txt");
        var deepFilePath = Path.Combine(deepDirPath, "deep.txt");

        Directory.CreateDirectory(deepDirPath);
        File.WriteAllText(topLevelFilePath, "root");
        File.WriteAllText(nestedFilePath, "nested");
        File.WriteAllText(deepFilePath, "deep");
        _readonlyService.SetReadonlyDir(dirPath, recursive: true);

        _readonlyService.UnsetReadonlyDir(dirPath, recursive: true);

        Assert.False(_metadataProvider.GetDirectoryMetadata(dirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.False(_metadataProvider.GetDirectoryMetadata(nestedDirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.False(_metadataProvider.GetDirectoryMetadata(deepDirPath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.False(_metadataProvider.GetFileMetadata(topLevelFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.False(_metadataProvider.GetFileMetadata(nestedFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
        Assert.False(_metadataProvider.GetFileMetadata(deepFilePath).Attributes.HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void UnsetReadonlyFile_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing-unset-readonly-file.txt");

        var ex = Assert.Throws<AppException>(() => _readonlyService.UnsetReadonlyFile(missingPath));

        Assert.Contains("Path not found or is not a file", ex.Message);
        Assert.Equal("ReadonlyAttributeService.UnsetReadonlyFile()", ex.ClassRootCauseName);
    }

    [Fact]
    public void SetReadonlyDir_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing-dir");

        var ex = Assert.Throws<AppException>(() => _readonlyService.SetReadonlyDir(missingPath));

        Assert.Contains("Path not found or is not a directory", ex.Message);
        Assert.Equal("ReadonlyAttributeService.SetReadonlyDir()", ex.ClassRootCauseName);
    }

    [Fact]
    public void UnsetReadonlyDir_MissingPath_ThrowsAppException()
    {
        var missingPath = At("missing-unset-dir");

        var ex = Assert.Throws<AppException>(() => _readonlyService.UnsetReadonlyDir(missingPath));

        Assert.Contains("Path not found or is not a directory", ex.Message);
        Assert.Equal("ReadonlyAttributeService.UnsetReadonlyDir()", ex.ClassRootCauseName);
    }

    [Fact]
    public void SetReadonlyFile_DirectoryPath_ThrowsAppException()
    {
        var dirPath = At("not-a-file");
        Directory.CreateDirectory(dirPath);

        var ex = Assert.Throws<AppException>(() => _readonlyService.SetReadonlyFile(dirPath));

        Assert.Contains("Path not found or is not a file", ex.Message);
        Assert.Equal("ReadonlyAttributeService.SetReadonlyFile()", ex.ClassRootCauseName);
    }

    [Fact]
    public void UnsetReadonlyFile_DirectoryPath_ThrowsAppException()
    {
        var dirPath = At("not-a-file-unset");
        Directory.CreateDirectory(dirPath);

        var ex = Assert.Throws<AppException>(() => _readonlyService.UnsetReadonlyFile(dirPath));

        Assert.Contains("Path not found or is not a file", ex.Message);
        Assert.Equal("ReadonlyAttributeService.UnsetReadonlyFile()", ex.ClassRootCauseName);
    }

    [Fact]
    public void SetReadonlyDir_FilePath_ThrowsAppException()
    {
        var filePath = At("not-a-dir.txt");
        File.WriteAllText(filePath, "content");

        var ex = Assert.Throws<AppException>(() => _readonlyService.SetReadonlyDir(filePath));

        Assert.Contains("Path not found or is not a directory", ex.Message);
        Assert.Equal("ReadonlyAttributeService.SetReadonlyDir()", ex.ClassRootCauseName);
    }

    [Fact]
    public void UnsetReadonlyDir_FilePath_ThrowsAppException()
    {
        var filePath = At("not-a-dir-unset.txt");
        File.WriteAllText(filePath, "content");

        var ex = Assert.Throws<AppException>(() => _readonlyService.UnsetReadonlyDir(filePath));

        Assert.Contains("Path not found or is not a directory", ex.Message);
        Assert.Equal("ReadonlyAttributeService.UnsetReadonlyDir()", ex.ClassRootCauseName);
    }
}
