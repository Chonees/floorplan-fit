---
type: implementation
date: 2026-05-09
project: floorplan-fit
tags:
  - loop-1
  - curation
  - walls
  - review
---

# Subtractive wall candidate curation

## What changed

Wall candidate curation now follows a subtractive workflow:

- newly extracted wall candidates are persisted as `Accepted` by default
- Review UI copy explains that lines are accepted by default
- the user rejects only false-positive wall lines
- rejecting an accepted wall candidate is valid and still removes associated pinch markers from the draft
- the review read-model excludes `Rejected` wall candidates and their geometry from the active preview/list
- rejected candidates remain persisted as audit/history instead of becoming active fit input

## Why

The admin workflow is not to approve hundreds of CAD line fragments one by one. The DXF extraction proposes the active wall set, and the human only subtracts what is wrong. That matches the product goal: fast CAD-faithful curation before publishing a reusable floor plan.

## Files touched

- `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs`
- `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidate.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.*` wall/review/curation tests
- `MVP-UX.md`
- `TECH-STACK-ARCHITECTURE-DATAFLOW.md`

## Important rule

Seeing geometry in Review means it is part of the persisted extraction/review draft surface, not automatically a published curated floor plan. Published curation is still created only by `Publish Curation`; Loop 2 must consume published curation plus active/non-rejected artifacts.
