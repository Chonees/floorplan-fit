---
type: decision
status: implemented-static-runtime-pending
project: FloorplanFit
area: Loop 1 Desktop
source_of_truth: user requirement
code_refs:
  - src/FloorplanFit.Desktop/AdjustSitePlanSetupDialog.axaml
  - src/FloorplanFit.Desktop/AdjustSitePlanSetupDialog.axaml.cs
  - src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml
  - src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs
  - src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs
  - src/FloorplanFit.Desktop/Presentation/ArchitecturalLengthText.cs
updated: 2026-07-17
---

# Dimension controls step by half an inch in architectural notation

## Decision

Dimension reduction controls must operate on one total length and subtract or add exactly `1/2"` per step. Feet and inches are only the architectural presentation.

Starting from `39'-0"`, decrementing produces:

`38'-11 1/2"`, `38'-11"`, `38'-10 1/2"`, `38'-10"`, ...

The same rule applies when editing an existing dimension or pinch capacity. Free-text architectural input remains available.

## Implemented result

- Simulated site width and height expose `- 1/2"` and `+ 1/2"` controls.
- Existing pinch-capacity editing exposes the same controls and remains transient until `Guardar`.
- Invalid or non-positive changes preserve the current text and show validation.
- A successful pinch correction replaces stale validation with a save-pending message.
- Manual decimal and architectural text entry remains available.

## Implementation rule

The controls parse the current value, change total inches by exactly `0.5`, and format the result with the existing architectural formatter. They do not maintain separate mutable feet/inches fields.

Static tests, XAML parsing, scoped diff checks, and bounded review pass. Build, test execution, and Desktop runtime proof remain external under repository policy.
