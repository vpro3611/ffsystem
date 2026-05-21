# WPF Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows Explorer-style WPF file manager wired to the existing Core services.

**Architecture:** MVVM with CommunityToolkit.Mvvm. `MainWindowViewModel` owns `CurrentPath` and coordinates `FileTreeViewModel` (left tree) and `FilePanelViewModel` (right panel + all commands). No logic in code-behind.

**Tech Stack:** WPF (.NET 9), CommunityToolkit.Mvvm, Shell32 P/Invoke, Windows registry for theme detection.

---

### Task 1: Setup
**Files:** `src/FileSystemP.WPF/FileSystemP.WPF.csproj`, create `ViewModels/`, `Models/`, `Helpers/`, `Themes/`, `Views/`
- [ ] Run: `dotnet add src/FileSystemP.WPF package CommunityToolkit.Mvvm`
- [ ] Create empty folders: `ViewModels/`, `Models/`, `Helpers/`, `Themes/`, `Views/`
- [ ] Commit: `chore: add CommunityToolkit.Mvvm, scaffold WPF folders`

---

### Task 2: ShellIconHelper
**Files:** Create `src/FileSystemP.WPF/Helpers/ShellIconHelper.cs`
- [ ] P/Invoke `SHGetFileInfo` from `shell32.dll` with `SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES`
- [ ] Convert `HICON` → `BitmapSource` via `Imaging.CreateBitmapSourceFromHIcon`, destroy handle after
- [ ] Cache `Dictionary<string, ImageSource>` keyed by lowercase extension; `"__folder"` and `"__drive"` as special keys
- [ ] Public API: `static ImageSource GetIcon(string path, bool isDirectory = false, bool isDrive = false)`
- [ ] Commit: `feat: add ShellIconHelper with Shell32 icon cache`

---

### Task 3: Models
**Files:** Create `src/FileSystemP.WPF/Models/FileEntry.cs`, `src/FileSystemP.WPF/ViewModels/FileTreeNode.cs`
- [ ] `FileEntry`: plain class — `string Path`, `string Name`, `ImageSource Icon`, `string Type`, `string Size`, `bool IsDirectory`, `DateTime DateModified`
- [ ] `FileTreeNode`: inherits `ObservableObject`; `[ObservableProperty]` on `bool IsExpanded` and `ObservableCollection<FileTreeNode> Children`; constructor adds single dummy child; `OnIsExpandedChanged(bool)` — if true, clear children, call `FileDirectorySystemService.GetEntries(Path)`, add only `DirectoryInfo` results as new nodes; catch `AppException` → leave empty
- [ ] Commit: `feat: add FileEntry and FileTreeNode`

---

### Task 4: System Theme
**Files:** Create `src/FileSystemP.WPF/Themes/Dark.xaml`, `Themes/Light.xaml`; modify `App.xaml`, `App.xaml.cs`
- [ ] Define in each dictionary: `AppBackground`, `AppSurface`, `AppForeground`, `AppMuted` brush keys
- [ ] Dark: `#1E1E1E` bg, `#2D2D2D` surface, `#F0F0F0` fg, `#666` muted
- [ ] Light: `#F3F3F3` bg, `#FFFFFF` surface, `#1A1A1A` fg, `#999` muted
- [ ] In `App.xaml.cs` `OnStartup`: read `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize` → `AppsUseLightTheme`; merge `Dark.xaml` if 0, `Light.xaml` otherwise
- [ ] Commit: `feat: apply system dark/light theme on startup`

---

### Task 5: FileTreeViewModel
**Files:** Create `src/FileSystemP.WPF/ViewModels/FileTreeViewModel.cs`
- [ ] Inherits `ObservableObject`; `[ObservableProperty] ObservableCollection<FileTreeNode> Roots`
- [ ] Constructor: call `DriveService.GetDrives()`, create one `FileTreeNode` per drive with `ShellIconHelper.GetIcon(drive.Name, isDrive: true)`
- [ ] `[ObservableProperty] FileTreeNode? SelectedNode` — `OnSelectedNodeChanged` sets `CurrentPath` on the parent VM via injected `Action<string>`
- [ ] Commit: `feat: add FileTreeViewModel`

---

### Task 6: FilePanelViewModel
**Files:** Create `src/FileSystemP.WPF/ViewModels/FilePanelViewModel.cs`
- [ ] Constructor accepts `Action<string> navigate` callback (same pattern as `FileTreeViewModel`)
- [ ] Properties: `ObservableCollection<FileEntry> Entries`, `FileEntry? SelectedEntry`, `string? ErrorMessage`, `bool IsEmpty`, `string? ClipboardPath`
- [ ] `LoadEntries(string path)`: clears entries, calls `FileDirectorySystemService.GetEntries(path)`, maps to `FileEntry`, sets `IsEmpty`/`ErrorMessage`; catch `AppException` → set `ErrorMessage`
- [ ] Commands (all `[RelayCommand]`): `Open` — if `IsDirectory` call `navigate(SelectedEntry.Path)`, else `Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true })`; `Rename` — `InputDialog.Show`, call `Rename`; `Delete` — `MessageBox` confirm, call `Delete`; `Copy` — set `ClipboardPath`; `Paste` — call `Copy(ClipboardPath, dest)`, enabled when `ClipboardPath != null`; `NewFile` — `InputDialog.Show`, call `CreateFile`; `NewFolder` — `InputDialog.Show`, call `CreateDirectory`
- [ ] Each mutating command reloads entries after success; shows `MessageBox` with `AppException.Message` on failure
- [ ] Commit: `feat: add FilePanelViewModel with all commands`

---

### Task 7: MainWindowViewModel
**Files:** Create `src/FileSystemP.WPF/ViewModels/MainWindowViewModel.cs`
- [ ] Inherits `ObservableObject`; exposes `FileTreeViewModel Tree` and `FilePanelViewModel Panel` as properties
- [ ] `[ObservableProperty] string CurrentPath` — `OnCurrentPathChanged` calls `Panel.LoadEntries(CurrentPath)`
- [ ] Passes `path => CurrentPath = path` as the `Action<string> navigate` callback to both `FileTreeViewModel` and `FilePanelViewModel`
- [ ] Commit: `feat: add MainWindowViewModel`

---

### Task 8: InputDialog
**Files:** Create `src/FileSystemP.WPF/Views/InputDialog.xaml` + `Views/InputDialog.xaml.cs`
- [ ] Window with: `Label` (prompt), `TextBox` (pre-filled with default), OK + Cancel buttons
- [ ] Code-behind: OK sets `DialogResult = true`; `static string? Show(string prompt, string? defaultValue = null)` opens dialog and returns `TextBox.Text` or null
- [ ] Commit: `feat: add InputDialog`

---

### Task 9: MainWindow
**Files:** Modify `src/FileSystemP.WPF/MainWindow.xaml`, `MainWindow.xaml.cs`
- [ ] `MainWindow.xaml.cs`: `DataContext = new MainWindowViewModel()`
- [ ] Layout: `Grid` two columns with `GridSplitter`; left = `TreeView` bound to `Tree.Roots`, `SelectedItem` two-way to `Tree.SelectedNode`; right = `Grid` with `ListView` bound to `Panel.Entries` + overlaid `TextBlock` for empty/error
- [ ] `ListView` columns: Icon (16px `Image`), Name, Type, Size, DateModified; double-click fires `Panel.OpenCommand`
- [ ] Define three `ContextMenu` resources: file menu (Open, Rename, Copy, Delete), folder menu (Open, Rename, Copy, Delete, New File, New Folder), empty-space menu (Paste, New File, New Folder) — all `Command` bindings point to `Panel.*Command`
- [ ] Attach menus: `ListView.ItemContainerStyle` sets menu based on `IsDirectory`; `ListView` background sets empty-space menu
- [ ] Apply theme brushes (`AppBackground`, `AppForeground` etc.) to root elements
- [ ] Commit: `feat: wire up MainWindow XAML layout and bindings`
