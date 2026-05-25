# Design Spec: Security Metadata Refinement

## Goal
Refine the `SecurityMetadataProvider` implementation to improve error handling, code cleanliness, and robustness while maintaining its core Windows-specific ACL retrieval logic.

## Architecture & Components

### `SecurityMetadataProvider` (Refinement)
- **Target Platform:** Windows only.
- **Improved logic:**
    - Explicitly check `OperatingSystem.IsWindows()` at the start of operations.
    - Use `FileSystemInfo` to handle both files and directories uniformly in `GetSecurity`.
    - Distinguish between "Platform not supported" and "Path not found" errors.
    - Remove unused private fields (`_className`, `_classNameStatic`).

### `SecurityMetadataRecord` & `PermissionEntryRecord` (DTOs)
- No changes required as they correctly represent the metadata structure.

## Testing Strategy (Approach A: Real-system Integration)
- **Test Framework:** xUnit (verified in `FileSystemP.Tests.csproj`).
- **Isolation:** Each test will create its own temporary file or directory using `Path.GetTempFileName()` or `Directory.CreateTempSubdirectory()`.
- **Cleanup:** Tests will delete temporary resources in a `finally` block or via `IDisposable`.
- **Coverage:**
    - Successful metadata retrieval for a file.
    - Successful metadata retrieval for a directory.
    - Correct error when the path does not exist.
    - Correct error when run on a non-Windows platform (if applicable/testable).

## Implementation Plan (TDD)
1.  **Red:** Write a test that expects `SecurityMetadataRecord` for a temporary file.
2.  **Green:** Ensure `SecurityMetadataProvider` passes the test (preserving existing logic).
3.  **Red:** Write a test for a non-existent path.
4.  **Green:** Update `SecurityMetadataProvider` to throw a specific `AppException` for "Path not found".
5.  **Refactor:** Consolidate `GetSecurity` logic using `FileSystemInfo` and remove unused fields.
6.  **Verify:** Ensure all tests pass.
