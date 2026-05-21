using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using FileSystemP.Core;
using FileSystemP.Core.Services;
using FileSystemP.WPF.Helpers;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;

namespace FileSystemP.WPF.ViewModels;

public partial class FileTreeNode : ObservableObject
{
    public string FullPath { get; }
    public string Name { get; }
    public ImageSource? Icon { get; }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private ObservableCollection<FileTreeNode> _children = new();

    private bool _isLoaded;

    public FileTreeNode(string fullPath, string name, ImageSource? icon)
    {
        FullPath = fullPath;
        Name = name;
        Icon = icon;
        // Add placeholder so expand arrow appears in TreeView
        Children.Add(new FileTreeNode("", "", null) { _isLoaded = true });
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (!value || _isLoaded) return;
        _isLoaded = true;
        LoadChildrenAsync();
    }

    private async void LoadChildrenAsync()
    {
        IEnumerable<FileSystemInfo>? entries = null;
        try
        {
            entries = await Task.Run(() => FileDirectorySystemService.GetEntries(FullPath).ToList());
        }
        catch (AppException)
        {
            await Application.Current.Dispatcher.InvokeAsync(() => Children.Clear());
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Children.Clear();
            foreach (var dir in entries.OfType<DirectoryInfo>())
            {
                Children.Add(new FileTreeNode(
                    dir.FullName,
                    dir.Name,
                    ShellIconHelper.GetIcon(dir.FullName, isDirectory: true)));
            }
        });
    }
}
