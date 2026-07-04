# 2026-06-18 - Global delete selected review action

## Type
Implementation

## Product loop
Loop 1 Edit / Review preview.

## Current truth
- UI fit correction: the toolbar action is icon-only (Button.tool danger) with tooltip Eliminar seleccionado (Supr), so it fits beside the other canvas tools.
- The review screen now exposes one global **Eliminar seleccionado** action above the preview.
- Pressing **Delete/Supr** runs the same delete flow, unless focus is inside a `TextBox`.
- Delete order is intentionally contextual and minimal:
  1. selected pinch marker -> remove pinch
  2. selected pinch group -> remove group
  3. selected measurement node -> remove node
  4. selected measurement corridor/franja -> remove franja
  5. selected wall/label/opening/fixed/protected/curated artifact -> existing `ExcludeSelectedArtifactAsync(...)`
- Dimensions/cotas are intentionally not treated as deletable here because their existing semantics are restore/desvincular/manual edit, not delete-from-plan.

## Files
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml` — adds the visible global delete button and `KeyDown` hook.
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs` — routes button and Delete key to the ViewModel; ignores Delete from text editing controls.
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` — adds `CanDeleteSelectedItem` and `DeleteSelectedItemAsync(...)` dispatcher.
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewNotificationCoordinator.cs` — includes delete enablement in UI notifications.
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs` — source-level contract for the global delete wiring.

## Verification
- No build was run per repo rule.
- Source contract check passed: `delete-selected source contract OK`.
- `git diff --check` exited `0`; output only contained LF-to-CRLF warnings.

