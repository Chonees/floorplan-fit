# Adjust Site Plan Source-Preserving Appearance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Loop 2 Adjust to Site Plan preview and export preserve full site-plan CAD appearance/content instead of filtering/recoloring/rebuilding a simplified site-plan overlay.

**Architecture:** Preview should use full `SitePlanPreviewDto.RenderPaths` and `Texts` while still using `BuildableArea` for fit logic. Rendering should honor `ColorArgb` for all paths/texts, with fallback only when DXF color is missing. Export should inject site-plan entities from source DXF group-code records, transforming coordinates but preserving layer/entity visual metadata and copying site-plan layer records into the combined floor-plan DXF.

**Tech Stack:** C#/.NET tests, Avalonia preview renderer, IxMilia DXF reader for preview, raw DXF group-code patching for AutoCAD-safe export.

---

### Task 1: Preview keeps full site-plan content and colors

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/SitePlanPreviewLayerRenderer.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`

- [ ] Step 1: Update renderer test so non-setback and setback both honor source `ColorArgb`.
- [ ] Step 2: Update projector filter test so all render paths and all texts are kept.
- [ ] Step 3: Run focused Desktop tests and confirm RED.
- [ ] Step 4: Change `SitePlanPreviewLayerRenderer.ResolveColor` to parse and return `ColorArgb` before fallback.
- [ ] Step 5: Change `FilterSitePlanForAdjustment` to return all render paths/texts.
- [ ] Step 6: Run focused Desktop tests and confirm GREEN.

### Task 2: Export preserves site-plan entity and layer visual metadata

**Files:**
- Modify: `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaAdjustedSitePlanExporterTests.cs`
- Modify: `src/FloorplanFit.Infrastructure/Dxf/IxMiliaAdjustedSitePlanExporter.cs`

- [ ] Step 1: Add RED test with a site plan layer using non-default color/linetype/lineweight and entity-level color/linetype, then assert the combined DXF preserves those group codes.
- [ ] Step 2: Run focused Infrastructure exporter tests and confirm RED.
- [ ] Step 3: Replace IxMilia entity reconstruction for site-plan injection with source-pair entity record cloning/transforming.
- [ ] Step 4: Copy site-plan layer records for missing site-plan layers instead of building default layer records.
- [ ] Step 5: Ensure handles/owners are regenerated, `$HANDSEED` advances, and coordinate group codes are transformed by placement.
- [ ] Step 6: Run focused Infrastructure exporter tests and confirm GREEN.

### Task 3: Verification and durable knowledge

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Modify or create: `obsidian-vault/Implementation/2026-06-15 - Adjust to Site Plan preserves full site plan appearance.md`

- [ ] Step 1: Run focused Desktop and Infrastructure tests.
- [ ] Step 2: Run `git diff --check`.
- [ ] Step 3: Update Obsidian Current State and Implementation notes.
- [ ] Step 4: Save Engram shadow memory.
