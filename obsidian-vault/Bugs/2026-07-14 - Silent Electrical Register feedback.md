---
type: bug
status: fixed
created: 2026-07-14
area: Desktop/Library
---

# Silent Electrical Register feedback

## What changed
- The Library Register flow now shows an immediate `Registering ...` status before resolving plan-set registration.
- Registration failures are caught and surfaced in the Library status instead of becoming invisible/silent UI behavior.
- Electrical manual-review failures now explicitly say no `Confirm` button is available until a `PendingConfirmation` registration is actually saved.
- The Library status moved out of the toolbar into a wrapped row under the import buttons, so long registration evidence is visible instead of clipped off-screen.

## Why
The user clicked `Register` for an ElectricalPlan and saw no useful visible state change. The previous status text could be clipped in the toolbar and the ViewModel only caught a narrow manual-review exception inside part of the flow.

## Files
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Desktop/MainWindow.axaml`
