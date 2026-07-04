# Pinch Group Naming Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let users name pinch groups when creating them and rename existing groups from the Loop 1 edit/review preview.

**Architecture:** Keep group identity stable in Domain/Application/Infrastructure and place the naming interaction only in Desktop. Creation receives an explicit name; rename updates only `pinch_groups.name` while preserving id, curation, axis, sort order, markers, and downstream Loop 2 references.

**Tech Stack:** C#/.NET, Avalonia UI, CommunityToolkit MVVM, SQLite persistence, xUnit.

---

### Task 1: Application rename use case

**Files:**
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/RenamePinchGroupHandlerTests.cs`
- Modify: `src/FloorplanFit.Application/Abstractions/IPinchGroupRepository.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/RenamePinchGroupHandler.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqlitePinchGroupRepository.cs`
- Modify test fakes implementing `IPinchGroupRepository`

- [ ] Write failing tests proving rename trims the new name and preserves id/axis/sort.
- [ ] Run focused Application tests and confirm RED from missing handler/repository method.
- [ ] Add repository `UpdateAsync(PinchGroup group, CancellationToken)` and SQLite update implementation.
- [ ] Add `RenamePinchGroupHandler` with draft/ownership validation.
- [ ] Run focused Application tests and confirm GREEN.

### Task 2: Desktop ViewModel commands

**Files:**
- Test: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

- [ ] Write failing tests proving explicit create name is used and selected group can be renamed.
- [ ] Run focused Desktop ViewModel tests and confirm RED.
- [ ] Change create command to accept explicit name while keeping `SuggestedPinchGroupName` for the dialog default.
- [ ] Add `CanRenameSelectedPinchGroup` and `RenameSelectedPinchGroupAsync`.
- [ ] Register/invoke `RenamePinchGroupHandler` through the mutation coordinator.
- [ ] Run focused Desktop ViewModel tests and confirm GREEN.

### Task 3: Avalonia popup and UI hooks

**Files:**
- Create: `src/FloorplanFit.Desktop/PinchGroupNameDialog.axaml`
- Create: `src/FloorplanFit.Desktop/PinchGroupNameDialog.axaml.cs`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

- [ ] Write failing source/layout tests for the rename button and naming dialog hooks.
- [ ] Run focused Desktop layout tests and confirm RED.
- [ ] Add a small modal dialog with TextBox, Cancel, and Save validation.
- [ ] Wire `Crear grupo de pinches` to open the dialog with suggested `Ajuste N`.
- [ ] Wire `Renombrar grupo` to open the same dialog with current name.
- [ ] Run focused Desktop layout tests and confirm GREEN.

### Task 4: Verification and documentation

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-06-10 - Pinch group naming and renaming.md`

- [ ] Run focused Application/Desktop tests touched by the change; do not run standalone build.
- [ ] Run `git diff --check`.
- [ ] Update Obsidian Current State and Implementation note.
- [ ] Save Engram shadow memory.
