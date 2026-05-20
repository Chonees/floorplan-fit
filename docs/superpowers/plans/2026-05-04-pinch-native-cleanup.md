# Pinch-Native Cleanup Implementation Plan


> [!IMPORTANT] PARTIALLY SUPERSEDED ? 2026-05-09
> This plan remains useful as historical context for deleting the curated-wall review flow.
>
> Do not execute the ?remove pinch groups? parts as written. Named pinch groups are active again and are required so fit can trim specific zones like Patio/Garage/Bedroom side instead of all Width/Height markers together.


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the curated-wall review flow with a pinch-native review flow that stores only rejected candidates and axis-tagged pinch markers.

**Architecture:** Keep `FloorPlanCuration` as the draft/publish envelope, but remove curated walls and pinch groups from the active Loop 1 review model. Persist pinches directly against candidates/geometries and simplify the Desktop UI to add/remove pinches plus width/height preview handles.

**Tech Stack:** C#/.NET 10, Avalonia UI, SQLite, xUnit

---

### Task 1: Rewrite the review contract around pinch markers only

**Files:**
- Modify: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- Modify: `src/FloorplanFit.Contracts/FloorPlans/PinchMarkerDto.cs`
- Delete: `src/FloorplanFit.Contracts/FloorPlans/CuratedWallDto.cs`
- Delete: `src/FloorplanFit.Contracts/FloorPlans/PinchGroupDto.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

- [ ] Step 1: Write failing tests that construct `FloorPlanReviewSessionDto` without `CuratedWalls`/`PinchGroups` and expect `PinchMarkers` with `AxisTag`
- [ ] Step 2: Run the targeted Desktop ViewModel test to verify compile/test failure
- [ ] Step 3: Update the DTOs so the review session carries only geometry, candidates, and pinch markers
- [ ] Step 4: Re-run the targeted test and verify it passes or moves to the next expected failure

### Task 2: Make pinch persistence candidate-native and remove pinch groups

**Files:**
- Modify: `src/FloorplanFit.Domain/FloorPlans/PinchMarker.cs`
- Keep: `src/FloorplanFit.Domain/FloorPlans/PinchAxisTag.cs`
- Delete: `src/FloorplanFit.Domain/FloorPlans/PinchGroup.cs`
- Modify: `src/FloorplanFit.Application/Abstractions/IPinchMarkerRepository.cs`
- Delete: `src/FloorplanFit.Application/Abstractions/IPinchGroupRepository.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Curation/AddPinchMarkerHandler.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Curation/RemovePinchMarkerHandler.cs`
- Delete: `src/FloorplanFit.Application/FloorPlans/Curation/CreatePinchGroupHandler.cs`
- Delete: `src/FloorplanFit.Application/FloorPlans/Curation/SyncCuratedWallsFromCandidatesHandler.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AddPinchMarkerHandlerTests.cs`

- [ ] Step 1: Write failing Application tests expecting `AddPinchMarkerHandler` to save `SourceCandidateId` + `AxisTag` without any `CuratedWall`/group lookup
- [ ] Step 2: Run the targeted Application pinch test to verify RED
- [ ] Step 3: Refactor the domain object, repository contract, and add/remove handlers to the pinch-native shape
- [ ] Step 4: Re-run the targeted Application pinch test and verify GREEN

### Task 3: Remove curated-wall review flow from handlers and publish rules

**Files:**
- Modify: `src/FloorplanFit.Application/FloorPlans/Curation/RejectWallCandidateHandler.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Curation/PublishFloorPlanCurationHandler.cs`
- Delete: `src/FloorplanFit.Application/FloorPlans/Curation/AcceptWallCandidateHandler.cs`
- Delete: `src/FloorplanFit.Application/FloorPlans/Curation/UpdateCuratedWallMetadataHandler.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Review/OpenFloorPlanReviewSessionHandler.cs`
- Delete: `src/FloorplanFit.Application/Abstractions/ICuratedWallRepository.cs`
- Delete: `src/FloorplanFit.Domain/FloorPlans/CuratedWall.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/RejectWallCandidateHandlerTests.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/PublishFloorPlanCurationHandlerTests.cs`

- [ ] Step 1: Write failing tests asserting reject removes pinch markers by candidate and publish now requires at least one pinch marker
- [ ] Step 2: Run the targeted Application curation tests to verify RED
- [ ] Step 3: Implement the minimal handler changes and remove dead curated-wall flow files
- [ ] Step 4: Re-run the targeted Application curation tests to verify GREEN

### Task 4: Simplify SQLite schema/read model/repositories

**Files:**
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqlitePinchMarkerRepository.cs`
- Delete: `src/FloorplanFit.Infrastructure/Persistence/SqliteCuratedWallRepository.cs`
- Delete: `src/FloorplanFit.Infrastructure/Persistence/SqlitePinchGroupRepository.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Review/FloorPlanReviewSessionReaderIntegrationTests.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Review/OpenFloorPlanReviewSessionIntegrationTests.cs`

- [ ] Step 1: Write failing integration tests for review-session loading and pinch-marker persistence without curated walls/groups
- [ ] Step 2: Run the targeted Infrastructure tests to verify RED
- [ ] Step 3: Implement schema/repository/reader changes and remove dead registrations
- [ ] Step 4: Re-run the targeted Infrastructure tests to verify GREEN

### Task 5: Rebuild the Desktop review experience as a minimal pinch board

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

- [ ] Step 1: Write failing Desktop tests for axis selection, add/remove pinch, simplified labels, and visible width/height handle behavior
- [ ] Step 2: Run the targeted Desktop tests to verify RED
- [ ] Step 3: Rewrite the ViewModel/window/control to the minimal pinch-native UX
- [ ] Step 4: Re-run the targeted Desktop tests to verify GREEN

### Task 6: Final cleanup and verification

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Add/Modify: implementation/bug notes under `obsidian-vault/`

- [ ] Step 1: Delete any remaining dead curated-wall review tests/files that are no longer part of the branch truth
- [ ] Step 2: Run `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --no-restore`
- [ ] Step 3: Run `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --no-restore`
- [ ] Step 4: Run `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --no-restore`
- [ ] Step 5: Update `Current State` and branch notes to reflect pinch-native curation as the new experimental truth
