---
type: Bug
date: 2026-06-01
project: floorplan-fit
status: superseded
tags:
  - floorplan-fit
  - loop1
  - fit-preview
  - height-axis
  - visual-contrast
---

# Height compression handles visually invisible on dark workspace

## Verified root cause

The previous Height-axis fix made the preview state coherent: when a valid Height pinch group/marker exists, the renderer can calculate the top and bottom compression handles.

The remaining UI failure was visual: `PreviewSemanticPalette.HandleStroke` was near-black (`ARGB 220,0,0,0`) and the preview workspace background is dark navy (`#0F172A`). The RED contrast test measured only `1.16:1`, so the top/bottom handles could be technically rendered but effectively invisible to the operator. This also contradicted the inspector hint that tells the user to drag the green handle.

## Fixed

Compression handles now use the shared selection green as stroke and a translucent green fill. This keeps the renderer contract unchanged while making Width and Height handles visible on the dark CAD-like workspace.

## Verification

- RED: `Compression_handle_stroke_is_visible_against_the_dark_preview_workspace` failed with `1.16:1` contrast.
- GREEN: `PreviewSemanticPaletteTests` passed 3/3 after switching handles to green.
- Focused Desktop slice passed 101/101:
  `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~PreviewSemanticPaletteTests|FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~FloorPlanReviewViewModelTests" --artifacts-path .testartifacts\dotnet-test-artifacts-height-handles-visible-desktop-slice`
- `git diff --check` exited 0 with only line-ending warnings.

## Superseded

This note was superseded on 2026-06-01 after the user clarified that the symptom was not handle contrast: Width/right-left handles were still appearing while Height/top-bottom handles were not. The palette color change was reverted. The active root-cause track is axis-state divergence between the measurement corridor axis selector and the preview/pinch axis, plus the existing requirement for Height pinch markers before Height compression handles can drive a preview.
