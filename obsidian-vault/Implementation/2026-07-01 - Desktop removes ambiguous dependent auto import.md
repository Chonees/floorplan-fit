---
type: Implementation
date: 2026-07-01
replaces:
  - 2026-07-01 - Desktop adds explicit dependent sheet import buttons
replaced_by: null
---

# Desktop removes ambiguous dependent auto import

## What
Removed the generic `Auto Import Sheet` Library action and kept only explicit dependent import actions: `Import Electrical`, `Import Roof`, and `Import Facade`.

## Why
The generic action was a confusing tool because it could still send an unknown sheet type and reproduce the `A known dependent sheet type is required` path. The senior/simple UI is: first select the canonical floor plan, then import the dependent sheet by explicit type.

## Boundary
- No wizard.
- No new classifier UI.
- No per-sheet fit engine.
- Auto-classification can still exist inside import logic for non-UI callers, but the Desktop Library no longer exposes the ambiguous button.

## Verification
- Static test expectation updated so layout coverage rejects the ambiguous auto-import button/handler.
- XAML/code-behind now expose only explicit dependent import buttons.
- Verification remains static only; no agent-run build/test per repository rule.

## Files
- `src/FloorplanFit.Desktop/MainWindow.axaml`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
