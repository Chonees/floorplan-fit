---
type: implementation
project: floorplan-fit
date: 2026-05-06
topic_key: implementation/wall-thickness-inference
status: implemented
---

# Geometry-based wall thickness inference

## What changed

Loop 1 extraction now infers likely wall assembly thickness from the DXF geometry itself.

## Why

The floor plan walls do not carry reliable per-entity notes or colors for 2x4 vs 2x6. The verified signal in the current DXF fixtures is spacing between parallel overlapping wall faces:

- near `4"` -> likely `2x4`
- near `6"` -> likely `2x6`

## Code

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaWallExtractor.cs`
  - detects axis-aligned physical wall faces from `WALLS`
  - ignores `ELECTRICAL WALLS` for thickness inference
  - pairs parallel overlapping faces
  - stores `ThicknessMm` as `101.6` or `152.4`
  - appends detection notes explaining the inference
- `src/FloorplanFit.Contracts/FloorPlans/WallCandidateDto.cs`
  - exposes `AssemblyHint` for UI display
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
  - shows the assembly hint in the line list
- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaWallExtractorTests.cs`
  - covers 4"/6" inference from `SEMINOLE2000.dxf`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/WallCandidateDtoTests.cs`
  - covers the displayed assembly hint labels

## Verification

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --no-restore` -> 20/20
- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --no-restore` -> 22/22
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --no-restore` -> 21/21

## Caveat

This is an inferred hint, not immutable truth. Future curation should let the user override the assembly when the geometry heuristic is wrong.
