using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FileSystemP.WPF.Models;
using FileSystemP.WPF.ViewModels;

namespace FileSystemP.WPF;

public partial class MainWindow : Window
{
    private Point _dragStartPoint;
    private FileEntry? _draggedEntry;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainWindowViewModel vm && e.NewValue is FileTreeNode node)
            vm.Tree.SelectedNode = node;
    }

    private void FileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.Panel.OpenCommand.CanExecute(null))
            vm.Panel.OpenCommand.Execute(null);
    }

    private void FileList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _draggedEntry = FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject)?.DataContext as FileEntry;

        if (_draggedEntry is not null)
        {
            FileList.SelectedItem = _draggedEntry;
        }
    }

    private void FileList_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedEntry is null)
            return;

        Point currentPosition = e.GetPosition(this);
        Vector diff = currentPosition - _dragStartPoint;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var data = new DataObject();
        data.SetData(typeof(string), _draggedEntry.FilePath);
        data.SetData(DataFormats.StringFormat, _draggedEntry.FilePath);
        DragDrop.DoDragDrop(FileList, data, DragDropEffects.Move);
        _draggedEntry = null;
    }

    private void FileList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? dep = e.OriginalSource as DependencyObject;
        while (dep != null && dep is not ListViewItem)
            dep = VisualTreeHelper.GetParent(dep);
        if (dep is ListViewItem item)
            FileList.SelectedItem = item.DataContext;
    }

    private void FileList_DragOver(object sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        string? sourcePath = GetDraggedPath(e);
        string? destinationDirectory = GetListDropDestination(e.OriginalSource as DependencyObject, vm.CurrentPath);
        e.Effects = CanMoveToDestination(sourcePath, destinationDirectory) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private async void FileList_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        string? sourcePath = GetDraggedPath(e);
        string? destinationDirectory = GetListDropDestination(e.OriginalSource as DependencyObject, vm.CurrentPath);
        if (!CanMoveToDestination(sourcePath, destinationDirectory))
            return;

        await vm.Panel.MoveEntryToDirectory(sourcePath!, destinationDirectory!);
        e.Handled = true;
    }

    private void FileTree_DragOver(object sender, DragEventArgs e)
    {
        string? sourcePath = GetDraggedPath(e);
        string? destinationDirectory = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject)?.DataContext is FileTreeNode node
            ? node.FullPath
            : null;

        e.Effects = CanMoveToDestination(sourcePath, destinationDirectory) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private async void FileTree_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        string? sourcePath = GetDraggedPath(e);
        string? destinationDirectory = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject)?.DataContext is FileTreeNode node
            ? node.FullPath
            : null;

        if (!CanMoveToDestination(sourcePath, destinationDirectory))
            return;

        await vm.Panel.MoveEntryToDirectory(sourcePath!, destinationDirectory!);
        e.Handled = true;
    }

    private static string? GetDraggedPath(DragEventArgs e)
    {
        return e.Data.GetData(typeof(string)) as string ?? e.Data.GetData(DataFormats.StringFormat) as string;
    }

    private static string? GetListDropDestination(DependencyObject? originalSource, string currentPath)
    {
        if (FindAncestor<ListViewItem>(originalSource)?.DataContext is FileEntry entry && entry.IsDirectory)
        {
            return entry.FilePath;
        }

        return string.IsNullOrWhiteSpace(currentPath) ? null : currentPath;
    }

    private static bool CanMoveToDestination(string? sourcePath, string? destinationDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationDirectory))
            return false;

        string normalizedSource = System.IO.Path.GetFullPath(sourcePath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
        string normalizedDestination = System.IO.Path.GetFullPath(destinationDirectory.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
        string finalDestination = System.IO.Path.Combine(normalizedDestination, System.IO.Path.GetFileName(normalizedSource));

        return !string.Equals(finalDestination, normalizedSource, StringComparison.OrdinalIgnoreCase);
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
                return match;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (e.Key == Key.Escape && vm.Palette.IsVisible && !vm.IsTerminalDetached)
        {
            vm.Palette.IsVisible = false;
            e.Handled = true;
            return;
        }

        string? gesture = GetGestureText(e);
        if (gesture is null)
        {
            return;
        }

        if (vm.TryExecuteKeyBinding(gesture))
        {
            e.Handled = true;
        }
    }

    private static string? GetGestureText(KeyEventArgs e)
    {
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return null;
        }

        string? keyName = key switch
        {
            >= Key.A and <= Key.Z => key.ToString().ToUpperInvariant(),
            >= Key.D0 and <= Key.D9 => key.ToString()[1..],
            >= Key.F1 and <= Key.F24 => key.ToString().ToUpperInvariant(),
            Key.Enter => "Enter",
            Key.Escape => "Escape",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Tab => "Tab",
            Key.Space => "Space",
            Key.Back => "Backspace",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            _ => null
        };

        if (keyName is null)
        {
            return null;
        }

        var parts = new List<string>();
        ModifierKeys modifiers = Keyboard.Modifiers;
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(keyName);
        return string.Join('+', parts);
    }
}
