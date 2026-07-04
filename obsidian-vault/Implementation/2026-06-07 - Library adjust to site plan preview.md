---
type: Implementation
date: 2026-06-07
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - library
  - preview
---

# Library Adjust to Site Plan step 1 preview

## Change

Library now exposes a published-only **Adjust to Site Plan** action next to each floor plan version, using the existing Library row-button UX. The old row **Open** label is now **Edit**, and the top Review action now says **Edit Selected Review**.

Clicking **Adjust to Site Plan** opens a DXF picker for the site plan, loads the active published curation for that version, and opens a preview-only window that overlays the floor plan on top of the site plan.

## Product loop

This is Loop 2 foundation work: site-plan adjustment starts from a published Loop 1 curation and produces a preview surface before any fit tools are enabled.

## Architecture

- Contracts: `SitePlanPreviewDto` / `SitePlanBuildableAreaDto`, plus `FloorPlanLibraryVersionDto.CanAdjustToSitePlan`.
- Application boundary: `ISitePlanPreviewReader` reads transient site-plan previews without importing them into the floor-plan library.
- Infrastructure: `IxMiliaSitePlanPreviewReader` extracts site-plan line/polyline geometry and detects a first buildable-area candidate.
- Desktop: Library opens `SitePlanAdjustmentWindow`; existing `FloorPlanPreviewControl` now supports `SitePlanGeometryPaths` as a muted underlay.

## Design decisions

- The enablement source is `ActivePublishedCurationId`, not the display status string. A version is adjustable only when the system knows which published curation should be used.
- Step 1 does not scale-to-fit. It unit-converts floor-plan coordinates into the site-plan unit system, then centers the floor-plan bounding box inside the detected buildable area.
- Current buildable-area detection first uses the bounds of detected setback geometry (`IsSetback` render paths). The earlier second-largest closed-shape heuristic is now only a fallback.
- Superseded: the first Step 1 heuristic chose the second-largest closed polyline bounding box, which was not reliable once full CAD content was extracted.
- The new screen intentionally has no Fit toolbar or inspector yet; it only reuses the preview and cotas visibility toggle.

## Verification

- RED: focused Desktop tests failed first because `CanAdjustToSitePlan`, `SitePlanGeometryPaths`, `SitePlanPreviewDto`, and `SitePlanAdjustmentPreviewProjector` did not exist.
- GREEN: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .testartifacts\dotnet-test-artifacts` passed 206/206.
- `git diff --check` exited 0 with only LF-to-CRLF warnings.

## Gotchas

The buildable-area detector is intentionally a Step 1 heuristic. Current priority is explicit setback layer/text semantics; future work should add user selection when real site plans do not expose a clean setback geometry set.

## Correction after visual QA

User visual QA showed the first version was wrong: the site plan appeared as a simplified gray underlay and missed most visible CAD content from the real `158 DAWSON STREET.dxf` site plan.

Root cause: the first `IxMiliaSitePlanPreviewReader` only extracted `DxfLine` and `DxfLwPolyline`, while `PreviewRenderComposer` rendered those site-plan paths with one hardcoded muted gray pen. That dropped text, title content, arcs/circles/ellipses/solids/faces, insert/block geometry, layer colors, and setback layer semantics.

Correction:

- `SitePlanPreviewDto` now carries `RenderPaths` and `Texts` in addition to geometry bounds paths.
- `IxMiliaSitePlanPreviewReader` now extracts colored render paths for lines, lightweight polylines, arcs, circles, ellipses, solids, 3DFaces, and nested block inserts.
- It also extracts `TEXT` and `MTEXT`, including nested insert text, with layer/source color and transformed position/height/rotation.
- Paths or text from `SETBACK` layers/text are marked with `IsSetback`; missing colors fall back to a setback highlight color.
- `FloorPlanPreviewControl` now exposes `SitePlanRenderPaths` and `SitePlanTexts`.
- `SitePlanPreviewLayerRenderer` renders site-plan paths/text using source colors instead of a single gray underlay.

Additional verification:

- `IxMiliaSitePlanPreviewReaderTests` uses the real fixture `PLANS/originalsSitePlans/158 DAWSON STREET.dxf` and confirms `SITE PLAN` text, `SETBACKS` layer semantics, multiple source colors, and non-line/non-polyline geometry are present.
- `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .testartifacts\dotnet-test-artifacts` passed 207/207.
- `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .testartifacts\dotnet-test-artifacts` passed 86/86.
- `git diff --check` exited 0 with only LF-to-CRLF warnings.

## Rendering correction: only setbacks are colored

User clarified the visual language: the site plan must show all CAD content, but only setback elements should be colored. Non-setback site-plan content should remain gray.

Correction:

- `SitePlanPreviewLayerRenderer.ResolveColor(...)` now ignores source DXF colors for non-setback entities and renders them as neutral gray.
- Setback entities (`IsSetback = true`) render with the configured setback highlight color (`#FFFFB000`).
- Source colors may still exist in DTO metadata for diagnostics/future use, but they are not used for non-setback display.

Verification:

- RED test first failed because non-setback `#FF00FFFF` rendered as Aqua.
- GREEN: `SitePlanPreviewLayerRenderer_colors_only_setbacks_and_keeps_the_rest_gray` now passes.
- Desktop tests passed 208/208.
- `git diff --check` exited 0 with only LF-to-CRLF warnings.

## Centering correction: buildable area now comes from setback geometry

User visual QA showed the floor-plan overlay was not centered inside the orange setback rectangle.

Root cause: after full CAD extraction, `IxMiliaSitePlanPreviewReader.ResolveBuildableArea(...)` still used the old closed-shape heuristic: sort all closed entities by area and pick the second-largest one. The real site-plan setback region in `158 DAWSON STREET.dxf` is represented by open setback render geometry, not one closed setback polyline, so the computed buildable area did not match the orange setback rectangle.

Correction:

- `ResolveBuildableArea(...)` now receives the full `SitePlanRenderPathDto` list.
- It first computes the union bounds of all paths with `IsSetback = true`.
- Only if no usable setback bounds exist does it fall back to the old closed-shape heuristic / whole drawing bounds.

Verification:

- RED: `ReadAsync_uses_setback_geometry_bounds_as_buildable_area` failed with `Expected MinX: 32.3656860690203` but `Actual MinX: 44.7736704268132`.
- GREEN: the same test passed after using setback geometry bounds.
- Focused reader tests passed 3/3.
- Full Infrastructure tests passed 87/87 after rerun.
- Desktop tests passed 208/208.

## Centering correction: floor-plan placement uses wall structure bounds

User visual QA still showed the overlay offset after the setback buildable-area correction.

Root cause: `SitePlanAdjustmentPreviewProjector.Project(...)` computed the floor-plan placement bounds from every preview geometry path. In this product, `reviewViewModel.GeometryPaths` includes walls/openings plus fixed components and protected details. Those fixture/component paths can extend farther than the building shell and skew the center.

Evidence from the current local `app.db` / active SEMINOLE2000 curation:

- All selected preview geometry bbox center: `X = 359.4108749017802`.
- Wall candidate geometry bbox center: `X = 320.6813958468742`.
- Difference: about `38.73` source inches, or about `3.23 ft` after inch-to-foot site-plan scaling.

Correction:

- `Project(...)` now accepts optional `floorPlanPlacementGeometryPathIds`.
- The Library `Adjust to Site Plan` flow passes wall candidate geometry ids as the placement basis.
- The projector still transforms all floor-plan geometry, labels, and dimensions; only the centering bbox ignores fixture/component outliers.
- If no wall placement ids are available, the projector falls back to all floor-plan geometry.

Verification:

- RED: `Project_centers_floor_plan_by_structural_placement_geometry_not_fixture_outliers` first failed at compile time because the projector did not expose placement geometry ids.
- GREEN: the test now passes and verifies the wall structure center lands on the site-plan buildable center while fixture outliers do not control placement.
- Projector tests passed 4/4.
- Site-plan reader tests passed 3/3.
- Desktop tests passed 209/209.
- `git diff --check` exited 0 with only LF-to-CRLF warnings.


## Manual floor-plan drag tool

Added Step 1 manual adjustment tool for `Adjust to Site Plan` preview.

Behavior:

- Button: **Move Floor Plan**.
- When active, left-button dragging in the preview moves only the floor-plan overlay.
- The site-plan geometry/text remains fixed.
- The move applies to floor geometry, room labels, opening labels, and dimensions together.
- The view model tracks cumulative `ManualOffsetX` / `ManualOffsetY` for the current preview session.

Implementation details:

- `FloorPlanPreviewControl` exposes `IsFloorPlanMoveToolActive` and emits `FloorPlanMoveDeltaRequested` in source/site-plan coordinates.
- `SitePlanAdjustmentWindow` listens for that event and delegates to `SitePlanAdjustmentViewModel.MoveFloorPlanBy(...)`.
- `SitePlanAdjustmentViewModel` translates current floor overlay collections without mutating site-plan collections.
- This does not persist or export the manual offset yet; it is preview-only for this step.

Verification:

- RED tests first failed because the move tool state, delta conversion, and view-model move method did not exist.
- Focused move tests passed.
- Full Desktop tests passed 211/211 using isolated artifacts path `.testartifacts\dotnet-test-artifacts-siteplan-move-full`.
- `git diff --check` exited 0 with only LF-to-CRLF warnings.

## Site-plan terrain/setback-only display filter

User clarified the Adjust to Site Plan preview should show only the terrain/lot and its setbacks, not the full site-plan annotation/title/street clutter.

Behavior:

- Site-plan display now filters render paths before binding to the preview.
- Kept paths:
  - `IsSetback = true` paths.
  - terrain/property/lot paths whose layer contains tokens like `PROP`, `PROPERTY`, `LOT`, `BOUND`, or `PARCEL`.
- Kept text:
  - setback text only.
- Removed from the preview display:
  - title block text like `SITE PLAN` / address / object place.
  - street labels and annotation tables.
  - non-property auxiliary CAD content.
- The full raw site-plan reader output remains available in `SitePlanPreviewDto`; only the adjustment screen display is filtered.

Verification:

- RED: `FilterSitePlanForAdjustment_keeps_only_terrain_and_setbacks` failed because no filter existed.
- GREEN: filter keeps property boundary + setbacks and drops street/title content.
- Projector tests passed 6/6.
- Full Desktop tests passed 212/212 using isolated artifacts path `.testartifacts\dotnet-test-artifacts-siteplan-filter-full`.
- `git diff --check` exited 0 with only LF-to-CRLF warnings.
