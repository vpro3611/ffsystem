# Corrections and Features Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix copy/paste, dialog, arrow, and spacing bugs; add back/forward navigation, undo stack, and CreateFileWithContent.

**Architecture:** All logic changes in ViewModels; UI changes in MainWindow.xaml + MainWindow.xaml.cs. New `IUndoAction` interface lives in `ViewModels/`. New `ContentDialog` in `Views/`.

**Tech Stack:** WPF .NET 9, CommunityToolkit.Mvvm 8.4.2.

---

### Task 1: Quick bug fixes
**Files:** Modify `src/FileSystemP.WPF/Views/InputDialog.xaml.cs`, `src/FileSystemP.WPF/MainWindow.xaml`, `src/FileSystemP.WPF/MainWindow.xaml.cs`
- [ ] In `InputDialog.Show()`: add `dialog.Owner = Application.Current.MainWindow;` before `ShowDialog()`
- [ ] In `MainWindow.xaml.cs`: add handler `FileList_PreviewMouseRightButtonDown` — walks visual tree to find clicked `ListViewItem` and sets `FileList.SelectedItem = item.DataContext`
- [ ] Wire in XAML: `<ListView ... PreviewMouseRightButtonDown="FileList_PreviewMouseRightButtonDown">`
- [ ] In `MainWindow.xaml` TreeViewItem style: add `<Setter Property="Padding" Value="4,3"/>`
- [ ] Build: `$env:DOTNET_ROOT="C:\Program Files\dotnet"; dotnet build src/FileSystemP.WPF --nologo`
- [ ] Commit: `fix: right-click selection, InputDialog owner, tree item spacing`

---

### Task 2: Expand arrow fix
**Files:** Modify `src/FileSystemP.WPF/ViewModels/FileTreeNode.cs`
- [ ] In `LoadChildrenAsync`, after the `Dispatcher.InvokeAsync` block that populates children: if `Children.Count == 0`, also call `await Application.Current.Dispatcher.InvokeAsync(() => { IsExpanded = false; _isLoaded = false; })` — this collapses the node and re-arms it so it disappears visually
- [ ] Build + commit: `fix: hide expand arrow on directories with no subdirectories`

---

### Task 3: ContentDialog
**Files:** Create `src/FileSystemP.WPF/Views/ContentDialog.xaml` + `ContentDialog.xaml.cs`
- [ ] XAML: Window 400×220, NoResize, CenterOwner. Three rows: `Label` "File name:", `TextBox x:Name="NameBox"`; `Label` "Content:"; `TextBox x:Name="ContentBox" AcceptsReturn="True" Height="60"`; OK/Cancel buttons (`IsDefault`/`IsCancel`)
- [ ] Code-behind: private constructor sets `Owner = Application.Current.MainWindow`, pre-fills `NameBox`; OK → `DialogResult = true`; `static (string name, string content)? Show(string prompt)` returns `(NameBox.Text, ContentBox.Text)` or null
- [ ] Build + commit: `feat: add ContentDialog for new file with content`

---

### Task 4: Navigation history
**Files:** Modify `src/FileSystemP.WPF/ViewModels/MainWindowViewModel.cs`
- [ ] Add `Stack<string> _backStack = new()` and `Stack<string> _forwardStack = new()`
- [ ] Replace `Action<string> navigate = path => CurrentPath = path` with `Action<string> navigate = NavigateTo`
- [ ] Add `void NavigateTo(string path)`: if `CurrentPath` non-empty push to `_backStack`; clear `_forwardStack`; set `CurrentPath = path`
- [ ] Add `[RelayCommand(CanExecute = nameof(CanGoBack))] void NavigateBack()`: push `CurrentPath` to `_forwardStack`; set `CurrentPath = _backStack.Pop()`; call `BackCommand.NotifyCanExecuteChanged()` + `ForwardCommand.NotifyCanExecuteChanged()`; `bool CanGoBack() => _backStack.Count > 0`
- [ ] Add `[RelayCommand(CanExecute = nameof(CanGoForward))] void NavigateForward()`: mirror of Back; `bool CanGoForward() => _forwardStack.Count > 0`
- [ ] Also call `NotifyCanExecuteChanged` on both at end of `NavigateTo`
- [ ] Build + commit: `feat: add back/forward navigation history`

---

### Task 5: Undo stack + CreateFileWithContent
**Files:** Modify `src/FileSystemP.WPF/ViewModels/FilePanelViewModel.cs`
- [ ] Add nested types at bottom of file: `interface IUndoAction { void Execute(); }` and three records: `record UndoRename(string NewPath, string OriginalName) : IUndoAction` → `Execute()` calls `FileDirectorySystemService.Rename(NewPath, OriginalName)`; `record UndoCreate(string Path) : IUndoAction` → `Execute()` calls `FileDirectorySystemService.Delete(Path)`; `record UndoPaste(string DestPath) : IUndoAction` → `Execute()` calls `FileDirectorySystemService.Delete(DestPath)`
- [ ] Add `Stack<IUndoAction> _undoStack = new()`
- [ ] After each successful mutating command push: `Rename` → `_undoStack.Push(new UndoRename(Path.Combine(parent, newName), oldName))`; `NewFile`/`NewFolder`/`NewFileWithContent` → `Push(new UndoCreate(fullPath))`; `Paste` → `Push(new UndoPaste(destination))`
- [ ] Add `[RelayCommand(CanExecute = nameof(CanUndo))] async Task Undo()`: pop, call `Execute()`, reload; `bool CanUndo() => _undoStack.Count > 0`; call `UndoCommand.NotifyCanExecuteChanged()` after each push
- [ ] Add `[RelayCommand] async Task NewFileWithContent()`: `ContentDialog.Show("New file with content")` → if null return; `CreateFileWithContent(path, content)`; push `UndoCreate`; reload
- [ ] Build + commit: `feat: undo stack and CreateFileWithContent command`

---

### Task 6: Toolbar + context menus
**Files:** Modify `src/FileSystemP.WPF/MainWindow.xaml`
- [ ] Wrap existing `<Grid>` (the two-column split) in an outer `<Grid>` with two rows: `Auto` (toolbar) and `*` (split). Add `<StackPanel Grid.Row="0" Orientation="Horizontal" Background="{DynamicResource AppSurface}" Height="34">` with three `<Button>` items: `←` bound to `{Binding NavigateBackCommand}`, `→` bound to `{Binding NavigateForwardCommand}`, `↩ Undo` bound to `{Binding Panel.UndoCommand}` — each with `Padding="10,4"` and `Foreground="{DynamicResource AppForeground}"`
- [ ] Add `<MenuItem Header="New File with Content" Command="{Binding DataContext.Panel.NewFileWithContentCommand, RelativeSource={RelativeSource AncestorType=Window}}"/>` to both `FolderMenu` and `EmptySpaceMenu` resources
- [ ] Build: `$env:DOTNET_ROOT="C:\Program Files\dotnet"; dotnet build src/FileSystemP.WPF --nologo`
- [ ] Commit: `feat: toolbar with back/forward/undo and new file with content menu items`
