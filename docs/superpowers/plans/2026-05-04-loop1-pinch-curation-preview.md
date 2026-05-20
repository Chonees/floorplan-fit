# Loop 1 Pinch Curation Preview Implementation Plan


> [!WARNING] SUPERSEDED ? 2026-05-09
> This implementation plan is historical. Do not execute it as written.
>
> The plan uses auto-staged curated walls and `SyncCuratedWallsFromCandidatesHandler` as transitional implementation devices. The current branch removed the curated-wall review flow and uses named pinch groups/markers plus separated CAD artifact families.


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the accept-first review UI with a pinch-first curation prototype that persists grouped pinches and previews grouped floor-plan compression.

**Architecture:** Keep `FloorPlanCuration` as the review container, auto-stage curated walls behind the scenes from non-rejected candidates, add first-class pinch groups/pinches, and let Desktop preview grouped compression with a simple axis-based transform. This keeps the prototype aligned to current Loop 1 infrastructure while aggressively simplifying the UX.

**Tech Stack:** C# .NET 10, Avalonia UI, CommunityToolkit.Mvvm, SQLite, xUnit

---

### Task 1: Add domain and contract primitives for pinch curation

**Files:**
- Create: `src/FloorplanFit.Domain/FloorPlans/PinchAxisTag.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/PinchGroup.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/PinchMarker.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/PinchGroupDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/PinchMarkerDto.cs`
- Modify: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Review/GetFloorPlanReviewSessionHandlerTests.cs`

- [ ] Write failing tests that expect review sessions to expose pinch groups and pinch markers.
- [ ] Run the targeted application review tests and confirm RED.
- [ ] Add the new domain records/entities and contract DTOs with minimal validation.
- [ ] Re-run the targeted tests and confirm GREEN or compiler-driven next failures.

### Task 2: Persist pinch groups and pinch markers in SQLite

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/IPinchGroupRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IPinchMarkerRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqlitePinchGroupRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqlitePinchMarkerRepository.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Review/FloorPlanReviewSessionReaderIntegrationTests.cs`

- [ ] Write failing integration tests for pinch-group/pinch-marker round-trip through the review session reader.
- [ ] Run the targeted infrastructure tests and confirm RED.
- [ ] Add schema tables and SQLite repositories.
- [ ] Load pinch groups and pinch markers through the session reader.
- [ ] Re-run the targeted tests and confirm GREEN.

### Task 3: Auto-stage curated walls and add pinch handlers

**Files:**
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/SyncCuratedWallsFromCandidatesHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/CreatePinchGroupHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/AddPinchMarkerHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/RemovePinchMarkerHandler.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Review/OpenFloorPlanReviewSessionHandler.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Curation/RejectWallCandidateHandler.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Review/OpenFloorPlanReviewSessionHandlerTests.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/PublishFloorPlanCurationHandlerTests.cs`

- [ ] Write failing tests for review-open auto-staging and pinch creation/removal behavior.
- [ ] Run the targeted application tests and confirm RED.
- [ ] Implement the auto-staging sync and pinch handlers with the smallest possible API.
- [ ] Update reject behavior to remove staged walls + pinches for rejected candidates.
- [ ] Re-run the targeted tests and confirm GREEN.

### Task 4: Add pure preview-compression geometry helpers

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`

- [ ] Write failing desktop geometry tests for grouped width/height compression distribution and capped trim behavior.
- [ ] Run the targeted desktop geometry tests and confirm RED.
- [ ] Implement pure helper methods that compute pinch coordinates, distribute requested trim, and transform path segments for preview.
- [ ] Re-run the targeted tests and confirm GREEN.

### Task 5: Extend the preview control for pinch rendering and drag preview

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`

- [ ] Write failing tests for pinch marker placement defaults or geometry helper inputs if direct control testing is too heavy.
- [ ] Run the targeted desktop geometry tests and confirm RED.
- [ ] Add preview-control properties/events for pinch groups, pinch markers, active group selection, and drag-based preview reduction.
- [ ] Render pinch markers and preview-transformed geometry.
- [ ] Re-run the targeted tests and confirm GREEN.

### Task 6: Simplify the review view model to pinch-first UX

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: `src/FloorplanFit.Contracts/FloorPlans/OpenFloorPlanReviewSessionResponse.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

- [ ] Write failing view-model tests for pinch-group creation, pinch add/remove, and preview state updates.
- [ ] Run the targeted desktop view-model tests and confirm RED.
- [ ] Remove accept/save-metadata-centric state from the VM and add pinch-first state/actions.
- [ ] Re-run the targeted view-model tests and confirm GREEN.

### Task 7: Replace the review window with the necessary prototype UI only

**Files:**
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

- [ ] Write failing layout tests that assert the new pinch-group area and simpler action surface exist.
- [ ] Run the targeted layout tests and confirm RED.
- [ ] Replace the old inspector-heavy XAML with a pinch-first layout.
- [ ] Wire the code-behind to the new view-model actions and preview events.
- [ ] Re-run the targeted layout tests and confirm GREEN.

### Task 8: Final sequential verification and documentation

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-05-04 - Pinch curation preview prototype.md`

- [ ] Run `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter FullyQualifiedName~FloorPlans` and confirm GREEN.
- [ ] Run `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter FullyQualifiedName~Review` and confirm GREEN.
- [ ] Run `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --filter FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~FloorPlanPreviewGeometryTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests` and confirm GREEN.
- [ ] Update Obsidian notes with the prototype behavior and tradeoffs.
- [ ] Review final diff for only relevant prototype files.
