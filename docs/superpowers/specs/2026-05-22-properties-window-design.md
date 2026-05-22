# Design Spec - Properties Window with NTFS Metadata

This document outlines the design for integrating NTFS metadata properties into the FileSystemP WPF application.

## 1. Purpose
Provide users with a detailed "Properties" view for files and directories, similar to Windows Explorer. This feature will leverage the `NtfsMetadataProvider` and provide a foundation for future security and permissions management.

## 2. Success Criteria
- Users can right-click any file or directory in the file panel and select "Properties".
- A new window opens with a tabbed interface.
- The "General" tab displays all information from `NtfsMetadataRecord` or `DirectoryNtfsMetadataRecord`.
- The layout is clean, professional, and consistent with the existing WPF theme.
- The window is non-blocking (user can still interact with the main window if desired, or we can make it modal if preferred - we'll start with a standard Window).

## 3. Architecture

### 3.1 Components
- **`PropertiesWindow.xaml`**: The view containing a `TabControl`.
- **`PropertiesViewModel.cs`**: The ViewModel managing the data for the properties window.
- **`NtfsMetadataProvider`**: Used to fetch the detailed metadata from the core.
- **`FilePanelViewModel`**: Updated to include a `ShowPropertiesCommand`.

### 3.2 Data Mapping (General Tab)
- **File Metadata:**
    - Name
    - Type (from `Extension`)
    - Location (`FullPath`)
    - Size (formatted string)
    - Created, Modified, Accessed dates
    - Attributes (Read-only, Hidden, etc.)
- **Directory Metadata:**
    - Name
    - Type ("File Folder")
    - Location (`FullPath`)
    - Parent Path
    - Root Path
    - Created, Modified, Accessed dates
    - Attributes

## 4. Implementation Strategy

### 4.1 UI Layout
- Use a `Grid` within the "General" tab for alignment.
- Labels on the left, read-only values (or non-editable TextBoxes for selection) on the right.
- Visual separation between top (identity), middle (location/size), and bottom (timestamps/attributes) sections.

### 4.2 Error Handling
- If `NtfsMetadataProvider` throws an `AppException`, display a user-friendly error message in a `MessageBox` or within the Properties window itself if it fails after opening.

### 4.3 Future Extensibility
- The `TabControl` will allow easy addition of "Security", "Details", and "Previous Versions" tabs in future iterations.

## 5. Testing Plan
- Manual verification that the data in the Properties window matches the file system state.
- Verify that both files and directories are handled correctly.
- Verify that the window opens and closes without crashing.
