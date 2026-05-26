using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FileSystemP.Core.AttributeService;

public class CompressAttributeService
{
    private const string _className = nameof(CompressAttributeService);

    private const uint FsctlSetCompression = 0x0009C040;
    private const short CompressionFormatNone = 0x0000;
    private const short CompressionFormatDefault = 0x0001;

    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint FileShareDelete = 0x00000004;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;

    public bool IsFileCompressed(string path)
    {
        return File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.Compressed);
    }

    public bool IsDirectoryCompressed(string path)
    {
        return Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.Compressed);
    }

    public void CompressFile(string path)
    {
        EnsureFileExists(path, nameof(CompressFile));
        EnsureCompressionSupported(path, nameof(CompressFile));
        ApplyCompression(path, isDirectory: false, compress: true, nameof(CompressFile));
    }

    public void DecompressFile(string path)
    {
        EnsureFileExists(path, nameof(DecompressFile));
        EnsureCompressionSupported(path, nameof(DecompressFile));
        ApplyCompression(path, isDirectory: false, compress: false, nameof(DecompressFile));
    }

    public void CompressDirectory(string path, bool recursive = false)
    {
        EnsureDirectoryExists(path, nameof(CompressDirectory));
        EnsureCompressionSupported(path, nameof(CompressDirectory));

        var dir = new DirectoryInfo(path);
        ApplyCompression(dir.FullName, isDirectory: true, compress: true, nameof(CompressDirectory));

        if (!recursive)
        {
            return;
        }

        foreach (var subdir in EnumerateDirectoriesBreadthFirst(dir))
        {
            ApplyCompression(subdir.FullName, isDirectory: true, compress: true, nameof(CompressDirectory));

            foreach (var file in EnumerateFiles(subdir))
            {
                ApplyCompression(file.FullName, isDirectory: false, compress: true, nameof(CompressDirectory));
            }
        }

        foreach (var file in EnumerateFiles(dir))
        {
            ApplyCompression(file.FullName, isDirectory: false, compress: true, nameof(CompressDirectory));
        }
    }

    public void DecompressDirectory(string path, bool recursive = false)
    {
        EnsureDirectoryExists(path, nameof(DecompressDirectory));
        EnsureCompressionSupported(path, nameof(DecompressDirectory));

        var dir = new DirectoryInfo(path);

        if (recursive)
        {
            List<DirectoryInfo> directories = EnumerateDirectoriesBreadthFirst(dir).ToList();

            foreach (var file in EnumerateFiles(dir))
            {
                ApplyCompression(file.FullName, isDirectory: false, compress: false, nameof(DecompressDirectory));
            }

            foreach (var subdir in directories)
            {
                foreach (var file in EnumerateFiles(subdir))
                {
                    ApplyCompression(file.FullName, isDirectory: false, compress: false, nameof(DecompressDirectory));
                }
            }

            foreach (var subdir in directories.OrderByDescending(directory => directory.FullName.Length))
            {
                ApplyCompression(subdir.FullName, isDirectory: true, compress: false, nameof(DecompressDirectory));
            }
        }

        ApplyCompression(dir.FullName, isDirectory: true, compress: false, nameof(DecompressDirectory));
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("NTFS compression is only supported on Windows.");
        }
    }

    private void EnsureCompressionSupported(string path, string method)
    {
        EnsureWindows();

        string fullPath = Path.GetFullPath(path);
        string? root = Path.GetPathRoot(fullPath);

        if (string.IsNullOrEmpty(root))
        {
            throw new AppException($"Unable to determine drive for path: {path}", $"{_className}.{method}()");
        }

        var drive = new DriveInfo(root);
        if (!drive.IsReady)
        {
            throw new AppException($"Drive is not ready for path: {path}", $"{_className}.{method}()");
        }

        if (!string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(
                $"Compression is only supported on NTFS drives. Path '{path}' is on '{drive.DriveFormat}'.",
                $"{_className}.{method}()");
        }
    }

    private void EnsureFileExists(string path, string method)
    {
        if (!File.Exists(path))
        {
            throw new AppException($"Path not found or is not a file: {path}", $"{_className}.{method}()");
        }
    }

    private void EnsureDirectoryExists(string path, string method)
    {
        if (!Directory.Exists(path))
        {
            throw new AppException($"Path not found or is not a directory: {path}", $"{_className}.{method}()");
        }
    }

    private void ApplyCompression(string path, bool isDirectory, bool compress, string method)
    {
        short compressionState = compress ? CompressionFormatDefault : CompressionFormatNone;

        using SafeFileHandle handle = CreateFile(
            path,
            GenericRead | GenericWrite,
            FileShareRead | FileShareWrite | FileShareDelete,
            IntPtr.Zero,
            OpenExisting,
            isDirectory ? FileFlagBackupSemantics : 0,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            ThrowCompressionError(path, method);
        }

        bool ok = DeviceIoControl(
            handle,
            FsctlSetCompression,
            ref compressionState,
            sizeof(short),
            IntPtr.Zero,
            0,
            out _,
            IntPtr.Zero);

        if (!ok)
        {
            ThrowCompressionError(path, method);
        }
    }

    private void ThrowCompressionError(string path, string method)
    {
        throw new AppException(
            $"Failed to change compression state for path: {path}. {new Win32Exception(Marshal.GetLastWin32Error()).Message}",
            $"{_className}.{method}()");
    }

    private static IEnumerable<DirectoryInfo> EnumerateDirectoriesBreadthFirst(DirectoryInfo root)
    {
        var pending = new Queue<DirectoryInfo>();

        foreach (var child in EnumerateChildDirectories(root))
        {
            pending.Enqueue(child);
        }

        while (pending.Count > 0)
        {
            DirectoryInfo current = pending.Dequeue();
            yield return current;

            foreach (var child in EnumerateChildDirectories(current))
            {
                pending.Enqueue(child);
            }
        }
    }

    private static IEnumerable<DirectoryInfo> EnumerateChildDirectories(DirectoryInfo directory)
    {
        foreach (var child in directory.EnumerateDirectories())
        {
            if (child.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            yield return child;
        }
    }

    private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles())
        {
            if (file.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            yield return file;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        ref short lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        out int lpBytesReturned,
        IntPtr lpOverlapped);
}
