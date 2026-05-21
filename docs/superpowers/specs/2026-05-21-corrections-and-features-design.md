# Corrections and Features Design

**Date:** 2026-05-21

## Overview

Bug fixes and feature additions for the WPF file manager. Covers six bug fixes and four new features.

---

## Bug Fixes

### 1. Copy/Paste — SelectedEntry null on right-click
WPF `ListView` does not auto-select an item on right-click, so `SelectedEntry` is null when Copy executes. Fix: handle `PreviewMouseRightButtonDown` in `MainWindow.xaml.cs` — find the clicked `ListViewItem` and set `FileList.SelectedItem` before the context menu opens.

### 2. InputDialog appears behind main window
`InputDialog.Show()` creates a window with no `Owner`, causing it to render behind the main window. Fix: set `dialog.Owner = Application.Current.MainWindow` in `InputDialog.Show()`.

### 3. Expand arrows on empty directories
Every `FileTreeNode` gets a placeholder child on construction, showing an expand arrow regardless of whether the directory has subdirectories. Fix: after `LoadChildrenAsync` completes with zero results, set `IsExpanded = false` on the node. The `TreeView` automatically hides the arrow when `Children` is empty.

### 4. Tree panel item spacing
TreeView items are too tight. Fix: add `Padding="4,3"` to the `TreeViewItem` style in `MainWindow.xaml`.

---

## New Features

### 5. Navigation History (Back / Forward)
`MainWindowViewModel` gains two stacks: `_backStack` and `_forwardStack` (`Stack<string>`).

**Navigate to new path** (from tree click or double-click): if `CurrentPath` is non-empty, push it to `_backStack`; clear `_forwardStack`; then set `CurrentPath`. (Guard prevents empty string from polluting the stack on first navigation.)

**Back:** pop from `_backStack` → push current `CurrentPath` to `_forwardStack` → set `CurrentPath`.

**Forward:** pop from `_forwardStack` → push current `CurrentPath` to `_backStack` → set `CurrentPath`.

Exposed as `[RelayCommand] NavigateBack` and `[RelayCommand] NavigateForward` with `CanExecute` guards (`_backStack.Count > 0` / `_forwardStack.Count > 0`). Navigation initiated by the navigate `Action<string>` callback must go through a new `NavigateTo(string path)` method that manages the stacks, replacing the direct `CurrentPath = path` assignment.

### 6. Undo Stack
```
interface IUndoAction { void Execute(); }
```
Three implementations (all in `FilePanelViewModel.cs`):
- `UndoRename(string path, string originalName)` — calls `FileDirectorySystemService.Rename(newPath, originalName)`
- `UndoCreate(string path)` — calls `FileDirectorySystemService.Delete(path)`
- `UndoPaste(string destPath)` — calls `FileDirectorySystemService.Delete(destPath)`

`FilePanelViewModel` holds `Stack<IUndoAction> _undoStack`. After each successful mutating command (Rename, NewFile, NewFolder, Paste, NewFileWithContent), push the corresponding undo action. `[RelayCommand] Undo` pops and executes, then reloads entries. `CanExecute` = `_undoStack.Count > 0`.

### 7. Toolbar
A `ToolBar` (or `StackPanel` with `Button`s) sits above the main split layout in `MainWindow.xaml`. Buttons: `←` (Back), `→` (Forward), `↩` (Undo). Each bound to the corresponding command on `MainWindowViewModel` / `FilePanelViewModel`. Foreground from `AppForeground` brush; disabled state muted.

### 8. CreateFileWithContent
New `ContentDialog.xaml` + `ContentDialog.xaml.cs` in `Views/` — a window with a filename `TextBox`, a multiline content `TextBox`, and OK/Cancel buttons. Static `Show(string prompt) → (string name, string content)?`.

New `[RelayCommand] NewFileWithContent` in `FilePanelViewModel`: opens `ContentDialog`, calls `FileDirectorySystemService.CreateFileWithContent(path, content)`, pushes `UndoCreate` to stack, reloads entries. Added to FolderMenu and EmptySpaceMenu in `MainWindow.xaml` as "New File with Content".

---

## Out of Scope
- Undo for Delete (irreversible without trash)
- Multi-level undo history limit
- Redo
