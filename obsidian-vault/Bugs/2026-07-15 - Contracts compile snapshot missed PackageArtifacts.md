---
type: bug
date: 2026-07-15
status: fixed-awaiting-external-build
---

# Contracts compile snapshot missed PackageArtifacts

## Symptom

Application reported `CS1061/CS0117` saying `CreateMultiSheetExportAuditRequest.PackageArtifacts` did not exist even though the source property was present.

## Root cause

The Contracts output and reference assemblies contained the newly added artifact DTO but not the request property. The source was written while the earlier compile was already in progress; the resulting DLL timestamp was newer than the changed source, so incremental `dotnet watch` reused the incomplete compile snapshot.

## Fix

Touched `CreateMultiSheetExportAuditRequest.cs` without changing its content so `dotnet watch` must recompile Contracts from the complete current source.

## Verification

Awaiting the user's external `dotnet watch` rebuild; repository rules prohibit agent-run builds.
