# Manual Wall Line Tool Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Loop 1 review/edit tool that lets the user create straight manual wall lines in the floor-plan preview.

**Architecture:** Manual wall lines are persisted as accepted wall candidates on the latest extraction run, not as temporary preview-only geometry. Desktop owns the click workflow and preview ghost; Application owns validation and persistence orchestration; Infrastructure writes `extracted_wall_candidates` plus `geometry_paths/geometry_segments`.

**Tech Stack:** Avalonia, CommunityToolkit.Mvvm, .NET 10, SQLite repositories, xUnit.

---

### Task 1: Application contract

**Files:**
- Modify: `src/FloorplanFit.Application/Abstractions/IExtractedWallCandidateRepository.cs`
- Modify: `src/FloorplanFit.Application/Abstractions/IWallExtractionRunRepository.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/AddManualWallCandidateHandler.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AddManualWallCandidateHandlerTests.cs`

- [ ] RED: test handler creates one accepted manual wall candidate with two geometry points and next sort order.
- [ ] GREEN: add repository methods and handler.

### Task 2: SQLite persistence

**Files:**
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedWallCandidateRepository.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteWallExtractionRunRepository.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs`

- [ ] RED: test manual wall candidate persists geometry and reloads through repository/review reader.
- [ ] GREEN: implement `AddAsync`, `GetNextSortOrderAsync`, and latest extraction run lookup.

### Task 3: Desktop workflow and preview

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs`
- Create: `src/FloorplanFit.Desktop/Controls/Preview/ManualWallLinePreviewLayerRenderer.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- Test: focused Desktop XAML/ViewModel/Control tests.

- [ ] RED: layout/binding test expects `Agregar pared`, armed binding, draft binding, and preview events.
- [ ] GREEN: implement two-click tool, ghost line, handler registration, refresh/select created wall.

### Task 4: Verification and docs

- [ ] Run focused Application/Desktop/Infrastructure tests with `--artifacts-path`.
- [ ] Run `git diff --check`.
- [ ] Update Obsidian Current State and Implementation/Bug note.
