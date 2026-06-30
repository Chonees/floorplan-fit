# HousePlanSet Roof Registration and Projection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Phase 5's first useful slice: register and project roof sheets while preserving roof-specific overhang metadata and avoiding an independent roof fit engine.

**Architecture:** Reuse the existing `SheetRegistration` and `SheetAdjustmentProjection` stores, adding one tiny rule metadata field instead of creating a separate roof subsystem. Roof registration is not electrical registration with a different label: it uses `RoofFootprintWithOverhang`, stores an overhang preservation rule, and roof projection carries that rule into the projected result for review/export audit.

**Tech Stack:** C#/.NET 10, existing Application/Domain/Contracts/Infrastructure projects, SQLite via `Microsoft.Data.Sqlite`, xUnit-style tests. No `dotnet build` after changes.

---

## Scope Boundary

In scope:

- roof registration request/handler
- roof projection request/handler
- `RuleSummary` metadata on registration/projection records
- SQLite migration for rule metadata
- DI wiring
- tests proving overhang metadata is preserved and non-roof sheets are rejected

Out of scope:

- roof eave/ridge anchor extraction
- actual roof DXF geometry rewrite/export
- roof-specific piecewise deformation
- facade/elevation behavior
- Desktop UI behavior

## Files

Create:

- `src/FloorplanFit.Contracts/PlanSets/RegisterRoofSheetRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/ProjectRoofSheetAdjustmentRequest.cs`
- `src/FloorplanFit.Application/PlanSets/Registration/RegisterRoofSheetHandler.cs`
- `src/FloorplanFit.Application/PlanSets/Projection/ProjectRoofSheetAdjustmentHandler.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Registration/RegisterRoofSheetHandlerTests.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Projection/ProjectRoofSheetAdjustmentHandlerTests.cs`

Modify:

- `src/FloorplanFit.Domain/PlanSets/SheetRegistrationMethod.cs`
- `src/FloorplanFit.Domain/PlanSets/SheetRegistration.cs`
- `src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionMethod.cs`
- `src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjection.cs`
- `src/FloorplanFit.Contracts/PlanSets/SheetRegistrationDto.cs`
- `src/FloorplanFit.Contracts/PlanSets/SheetAdjustmentProjectionDto.cs`
- `src/FloorplanFit.Application/PlanSets/Registration/RegisterElectricalSheetHandler.cs`
- `src/FloorplanFit.Application/PlanSets/Projection/ProjectElectricalSheetAdjustmentHandler.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetRegistrationRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetAdjustmentProjectionRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

---

### Task 1: RED tests

**Files:**
- Create: `tests/FloorplanFit.Application.Tests/PlanSets/Registration/RegisterRoofSheetHandlerTests.cs`
- Create: `tests/FloorplanFit.Application.Tests/PlanSets/Projection/ProjectRoofSheetAdjustmentHandlerTests.cs`

- [ ] **Step 1: Add tests**

Registration tests must prove:

1. A roof sheet can be registered with method `RoofFootprintWithOverhang` and rule summary `PreserveOverhangInches=<value>`.
2. A non-roof sheet is rejected by the roof registration handler.

Projection tests must prove:

1. A confirmed roof registration projects canonical placement using method `RoofOverhangPreserving` and carries the overhang rule summary.
2. Missing overhang rule metadata blocks automatic export with `RequiresManualConfirmation`.

- [ ] **Step 2: Verify RED without compiling**

```powershell
Test-Path src\FloorplanFit.Application\PlanSets\Registration\RegisterRoofSheetHandler.cs
Test-Path src\FloorplanFit.Application\PlanSets\Projection\ProjectRoofSheetAdjustmentHandler.cs
```

Expected:

```text
False
False
```

Do not run `dotnet build`.

### Task 2: Domain/contracts/application

**Files:**
- Modify/create files listed above.

- [ ] **Step 1: Extend shared records minimally**

Add nullable `RuleSummary` to `SheetRegistration`, `SheetRegistrationDto`, `SheetAdjustmentProjection`, and `SheetAdjustmentProjectionDto`.

- [ ] **Step 2: Add roof method values**

- `SheetRegistrationMethod.RoofFootprintWithOverhang = 2`
- `SheetAdjustmentProjectionMethod.RoofOverhangPreserving = 2`

- [ ] **Step 3: Add roof handlers**

Rules:

- `RegisterRoofSheetHandler` accepts only `PlanSheetType.RoofPlan`.
- `RegisterRoofSheetHandler` rejects negative overhang.
- `RegisterRoofSheetHandler` writes `RuleSummary = "PreserveOverhangInches=<overhang>"`.
- `ProjectRoofSheetAdjustmentHandler` composes the same canonical affine placement as electrical projection, but method is `RoofOverhangPreserving` and the rule summary must be present.
- Missing rule summary, unconfirmed registration, confidence below `0.8`, or compression steps mark projection `RequiresManualConfirmation`.

- [ ] **Step 4: Verify diff**

```powershell
git diff --check -- src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Application/PlanSets/Registration src/FloorplanFit.Application/PlanSets/Projection tests/FloorplanFit.Application.Tests/PlanSets
```

### Task 3: SQLite and DI

**Files:**
- Modify SQLite repositories/schema and Desktop DI.

- [ ] **Step 1: Add `rule_summary` columns**

Add nullable `rule_summary TEXT NULL` to:

- `sheet_registrations`
- `sheet_adjustment_projections`

- [ ] **Step 2: Wire DI**

```csharp
services.AddScoped<RegisterRoofSheetHandler>();
services.AddScoped<ProjectRoofSheetAdjustmentHandler>();
```

- [ ] **Step 3: Verify and commit code**

```powershell
git diff --check -- src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Application/PlanSets/Registration src/FloorplanFit.Application/PlanSets/Projection src/FloorplanFit.Infrastructure/Persistence src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/PlanSets
git add -- src/FloorplanFit.Domain/PlanSets/SheetRegistrationMethod.cs src/FloorplanFit.Domain/PlanSets/SheetRegistration.cs src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionMethod.cs src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjection.cs src/FloorplanFit.Contracts/PlanSets/RegisterRoofSheetRequest.cs src/FloorplanFit.Contracts/PlanSets/ProjectRoofSheetAdjustmentRequest.cs src/FloorplanFit.Contracts/PlanSets/SheetRegistrationDto.cs src/FloorplanFit.Contracts/PlanSets/SheetAdjustmentProjectionDto.cs src/FloorplanFit.Application/PlanSets/Registration/RegisterElectricalSheetHandler.cs src/FloorplanFit.Application/PlanSets/Registration/RegisterRoofSheetHandler.cs src/FloorplanFit.Application/PlanSets/Projection/ProjectElectricalSheetAdjustmentHandler.cs src/FloorplanFit.Application/PlanSets/Projection/ProjectRoofSheetAdjustmentHandler.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSheetRegistrationRepository.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSheetAdjustmentProjectionRepository.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/PlanSets/Registration/RegisterRoofSheetHandlerTests.cs tests/FloorplanFit.Application.Tests/PlanSets/Projection/ProjectRoofSheetAdjustmentHandlerTests.cs
git diff --cached --check
git commit -m "feat: support roof sheet projection rules"
```

### Task 4: Document Phase 5 bridge

**Files:**
- Modify: `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

- [ ] **Step 1: Add Phase 5 implementation bridge**

Under `## Phase 5 - Roof Registration and Projection`, add:

```markdown
### Phase 5 implementation bridge

The first Phase 5 implementation reuses the shared registration/projection stores but adds `rule_summary` so roof-specific overhang preservation is not lost. Roof registration uses `RoofFootprintWithOverhang`; roof projection uses `RoofOverhangPreserving` and carries `PreserveOverhangInches=<value>` into the persisted projection.

This phase still does not rewrite/export roof DXF geometry. Missing overhang rules, low confidence, unconfirmed registration, or canonical compression steps force manual confirmation before export.
```

- [ ] **Step 2: Verify and commit docs/plan**

```powershell
git diff --check -- docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md docs/superpowers/plans/2026-06-30-house-plan-set-roof-registration-projection.md
git add -- docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md docs/superpowers/plans/2026-06-30-house-plan-set-roof-registration-projection.md
git diff --cached --check
git commit -m "docs: plan roof sheet projection rules"
```

## Completion Checklist

- [ ] Roof registration preserves overhang metadata.
- [ ] Roof projection persists overhang rule summary.
- [ ] Non-roof sheets cannot use roof registration.
- [ ] Missing roof rule metadata blocks automatic export.
- [ ] No independent roof fit engine is created.
- [ ] No `dotnet build` command is run.
