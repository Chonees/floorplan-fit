---
type: implementation
date: 2026-06-18
status: active
---

# Edit preview floor-plan zoom without tiny grid squares

## What changed

Loop 1 Edit preview now allows more precise placement work:

- Maximum user zoom increased from `80x` to `1,000,000x` for practical wall/block inspection.
- CAD minor grid target spacing was restored to the original `16px`; the last tiny-grid changes (`8px`, `4px`, `2px`) are no longer the active behavior.
- Wheel zoom step increased from `1.12x` per wheel delta to `2x`, so the floor plan itself zooms aggressively instead of solving precision by shrinking the background grid.

## Why

The user wanted the Edit preview to be much more precise when placing pinch points and similar canvas interactions.


## Follow-up correction

The render path was checked: the CAD grid and floor-plan geometry both receive the same `scene.Viewport`, and base wall segments are drawn through `viewport.Project(...)`. That means the correct fix is not a separate renderer for lines; the active correction is a much higher/faster floor-plan zoom while restoring the grid to its original density.

The wheel zoom calculation body was also changed, not only the constants, so `dotnet watch` has an actual method-body change to hot-reload or restart around.

## Scope

- Product loop: Loop 1 floor plan curation.
- Architecture layer: Desktop.

## Files

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/CadViewportContext.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`

## Verification

No build was run, following repo rules.

Source checks verified:

- `MaximumUserZoomFactor = 1_000_000d`
- `TargetMinorGridSpacingPixels = 16d`
- `UserZoomStep = 2d`
- tests assert the new zoom cap, faster wheel step, wall-inspection zoom above 1000x within ten ticks, and restored default CAD grid expectations
- `git diff --check` passed for touched files, with CRLF warnings only

## Crash follow-up

After raising floor-plan zoom to `1,000,000x`, the render path could crash because annotation text and dimension circle/arc primitives still multiplied their screen size by `viewport.Scale`. At extreme zoom that can create enormous `FormattedText` instances or huge radii during Avalonia render.

Current correction:

- floor-plan line geometry still zooms through the shared viewport;
- CAD/site-plan text font size is capped at `512px`;
- far-offscreen text is skipped;
- dimension circle/arc primitive radii are capped at `512px`;
- the duplicated unused `UserZoomStep` constant in `FloorPlanPreviewControl` was removed because the real wheel zoom calculation lives in `PreviewInteractionCoordinator`.

No build was run, following repo rules. Source checks and `git diff --check` verified the changed constants, guards, and test expectations.
