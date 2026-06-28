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
- terminal presentation and detached terminal window hosting

The WPF project follows a practical MVVM style. Most behavior lives in view models, while the code-behind is limited to dialog orchestration and view-specific events.

### `tests/FileSystemP.Tests`

Contains:

- service-level tests for core behaviors
- metadata provider tests
- search tests
- attribute behavior tests
- parser tests
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

### Add or extend a terminal command

1. Add the command enum entry in `src/FileSystemP.Core/CommandService/AvailableCommands.cs`.
2. Register the command name in `src/FileSystemP.Core/CommandService/Parser.cs`.
3. Add or update argument validation in `CheckMinLengthForEachCommand`.
4. Implement the command execution branch in `Parser`.
5. Return a `CommandResult` and payload shape that the UI can interpret.
6. If the command requires new UI behavior, handle the new `CommandResult` signal in `CommandPaletteViewModel`.
7. Add parser tests and, if needed, command-palette view-model tests.

### Extend terminal input syntax

The terminal command line is tokenized in `src/FileSystemP.WPF/ViewModels/CommandPaletteViewModel.cs` before anything reaches `Parser`.

Use that tokenizer for input-level features such as:

- grouped arguments
- friendly unmatched-quote style errors
- future escaping or alternative delimiter rules

Do not add input-tokenization logic to the core parser unless the parser contract itself changes.

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

### Why terminal parsing is split across UI and core

The WPF layer owns user-input tokenization because it is responsible for terminal-specific syntax, grouped arguments, and friendly input errors. The core parser owns command semantics once those tokens are already well-formed. This split keeps `Parser` reusable and keeps terminal UX concerns out of the core project.

## Testing Coverage Summary

The current suite verifies:

- `AppException` behavior
- file and directory service behavior
- drive enumeration
- search filtering and cancellation
- command parsing, help, `ls`, and `find` behavior
- NTFS metadata extraction
- shell metadata lookup
- attribute behavior, including recursive cases
- security metadata retrieval and modification
- properties, permission-editor, terminal-command, and detached-terminal view-model behavior

## Known Limitations

- Search collects results into memory before the UI consumes them.
- Security editing is Windows-specific and assumes Windows account resolution.
- Compression behavior depends on Windows NTFS support and may not be available on all volumes.
- Directory metadata size calculation can be expensive on large trees because it recursively enumerates all files.
- The terminal tokenizer currently supports grouped arguments with backticks rather than shell-style escaping or nested quoting.

## Recommended Next Documentation Additions

- release and packaging instructions
- screenshot-based end-user manual if the project will be distributed outside the repository
- troubleshooting guidance for ACL failures, inaccessible paths, and non-NTFS volumes
