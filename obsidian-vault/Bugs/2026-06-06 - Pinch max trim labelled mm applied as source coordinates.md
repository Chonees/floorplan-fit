---
type: Bug
date: 2026-06-06
project: floorplan-fit
status: fixed-pending-visual-qa
tags:
  - floorplan-fit
  - pinches
  - units
  - fit-preview
replaces: []
replaced_by: []
---

# Pinch max trim labelled mm applied as source coordinates

## Symptom / question

User asked whether pinches have a maximum reduction limit, whether that limit is in millimeters, and whether the limit belongs to pinches or dimension franjas. Follow-up request changed the product-facing input from millimeters to inches with default `1`.

## Verified evidence

- `PinchMarkerDto` and `PinchMarker` expose `MaxTrimMm`.
- SQLite persists the value as `pinch_markers.max_trim_mm`.
- `ArticulationBandProjector` computes a band's `MaxTrimMm` by summing marker `MaxTrimMm` values.
- `DimensionIntervalBindingDto` stores interval coordinates, not a max trim limit.
- Before the fix, `FloorPlanPreviewGeometry.CreatePreviewGeometry` clamped each marker by raw `marker.MaxTrimMm` and applied that value directly to drawing coordinates.
- SEMINOLE2000 declares `$INSUNITS=1`, which maps to inches, so raw millimeters applied as source coordinates were conceptually wrong.

## Root cause

The model name `MaxTrimMm` was correct for storage, but the preview transform boundary forgot to convert the persisted millimeter cap into the source drawing unit system before translating geometry.

## Fix

- Desktop input now uses inches: `NewPinchMaxTrimInches`, default `"1"`.
- New pinches still persist in millimeters by converting `inches * 25.4` before calling `AddPinchMarkerHandler`.
- Existing pinches display in inches through `PinchMarkerDto.MaxTrimInches`.
- Preview compression now receives `MeasurementContext.ToMillimetersFactor` and converts `MaxTrimMm / factor` before applying the cap to source-coordinate geometry.
- The interaction coordinator naming was corrected from `ActivePreviewTrimMm` to `ActivePreviewTrimSourceUnits` to reflect that pointer drag deltas are drawing units, not real millimeters.

## Verification

- RED confirmed missing behavior/API:
  - `CreatePreviewGeometry` lacked `requestedTrimSourceUnits/sourceToMillimetersFactor`.
  - `FloorPlanReviewViewModel` lacked `NewPinchMaxTrimInches`.
- GREEN command:
  - `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewGeometryTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests|FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .testartifacts\dotnet-test-artifacts`
  - Result: 91/91 passed.
- `git diff --check` exited 0 with only LF-to-CRLF warnings.

## Current truth

User-facing pinch cap is inches; persisted/internal cap is millimeters; preview/export geometry applies the cap only after converting from millimeters to the source DXF unit system.
