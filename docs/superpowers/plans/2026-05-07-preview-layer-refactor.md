# Preview Layer Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the growing `FloorPlanPreviewControl` into focused preview-layer collaborators before adding dimensions and more CAD-faithful artifacts.

**Architecture:** Keep `FloorPlanPreviewControl` as the Avalonia interaction/composition shell. Move path-backed overlay concerns into small internal classes under `FloorplanFit.Desktop.Controls.Preview`: one geometry index for hit-test/render classification, one renderer for openings, and one renderer for fixed components. This is a behavior-preserving refactor.

**Tech Stack:** C#/.NET 10, Avalonia `DrawingContext`, xUnit desktop tests, existing `GeometryPathDto`, `OpeningCandidateDto`, and `FixedPlanComponentDto` contracts.

---

## File Structure

- Create `src/FloorplanFit.Desktop/Controls/Preview/PreviewArtifactGeometryIndex.cs`
  - Purpose: own the path-id classification and hit-test ordering for path-backed review artifacts.
- Create `src/FloorplanFit.Desktop/Controls/Preview/OpeningPreviewLayerRenderer.cs`
  - Purpose: render opening candidate geometry and own opening pen/style decisions.
- Create `src/FloorplanFit.Desktop/Controls/Preview/FixedPlanComponentPreviewLayerRenderer.cs`
  - Purpose: render fixed component geometry and own fixed-component pen/style decisions, including original DXF colors.
- Create `src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs`
  - Purpose: render CAD text overlays and own DXF text-height, rotation, alignment, baseline, and readable preview color policy.
- Create `src/FloorplanFit.Desktop/Controls/Preview/PreviewWorkspaceRenderer.cs`
  - Purpose: render the dotted workspace background and own workspace dot layout.
- Create `src/FloorplanFit.Desktop/Controls/Preview/CompressionHandlePreviewLayerRenderer.cs`
  - Purpose: render visible compression handles and own the armed/unarmed handle visibility rule.
- Create `src/FloorplanFit.Desktop/Controls/Preview/PinchMarkerPreviewLayerRenderer.cs`
  - Purpose: render pinch markers, own active group/axis styling, and expose selected-group filtering for compression preview.
- Modify `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
  - Purpose: delegate artifact overlays to the new collaborators while keeping pointer interaction, zoom/pan, and high-level composition.
- Modify `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
  - Purpose: pin the new collaborators' behavior before implementation.

---

## Tasks

### Task 1: Introduce preview artifact geometry indexing

**Files:**
- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewArtifactGeometryIndex.cs`
- Modify/Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`

- [x] Write failing tests proving fixed component paths sort above opening paths and wall paths.
- [x] Implement `PreviewArtifactGeometryIndex.Create(openings, fixedPlanComponents)`.
- [x] Implement `OrderForHitTesting(geometryPaths)`.
- [x] Replace `FloorPlanPreviewControl.BuildHitTestGeometry` internals with the index.

### Task 2: Extract opening renderer

**Files:**
- Create: `src/FloorplanFit.Desktop/Controls/Preview/OpeningPreviewLayerRenderer.cs`
- Modify/Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`

- [x] Write failing tests for opening pen colors and highlight behavior.
- [x] Move opening pen creation and rendering into `OpeningPreviewLayerRenderer`.
- [x] Call the renderer from `FloorPlanPreviewControl.Render`.

### Task 3: Extract fixed component renderer

**Files:**
- Create: `src/FloorplanFit.Desktop/Controls/Preview/FixedPlanComponentPreviewLayerRenderer.cs`
- Modify/Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`

- [x] Write failing tests for original DXF color and semantic fallback colors.
- [x] Move fixed component pen creation and rendering into `FixedPlanComponentPreviewLayerRenderer`.
- [x] Call the renderer from `FloorPlanPreviewControl.Render`.

### Task 4: Extract CAD text renderer

**Files:**
- Create: `src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs`
- Modify/Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`

- [x] Write failing tests that move room/opening label render-plan behavior out of the control.
- [x] Move label projection, text-height scaling, readable black color policy, rotation, and baseline alignment into `CadTextPreviewLayerRenderer`.
- [x] Call the renderer from `FloorPlanPreviewControl.Render`.
- [x] Remove duplicated CAD text helper logic from `FloorPlanPreviewControl`.

### Task 5: Extract preview shell renderers

**Files:**
- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewWorkspaceRenderer.cs`
- Create: `src/FloorplanFit.Desktop/Controls/Preview/CompressionHandlePreviewLayerRenderer.cs`
- Create: `src/FloorplanFit.Desktop/Controls/Preview/PinchMarkerPreviewLayerRenderer.cs`
- Modify/Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`

- [x] Write failing tests for workspace dot layout ownership, handle visibility, selected pinch-group filtering, and marker styling.
- [x] Move dotted workspace rendering into `PreviewWorkspaceRenderer`.
- [x] Move compression handle rendering/visibility into `CompressionHandlePreviewLayerRenderer`.
- [x] Move pinch marker rendering/styling and selected-group filtering into `PinchMarkerPreviewLayerRenderer`.
- [x] Remove duplicated workspace, handle, and pinch marker helper logic from `FloorPlanPreviewControl`.

### Task 6: Verify no behavior drift

**Files:**
- Existing test projects only.

- [x] Run focused Desktop preview tests with `--artifacts-path`.
- [x] Run Desktop tests with `--artifacts-path`.
- [x] Run Application tests with `--artifacts-path`.
- [x] Run Infrastructure tests with `--artifacts-path`.
- [x] Run `git diff --check`.
- [x] Delete `.artifacts-test`.

---

## Self-review

- This plan intentionally does **not** add dimensions yet.
- This plan intentionally does **not** implement the full curation correction backbone yet.
- The purpose is to make the preview operable before the next artifact families arrive.
