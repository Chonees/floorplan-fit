---
project: floorplan-fit
type: bugfix
date: 2026-05-07
status: fixed
replaces: []
replaced_by:
---

# Opening labels rendered white and included non-opening notes

## What

Opening labels in the review preview now render black for readability, and the opening label extractor ignores non-opening notes that happened to live on the DXF `DOORTEXT` layer.

## Why

`SEMINOLE2000.dxf` has several `DOORTEXT` entities with ACI white / near-white colors and non-opening note text such as `STAND`, `TUB`, `WTR`, `DRAIN`, and safety-glass notes. Preserving those colors made labels invisible on the light workspace, and preserving every text entity from `DOORTEXT` polluted the opening overlay with notes that are not door/window model or size labels.

## Where

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaOpeningExtractor.cs`
  - Added a model/size label filter for opening labels.
  - Keeps labels such as `2668`, `24"DR.`, `27" R.O.`, `3050 S.H.`, and `(3) 3050 FXD. HDR. @ 6'-8"`.
  - Drops non-opening notes on the same layer.
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
  - Forces room/opening preview label text to black while preserving DXF position, size, rotation, alignment, and baseline.

## Verification

- RED reproduced:
  - `IxMiliaOpeningExtractorTests.ExtractAsync_ignores_non_opening_notes_that_share_door_text_layer` failed on labels `WTR`, `LINES`, `STAND`, `TUB`, `DRAIN`.
  - Desktop black-label tests initially failed / were blocked by a running Desktop process lock, then rerun after stopping it.
- GREEN verified:
  - `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaOpeningExtractorTests"` -> 4/4 passed.
  - `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj` -> 31/31 passed.
  - `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj` -> 37/37 passed.
  - `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj` -> 20/20 passed.
  - `git diff --check` -> exit 0.

## Notes

The apparent isolated shower/door symbol in the master bath area comes from the DXF `DOORS`/`DOORTEXT` data itself (`24"DR.` / `27" R.O.` nearby). This change prevents unrelated note text from making that area look like random invisible/white labels, but does not remove actual DOORS geometry from the source plan.
