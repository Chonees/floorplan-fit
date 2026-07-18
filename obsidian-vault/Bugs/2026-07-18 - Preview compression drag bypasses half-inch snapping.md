---
type: bug
status: implemented-static-runtime-pending
date: 2026-07-18
project: FloorplanFit
area: Loop 1 Desktop
source_of_truth: code
---

# Preview compression drag bypasses half-inch snapping

## Symptom

Dragging a compression handle can make reactive dimensions show values such as `5/8"` or `15/16"` instead of moving in fixed `1/2"` increments.

## Verified cause

`PreviewInteractionCoordinator.HandlePointerMoved` converts pointer pixel distance directly into `ActivePreviewTrimSourceUnits`. Unlike the new buttons, this interaction has no architectural increment snap.

`DimensionDisplayTextFormatter` then honestly renders exact sixteenth-inch geometry. Changing only its formatter would hide the real trim and make the displayed dimension disagree with geometry.

## Required correction

- Snap the real preview trim to a `1/2"` grid before applying geometry deformation.
- Convert `1/2"` to source units through `MeasurementContext.ToMillimetersFactor` so Inch, Foot, and millimeter DXFs behave consistently.
- Preserve the existing formatter precision for source dimensions; do not fake a half-inch label over unsnapped geometry.

## Evidence boundary

Source diagnosis and implementation are verified statically. Runtime verification remains external.

## Implemented correction

- Pointer trim is quantized before entering preview state, so geometry receives the snapped value rather than a rounded label.
- The snap step is `12.7 mm / ToMillimetersFactor`, producing `0.5` source units for inch DXFs and `12.7` for millimeter DXFs.
- Missing, invalid, or overflowing unit context fails safe by disabling snap instead of inventing a unit.
- Non-finite or unrepresentable pointer math produces a safe zero trim; snap overflow preserves the representable raw trim.
- Focused tests cover `0.625 -> 0.5`, `0.76 -> 1.0`, unit conversion, missing context, and overflow boundaries.
- Scoped `git diff --check` and final static review pass; no .NET or Desktop command ran.
