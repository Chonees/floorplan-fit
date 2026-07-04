---
type: Implementation
date: 2026-06-03
project: floorplan-fit
status: implemented
tags:
  - floorplan-fit
  - loop1
  - desktop
  - application
  - infrastructure
  - edit-mode
---

# Published curation edit flow creates copied draft

## Summary

The Review window now has an explicit **Editar** action for published curations. Published sessions open read-only and show the latest published Fit data. Clicking **Editar** creates/resumes an editable draft, clones published Fit rows into that draft, and reloads Review against the draft curation.

## Changed areas

- Application: added `EditPublishedFloorPlanCurationHandler` and `IFloorPlanCurationDataCloneService`.
- Infrastructure: added `SqliteFloorPlanCurationDataCloneService` and curation-specific Review reads.
- Desktop: added `CanEditPublishedCuration`, `CanPublishCuration`, `StartEditingPublishedCurationAsync`, and the `Editar` header button.
- Session refresh: when a draft exists, refresh reads by curation id; when the requested curation is now `Published`, the coordinator clears the draft id.

## Verification

- `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --nologo --verbosity minimal` → 87/87 passed.
- `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --nologo --verbosity minimal` → 78/78 passed.
- `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --verbosity minimal --artifacts-path .testartifacts\desktop-full` → 192/192 passed.
- `git diff --check` → exit 0; only LF→CRLF warnings.

## Notes

The Desktop app was running and locking the normal output executable, so Desktop tests used a temporary artifacts path instead of killing the user's app process.

## 2026-06-03 follow-up fix

The first implementation skipped cloning if the edit draft had any curation data. That was too broad: an unrelated override row blocked Fit-owned rows. The clone now only skips Fit-owned copying when the destination already has Fit-owned rows, and `EditPublishedFloorPlanCurationHandler` commits the full edit operation after cloning.
