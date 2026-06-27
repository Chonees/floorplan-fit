# Adjust setup dialog content did not fit

Date: 2026-06-27
Type: Bugfix
Scope: Loop 2 Adjust-to-Site-Plan setup dialog

## Symptom
When opening **Adjust to Site Plan**, the setup dialog content did not fit inside the modal. The lower simulation fields/actions could be clipped or visually overflow the window.

## Root Cause
`AdjustSitePlanSetupDialog.axaml` used a fixed `Height="360"` with `CanResize="False"`. The dialog content is taller than that once title text, import action, simulation copy, two inputs, validation text, and action buttons are laid out.

## Fix
The dialog now uses `SizeToContent="Height"` instead of a fixed height, and its width was increased from `520` to `640` to reduce wrapping and keep the content readable.

## Evidence
- RED: `Adjust_to_site_plan_setup_dialog_sizes_to_fit_its_content` failed because `SizeToContent="Height"` was missing.
- GREEN: focused test passed `1/1` using isolated artifacts path because the running desktop app locked the normal output exe.
- Regression slice: `ReviewFloorPlanWindowLayoutTests` passed `12/12` with `--no-build` from the isolated output.
- `git diff --check` passed with CRLF warnings only.

## Files
- `src/FloorplanFit.Desktop/AdjustSitePlanSetupDialog.axaml`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
