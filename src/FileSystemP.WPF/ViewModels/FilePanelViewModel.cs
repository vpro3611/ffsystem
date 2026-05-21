using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSystemP.Core;
using FileSystemP.Core.Services;
using FileSystemP.WPF.Helpers;
using FileSystemP.WPF.Models;
using FileSystemP.WPF.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FileSystemP.WPF.ViewModels;

public partial class FilePanelViewModel : ObservableObject
{
    private readonly Action<string> _navigate;
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FileEntry> _entries = new();

    [ObservableProperty]
    private FileEntry? _selectedEntry;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private string? _clipboardPath;

    partial void OnClipboardPathChanged(string? value)
    {
        PasteCommand.NotifyCanExecuteChanged();
    }

    public FilePanelViewModel(Action<string> navigate)
    {
        _navigate = navigate;
    }

    public async Task LoadEntries(string path)
    {
        _currentPath = path;
        ErrorMessage = null;
        try
        {
            var entries = await Task.Run(() =>
                FileDirectorySystemService.GetEntries(path)
                    .Select(MapToEntry)
                    .ToList());
            Entries.Clear();
            foreach (var e in entries) Entries.Add(e);
            IsEmpty = Entries.Count == 0;
        }
        catch (Exception ex)
        {
            Entries.Clear();
            IsEmpty = false;
            ErrorMessage = $"Could not load folder contents: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Open()
    {
        if (SelectedEntry is null) return;

        if (SelectedEntry.IsDirectory)
        {
            _navigate(SelectedEntry.FilePath);
        }
        else
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SelectedEntry.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task Rename()
    {
        if (SelectedEntry is null) return;

        var newName = InputDialog.Show("Enter new name:", SelectedEntry.Name);
        if (string.IsNullOrWhiteSpace(newName)) return;

        try
        {
            FileDirectorySystemService.Rename(SelectedEntry.FilePath, newName);
            await LoadEntries(_currentPath);
        }
        catch (AppException ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (SelectedEntry is null) return;

        if (MessageBox.Show($"Delete '{SelectedEntry.Name}'?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        try
        {
            FileDirectorySystemService.Delete(SelectedEntry.FilePath);
            await LoadEntries(_currentPath);
        }
        catch (AppException ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Copy()
    {
        if (SelectedEntry is null) return;
        ClipboardPath = SelectedEntry.FilePath;
    }

    [RelayCommand(CanExecute = nameof(CanPaste))]
    private async Task Paste()
    {
        if (ClipboardPath is null || string.IsNullOrEmpty(_currentPath)) return;
        if (Directory.Exists(ClipboardPath))
        {
            MessageBox.Show("Copying folders is not supported.", "Unsupported", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var destination = Path.Combine(_currentPath, Path.GetFileName(ClipboardPath));
        try
        {
            FileDirectorySystemService.Copy(ClipboardPath, destination);
            await LoadEntries(_currentPath);
        }
        catch (AppException ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanPaste() => ClipboardPath != null;

    [RelayCommand]
    private async Task NewFile()
    {
        var name = InputDialog.Show("Enter file name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var path = Path.Combine(_currentPath, name);
        try
        {
            FileDirectorySystemService.CreateFile(path);
            await LoadEntries(_currentPath);
        }
        catch (AppException ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task NewFolder()
    {
        var name = InputDialog.Show("Enter folder name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var path = Path.Combine(_currentPath, name);
        try
        {
            FileDirectorySystemService.CreateDirectory(path);
            await LoadEntries(_currentPath);
        }
        catch (AppException ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // -------------------------------------------------------------------------
    // Mapping helpers
    // -------------------------------------------------------------------------

    private static FileEntry MapToEntry(FileSystemInfo info)
    {
        bool isDir = info is DirectoryInfo;
        return new FileEntry
        {
            FilePath = info.FullName,
            Name = info.Name,
            Icon = ShellIconHelper.GetIcon(info.FullName, isDirectory: isDir),
            Type = isDir ? "File Folder" : GetFileType(info.Name),
            Size = isDir ? "" : FormatSize(((FileInfo)info).Length),
            IsDirectory = isDir,
            DateModified = info.LastWriteTime
        };
    }

    private static string GetFileType(string name)
    {
        var ext = Path.GetExtension(name).ToUpperInvariant();
        return string.IsNullOrEmpty(ext) ? "File" : $"{ext.TrimStart('.')} File";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}
