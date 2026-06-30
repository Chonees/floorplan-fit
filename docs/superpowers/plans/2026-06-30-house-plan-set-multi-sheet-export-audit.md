# HousePlanSet Multi-Sheet Export Audit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:test-driven-development`. Keep ponytail discipline: do not build a second exporter or per-sheet fit engine. This phase creates the audit package boundary first.

**Goal:** Implement Phase 7's first useful slice: create a persisted multi-sheet export audit that includes the canonical floor plan plus projected dependent sheets, reporting automatic vs manual status, confidence, warnings, and a quality event.

**Architecture:** Add a small `CreateMultiSheetExportAuditHandler` under `Application/PlanSets/ExportAudit`. It consumes existing `SheetAdjustmentProjection` records and persists an export audit plus exported-sheet rows. It records one `audit_events` quality event but does not let telemetry failure block the export audit.

**Tech Stack:** C#/.NET 10, existing layered monolith, SQLite persistence, xUnit-style tests. No `dotnet build` after changes.

---

## Scope Boundary

In scope:

- `plan_set_exports` audit run persistence
- `plan_set_exported_sheets` per-sheet audit persistence
- `audit_events` first DataCollection sink
- canonical floor-plan export row
- dependent projection rows with status/confidence/warning/rule summary
- one application handler and focused tests

Out of scope:

- writing dependent-sheet DXF files
- recalculating fit or registration
- selecting missing projections automatically
- Desktop UI
- new telemetry subsystem beyond `audit_events`

## Files

Create:

- `src/FloorplanFit.Contracts/PlanSets/CreateMultiSheetExportAuditRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/MultiSheetExportProjectionRequestDto.cs`
- `src/FloorplanFit.Contracts/PlanSets/MultiSheetExportAuditDto.cs`
- `src/FloorplanFit.Contracts/PlanSets/ExportedPlanSheetDto.cs`
- `src/FloorplanFit.Contracts/PlanSets/ProjectionAuditSummaryDto.cs`
- `src/FloorplanFit.Domain/PlanSets/PlanSetExport.cs`
- `src/FloorplanFit.Domain/PlanSets/PlanSetExportStatus.cs`
- `src/FloorplanFit.Domain/PlanSets/PlanSetExportedSheet.cs`
- `src/FloorplanFit.Domain/PlanSets/PlanSetExportedSheetStatus.cs`
- `src/FloorplanFit.Domain/PlanSets/PlanSetAuditEvent.cs`
- `src/FloorplanFit.Application/Abstractions/IPlanSetExportRepository.cs`
- `src/FloorplanFit.Application/Abstractions/IPlanSetAuditEventRepository.cs`
- `src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSetExportRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSetAuditEventRepository.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandlerTests.cs`

Modify:

- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

---

## Tasks

### Task 1: RED tests

- [ ] Add handler tests proving:
  1. The audit includes the canonical floor-plan sheet.
  2. `ReadyForExport` dependent projections are marked `ProjectedAutomatically`.
  3. `RequiresManualConfirmation` projections stay in the package audit as manual, not silently trusted.
  4. Summary counts total/automatic/manual and lowest confidence.
  5. A `PlanSetExport` audit and `PlanSetExport` quality event are persisted.
  6. Telemetry failure does not block export audit persistence.

- [ ] Verify RED without compiling:

```powershell
Test-Path src\FloorplanFit.Application\PlanSets\ExportAudit\CreateMultiSheetExportAuditHandler.cs
Test-Path src\FloorplanFit.Domain\PlanSets\PlanSetExport.cs
```

Expected:

```text
False
False
```

### Task 2: Contracts/domain/application

- [ ] Add DTOs listed above.
- [ ] Add domain export/audit event entities.
- [ ] Add repository ports.
- [ ] Add `CreateMultiSheetExportAuditHandler`.

Rules:

- Validate `PlanSetVersionId`, `CanonicalFloorPlanVersionId`, `CanonicalAdjustmentId`, and canonical export path.
- Fetch each requested dependent projection by id.
- Reject projection ids from another plan set or canonical adjustment.
- Do not recalculate registration, projection, or fit.
- Summary status is `ReadyForExport` only if no dependent projection requires manual confirmation.
- Catch `IPlanSetAuditEventRepository.AddAsync` failures and continue saving the export audit.

### Task 3: SQLite/DI/docs

- [ ] Add SQLite tables:
  - `plan_set_exports`
  - `plan_set_exported_sheets`
  - `audit_events`
- [ ] Add `Ensure*Schema` methods using `EnsureColumnExists` for existing DBs.
- [ ] Wire repositories and handler in Desktop DI.
- [ ] Add Phase 7 bridge to the HousePlanSet spec.

### Task 4: Verify and commit

```powershell
git diff --check -- src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Application/Abstractions src/FloorplanFit.Application/PlanSets/ExportAudit src/FloorplanFit.Infrastructure/Persistence src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/PlanSets/ExportAudit docs/superpowers
```

Commit plan, code, and bridge docs separately. Do not run `dotnet build`.

## Completion Checklist

- [ ] Audit includes canonical floor sheet.
- [ ] Audit reports automatic vs manual dependent sheets.
- [ ] Audit includes confidence, warnings, method, and rule summary per dependent sheet.
- [ ] Audit event is recorded when possible.
- [ ] Telemetry failure does not block export audit creation.
- [ ] No fit/export recalculation is introduced.
- [ ] No `dotnet build` command is run.
