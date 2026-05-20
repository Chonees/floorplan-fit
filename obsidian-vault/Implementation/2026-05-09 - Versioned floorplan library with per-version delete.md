---
type: Implementation
date: 2026-05-09
status: active
tags:
  - library
  - versions
  - curation
  - desktop-ui
---

# Versioned floorplan library with per-version delete

## What

The Library now treats a floor plan template as the parent record and exposes each imported `FloorPlanVersion` as a child row. Re-importing the same plan no longer only increments a hidden/current version number; the UI can show every version and each version has its own Open and Delete action.

## Why

Admins need to import and compare multiple DXF attempts for the same Pointe floorplan without losing track of which imported version they are reviewing or curating. Version-level delete is required because bad/repeated imports should be removable without deleting the whole template family.

## Where

- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryItemDto.cs`
- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryVersionDto.cs`
- `src/FloorplanFit.Application/FloorPlans/Library/RemoveFloorPlanVersionHandler.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Desktop/MainWindow.axaml`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionRepository.cs`

## Learned

- The previous Library reader collapsed the template to `current_version_id`, so the UI could only show one active version even when the database had many versions.
- Opening review by template alone is not enough once older imports are selectable; review loading now needs a version-aware path.
- Deleting a version must clean dependent curation/extraction rows and then promote the latest remaining version if the deleted version was current.
- The current deletion implementation removes SQLite/product rows but does not yet delete the physical managed DXF file from disk.

## UX Follow-up: visible delete and black/white chrome

After runtime feedback, the per-version delete action was made visible from the main Library toolbar as `Delete Selected Version`. Version rows now include `Select`, `Open`, and `Delete`, and the selected version is shown as a first-class label.

The main Library stopped using a selectable `ListBox` so Avalonia's default blue selected row no longer appears. Desktop chrome was pushed to black background with white text/borders; generated preview handles/markers use black/white defaults instead of blue/green accents.

