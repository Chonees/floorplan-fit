---
type: implementation
date: 2026-07-15
status: partially-superseded
replaced_by: "[[2026-07-15 - Comparison DXF repeated AutoCAD black invalid serialization]]"
---

# Atomic Floor/Electrical package comparison

> [!warning] Comparison portion superseded
> The atomic Floor/Electrical package and generic artifact manifest remain current. Production of `X-comparison.dxf`, its composer, DI registration, and `ComparisonReview` artifact were removed after repeated AutoCAD-invalid serialization. See [[2026-07-15 - Comparison DXF repeated AutoCAD black invalid serialization]].

## Scope

- Loop 2 HousePlanSet export.
- Application orchestration, Contracts manifest metadata, Infrastructure DXF composition, Desktop DI registration.

## Implemented

- Preserves the existing top-level canonical `X.dxf` byte-for-byte.
- Uses deterministic sanitized package filenames, adding numeric suffixes only for real sheet-role collisions.
- Copies the canonical FloorPlan and writes projected dependent sheets inside the existing sibling staging directory.
- ~~Produces `X-comparison.dxf` only when an ElectricalPlan was automatically exported.~~ Superseded; no comparison is currently produced.
- Computes visible model-space bounds before adding review geometry, derives each complete frame/label envelope, and translates complete documents side-by-side without overlap, including tall/narrow plans.
- Namespaces layers, blocks, linetypes, text styles, dimension styles, and their references across model entities, nested block entities, insert attributes, and classic polyline vertices before merging.
- Adds separate review frame/label layers so the comparison remains visibly understandable but technically isolated from the individual source outputs.
- Reloads the comparison and fails composition if model entities or blocks were dropped.
- Publishes the user package through the existing atomic directory gate; composition failure cleans staging and leaves no final folder.
- Adds manifest `Artifacts` entries for the technical `FloorPlan` and ready dependent-sheet outputs. `ComparisonReview` was removed.

## Verification status

- Focused contracts were written first.
- Two independent static-review rounds found and corrected review-envelope overlap and shared DXF symbol-table collisions; the final static re-review found no concrete blocker.
- Static checks only: `git diff --check` passed.
- Per repository/user constraint, no `dotnet`, build, restore, test, watch, or Desktop command was run. Compilation and runtime test status remain unknown until external execution.

## Relevant files

- `src/FloorplanFit.Application/PlanSets/Export/ExportMultiSheetPlanSetPackageHandler.cs`
- `src/FloorplanFit.Infrastructure/Dxf/PlanSetComparisonDxfComposer.cs`
- `src/FloorplanFit.Contracts/PlanSets/PlanSetPackageArtifactDto.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Export/ExportMultiSheetPlanSetPackageHandlerTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/PlanSetComparisonDxfComposerTests.cs`
