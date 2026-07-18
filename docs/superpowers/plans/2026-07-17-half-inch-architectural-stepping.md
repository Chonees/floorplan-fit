# Half-Inch Architectural Stepping Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add AutoCAD-style `- 1/2"` and `+ 1/2"` controls to simulated site width/height and existing pinch-capacity editing.

**Architecture:** Keep one exact total length in inches. Reuse `ArchitecturalLengthText` to parse the current text, add a signed `0.5m`, reject non-positive results, and format the result back to canonical feet/inches. UI controls remain thin and do not touch Domain, persistence, DXF units, or adjustment recipes.

**Tech Stack:** C#/.NET, Avalonia XAML, xUnit. Repository policy forbids executing .NET commands from the agent, so tests are authored before production code and runtime execution remains external.

---

### Task 1: Exact architectural stepping primitive

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/Presentation/ArchitecturalLengthTextTests.cs`
- Modify: `src/FloorplanFit.Desktop/Presentation/ArchitecturalLengthText.cs`

- [x] **Step 1: Write focused tests first**

Cover this exact countdown with a half-inch decrement:

```text
39'-0" -> 38'-11 1/2" -> 38'-11" -> 38'-10 1/2"
```

Also prove bare-decimal caller units, half-inch increment, arbitrary supported fractions, invalid text, and refusal to produce zero/negative lengths.

- [x] **Step 2: Implement the minimum helper**

Add one method that parses through `TryParsePositiveInches`, adds the supplied signed delta, rejects results `<= 0`, and returns `FormatInches(adjusted)`.

- [x] **Step 3: Static review only**

Do not run `dotnet`, build, test, restore, watch, or Desktop.

### Task 2: Simulated site width and height controls

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/Layout/AdjustSitePlanSetupDialogLayoutTests.cs` if present; otherwise add the assertion to the existing Desktop layout-test file that owns this dialog.
- Modify: `src/FloorplanFit.Desktop/AdjustSitePlanSetupDialog.axaml`
- Modify: `src/FloorplanFit.Desktop/AdjustSitePlanSetupDialog.axaml.cs`

- [x] **Step 1: Add layout assertions first**

Require visible `- 1/2"` and `+ 1/2"` controls for both `BuildableWidthFeetTextBox` and `BuildableHeightFeetTextBox`.

- [x] **Step 2: Add thin UI controls**

Each click calls the shared helper with `ArchitecturalLengthDefaultUnit.Feet` and delta `-0.5m` or `+0.5m`. Invalid/empty/non-positive results keep the previous text and show the existing validation message.

### Task 3: Existing pinch-capacity edit controls

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`

- [x] **Step 1: Add ViewModel and layout tests first**

Prove decrement/increment changes only `EditableSelectedPinchMaxTrim`, uses half-inch steps, formats canonically, and leaves invalid/non-positive input unchanged with an actionable status. Require both buttons inside the existing edit panel.

- [x] **Step 2: Implement two thin button handlers over one ViewModel operation**

The ViewModel delegates arithmetic and formatting to the shared helper. Saving remains the existing identity-preserving update flow.

### Task 4: Bounded static verification and durable truth

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Modify: `obsidian-vault/Decisions/2026-07-17 - Dimension controls step by half an inch in architectural notation.md`

- [x] **Step 1: Parse changed XAML as XML and run scoped `git diff --check`**

No .NET command is permitted.

- [x] **Step 2: Update documentation from accepted-not-implemented to implemented-static-runtime-pending**

Record the exact files, behavior, and external runtime proof still required.
