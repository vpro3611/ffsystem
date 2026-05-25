# Security Metadata Cleanup and CA1416 Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Commit missed files and fix CA1416 platform warnings in SecurityMetadataProvider.

**Architecture:** Add `[SupportedOSPlatform("windows")]` to `SecurityMetadataProvider` class to resolve platform-specific API warnings. Use standard git commands for staging and committing.

**Tech Stack:** .NET 8, C#, Git

---

### Task 1: Fix CA1416 Warnings in SecurityMetadataProvider

**Files:**
- Modify: `src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProvider.cs`

- [ ] **Step 1: Add using directive and attribute**
Add `using System.Runtime.Versioning;` and `[SupportedOSPlatform("windows")]` to the `SecurityMetadataProvider` class.

- [ ] **Step 2: Verify build and warnings**
Run `dotnet build src/FileSystemP.Core/FileSystemP.Core.csproj` and ensure no CA1416 warnings are present for this file.

### Task 2: Verify and Commit Missed Files

**Files:**
- Commit: `src/FileSystemP.Core/FileSystemP.Core.csproj`
- Commit: `docs/superpowers/plans/2026-05-25-security-metadata-refinement-plan.md`

- [ ] **Step 1: Stage missed files**
Run `git add src/FileSystemP.Core/FileSystemP.Core.csproj docs/superpowers/plans/2026-05-25-security-metadata-refinement-plan.md`

- [ ] **Step 2: Commit missed files**
Run `git commit -m "chore: commit missed project and plan files from previous task"`

### Task 3: Verify and Commit Fixes

**Files:**
- Commit: `src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProvider.cs`

- [ ] **Step 1: Run tests**
Run `dotnet test tests/FileSystemP.Tests/FileSystemP.Tests.csproj` to ensure no regressions.

- [ ] **Step 2: Stage and commit fix**
Run `git add src/FileSystemP.Core/MetadataService/Providers/SecurityAndACL/SecurityMetadataProvider.cs`
Run `git commit -m "fix: resolve CA1416 platform warnings in SecurityMetadataProvider"`
