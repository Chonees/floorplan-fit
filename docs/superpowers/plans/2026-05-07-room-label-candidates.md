# Room Label Candidates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring DXF room names into Loop 1 review as extracted room label candidates, separate from wall candidates.

**Architecture:** Add a dedicated room-label extraction path beside wall extraction. Room labels are text candidates with position and source layer, persisted in SQLite and exposed through the review read model/UI without attempting room boundary inference.

**Tech Stack:** C#/.NET 10, IxMilia.Dxf, SQLite, Avalonia MVVM, xUnit.

---

### Task 1: Extract room label candidates from DXF

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/DetectedRoomLabel.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IRoomLabelExtractor.cs`
- Create: `src/FloorplanFit.Infrastructure/Dxf/IxMiliaRoomLabelExtractor.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaRoomLabelExtractorTests.cs`

- [ ] Write a failing test that extracts `KITCHEN`, `LIVING ROOM`, and `BEDROOM 2` from `ROOM LBLS`.
- [ ] Implement a DXF text extractor over `DxfText`, keeping only room-label layers.
- [ ] Normalize whitespace and skip non-room utility text.
- [ ] Run the extractor test and verify it passes.

### Task 2: Persist room labels

**Files:**
- Create: `src/FloorplanFit.Domain/FloorPlans/ExtractedRoomLabel.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IExtractedRoomLabelRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedRoomLabelRepository.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs`

- [ ] Write a failing persistence test proving labels are stored and loaded by extraction run.
- [ ] Create `extracted_room_labels`.
- [ ] Insert labels during extraction.
- [ ] Run persistence tests.

### Task 3: Expose room labels in review

**Files:**
- Create: `src/FloorplanFit.Contracts/FloorPlans/RoomLabelDto.cs`
- Modify: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- Modify tests that construct `FloorPlanReviewSessionDto`

- [ ] Write a failing read-model test proving Review loads room labels.
- [ ] Add `RoomLabels` to the review DTO.
- [ ] Read labels for the latest extraction run.
- [ ] Update affected tests.

### Task 4: Show rooms in Desktop Review

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Test: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

- [ ] Write a failing ViewModel test proving room labels populate.
- [ ] Add a `RoomLabels` collection to the ViewModel.
- [ ] Add a `Rooms` panel/list in the Review UI.
- [ ] Run Desktop tests.
