---
type: Implementation
date: 2026-07-01
replaces: []
replaced_by: null
---

# Library registration uses manual transform dialog

## What
The Library `Register` action now opens a small manual transform dialog before registering a dependent sheet.

## Why
The previous Desktop action silently registered every dependent sheet with identity transform and `confidence=0.25`. That was safe as a placeholder but weak as a tool because users could not enter known scale, rotation, translation, roof overhang, facade reference, or confidence.

## Changed
- Added `RegistrationTransformDialog.axaml` and code-behind.
- The dialog accepts scale, rotation degrees, translate X/Y, confidence, roof overhang inches, and optional facade horizontal reference.
- `MainWindow.RegisterDependentSheetButton_OnClick` passes the dialog result to `LibraryViewModel.RegisterDependentSheetAsync(...)`.

## Boundary
This is not an automatic anchor picker or visual calibration workspace. It is the smallest manual input that makes registration less fake while preserving the no-per-sheet-fit-engine rule.
