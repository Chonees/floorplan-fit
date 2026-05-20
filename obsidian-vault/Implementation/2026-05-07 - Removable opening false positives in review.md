---
project: floorplan-fit
type: implementation
date: 2026-05-07
status: implemented
replaces: []
replaced_by:
---

# Removable opening false positives in review

## What

The Review UI can now remove false-positive opening geometry and opening labels from the persisted extraction result.

## Why

Some DXF entities are real source data but are not useful for the future fit engine as protected doors/windows. The user called these visual hallucinations: they should be removable by the curator and should not keep rendering or persist as curation input.

## Where

- `src/FloorplanFit.Application/FloorPlans/Curation/RemoveOpeningCandidateHandler.cs`
  - Removes a selected opening geometry candidate.
- `src/FloorplanFit.Application/FloorPlans/Curation/RemoveOpeningLabelHandler.cs`
  - Removes a selected opening label.
- `src/FloorplanFit.Application/Abstractions/IExtractedOpeningCandidateRepository.cs`
  - Adds `RemoveAsync` for opening candidates.
- `src/FloorplanFit.Application/Abstractions/IExtractedOpeningLabelRepository.cs`
  - Adds `RemoveAsync` for opening labels.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedOpeningCandidateRepository.cs`
  - Deletes the opening candidate and its `geometry_segments` / `geometry_paths` rows.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedOpeningLabelRepository.cs`
  - Deletes the opening label row.
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
  - Adds selected opening candidate/label state and remove commands.
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
  - Adds separate selectable lists for opening geometry and opening labels plus remove buttons.

## UX behavior

1. In `Openings`, select a suspicious opening geometry.
2. Click `Remove Selected Opening`.
3. The row is deleted from SQLite and the preview stops drawing that geometry.
4. Select a suspicious opening label.
5. Click `Remove Selected Label`.
6. The label row is deleted from SQLite and the preview stops drawing that label.

## Verification

- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj` -> 22/22 passed.
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj` -> 32/32 passed.
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj` -> 37/37 passed.
- `git diff --check` -> exit 0.

## Notes

This removes false positives from the current persisted extraction. If `Extract Walls` is run again, the extractor can recreate source-derived candidates; long-term suppression across re-extraction would need a separate ignore-list keyed by source entity ref or geometry fingerprint.
