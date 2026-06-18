# 2026-06-18 - Edit screen chrome cleanup

## Type
Implementation

## What changed
Removed visual chrome from the Loop 1 Edit/Review screen:
- Removed the large review header/name/status/queue summary area.
- Removed visible section titles for `Review Queue`, `Preview`, and `Inspector`.
- Removed the preview interaction-hint card.
- Compact layout: `Margin=12`, columns `260,*,400,64`, preview rows `Auto,Auto,*`, review queue rows `Auto,Auto,Auto,Auto,Auto,Auto,*`.
- Moved `Editar` and `Publish Curation` into the right-side Actions panel, before `Exportar DXF`.
- Added `Padding=8` to the icon toolbar so 48px tool buttons fit inside the 64px column.

## Why
The user called the review chrome useless in the edit panel and asked to give space back to the preview and right panels. The minimal fix is deletion and column rebalancing, not a new navigation concept.

## Verification
- Source check confirmed removed chrome and compact layout.
- XML parse passed for `ReviewFloorPlanWindow.axaml`.
- Layout-test source expectations were updated to assert the removed titles/chrome stay gone.
- `git diff --check` exited `0` with CRLF warnings only.
- No .NET build was run per repo rule.

## Files
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

## Follow-up: removed left review queue column
The entire left review-queue column was removed from the Edit/Review layout after the user called it useless. The screen now uses `ColumnDefinitions="*,440,64"`: preview first, inspector second, toolbar third.

Removed visual queue UI:
- Search
- Quick Filters
- Structure / Room Names / Door-Window Codes / Dimensions / Curated Objects folder toggles
- `ReviewQueueContentHost` list area

Verification:
- Source check confirmed the queue bindings and folder toggles are gone from XAML.
- XML parse passed for `ReviewFloorPlanWindow.axaml`.
- Layout-test source expectations were updated.
- `git diff --check` passed for touched files.
- No .NET build was run per repo rule.
