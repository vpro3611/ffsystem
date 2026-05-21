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

    // Private placeholder constructor — does NOT add a child, breaking the recursion
    private FileTreeNode()
    {
        FullPath = string.Empty;
        Name = string.Empty;
        Icon = null;
        _isLoaded = true;
    }

    public FileTreeNode(string fullPath, string name, ImageSource? icon)
    {
        FullPath = fullPath;
        Name = name;
        Icon = icon;
        Children.Add(new FileTreeNode()); // placeholder so expand arrow appears
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

        if (Children.Count == 0)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsExpanded = false;
                _isLoaded = false;
            });
        }
    }
}
