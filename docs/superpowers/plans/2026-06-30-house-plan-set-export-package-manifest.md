# HousePlanSet Export Package Manifest Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:test-driven-development`. Ponytail rule: write one manifest file; do not build zip/package infrastructure or dependent-sheet DXF rewriting yet.

**Goal:** Make Phase 7 produce a physical package manifest artifact for each multi-sheet export audit.

**Architecture:** Extend `CreateMultiSheetExportAuditHandler` with a tiny `IPlanSetExportManifestWriter` port. The handler writes the existing audit DTO as JSON, persists the manifest path on `plan_set_exports`, and returns it to callers. This turns the package audit into a concrete exported artifact without inventing per-sheet exporters.

**Tech Stack:** C#/.NET 10, existing Application/Domain/Contracts/Infrastructure projects, System.Text.Json, file system via existing `AppWorkspace`. No `dotnet build` after changes.

---

## Scope Boundary

In scope:

- package manifest path on `PlanSetExport` and `MultiSheetExportAuditDto`
- `IPlanSetExportManifestWriter`
- file-system manifest writer under workspace export directory
- SQLite `package_manifest_path` column
- tests proving manifest writer is called and path is persisted/returned

Out of scope:

- zip archives
- copying dependent DXFs
- rewriting dependent DXFs
- UI button
- manifest versioning beyond the current DTO shape

## Files

Create:

- `src/FloorplanFit.Application/Abstractions/IPlanSetExportManifestWriter.cs`
- `src/FloorplanFit.Infrastructure/Storage/PlanSetExportManifestWriter.cs`

Modify:

- `src/FloorplanFit.Contracts/PlanSets/MultiSheetExportAuditDto.cs`
- `src/FloorplanFit.Domain/PlanSets/PlanSetExport.cs`
- `src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSetExportRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandlerTests.cs`
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

---

## Tasks

### Task 1: RED tests

- [ ] Update export audit tests with a fake `IPlanSetExportManifestWriter`.
- [ ] Assert `response.PackageManifestPath == "exports/package/manifest.json"`.
- [ ] Assert saved `PlanSetExport.PackageManifestPath` matches.
- [ ] Assert fake writer received the audit DTO containing canonical + dependent sheet statuses.

RED evidence without compiling:

```powershell
Test-Path src\FloorplanFit.Application\Abstractions\IPlanSetExportManifestWriter.cs
Select-String src\FloorplanFit.Contracts\PlanSets\MultiSheetExportAuditDto.cs -Pattern PackageManifestPath
```

Expected: `False` and no matches.

### Task 2: Minimal implementation

- [ ] Add nullable `PackageManifestPath` to `MultiSheetExportAuditDto`.
- [ ] Add nullable `PackageManifestPath` to `PlanSetExport`.
- [ ] Add `IPlanSetExportManifestWriter.WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken)`.
- [ ] Handler builds audit DTO, calls writer, then persists `PlanSetExport` with returned path.
- [ ] SQLite repository writes `package_manifest_path`.
- [ ] Schema initializer creates/ensures `package_manifest_path TEXT NULL`.
- [ ] Infrastructure writer writes JSON to `workspace.RootPath/exports/plan-sets/{ExportId}/manifest.json`.

### Task 3: Verify and commit

```powershell
git diff --check -- src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Application/Abstractions src/FloorplanFit.Application/PlanSets/ExportAudit src/FloorplanFit.Infrastructure src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/PlanSets/ExportAudit docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md
```

Commit plan, code, and bridge docs separately. Do not run `dotnet build`.

## Completion Checklist

- [ ] Every export audit returns a manifest path.
- [ ] Manifest path is persisted on `plan_set_exports`.
- [ ] Manifest writer writes one JSON file.
- [ ] Existing automatic/manual/missing sheet audit remains intact.
- [ ] No dependent DXF rewriting is added.
- [ ] No `dotnet build` command is run.
