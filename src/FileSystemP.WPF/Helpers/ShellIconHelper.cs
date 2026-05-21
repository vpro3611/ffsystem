using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FileSystemP.WPF.Helpers;

/// <summary>
/// Provides Windows shell icons for files, folders, and drives using SHGetFileInfo.
/// Icons are cached by file extension (or special keys for folders/drives).
/// </summary>
public static class ShellIconHelper
{
    #region Win32 Interop

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbSizeFileInfo,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    private const uint SHGFI_ICON             = 0x000000100;
    private const uint SHGFI_SMALLICON        = 0x000000001;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

    private const uint FILE_ATTRIBUTE_NORMAL    = 0x00000080;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

    #endregion

    private static readonly Dictionary<string, ImageSource> _cache = new();

    /// <summary>
    /// Returns a small (16×16) shell icon for the given path.
    /// </summary>
    /// <param name="path">
    /// A file path whose extension determines the icon. When <paramref name="isDirectory"/>
    /// or <paramref name="isDrive"/> is <see langword="true"/> the path is used only as a
    /// hint and does not need to exist on disk.
    /// </param>
    /// <param name="isDirectory">When <see langword="true"/>, return the generic folder icon.</param>
    /// <param name="isDrive">When <see langword="true"/>, return the drive icon for <paramref name="path"/>.</param>
    /// <returns>
    /// A frozen <see cref="ImageSource"/>, or <see langword="null"/> if the icon could not
    /// be retrieved.
    /// </returns>
    public static ImageSource? GetIcon(string path, bool isDirectory = false, bool isDrive = false)
    {
        string cacheKey = BuildCacheKey(path, isDirectory, isDrive);

        if (_cache.TryGetValue(cacheKey, out ImageSource? cached))
            return cached;

        ImageSource? icon = FetchIcon(path, isDirectory, isDrive);

        if (icon != null)
            _cache[cacheKey] = icon;

        return icon;
    }

    // -----------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------

    private static string BuildCacheKey(string path, bool isDirectory, bool isDrive)
    {
        if (isDrive)
            return "__drive";

        if (isDirectory)
            return "__folder";

        string ext = System.IO.Path.GetExtension(path);
        return string.IsNullOrEmpty(ext) ? "__noext" : ext.ToLowerInvariant();
    }

    private static ImageSource? FetchIcon(string path, bool isDirectory, bool isDrive)
    {
        uint fileAttributes;
        uint flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;

        if (isDirectory || isDrive)
        {
            fileAttributes = FILE_ATTRIBUTE_DIRECTORY;
            // For a generic folder icon use a constant dummy path so the
            // result is purely attribute-driven.
            if (isDirectory)
                path = "folder";
        }
        else
        {
            fileAttributes = FILE_ATTRIBUTE_NORMAL;
            // Keep only the extension part — the file doesn't need to exist.
            string ext = System.IO.Path.GetExtension(path);
            path = string.IsNullOrEmpty(ext) ? "file" : "file" + ext;
        }

        var shfi = new SHFILEINFO();
        IntPtr result = SHGetFileInfo(
            path,
            fileAttributes,
            ref shfi,
            (uint)Marshal.SizeOf(shfi),
            flags);

        if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
            return null;

        BitmapSource bitmapSource;
        try
        {
            bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                shfi.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            bitmapSource.Freeze(); // make it safe for cross-thread use
        }
        finally
        {
            DestroyIcon(shfi.hIcon);
        }

        return bitmapSource;
    }
}
