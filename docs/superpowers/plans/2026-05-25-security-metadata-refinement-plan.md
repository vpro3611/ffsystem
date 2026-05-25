# Security Metadata Refinement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refine the `SecurityMetadataProvider` implementation to improve error handling, code cleanliness, and robustness while maintaining its core Windows-specific ACL retrieval logic.

**Architecture:** Use `FileSystemInfo` to handle files and directories uniformly and distinguish between platform and path existence errors. Tests will use temporary local filesystem resources.

**Tech Stack:** .NET 9, xUnit, System.IO.FileSystem.AccessControl

---

### Task 1: Success Scenarios (File and Directory)

**Files:**
- Modify: `src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProvider.cs`
- Test: `tests/FileSystemP.Tests/SecurityMetadataProviderTests.cs`

- [ ] **Step 1: Write the failing test for file metadata**

```csharp
using FileSystemP.Core.MetadataService.Providers.SecurityAndACL;
using Xunit;

namespace FileSystemP.Tests;

public class SecurityMetadataProviderTests
{
    private readonly SecurityMetadataProvider _provider = new();

    [Fact]
    public void GetSecurityMetadata_FileExists_ReturnsMetadata()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            var metadata = _provider.GetSecurityMetadata(tempFile);
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Owner);
            Assert.NotEmpty(metadata.Permissions);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails (or passes if existing code works)**

Run: `dotnet test --filter "FullyQualifiedName~SecurityMetadataProviderTests"`
Expected: PASS (as existing code is preserved)

- [ ] **Step 3: Write the failing test for directory metadata**

```csharp
    [Fact]
    public void GetSecurityMetadata_DirectoryExists_ReturnsMetadata()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var metadata = _provider.GetSecurityMetadata(tempDir);
            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Owner);
            Assert.NotEmpty(metadata.Permissions);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir);
        }
    }
```

- [ ] **Step 4: Run tests and make sure they pass**

Run: `dotnet test --filter "FullyQualifiedName~SecurityMetadataProviderTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tests/FileSystemP.Tests/SecurityMetadataProviderTests.cs src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProvider.cs src/FileSystemP.Core/MetadataService/DTO/PermissionEntryRecord.cs src/FileSystemP.Core/MetadataService/DTO/SecurityMetadataRecord.cs src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProviderInterface.cs
git commit -m "test: add success tests for SecurityMetadataProvider"
```

---

### Task 2: Error Scenarios (Path Not Found)

**Files:**
- Modify: `src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProvider.cs`
- Test: `tests/FileSystemP.Tests/SecurityMetadataProviderTests.cs`

- [ ] **Step 1: Write the failing test for non-existent path**

```csharp
    [Fact]
    public void GetSecurityMetadata_PathDoesNotExist_ThrowsAppExceptionWithCorrectMessage()
    {
        string nonExistentPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        
        var exception = Assert.Throws<AppException>(() => _provider.GetSecurityMetadata(nonExistentPath));
        Assert.Contains("Path not found", exception.Message);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~SecurityMetadataProviderTests"`
Expected: FAIL (Existing code throws "Unsupported platform." or similar if path check fails in a specific way)

- [ ] **Step 3: Implement minimal code to make the test pass**

```csharp
    private static FileSystemSecurity GetSecurity(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new AppException("Unsupported platform.", $"{_classNameStatic}.{nameof(GetSecurity)}()");
        }

        if (Directory.Exists(path))
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            return dir.GetAccessControl();
        }
        if (File.Exists(path))
        {
            FileInfo file = new FileInfo(path);
            return file.GetAccessControl();
        }

        throw new AppException($"Path not found: {path}", $"{_classNameStatic}.{nameof(GetSecurity)}()");
    }
```

- [ ] **Step 4: Run tests and make sure they pass**

Run: `dotnet test --filter "FullyQualifiedName~SecurityMetadataProviderTests"`
Expected: ALL PASS

- [ ] **Step 5: Commit**

```bash
git add src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProvider.cs tests/FileSystemP.Tests/SecurityMetadataProviderTests.cs
git commit -m "feat: improve error handling for non-existent paths"
```

---

### Task 3: Refactoring and Cleanup

**Files:**
- Modify: `src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProvider.cs`

- [ ] **Step 1: Refactor GetSecurity and remove unused fields**

```csharp
public class SecurityMetadataProvider : ISecurityMetadataProvider
{
    private static FileSystemSecurity GetSecurity(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new AppException("Unsupported platform.", $"{nameof(SecurityMetadataProvider)}.{nameof(GetSecurity)}()");
        }

        FileSystemInfo info = Directory.Exists(path) 
            ? new DirectoryInfo(path) 
            : new FileInfo(path);

        if (!info.Exists)
        {
             throw new AppException($"Path not found: {path}", $"{nameof(SecurityMetadataProvider)}.{nameof(GetSecurity)}()");
        }

        return info switch
        {
            DirectoryInfo d => d.GetAccessControl(),
            FileInfo f => f.GetAccessControl(),
            _ => throw new AppException($"Unsupported file system object: {path}")
        };
    }

    public SecurityMetadataRecord GetSecurityMetadata(string path)
    {
        // ... (rest of method remains same, but using cleaned up GetSecurity)
    }
}
```

- [ ] **Step 2: Run tests and make sure they pass**

Run: `dotnet test --filter "FullyQualifiedName~SecurityMetadataProviderTests"`
Expected: ALL PASS

- [ ] **Step 3: Commit**

```bash
git add src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProvider.cs
git commit -m "refactor: use FileSystemInfo and clean up SecurityMetadataProvider"
```
