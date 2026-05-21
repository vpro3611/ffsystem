using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FileSystemP.Core;
using FileSystemP.Core.Services;

namespace FileSystemP.WPF.ViewModels;

public partial class FileTreeNode : ObservableObject
{
    public string Path { get; }
    public string Name { get; }
    public ImageSource? Icon { get; }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private ObservableCollection<FileTreeNode> _children;

    public FileTreeNode(string path, string name, ImageSource? icon)
    {
        Path = path;
        Name = name;
        Icon = icon;
        _children = new ObservableCollection<FileTreeNode>
        {
            new FileTreeNode("__dummy", "__dummy", null)
        };
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (!value) return;

        // Only expand if the first child is the dummy placeholder
        if (_children.Count != 1 || _children[0].Path != "__dummy") return;

        _children.Clear();

        try
        {
            var entries = FileDirectorySystemService.GetEntries(Path);
            foreach (var entry in entries.OfType<DirectoryInfo>())
            {
                _children.Add(new FileTreeNode(entry.FullName, entry.Name, null));
            }
        }
        catch (AppException)
        {
            // Leave children empty on access errors
        }
    }
}
