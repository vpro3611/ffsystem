# Attribute behavior

This project currently supports four editable NTFS-oriented attributes in core:

- `Archive`
- `Compressed`
- `Hidden`
- `NotContentIndexed`
- `ReadOnly`

The behavior is intentionally different for files and directories.

## Archive

- Files: `SetArchive(path)` adds the `Archive` flag, `UnsetArchive(path)` removes it.
- Directories: the same rule applies to the directory entry itself.
- Metadata: `NtfsMetadataProvider` reports the current archive state through `Attributes`.

## Compressed

- Files: `CompressFile(path)` and `DecompressFile(path)` perform a real NTFS compression operation through the Windows API, not just a raw attribute-bit toggle.
- Directories: `CompressDirectory(path, recursive)` compresses the target directory itself and, when `recursive: true`, applies compression to all descendant subdirectories and files.
- `DecompressDirectory(path, recursive)` reverses that process. In recursive mode it decompresses files first, then child directories, then the target directory.
- Metadata: `NtfsMetadataProvider` reports the resulting compressed state through `Attributes`.
- Platform note: compression support is Windows/NTFS-specific.

## Hidden

- Files: `SetHidden(path)` adds the `Hidden` flag, `UnsetHidden(path)` removes it.
- Directories: the same rule applies to the directory entry itself.
- Metadata: `NtfsMetadataProvider` reports the current hidden state through `Attributes`.

## NotContentIndexed

- Files: `SetNotContentIndexed(path)` adds the `NotContentIndexed` flag, `UnsetNotContentIndexed(path)` removes it.
- Directories: the same rule applies to the directory entry itself.
- Metadata: `NtfsMetadataProvider` reports the current indexing-exclusion state through `Attributes`.

## ReadOnly

- Files: `SetReadonlyFile(path)` adds the `ReadOnly` flag, `UnsetReadonlyFile(path)` removes it.
- Directories: `SetReadonlyDir(path, recursive)` always marks the target directory itself as `ReadOnly` so metadata and UI can reflect that state.

Directory `ReadOnly` also affects child items with these rules:

- `recursive: false`
  - marks the target directory as `ReadOnly`
  - marks files directly inside that directory as `ReadOnly`
  - does not touch subdirectories
  - does not touch files inside subdirectories
- `recursive: true`
  - marks the target directory as `ReadOnly`
  - marks all descendant subdirectories as `ReadOnly`
  - marks all files in the whole subtree as `ReadOnly`

`UnsetReadonlyDir(path, recursive)` follows the same scope rules in reverse and clears the corresponding `ReadOnly` flags.

## Error behavior

- Archive operations throw `AppException` when the path does not exist.
- Compression operations throw `AppException` when the target path is invalid or when Windows cannot complete the NTFS compression request.
- Hidden operations throw `AppException` when the path does not exist.
- NotContentIndexed operations throw `AppException` when the path does not exist.
- File read-only operations throw `AppException` when the path does not exist or is not a file.
- Directory read-only operations throw `AppException` when the path does not exist or is not a directory.

## Test coverage

The test suite covers:

- archive set/unset for files and directories
- hidden set/unset for files and directories
- not-content-indexed set/unset for files and directories
- read-only set/unset for files
- directory read-only behavior for non-recursive and recursive modes
- metadata visibility of the resulting attribute flags
- exception behavior for invalid targets
