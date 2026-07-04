---
type: implementation
date: 2026-06-16
topic: loop2-adjust-to-site-plan-modular-fit-affected-dimensions
replaces:
  - Implementation/2026-06-09 - Auto-fit changed-number dimensions highlighted red.md
---
# Adjust to Site Plan modular fit and affected dimensions

## Context
The user reported that small synth deficits looked like huge overflow in the preview and that red cota highlighting no longer represented only the dimensions affected by the adjustment.

## Root cause
- Structural fit was already based on the wall/structural footprint, but the preview also renders visual dimension geometry that can extend far outside that footprint.
- `MoveFloorPlanBy(...)` translated geometry/baseline but left `autoFitSuggestionFacts` stale, so later apply logic could choose an edge from old side-overflow facts.
- Red highlighting was computed from changed visible `DisplayText` only, which misses affected dimensions whose geometry changes but whose rounded text remains the same.

## Implementation
- Added `SitePlanAdjustmentFitAnalyzer` to centralize fit-geometry selection, auto-fit fact building, and safe side-overflow translation after manual moves.
- Added `ChangedDimensionDetector` to detect affected dimensions by visible text, measurement values, and native geometry/primitives.
- Added `AdjustedDimensionImpactResolver` to resolve bound dimensions affected by the selected candidate band while excluding bound cotas outside that band.
- Updated `SitePlanAdjustmentViewModel` so manual moves refresh facts and `ApplyAutoFitPlan(...)` uses affected-dimension semantics for red/export patches.
- Kept `SitePlanAdjustmentPreviewProjector` as a compatibility facade while moving fit-fact responsibility into Application.

## Verification
- RED: manual move then apply expected `Bottom` edge but got stale `Top`.
- RED: adjusted dimension geometry changed with unchanged visible text, but no red ID was produced.
- GREEN: `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter FullyQualifiedName~SitePlanAdjustment --no-restore` passed 23/23.
- GREEN: `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --filter FullyQualifiedName~SitePlanAdjustmentPreviewProjectorTests --no-restore --output .testartifacts/verify-desktop-siteplan-adjustment-final --verbosity minimal` passed 28/28. The isolated output path avoids locks from a running Desktop process.
- GREEN: `git diff --check` exited 0 with CRLF warnings only.
