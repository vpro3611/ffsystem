# WPF Frontend Design

**Date:** 2026-05-21

## Overview

Windows Explorer-style file manager UI. Tree panel on the left for navigation, file list panel on the right for contents. All Core service operations exposed via context-aware right-click menus. Shell icons throughout. System dark/light theme applied on launch.

---

## Architecture

Pattern: MVVM using CommunityToolkit.Mvvm (NuGet). `MainWindow.xaml.cs` only sets `DataContext` — all logic lives in ViewModels.

```
FileSystemP.WPF/
├── ViewModels/
│   ├── MainWindowViewModel.cs   — owns CurrentPath, coordinates tree ↔ panel
│   ├── FileTreeViewModel.cs     — drives as root nodes, lazy folder expansion
│   ├── FileTreeNode.cs          — single tree node with lazy-loaded children
│   └── FilePanelViewModel.cs    — current directory contents + all commands
├── Models/
│   └── FileEntry.cs             — display model wrapping FileSystemInfo
├── Helpers/
│   └── ShellIconHelper.cs       — Shell32 P/Invoke, icon cache by extension
├── MainWindow.xaml
└── MainWindow.xaml.cs
```

---

## Tree Panel

- Root nodes: all ready drives from `DriveService.GetDrives()`
- Each node gets a dummy child on creation so the expand arrow appears
- On expand: dummy replaced by real `DirectoryInfo` children via `FileDirectorySystemService.GetEntries()`
- Only directories shown in tree; files live in right panel only
- Selecting a node sets `MainWindowViewModel.CurrentPath`
- Inaccessible folders on expand: node shows empty silently

**`FileTreeNode` properties:** `Path`, `Name`, `Icon (ImageSource)`, `Children (ObservableCollection<FileTreeNode>)`, `IsExpanded`

---

## File Panel

ListView with columns: Icon (16×16), Name, Type, Size, Date Modified.

**`FileEntry` properties:** `Path`, `Name`, `Icon`, `Type`, `Size`, `IsDirectory`, `DateModified`

| Interaction | Behaviour |
|---|---|
| Single click | Selects item, updates `SelectedEntry` |
| Double-click folder | Navigates into it (updates tree + reloads panel) |
| Double-click file | `Process.Start` with `UseShellExecute = true` |
| Empty folder | Centered muted text: `"This folder is empty"` |
| Load error | Centered muted text: `"Could not load folder contents: <message>"` |
| Nothing loaded | Panel blank |

---

## Context Menus

All actions are `[RelayCommand]` on `FilePanelViewModel`.

**File:**
Open, Rename, Copy, Delete

**Folder (in right panel):**
Open, Rename, Copy, Delete, New File, New Folder

**Empty space in panel:**
Paste *(enabled only when something copied)*, New File, New Folder

**Rename / New File / New Folder:** lightweight `InputDialog` window (label + TextBox + OK/Cancel).  
**Delete:** `MessageBox` confirmation before calling Core.  
**Copy/Paste:** two-step — Copy stores source path in VM state, Paste calls `FileDirectorySystemService.Copy(source, destination)`.

---

## Shell Icons

`ShellIconHelper.GetIcon(string path)` calls `SHGetFileInfo` from `Shell32.dll` via P/Invoke and returns `ImageSource`. Icons cached by file extension; folders and drives cached as single entries. Used in both tree nodes and file panel entries.

---

## System Theme

On app launch, read `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme` from registry. Swap in `Dark.xaml` or `Light.xaml` `ResourceDictionary` accordingly. No live switching — theme is fixed for the session.

---

## Error Handling

All Core calls wrapped in try/catch for `AppException`. Tree expand errors: node stays empty. Panel load errors: error text shown in panel background. File operations (rename, delete, copy): show `MessageBox` with `AppException.Message`.

---

## Out of Scope (this version)

- Toolbar
- Address bar / breadcrumb navigation
- Search
- Live theme switching
- Drag and drop
- Multi-select operations
