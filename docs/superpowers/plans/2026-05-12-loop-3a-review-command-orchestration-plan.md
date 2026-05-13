# Loop 3A Review Command Orchestration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract review mutation / command orchestration out of `FloorPlanReviewViewModel` so the ViewModel stops inlining DI scope creation, handler resolution, mutation execution, and refresh ceremony.

**Architecture:** Keep `FloorPlanReviewViewModel` as the Desktop state shell. Introduce a focused collaborator that owns review mutation orchestration and returns explicit outcomes where needed. Do not mix this slice with selection partial extraction or queue refactors.

**Tech Stack:** C# / .NET 10, CommunityToolkit MVVM, Microsoft DI scopes, xUnit Desktop tests, PowerShell verification.

---

## File Structure

- Create: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs`
  - Purpose: own review mutation orchestration that currently repeats inside the ViewModel.
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
  - Purpose: delegate mutation ceremony while remaining the state shell.
- Create or modify: focused tests under `tests/FloorplanFit.Desktop.Tests/ViewModels/`
  - Purpose: pin the orchestration seam before and after delegation.
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 3A review command orchestration.md`
  - Purpose: durable implementation note for this slice.
- Modify: `obsidian-vault/Current State.md`
  - Purpose: record the chosen Loop 3A seam and the next slice after it.

---

## Tasks

### Task 1: Add failing seam tests around review mutation orchestration

**Files:**
- Modify or create focused tests in `tests/FloorplanFit.Desktop.Tests/ViewModels/`

- [ ] **Step 1: Add source-facing architectural tests**

Add tests that read `FloorPlanReviewViewModel.cs` and assert that the final delegated slice references the new coordinator for the first extracted mutation group.

Start with the lowest-risk action cluster, such as:
- save / restore label text height
- save moved artifact position
- save edited dimension
- save / restore curated artifact classification

The first RED should pin that `FloorPlanReviewViewModel` still owns inline `CreateScope + GetRequiredService + HandleAsync` ceremony for that cluster.

- [ ] **Step 2: Run the focused tests to verify RED**

Run a focused ViewModel test command targeting the new seam tests plus any existing tests that cover the same mutation family.

Expected: FAIL because the ViewModel still owns the mutation orchestration inline.

---

### Task 2: Introduce the mutation coordinator minimally and turn the seam GREEN

**Files:**
- Create: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: focused tests under `tests/FloorplanFit.Desktop.Tests/ViewModels/`

- [ ] **Step 1: Introduce the smallest useful collaborator**

Create a focused collaborator that receives `IServiceScopeFactory` and owns the repeated pattern:
- create scope
- resolve application handler
- invoke mutation

Keep the first implementation small and explicit rather than over-generic.

- [ ] **Step 2: Delegate one coherent mutation family first**

Recommended first family:
- label text height save / restore
- or artifact position save / restore

This keeps the first GREEN narrow and gives a pattern for the remaining actions.

- [ ] **Step 3: Expand delegation to the rest of the chosen Loop 3A command slice**

Move the rest of the agreed mutation methods onto the coordinator while keeping:
- selection snapshot capture
- `RefreshSessionAsync(...)`
- final `StatusMessage` ownership

in the ViewModel unless the extraction proves that outcome reporting should also move.

- [ ] **Step 4: Run the focused tests again to verify GREEN**

Run the same focused ViewModel tests plus the existing dedicated suites that cover moved artifacts, dimensions, labels, and curated artifact classification.

Expected: PASS.

- [ ] **Step 5: Commit the orchestration seam**

```bash
git add -- 'src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs' 'src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs' 'tests/FloorplanFit.Desktop.Tests/ViewModels/*'
git commit -m "refactor: add review mutation coordinator"
```

---

### Task 3: Thin the ViewModel methods and lock the boundary

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: source-facing ViewModel seam tests

- [ ] **Step 1: Add or tighten source-facing assertions**

Assert that the final ViewModel source:
- references `FloorPlanReviewMutationCoordinator`
- materially reduces inline `CreateScope()` repetition in the extracted slice
- no longer owns the handler-resolution ceremony for the delegated mutation family

- [ ] **Step 2: Keep the ViewModel as state shell**

Ensure the ViewModel still owns:
- selected state
- status text updates
- capture of selection snapshots
- session refresh / apply flow

Do **not** let this slice drift into selection partials or queue logic.

- [ ] **Step 3: Run focused mutation-related ViewModel tests**

Run the mutation-focused ViewModel suites again.

Expected: PASS.

- [ ] **Step 4: Commit the thin-ViewModel slice**

```bash
git add -- 'src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs' 'tests/FloorplanFit.Desktop.Tests/ViewModels/*'
git commit -m "refactor: delegate review mutation orchestration"
```

---

### Task 4: Verify, document, and close Loop 3A

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 3A review command orchestration.md`

- [ ] **Step 1: Run focused ViewModel verification**

Run a focused test command that covers:
- `FloorPlanReviewViewModelTests`
- `LabelTextHeightFloorPlanReviewViewModelTests`
- `MovableArtifactFloorPlanReviewViewModelTests`
- `DimensionEditingFloorPlanReviewViewModelTests`
- `CuratedArtifactFloorPlanReviewViewModelTests`

Expected: PASS.

- [ ] **Step 2: Run the full Desktop suite**

Run the complete Desktop test project.

Expected: PASS.

- [ ] **Step 3: Update docs**

Write the implementation note and update `Current State.md` so it records:
- Loop 3A extracted review mutation orchestration
- the ViewModel remains the Desktop state shell
- the next recommended slice becomes selection state

- [ ] **Step 4: Run diff hygiene and commit the loop**

Run:

```powershell
git diff --check
git diff --stat
git status --short
```

Expected:
- `git diff --check` clean
- diff stays limited to the coordinator, ViewModel, tests, and Obsidian notes

Then commit:

```bash
git add -- 'src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs' 'src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs' 'tests/FloorplanFit.Desktop.Tests/ViewModels/*' 'obsidian-vault/Current State.md' 'obsidian-vault/Implementation/2026-05-12 - Loop 3A review command orchestration.md'
git commit -m "refactor: extract review mutation orchestration"
```

---

## Self-review

- This plan deliberately starts with **command orchestration**, not selection partials.
- This plan deliberately keeps **queue / visible-item orchestration** out of scope.
- This plan deliberately keeps **Application handlers unchanged**.
- This plan targets the biggest repeated procedural block in `FloorPlanReviewViewModel` first.
