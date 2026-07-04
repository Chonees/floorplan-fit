---
type: Implementation
date: 2026-07-01
replaces: []
replaced_by: 2026-07-01 - Desktop removes ambiguous dependent auto import
---

﻿# 2026-07-01 - Desktop adds explicit dependent sheet import buttons

> Superseded by [[2026-07-01 - Desktop removes ambiguous dependent auto import]]. The generic `Auto Import Sheet` action was later removed from Desktop.

## What
- Historical: this slice temporarily renamed the generic dependent sheet import action to `Auto Import Sheet`. That generic Desktop action was later removed.
- Added explicit Library header actions:
  - `Import Electrical` -> `ElectricalPlan`
  - `Import Roof` -> `RoofPlan`
  - `Import Facade` -> `FacadeElevation`
- All explicit actions reuse the same DXF picker/import helper and pass a known `SheetType` into `LibraryViewModel.ImportDependentSheetAsync(...)`.

## Why
The user verified the app no longer crashed after the SQLite transaction fix, but importing a dependent sheet with the generic button failed with `A known dependent sheet type is required`. Root cause: the generic UI sent an empty sheet type and relied on auto-classification; some real DXFs cannot be identified from filename/layer hints.

## Boundary
- No wizard.
- No transform picker.
- No per-sheet adjustment engine.
- Superseded: the Desktop UI no longer exposes auto import; explicit type buttons are now the only Library import path for dependent sheets.

## Verification
- Added layout/cable assertions for explicit import buttons and sheet type strings.
- Static RED check confirmed explicit buttons were missing before implementation.
- Static verification confirmed XAML and code-behind wiring.
- Scoped `git diff --check` passed.
- No agent-run build/test due repository rule; user `dotnet watch` had already confirmed build success before this fix.

## Files
- `src/FloorplanFit.Desktop/MainWindow.axaml`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
