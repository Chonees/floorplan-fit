# Desktop exposes dependent sheet import action

## What changed
Added a visible Desktop Library action to import dependent HousePlanSet sheets.

## Implemented
- `MainWindow.axaml` now has an `Import Sheet` button beside the existing floor-plan `Import DXF` button.
- `MainWindow.axaml.cs` reuses a shared DXF picker helper.
- The new handler calls `LibraryViewModel.ImportDependentSheetAsync(...)` with empty sheet type so SheetClassification can classify by filename/title/layer hints.
- If classification/import requires review, the handler writes a status message instead of crashing the UI.

## Why it matters
The backend already had dependent sheet import/classification, but the library screen still exposed only floor-plan import. This makes the HousePlanSet direction visible as a tool: a selected house/version can now receive electrical, roof, or facade/elevation sheets from the Desktop entry point.

## Boundary
No final wizard, no sheet-type picker, no registration UI, and no automatic transform estimation. This is intentionally the smallest UX seam over the existing application flow.

## Verification
- Added layout/cable test assertions for `Import Sheet`, handler wiring, picker title, ViewModel call, and review fallback message.
- `git diff --check` passed for touched files.
- No `dotnet test` or `dotnet build` was run due repository rule.
