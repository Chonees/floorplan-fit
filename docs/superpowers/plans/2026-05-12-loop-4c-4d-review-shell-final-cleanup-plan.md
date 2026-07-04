# Loop 4C/4D Review Shell Final Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish the remaining Loop 4 modularization work by extracting the residual apply/notify shell from `FloorPlanReviewViewModel` and refreshing the canonical documentation so the repo truth matches the final Desktop ownership model.

**Architecture:** Keep `FloorPlanReviewViewModel` as the owner of observable state and final property notifications, but move apply truth into a new apply coordinator and notification truth into a new notification coordinator. Close with a final documentation/map audit so Loop 4 ends with code and docs aligned.

**Tech Stack:** C# / .NET 10, CommunityToolkit MVVM, xUnit Desktop tests, PowerShell verification, Obsidian notes.

---

## File Structure

- Create: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewApplyCoordinator.cs`
  - Purpose: prepare session-apply and selection-presentation apply outcomes for the ViewModel shell.
- Create: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewNotificationCoordinator.cs`
  - Purpose: centralize queue/UX property notification lists and inspector-tool normalization rules.
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
  - Purpose: delegate the residual shell orchestration while staying the owner of observable properties and collections.
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
  - Purpose: add source-facing guardrails for the new apply/notify seams.
- Modify: focused ViewModel runtime tests under `tests/FloorplanFit.Desktop.Tests/ViewModels/`
  - Purpose: keep refresh/apply/editor sync behavior pinned through the refactor.
- Modify: `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`
  - Purpose: refresh the canonical architecture map to include the review coordinator cluster and Loop 4 closure truth.
- Modify: `obsidian-vault/Current State.md`
  - Purpose: record Loop 4C/4D closure and shift next steps away from architecture cleanup.
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 4C review shell apply-notify cleanup.md`
  - Purpose: durable implementation note for the final code seam.
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 4D final modularization audit.md`
  - Purpose: durable closure note for the final architecture/doc audit.

---

## Tasks

### Task 1: Add RED guardrails for the apply shell seam

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
- Modify: focused existing runtime tests only if needed

- [ ] **Step 1: Extend the source-facing architecture test**

Add assertions that the final ViewModel source:
- references `FloorPlanReviewApplyCoordinator`
- no longer owns `private void ApplySessionProjection(`
- no longer owns `private void ApplySelectionPresentationOutcome(`
- materially reduces inline editor-sync helpers if they move behind the apply seam

- [ ] **Step 2: Reuse or tighten runtime tests that already pin the behavior**

Use the existing refresh/apply-sensitive surfaces as the main guardrails:
- `FloorPlanReviewViewModelTests`
- `CuratedArtifactFloorPlanReviewViewModelTests`
- `LabelTextHeightFloorPlanReviewViewModelTests`
- `DimensionEditingFloorPlanReviewViewModelTests`

Add a new focused runtime test only if the current suite does not already pin:
- session refresh preserving selected state
- curated editor sync/clear behavior
- label text height editor sync/clear behavior

- [ ] **Step 3: Run the focused tests to verify RED**

Run a focused command that includes the architecture seam test plus the affected runtime suites.

Expected: FAIL because `FloorPlanReviewViewModel` still owns the apply shell inline.

---

### Task 2: Implement Loop 4C1 by extracting apply orchestration

**Files:**
- Create: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewApplyCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: focused tests as needed

- [ ] **Step 1: Introduce the smallest useful apply coordinator**

Create `FloorPlanReviewApplyCoordinator` with explicit outcome/result shapes for:
- session projection apply
- selection replay resolution
- selection presentation apply

Keep the first version explicit; do not invent a generic framework.

- [ ] **Step 2: Delegate `ApplySessionProjection(...)`**

Move the truth that decides:
- which collections/fields update from `ReviewSessionProjection`
- how selection replay resolves
- what follow-up apply work the shell must do

The ViewModel should still perform final collection/property assignment.

- [ ] **Step 3: Delegate `ApplySelectionPresentationOutcome(...)`**

Move the truth that decides:
- which selection slots clear
- what linked candidate/pinch group apply
- what highlight/preview label apply
- which editors sync/clear

The ViewModel should still assign final observable properties and editor values.

- [ ] **Step 4: Run the focused tests again to verify GREEN**

Run the same focused architecture + runtime suites.

Expected: PASS.

- [ ] **Step 5: Commit the apply seam**

```bash
git add -- 'src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewApplyCoordinator.cs' 'src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs' 'tests/FloorplanFit.Desktop.Tests/ViewModels/*'
git commit -m "refactor: extract review shell apply coordinator"
```

---

### Task 3: Add RED guardrails for the notification shell seam

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
- Modify: focused runtime tests only if needed

- [ ] **Step 1: Extend the source-facing architecture test**

Add assertions that the final ViewModel source:
- references `FloorPlanReviewNotificationCoordinator`
- no longer owns `private void NotifyReviewQueueStateChanged(`
- no longer owns `private void NotifyUxStateChanged(`

- [ ] **Step 2: Reuse runtime tests that indirectly pin notification correctness**

Lean on existing ViewModel tests that assert:
- queue summaries/counts/visible sections
- inspector tool availability and selected tool normalization
- interaction hints and selection-derived inspector state

Add a focused runtime test only if a gap appears while extracting the seam.

- [ ] **Step 3: Run the focused tests to verify RED**

Run a focused command covering the architecture test plus the main queue/inspector runtime suites.

Expected: FAIL because notification truth still lives inline.

---

### Task 4: Implement Loop 4C2 by extracting notification orchestration

**Files:**
- Create: `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewNotificationCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: focused tests as needed

- [ ] **Step 1: Introduce the notification coordinator**

Create `FloorPlanReviewNotificationCoordinator` with explicit methods for:
- normalizing selected inspector tool
- returning queue property names that must raise notifications
- returning UX property names that must raise notifications

- [ ] **Step 2: Thin `NotifyReviewQueueStateChanged()` and `NotifyUxStateChanged()`**

Refactor the ViewModel so these methods become tiny appliers:
- ask the coordinator for normalized tool / property sets
- call `OnPropertyChanged(...)` over those sets

- [ ] **Step 3: Run the focused tests to verify GREEN**

Run the same focused architecture + runtime suites.

Expected: PASS.

- [ ] **Step 4: Commit the notification seam**

```bash
git add -- 'src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewNotificationCoordinator.cs' 'src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs' 'tests/FloorplanFit.Desktop.Tests/ViewModels/*'
git commit -m "refactor: extract review shell notifications"
```

---

### Task 5: Close Loop 4 with final audit, docs, and full verification

**Files:**
- Modify: `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 4C review shell apply-notify cleanup.md`
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 4D final modularization audit.md`

- [ ] **Step 1: Refresh the canonical architecture map**

Update the map so it explicitly reflects:
- the review coordinator cluster under `src/FloorplanFit.Desktop/ViewModels/Review/`
- the new role of `FloorPlanReviewViewModel` as a thinner Desktop shell
- the closure of the Loop 4 modularization program

- [ ] **Step 2: Update Obsidian**

Write implementation notes and update `Current State.md` so it records:
- Loop 4C extracted the apply/notify shell
- Loop 4D closed the final audit
- the next work returns to product evolution / new features instead of architectural cleanup

- [ ] **Step 3: Run focused verification**

Run a focused command covering:
- `FloorPlanReviewViewModelArchitectureTests`
- `FloorPlanReviewViewModelTests`
- `CuratedArtifactFloorPlanReviewViewModelTests`
- `LabelTextHeightFloorPlanReviewViewModelTests`
- `DimensionEditingFloorPlanReviewViewModelTests`

Expected: PASS.

- [ ] **Step 4: Run the full Desktop suite**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop4-final-full
```

Expected: PASS.

- [ ] **Step 5: Run diff hygiene and commit final closure**

Run:

```powershell
git diff --check
git diff --stat
git status --short
```

Expected:
- `git diff --check` clean
- diff limited to the new coordinators, ViewModel, tests, canonical map, and Obsidian notes

Then commit:

```bash
git add -- 'src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewApplyCoordinator.cs' 'src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewNotificationCoordinator.cs' 'src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs' 'tests/FloorplanFit.Desktop.Tests/ViewModels/*' 'docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md' 'obsidian-vault/Current State.md' 'obsidian-vault/Implementation/2026-05-12 - Loop 4C review shell apply-notify cleanup.md' 'obsidian-vault/Implementation/2026-05-12 - Loop 4D final modularization audit.md'
git commit -m "docs: close loop 4 modularization"
```

---

## Self-review

- This plan keeps `FloorPlanReviewViewModel` as the observable shell instead of pushing `ObservableCollection` mutation into coordinators.
- This plan separates **apply truth** from **notification truth** so the cleanup does not simply create another god-object.
- This plan includes the canonical architecture map refresh because the repo truth is currently drifting behind the Loop 3/4 coordinator extraction work.
- This plan ends with full Desktop verification and durable docs so Loop 4 can close honestly, not cosmetically.
