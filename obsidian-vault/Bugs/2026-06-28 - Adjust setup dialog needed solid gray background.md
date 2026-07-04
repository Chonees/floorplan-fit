# Adjust setup dialog uses solid gray background

Date: 2026-06-28
Type: Bugfix
Scope: Loop 2 Adjust-to-Site-Plan setup dialog

## Symptom
The **Adjust to Site Plan** setup dialog was hard to read/appreciate because its window background inherited the global transparent `Window` style and the panel styling is semi-transparent.

## Root Cause
`App.axaml` sets all `Window` backgrounds to `Transparent`. `AdjustSitePlanSetupDialog.axaml` did not override that, so the modal visually blended with the library behind it.

## Fix
The setup dialog now sets a solid gray window background: `#FF2F333A`.

## Evidence
- RED: `Adjust_to_site_plan_setup_dialog_uses_solid_gray_background` failed because the dialog lacked the explicit gray background.
- GREEN: focused test passed `1/1` using isolated artifacts path.
- Regression slice: `ReviewFloorPlanWindowLayoutTests` passed `13/13` with `--no-build` from the isolated output.
- `git diff --check` passed with CRLF warnings only.

## Files
- `src/FloorplanFit.Desktop/AdjustSitePlanSetupDialog.axaml`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
