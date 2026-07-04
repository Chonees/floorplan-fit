---
type: Inbox
date: 2026-06-15
project: floorplan-fit
status: requested
tags:
  - floorplan-fit
  - loop2
  - adjust-to-site-plan
  - dxf
  - preview
---

# Adjust to Site Plan must preserve full site plan colors and content

User clarified that in `Adjust to Site Plan`, both the preview and exported DXF must preserve the site plan appearance and content: colors, title/block text, layers, and the rest of the CAD visual information.

Verified current gap:

- `SitePlanAdjustmentPreviewProjector.FilterSitePlanForAdjustment(...)` filters the site-plan preview down to terrain/setback paths and setback text.
- `SitePlanPreviewLayerRenderer.ResolveColor(...)` ignores source `ColorArgb` and forces non-setback site-plan geometry to gray and setback geometry to a hard-coded orange.
- `IxMiliaAdjustedSitePlanExporter` rebuilds supported site-plan entities from IxMilia objects and injects missing layers with default metadata (`62=7`, `Continuous`, etc.), which does not preserve the source site-plan visual metadata/styles.

Desired truth:

- Loop 2 preview should show the site plan with the source colors/content preserved, while still using the detected `BuildableArea` for fitting.
- Loop 2 export should preserve the site-plan visual/CAD metadata as much as possible instead of reconstructing a simplified version.
