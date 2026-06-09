using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSystemP.Core;
using FileSystemP.Core.MetadataService.Providers.Ntfs;
using FileSystemP.Core.Services;
using FileSystemP.WPF.Helpers;
using FileSystemP.WPF.Models;
using FileSystemP.WPF.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FileSystemP.WPF.ViewModels;

public partial class FilePanelViewModel : ObservableObject
{
    private readonly Action<string> _navigate;
    private readonly INtfsMetadataProvider _metadataProvider;
    private readonly IUndoService _undoService;
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

    [ObservableProperty]
    private bool _isCopying;

    [ObservableProperty]
    private double _copyProgress;

    [ObservableProperty]
    private bool _showHiddenFiles;

    partial void OnShowHiddenFilesChanged(bool value)
    {
        _ = LoadEntries(_currentPath);
    }

    partial void OnClipboardPathChanged(string? value)
    {
        PasteCommand.NotifyCanExecuteChanged();
    }

    public FilePanelViewModel(Action<string> navigate, INtfsMetadataProvider metadataProvider, IUndoService undoService)
    {
        _navigate = navigate;
        _metadataProvider = metadataProvider;
        _undoService = undoService;
        _undoService.CanUndoChanged += (s, e) => UndoCommand.NotifyCanExecuteChanged();
    }

    public async Task LoadEntries(string path)
    {
        _currentPath = path;
        ErrorMessage = null;
        try
        {
            var entries = await Task.Run(() =>
                FileDirectorySystemService.GetEntries(path)
                    .Where(e => ShowHiddenFiles || !e.Attributes.HasFlag(FileAttributes.Hidden))
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
                })?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void ShowProperties()
    {
        if (SelectedEntry is null) return;
        ShowPropertiesForPath(SelectedEntry.FilePath, SelectedEntry.IsDirectory);
    }

    public void ShowPropertiesForPath(string path, bool isDirectory)
    {
        try
        {
            object metadata = isDirectory
                ? _metadataProvider.GetDirectoryMetadata(path)
                : _metadataProvider.GetFileMetadata(path);

            bool changed = PropertiesWindow.ShowFor(
                metadata,
                Application.Current.MainWindow,
                onChangesApplied: () => _ = LoadEntries(_currentPath));

            if (changed)
            {
                _ = LoadEntries(_currentPath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error fetching metadata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Rename()
    {
        if (SelectedEntry is null) return;

        var newName = InputDialog.Show("Enter new name:", SelectedEntry.Name);
        if (string.IsNullOrWhiteSpace(newName)) return;

        var oldPath = SelectedEntry.FilePath;
        var parent = System.IO.Path.GetDirectoryName(oldPath)!;

        try
        {
            FileDirectorySystemService.Rename(oldPath, newName);
            var newFullPath = System.IO.Path.Combine(parent, newName);
            _undoService.Push(new UndoRenameAction(newFullPath, oldPath));
            await LoadEntries(_currentPath);
        }
        catch (Exception ex)
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
        catch (Exception ex)
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
        if (ClipboardPath is null) return;
        if (string.IsNullOrEmpty(_currentPath))
        {
            MessageBox.Show("Navigate to a destination folder first.", "No folder selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var destination = Path.Combine(_currentPath, Path.GetFileName(ClipboardPath));

        // Check if destination already exists (UI Policy)
        if (File.Exists(destination) || Directory.Exists(destination))
        {
            MessageBox.Show($"The destination '{Path.GetFileName(ClipboardPath)}' already exists.", "Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsCopying = true;
            CopyProgress = 0;
            var progress = new Progress<double>(p => CopyProgress = p);

            await Task.Run(() => FileSystemP.Core.Services.FileDirectorySystemService.Copy(ClipboardPath, destination, overwrite: false, progress));
            
            _undoService.Push(new UndoCreateAction(destination));
            await LoadEntries(_currentPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCopying = false;
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
            _undoService.Push(new UndoCreateAction(path));
            await LoadEntries(_currentPath);
        }
        catch (Exception ex)
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
            _undoService.Push(new UndoCreateAction(path));
            await LoadEntries(_currentPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task Undo()
    {
        if (!_undoService.CanUndo) return;
        try
        {
            _undoService.Undo();
            await LoadEntries(_currentPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Undo failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanUndo() => _undoService.CanUndo;

    [RelayCommand]
    private async Task NewFileWithContent()
    {
        var result = ContentDialog.Show();
        if (result is null || string.IsNullOrWhiteSpace(result.Value.name)) return;
        var fullPath = System.IO.Path.Combine(_currentPath, result.Value.name);
        try
        {
            await FileDirectorySystemService.CreateFileWithContent(fullPath, result.Value.content);
            _undoService.Push(new UndoCreateAction(fullPath));
            await LoadEntries(_currentPath);
        }
        catch (Exception ex)
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
