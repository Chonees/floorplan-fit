# HousePlanSet Electrical Projection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Phase 4's first useful slice: project an approved canonical floor-plan placement onto a registered electrical sheet and persist the projection result.

**Architecture:** Reuse the existing canonical `AdjustedSitePlanPlacementDto`; do not search for a new fit. The projection composes the electrical sheet registration transform with the canonical floor-to-site affine placement, records confidence/status/warnings, and marks export eligibility through status only. Compression steps are counted and force review in this first slice rather than silently pretending a piecewise adjustment is fully export-ready.

**Tech Stack:** C#/.NET 10, existing Application/Domain/Contracts/Infrastructure projects, SQLite via `Microsoft.Data.Sqlite`, xUnit-style tests. No `dotnet build` after changes.

---

## Scope Boundary

In scope:

- electrical projection domain record
- request/response DTOs
- projection handler that consumes `AdjustedSitePlanPlacementDto`
- SQLite persistence for projection records
- DI wiring
- tests proving canonical placement is consumed and pending/complex projections are not export-ready

Out of scope:

- actual electrical DXF export
- geometry rewriting
- independent electrical fit search
- roof/facade projection
- Desktop UI behavior

## Files

Create:

- `src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionMethod.cs`
- `src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionStatus.cs`
- `src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionTransform.cs`
- `src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjection.cs`
- `src/FloorplanFit.Contracts/PlanSets/ProjectElectricalSheetAdjustmentRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/SheetAdjustmentProjectionDto.cs`
- `src/FloorplanFit.Contracts/PlanSets/SheetAdjustmentProjectionTransformDto.cs`
- `src/FloorplanFit.Application/Abstractions/ISheetAdjustmentProjectionRepository.cs`
- `src/FloorplanFit.Application/PlanSets/Projection/ProjectElectricalSheetAdjustmentHandler.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetAdjustmentProjectionRepository.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Projection/ProjectElectricalSheetAdjustmentHandlerTests.cs`

Modify:

- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

---

### Task 1: RED tests

**Files:**
- Create: `tests/FloorplanFit.Application.Tests/PlanSets/Projection/ProjectElectricalSheetAdjustmentHandlerTests.cs`

- [ ] **Step 1: Create test folder**

```powershell
New-Item -ItemType Directory -Force tests\FloorplanFit.Application.Tests\PlanSets\Projection
```

- [ ] **Step 2: Add two tests**

Tests must prove:

1. A confirmed high-confidence electrical registration plus a simple canonical placement produces `ReadyForExport` and a composed dependent-to-site transform.
2. A pending registration produces `RequiresManualConfirmation` and is not export-ready.

- [ ] **Step 3: Verify RED without compiling**

```powershell
Test-Path src\FloorplanFit.Application\PlanSets\Projection\ProjectElectricalSheetAdjustmentHandler.cs
Test-Path src\FloorplanFit.Application\Abstractions\ISheetAdjustmentProjectionRepository.cs
```

Expected:

```text
False
False
```

Do not run `dotnet build`.

### Task 2: Domain/contracts/application

**Files:**
- Create all domain, contract, port, and handler files listed above.

- [ ] **Step 1: Add projection domain**

Use these rules:

- `SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity = 1`.
- `SheetAdjustmentProjectionStatus.ReadyForExport = 1`.
- `SheetAdjustmentProjectionStatus.RequiresManualConfirmation = 2`.
- Transform scale must be greater than zero.
- Confidence must be between `0m` and `1m`.
- Compression step count must be zero or greater.

- [ ] **Step 2: Add handler**

Handler flow:

1. Load registration with `ISheetRegistrationRepository.GetByIdAsync`.
2. Reject missing registration.
3. Compose transform from registration and canonical placement:
   - `scale = registration.Transform.Scale * placement.FloorToSiteScale`
   - `rotation = registration.Transform.RotationDegrees`
   - `translateX = registration.Transform.TranslateX * placement.FloorToSiteScale + placement.SiteOffsetX`
   - `translateY = registration.Transform.TranslateY * placement.FloorToSiteScale + placement.SiteOffsetY`
4. Mark `ReadyForExport` only when registration is confirmed, confidence is at least `0.8m`, and canonical compression step count is zero.
5. Otherwise mark `RequiresManualConfirmation` with a warning.
6. Persist projection and return DTO.

- [ ] **Step 3: Verify diff**

```powershell
git diff --check -- src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Application/Abstractions src/FloorplanFit.Application/PlanSets/Projection tests/FloorplanFit.Application.Tests/PlanSets/Projection
```

### Task 3: SQLite and DI

**Files:**
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetAdjustmentProjectionRepository.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

- [ ] **Step 1: Add schema**

Create `sheet_adjustment_projections`:

```sql
CREATE TABLE IF NOT EXISTS sheet_adjustment_projections (
    id TEXT PRIMARY KEY,
    plan_set_version_id TEXT NOT NULL,
    dependent_sheet_id TEXT NOT NULL,
    sheet_registration_id TEXT NOT NULL,
    canonical_adjustment_id TEXT NOT NULL,
    method TEXT NOT NULL,
    transform_json TEXT NOT NULL,
    confidence TEXT NOT NULL,
    status TEXT NOT NULL,
    warning TEXT NULL,
    canonical_compression_step_count INTEGER NOT NULL,
    created_at_utc TEXT NOT NULL
);
```

- [ ] **Step 2: Wire DI**

```csharp
services.AddScoped<ISheetAdjustmentProjectionRepository, SqliteSheetAdjustmentProjectionRepository>();
services.AddScoped<ProjectElectricalSheetAdjustmentHandler>();
```

- [ ] **Step 3: Verify and commit code**

```powershell
git diff --check -- src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Application/Abstractions src/FloorplanFit.Application/PlanSets/Projection src/FloorplanFit.Infrastructure/Persistence src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/PlanSets/Projection
git add -- src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionMethod.cs src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionStatus.cs src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionTransform.cs src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjection.cs src/FloorplanFit.Contracts/PlanSets/ProjectElectricalSheetAdjustmentRequest.cs src/FloorplanFit.Contracts/PlanSets/SheetAdjustmentProjectionDto.cs src/FloorplanFit.Contracts/PlanSets/SheetAdjustmentProjectionTransformDto.cs src/FloorplanFit.Application/Abstractions/ISheetAdjustmentProjectionRepository.cs src/FloorplanFit.Application/PlanSets/Projection/ProjectElectricalSheetAdjustmentHandler.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSheetAdjustmentProjectionRepository.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/PlanSets/Projection/ProjectElectricalSheetAdjustmentHandlerTests.cs
git diff --cached --check
git commit -m "feat: project electrical sheet adjustments"
```

### Task 4: Document Phase 4 bridge

**Files:**
- Modify: `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

- [ ] **Step 1: Add Phase 4 implementation bridge**

Under `## Phase 4 - Project Canonical Adjustment to Electrical`, add:

```markdown
### Phase 4 implementation bridge

The first Phase 4 implementation stores electrical projection records in `sheet_adjustment_projections`. It composes the stored electrical registration transform with the approved canonical `AdjustedSitePlanPlacementDto` affine placement instead of running a second fit engine.

A projection is `ReadyForExport` only when registration is confirmed, confidence is high enough, and the canonical adjustment has no compression steps. Canonical compression is recorded as a review blocker in this first slice so piecewise deformation is not silently exported as a simple affine transform.
```

- [ ] **Step 2: Verify and commit docs/plan**

```powershell
git diff --check -- docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md docs/superpowers/plans/2026-06-30-house-plan-set-electrical-projection.md
git add -- docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md docs/superpowers/plans/2026-06-30-house-plan-set-electrical-projection.md
git diff --cached --check
git commit -m "docs: plan electrical sheet projection"
```

## Completion Checklist

- [ ] Projection consumes canonical placement.
- [ ] Projection persists method, transform, confidence, warning, and status.
- [ ] Export readiness is gated by confirmation/confidence/compression review.
- [ ] No independent electrical fit engine is created.
- [ ] No `dotnet build` command is run.
