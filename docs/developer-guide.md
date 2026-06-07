# Developer Guide

## Repository Layout

```text
FileSystemP.sln
src/
  FileSystemP.Core/
  FileSystemP.WPF/
tests/
  FileSystemP.Tests/
docs/
```

## Project Roles

### `src/FileSystemP.Core`

Contains the application logic that is independent of WPF presentation:

- file and directory operations
- drive discovery
- recursive search
- NTFS metadata access
- shell metadata access
- Windows security metadata and mutation
- attribute editing services

### `src/FileSystemP.WPF`

Contains:

- XAML views
- view models
- shell icon helpers
- theme resources
- dialog composition and event glue

The WPF project follows a practical MVVM style. Most behavior lives in view models, while the code-behind is limited to dialog orchestration and view-specific events.

### `tests/FileSystemP.Tests`

Contains:

- service-level tests for core behaviors
- metadata provider tests
- search tests
- attribute behavior tests
- selected view-model tests

## Dependencies

### Core project

- `Microsoft-WindowsAPICodePack-Shell`
- `System.IO.FileSystem.AccessControl`

### WPF project

- `CommunityToolkit.Mvvm`
- `Tulpep.ActiveDirectoryObjectPicker`

### Test project

- `xUnit`
- `Moq`
- `Microsoft.NET.Test.Sdk`
- `coverlet.collector`

## Extension Points

### Add a new editable attribute

1. Add a service in `src/FileSystemP.Core/AttributeService`.
2. Expose and persist the state in `PropertiesViewModel`.
3. Update the properties and advanced-attributes views if the attribute should be user-editable.
4. Add tests in `tests/FileSystemP.Tests`.

### Add a new metadata source

1. Create a DTO if the shape is new.
2. Add a provider under `MetadataService/Providers`.
3. Decide whether it belongs in the Properties dialog or another UI workflow.
4. Add provider tests and any required UI-facing tests.

### Extend search

1. Add the filter to `ExtendedOptions`.
2. Implement matching logic in `SearchService`.
3. Bind the new filter in `SearchViewModel`.
4. Add the corresponding controls in `MainWindow.xaml`.
5. Add search tests for the new behavior.

## Build and Test Commands

### Build solution

```powershell
dotnet build .\FileSystemP.sln
```

### Run WPF application

```powershell
dotnet run --project .\src\FileSystemP.WPF\FileSystemP.WPF.csproj
```

### Run tests

```powershell
dotnet test .\tests\FileSystemP.Tests\FileSystemP.Tests.csproj
```

## Design Decisions

### Why a separate core library

The separation between `FileSystemP.WPF` and `FileSystemP.Core` keeps file-system and metadata logic out of the UI layer, which improves testability and reduces coupling to WPF.

### Why custom `AppException`

`AppException` provides a consistent way to:

- preserve a user-facing message
- record the logical source of the error
- wrap lower-level exceptions where needed

### Why transactions for security changes

ACL editing is easier to reason about when the UI produces a declarative description of what changed and the core service applies that delta to the target object.

## Testing Coverage Summary

The current suite verifies:

- `AppException` behavior
- file and directory service behavior
- drive enumeration
- search filtering and cancellation
- NTFS metadata extraction
- shell metadata lookup
- attribute behavior, including recursive cases
- security metadata retrieval and modification
- properties and permission-editor view-model behavior

## Known Limitations

- Search collects results into memory before the UI consumes them.
- Security editing is Windows-specific and assumes Windows account resolution.
- Compression behavior depends on Windows NTFS support and may not be available on all volumes.
- Directory metadata size calculation can be expensive on large trees because it recursively enumerates all files.

## Recommended Next Documentation Additions

- release and packaging instructions
- screenshot-based end-user manual if the project will be distributed outside the repository
- troubleshooting guidance for ACL failures, inaccessible paths, and non-NTFS volumes
