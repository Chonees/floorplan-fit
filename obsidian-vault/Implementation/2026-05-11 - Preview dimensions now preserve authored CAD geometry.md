# 2026-05-11 - Preview dimensions now preserve authored CAD geometry

## What

Disabled the live preview path that re-projected native dimensions through inferred wall/opening associations on every render. Review preview now keeps the extracted CAD-authored dimension primitives 1:1 unless the user is actively editing that dimension handle, in which case only the edited dimension is rebuilt and re-measured.

## Why

The reactive association path was destructively deforming authored dimension geometry away from the original DXF because it rebuilt dimensions from simplified measurable edges and generated primitive templates instead of leaving the imported `*D...` block geometry intact.

## Where

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewProjector.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`

## Learned

- The destructive behavior was not the manual remeasurement logic; it was the preview layer automatically invoking `ReactiveDimensionProjector` whenever `DimensionAssociations` existed, even with no active geometry edit.
- For CAD-fidelity, inferred associations are useful as future curation metadata, but they are too lossy to be the default render source for imported native dimensions.
