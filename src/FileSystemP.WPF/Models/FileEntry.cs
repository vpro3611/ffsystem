using System.Windows.Media;

namespace FileSystemP.WPF.Models;

public class FileEntry
{
    public string FilePath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ImageSource? Icon { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public DateTime DateModified { get; set; }
}
