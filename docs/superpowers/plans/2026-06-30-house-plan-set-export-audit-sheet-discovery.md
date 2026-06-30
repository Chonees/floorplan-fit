# HousePlanSet Export Audit Sheet Discovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:test-driven-development`. Keep ponytail discipline: discover existing sheets/projections; do not write dependent DXFs or create another export engine.

**Goal:** Let `CreateMultiSheetExportAuditHandler` build a package audit from the PlanSet itself when no explicit projection ids are supplied, including dependent sheets that have no projection as manual/missing.

**Architecture:** Reuse `IPlanSheetReader` to list dependent sheets and extend `ISheetAdjustmentProjectionRepository` with a single read method for projections by plan-set version and canonical adjustment. The handler still supports explicit projection ids, but an empty projection request now means "audit every dependent sheet in this plan set".

**Tech Stack:** C#/.NET 10, existing Application/Domain/Contracts/Infrastructure projects, SQLite persistence, xUnit-style tests. No `dotnet build` after changes.

---

## Scope Boundary

In scope:

- auto-discover dependent sheets for a plan-set export audit
- list projections by plan-set version and canonical adjustment
- mark sheets without a projection as `MissingProjection`
- count missing projections as manual-confirmation blockers
- keep explicit projection id behavior working

Out of scope:

- dependent-sheet DXF rewriting
- UI commands
- discovering/registering sheets automatically
- generating missing projections automatically
- changing existing canonical floor-plan export behavior

## Files

Modify:

- `src/FloorplanFit.Application/Abstractions/ISheetAdjustmentProjectionRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetAdjustmentProjectionRepository.cs`
- `src/FloorplanFit.Domain/PlanSets/PlanSetExportedSheetStatus.cs`
- `src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs` only if constructor dependency wiring needs explicit change
- `tests/FloorplanFit.Application.Tests/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandlerTests.cs`
- projection handler test fakes that implement `ISheetAdjustmentProjectionRepository`
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

---

## Tasks

### Task 1: RED test for auto-discovery

- [ ] Add a test where `CreateMultiSheetExportAuditRequest.DependentProjections` is empty.
- [ ] Fake `IPlanSheetReader` returns two dependent sheets.
- [ ] Fake projection repository returns one projection for one sheet.
- [ ] Assert output has canonical + projected sheet + missing-projection sheet.
- [ ] Assert missing sheet status is `MissingProjection`, manual count is `1`, and package status is `RequiresManualConfirmation`.

RED evidence without compiling:

```powershell
Select-String src\FloorplanFit.Domain\PlanSets\PlanSetExportedSheetStatus.cs -Pattern MissingProjection
Select-String src\FloorplanFit.Application\Abstractions\ISheetAdjustmentProjectionRepository.cs -Pattern ListByPlanSetVersionAndCanonicalAdjustmentAsync
```

Expected: no matches.

### Task 2: Minimal implementation

- [ ] Add `MissingProjection = 4` to `PlanSetExportedSheetStatus`.
- [ ] Add repository method:

```csharp
Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
    Guid planSetVersionId,
    Guid canonicalAdjustmentId,
    CancellationToken cancellationToken);
```

- [ ] Implement it in SQLite with one `SELECT` filtered by `plan_set_version_id` and `canonical_adjustment_id`.
- [ ] Inject `IPlanSheetReader` into `CreateMultiSheetExportAuditHandler`.
- [ ] If explicit projections are supplied, keep existing behavior.
- [ ] If explicit projections are empty, list dependent sheets and projections, choose the latest projection per sheet, and add `MissingProjection` rows for sheets without one.
- [ ] Update summary logic so `MissingProjection` counts as manual-confirmation required.

### Task 3: Verify and commit

```powershell
git diff --check -- src/FloorplanFit.Application/Abstractions src/FloorplanFit.Application/PlanSets/ExportAudit src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Infrastructure/Persistence tests/FloorplanFit.Application.Tests/PlanSets docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md
```

Commit plan, code, and bridge docs separately. Do not run `dotnet build`.

## Completion Checklist

- [ ] Empty dependent projection request audits every dependent sheet in the PlanSet.
- [ ] Missing projection is visible and blocks automatic package export.
- [ ] Existing explicit projection id behavior remains supported.
- [ ] No fit/registration/projection recalculation is added.
- [ ] No dependent DXF export is added in this slice.
- [ ] No `dotnet build` command is run.
