# HousePlanSet Facade/Elevation Registration and Projection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:test-driven-development` for the code slice. Keep `ponytail` discipline: smallest useful facade/elevation behavior, no new fit engine, no export rewrite.

**Goal:** Implement Phase 6's first useful slice: facade/elevation sheets can be registered and projected as vertical drawings that relate to the canonical floor plan only through a horizontal reference rule.

**Architecture:** Reuse `SheetRegistration` and `SheetAdjustmentProjection`. Facade/elevation is not an electrical/roof overlay. It stores a facade-specific rule summary and projects only the horizontal relation from the canonical floor placement; vertical scale/offset remain sheet-native until real elevation examples justify more automation.

**Tech Stack:** C#/.NET 10, existing Application/Domain/Contracts/Infrastructure projects, xUnit-style tests. No `dotnet build` after changes.

---

## Scope Boundary

In scope:

- facade/elevation registration request/handler
- facade/elevation projection request/handler
- method enum values for facade/elevation registration and projection
- rule summary that marks vertical preservation and names the horizontal reference
- DI wiring
- tests proving vertical preservation and non-facade rejection

Out of scope:

- automatic opening/window extraction
- real elevation height calibration
- facade DXF geometry rewrite/export
- independent fit proposal search
- piecewise compression projection into elevations
- Desktop UI behavior

## Files

Create:

- `src/FloorplanFit.Contracts/PlanSets/RegisterFacadeElevationSheetRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/ProjectFacadeElevationSheetAdjustmentRequest.cs`
- `src/FloorplanFit.Application/PlanSets/Registration/RegisterFacadeElevationSheetHandler.cs`
- `src/FloorplanFit.Application/PlanSets/Projection/ProjectFacadeElevationSheetAdjustmentHandler.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Registration/RegisterFacadeElevationSheetHandlerTests.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Projection/ProjectFacadeElevationSheetAdjustmentHandlerTests.cs`

Modify:

- `src/FloorplanFit.Domain/PlanSets/SheetRegistrationMethod.cs`
- `src/FloorplanFit.Domain/PlanSets/SheetAdjustmentProjectionMethod.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

---

## Tasks

### Task 1: RED tests

- [ ] Add registration tests proving:
  1. A `FacadeElevation` sheet registers with method `FacadeHorizontalReference`.
  2. Registration stores `RuleSummary = PreserveVertical=true;HorizontalReference=<name>`.
  3. Non-facade sheets are rejected.

- [ ] Add projection tests proving:
  1. Facade projection uses method `FacadeHorizontalPreservingVerticals`.
  2. Canonical scale/offset affects horizontal transform only.
  3. Projection rotation and translate Y stay `0` to preserve the elevation's vertical truth.
  4. Missing vertical-preservation rule blocks automatic export.

- [ ] Verify RED without compiling:

```powershell
Test-Path src\FloorplanFit.Application\PlanSets\Registration\RegisterFacadeElevationSheetHandler.cs
Test-Path src\FloorplanFit.Application\PlanSets\Projection\ProjectFacadeElevationSheetAdjustmentHandler.cs
```

Expected:

```text
False
False
```

### Task 2: Domain/contracts/application

- [ ] Add enum values:
  - `SheetRegistrationMethod.FacadeHorizontalReference = 3`
  - `SheetAdjustmentProjectionMethod.FacadeHorizontalPreservingVerticals = 3`

- [ ] Add registration request:
  - `PlanSetVersionId`
  - `FacadeElevationSheetId`
  - `HorizontalScale`
  - `HorizontalOffset`
  - `Confidence`
  - `ConfirmRegistration`
  - optional `HorizontalReferenceName`
  - optional `Warning`

- [ ] Add registration handler rules:
  - accepts only `PlanSheetType.FacadeElevation`
  - stores transform as scale=`HorizontalScale`, rotation=`0`, translateX=`HorizontalOffset`, translateY=`0`
  - stores rule summary `PreserveVertical=true;HorizontalReference=<name-or-GeneralFacadeDatum>`

- [ ] Add projection request/handler rules:
  - accepts only `FacadeHorizontalReference` registrations
  - composes horizontal scale/offset from canonical floor placement
  - always emits rotation=`0` and translateY=`0`
  - requires confirmed registration, confidence >= `0.8`, no compression steps, and a vertical-preservation rule for `ReadyForExport`

### Task 3: Verify and commit

```powershell
git diff --check -- src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Application/PlanSets/Registration src/FloorplanFit.Application/PlanSets/Projection src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/PlanSets docs/superpowers
```

Commit plan and code separately using conventional commits. Do not run `dotnet build`.

## Completion Checklist

- [ ] Facade/elevation registration is sheet-type specific.
- [ ] Facade/elevation projection preserves vertical values by default.
- [ ] Facade/elevation projection receives canonical horizontal placement without running a fit engine.
- [ ] Missing rule/low confidence/pending registration/compression requires manual confirmation.
- [ ] No independent facade/elevation fit engine is created.
- [ ] No `dotnet build` command is run.
