---
type: Implementation
project: floorplan-fit
date: 2026-05-07
status: current
replaces:
replaced_by:
---

# Room label baseline alignment fix

## What

Room label text origin now respects DXF text baseline/alignment semantics instead of using total rendered text height for every default `TEXT` label.

## Why

The labels were visibly closer after DXF-like rendering, but still not exactly centered/placed because default AutoCAD `TEXT` insertion points are baseline-based. The previous render offset used `FormattedText.Height`, which shifts the text vertically relative to the DXF insertion point.

## Where

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`

## Behavior

- Default `TEXT` now positions top-left as `anchor.Y - text.Baseline`, matching baseline insertion semantics more closely.
- Middle/center aligned text still uses width/height centering.
- Top/bottom attachment semantics are handled separately for MTEXT-like labels.
- Tests cover baseline and middle-centered origin math directly.

## Important limitation

This fixes our baseline math. If the text still differs visually from AutoCAD, the remaining cause is likely font metrics: the DXF style uses `SWIS72`, and if that exact font/SHX is unavailable, Avalonia will substitute a different font, changing rendered width and therefore perceived centering.

## Verification

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj` -> 20/20
- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj` -> 26/26
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj` -> 26/26
- `git diff --check` -> exit 0

## Links

- [[Current State]]
- [[Implementation/2026-05-07 - DXF-like room label rendering]]
