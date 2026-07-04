---
type: Implementation
date: 2026-05-07
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop1
  - openings
  - review-ui
---

# Preview-selectable openings for false positive removal

## What changed

Opening geometry is now selectable directly from the Review preview canvas, the same way wall lines are selectable.

## Why

Opening false positives were removable from the right-side `Openings` list, but the UX was still indirect: the user visually saw the bad door/window on the plan and then had to find the matching row manually. For curation, the visual object should be the primary interaction target.

## Current behavior

- The preview hit-test includes opening geometry instead of filtering it out.
- Opening paths are ordered before wall paths for hit-testing, so a door/window drawn on top of a wall wins the click when distances tie.
- Selecting an opening from the canvas sets `SelectedOpeningCandidate`.
- The selected opening is highlighted in orange/red on the preview.
- The interaction hint tells the user to press `Remove Selected Opening` when the selected opening is a false positive.

## Where

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop` -> 39/39
- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .\.artifacts-test\application` -> 22/22
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .\.artifacts-test\infrastructure` -> 32/32
- `git diff --check` -> exit 0

## Gotcha

When `dotnet watch` has the Desktop app running, normal test output under `bin/Debug` can be locked by `FloorplanFit.Desktop.exe`. Using `dotnet test --artifacts-path .\.artifacts-test\...` keeps verification outputs under the repo root, avoids the lock, and still lets tests that search upward find `FloorplanFit.sln`.
