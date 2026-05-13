# Loop 3B Selection State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract selection-state orchestration out of `FloorPlanReviewViewModel` so the ViewModel stops mixing selection routing truth, snapshot replay, and selection presentation side effects in one giant Desktop shell.

**Architecture:** Keep `FloorPlanReviewViewModel` as the owner of observable properties and final property assignment. Introduce a focused Desktop collaborator for selection truth and split the work into two internal sub-slices: **Loop 3B1 routing/snapshots** first, then **Loop 3B2 presentation side effects**.

**Tech Stack:** C# / .NET 10, CommunityToolkit MVVM, xUnit Desktop tests, PowerShell verification.

---

## File Structure

- Create: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSelectionCoordinator.cs`
  - Purpose: own selection routing, snapshot capture/replay, and later selection presentation outcomes.
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
  - Purpose: delegate selection truth while remaining the Desktop state shell.
- Create or modify: focused tests under `tests/FloorplanFit.Desktop.Tests/ViewModels/`
  - Purpose: pin the routing seam first and then the presentation seam.
- Modify: `obsidian-vault/Current State.md`
  - Purpose: record the chosen Loop 3B seam and the next slice after it.
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 3B selection state extraction.md`
  - Purpose: durable implementation note when the loop closes.

---

## Tasks

### Task 1: Add failing seam tests for Loop 3B1 routing and snapshot truth

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
- Modify: focused existing selection-heavy ViewModel tests

- [ ] **Step 1: Tighten the source-facing architecture guard**

Extend `FloorPlanReviewViewModelArchitectureTests` so the final source must:
- reference `FloorPlanReviewSelectionCoordinator`
- stop owning the bulk of preview-hit routing inline
- stop owning the bulk of snapshot capture/replay inline

At minimum, target these seams:
- `SelectPreviewPath(...)`
- `CaptureSelection(...)`
- `BuildSelectionSnapshotForMovedArtifact(...)`
- replay of `ReviewSelectionSnapshot` inside `ApplySession(...)`

- [ ] **Step 2: Add or tighten runtime tests around selection routing**

Use existing selection-facing tests as the starting surface:
- `FloorPlanReviewViewModelTests.SelectPreviewPath_selects_the_matching_candidate_line`
- `FloorPlanReviewViewModelTests.SelectPreviewPath_selects_the_matching_opening_geometry`
- `FloorPlanReviewViewModelTests.SelectPreviewPath_selects_the_matching_fixed_plan_component_geometry`
- `FloorPlanReviewViewModelTests.SelectPreviewPath_selects_the_matching_protected_detail_assembly_geometry`
- `CuratedArtifactFloorPlanReviewViewModelTests.SelectPreviewPath_selects_the_matching_curated_artifact_geometry`
- moved-artifact refresh tests in `MovableArtifactFloorPlanReviewViewModelTests`
- dimension restore persistence in `DimensionEditingFloorPlanReviewViewModelTests`

Add any missing focused test only if the current suite does not already pin the required routing behavior.

- [ ] **Step 3: Run the focused tests to verify RED**

Run a focused command covering the architecture seam test plus the selection-routing runtime suites.

Expected: FAIL because `FloorPlanReviewViewModel` still owns the routing/snapshot seam inline.

---

### Task 2: Implement Loop 3B1 by extracting routing and snapshot logic

**Files:**
- Create: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSelectionCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: focused tests under `tests/FloorplanFit.Desktop.Tests/ViewModels/`

- [ ] **Step 1: Introduce the smallest useful routing collaborator**

Create `FloorPlanReviewSelectionCoordinator` with explicit methods for:
- preview-hit resolution
- selection snapshot capture
- moved-artifact snapshot derivation
- replay resolution after refresh

Keep the first shape explicit and data-driven. Do not over-generalize.

- [ ] **Step 2: Delegate preview-hit routing**

Move the truth behind `SelectPreviewPath(...)` into the coordinator so the ViewModel becomes the applier of the resolved target instead of the place where hit priority is decided inline.

- [ ] **Step 3: Delegate snapshot capture and replay**

Move the truth behind:
- `CaptureSelection(...)`
- `BuildSelectionSnapshotForMovedArtifact(...)`
- replay matching after refresh

into the coordinator while keeping final property assignment in the ViewModel.

- [ ] **Step 4: Run focused tests again to verify GREEN for Loop 3B1**

Run the same focused architecture + routing suites.

Expected: PASS.

- [ ] **Step 5: Commit the routing seam**

```bash
git add -- 'src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSelectionCoordinator.cs' 'src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs' 'tests/FloorplanFit.Desktop.Tests/ViewModels/*'
git commit -m "refactor: add review selection coordinator"
```

---

### Task 3: Add failing seam tests for Loop 3B2 selection presentation

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
- Modify: existing selection-behavior tests

- [ ] **Step 1: Add source-facing assertions for repeated selection side effects**

Pin that the final ViewModel should materially reduce repeated inline responsibility for:
- cross-clearing other selected items
- setting `HighlightGeometryPathId`
- setting `PreviewSelectionLabel`
- syncing curated editors
- syncing/clearing selected label text height editors

Target the artifact-specific `OnSelectedXChanged(...)` methods without requiring every single partial to disappear.

- [ ] **Step 2: Add or tighten runtime tests that protect current presentation behavior**

Use or extend tests that already assert:
- `PreviewSelectionLabel`
- `HighlightGeometryPathId`
- selected curated editor values
- selected label text height editor behavior
- pinch-axis side effects from selected pinch markers

- [ ] **Step 3: Run the focused tests to verify RED**

Run the presentation-focused seam tests plus the affected runtime suites.

Expected: FAIL because the ViewModel still owns the bulk of selection side effects inline.

---

### Task 4: Implement Loop 3B2 by extracting selection presentation outcomes

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSelectionCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: focused tests under `tests/FloorplanFit.Desktop.Tests/ViewModels/`

- [ ] **Step 1: Extend the coordinator with presentation outcomes**

Add explicit outcome methods or small result DTOs so the coordinator can describe:
- which selections should clear
- what highlight path should apply
- what preview label should show
- whether curated editors should sync/clear
- whether label text height editor should sync/clear

- [ ] **Step 2: Thin the `OnSelectedXChanged(...)` partials**

Refactor the ViewModel partials so they mostly:
- call the coordinator
- apply the outcome to observable properties
- call `NotifyUxStateChanged()`

Do not let this drift into queue/filter logic.

- [ ] **Step 3: Run the focused presentation tests to verify GREEN**

Run the same focused architecture + presentation suites.

Expected: PASS.

- [ ] **Step 4: Commit the selection-side-effect seam**

```bash
git add -- 'src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSelectionCoordinator.cs' 'src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs' 'tests/FloorplanFit.Desktop.Tests/ViewModels/*'
git commit -m "refactor: delegate review selection state"
```

---

### Task 5: Verify, document, and close Loop 3B

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 3B selection state extraction.md`

- [ ] **Step 1: Run focused selection verification**

Run a focused test command that covers:
- `FloorPlanReviewViewModelArchitectureTests`
- `FloorPlanReviewViewModelTests`
- `CuratedArtifactFloorPlanReviewViewModelTests`
- `MovableArtifactFloorPlanReviewViewModelTests`
- `LabelTextHeightFloorPlanReviewViewModelTests`
- `DimensionEditingFloorPlanReviewViewModelTests`

Expected: PASS.

- [ ] **Step 2: Run the full Desktop suite**

Run the complete Desktop test project.

Expected: PASS.

- [ ] **Step 3: Update docs**

Write the implementation note and update `Current State.md` so it records:
- Loop 3B extracted selection state
- the ViewModel remains the Desktop property shell
- the next recommended slice becomes queue/filter orchestration evaluation or Loop 4

- [ ] **Step 4: Run diff hygiene and commit the loop**

Run:

```powershell
git diff --check
git diff --stat
git status --short
```

Expected:
- `git diff --check` clean
- diff stays limited to the selection coordinator, ViewModel, tests, and Obsidian notes

Then commit:

```bash
git add -- 'src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSelectionCoordinator.cs' 'src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs' 'tests/FloorplanFit.Desktop.Tests/ViewModels/*' 'obsidian-vault/Current State.md' 'obsidian-vault/Implementation/2026-05-12 - Loop 3B selection state extraction.md'
git commit -m "refactor: extract review selection state"
```

---

## Self-review

- This plan deliberately starts with **routing/snapshot truth** before touching selection presentation side effects.
- This plan deliberately keeps **queue / visible-item orchestration** out of scope until Loop 3B closes.
- This plan deliberately keeps **Application handlers and mutation orchestration unchanged**.
- This plan uses the now-cleaner post-Loop-3A shell to attack the next real Desktop hotspot with lower risk than a one-shot selection rewrite.
