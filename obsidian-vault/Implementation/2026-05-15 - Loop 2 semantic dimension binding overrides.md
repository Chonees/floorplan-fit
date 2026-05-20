---
project: floorplan-fit
repo: https://github.com/Chonees/floorplan-fit
status: active
updated: 2026-05-15
loop: 2
area: dimensions
---

# 2026-05-15 - Loop 2 semantic dimension binding overrides

## What
Implemented Loop 2 for native dimensions: endpoint edits can now persist semantic binding overrides, body drags stay cosmetic, review-session hydration overlays manual binding overrides onto canonical `DimensionBindings`, and reactive projection no longer skips edited dimensions when resolved associations exist.

## Why
Loop 1 made linear dimensions react during live pinch preview, but edited dimensions still risked falling out of the associative system because semantic rebinding was not persisted or rehydrated.

## Files
- `src/FloorplanFit.Domain/FloorPlans/FloorPlanDimensionBindingOverride.cs`
- `src/FloorplanFit.Application/Abstractions/IFloorPlanDimensionBindingOverrideRepository.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/SaveFloorPlanDimensionOverrideHandler.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/RestoreFloorPlanDimensionOverrideHandler.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/ReactiveDimensionProjector.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanDimensionBindingOverrideRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/ResolvedFloorPlanDimensionBindingProjector.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

## Verification
- `dotnet test .\\tests\\FloorplanFit.Application.Tests\\FloorplanFit.Application.Tests.csproj --filter "FloorPlanDimensionOverrideHandlersTests|ReactiveDimensionProjectorTests"`
- `dotnet test .\\tests\\FloorplanFit.Infrastructure.Tests\\FloorplanFit.Infrastructure.Tests.csproj --filter "FloorPlanDimensionBindingOverridePersistenceIntegrationTests|FloorPlanReviewSessionReaderIntegrationTests|DimensionOverrideReviewSessionIntegrationTests"`
- `dotnet test .\\tests\\FloorplanFit.Desktop.Tests\\FloorplanFit.Desktop.Tests.csproj --filter "DimensionEditingFloorPlanReviewViewModelTests|FloorPlanReviewViewModelTests|NativeDimensionPreviewControlTests|FloorPlanPreviewControlTests|PreviewInteractionCoordinatorTests"`

## Notes
- Manual binding overrides persist as semantic truth parallel to geometric dimension overrides.
- `DimensionAssociations` remains a compatibility layer; when a manual binding override exists, the review session now projects the association from that binding.
- Desktop test runs can still require killing a stray `FloorplanFit.Desktop.exe` lock before execution.
