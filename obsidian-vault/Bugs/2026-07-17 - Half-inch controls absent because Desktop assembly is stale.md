---
type: bug
status: diagnosed-runtime-rebuild-pending
project: FloorplanFit
area: Loop 1 Desktop
source_of_truth: local source and build artifact timestamps
code_refs:
  - src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml
  - src/FloorplanFit.Desktop/bin/Debug/net10.0/FloorplanFit.Desktop.dll
updated: 2026-07-17
---

# Half-inch controls absent because Desktop assembly is stale

## Symptom

The running pinch-capacity editor shows only the input, Cancel, and Save; the new `- 1/2"` and `+ 1/2"` controls are absent.

## Root cause evidence

- Source `ReviewFloorPlanWindow.axaml` contains both controls and handlers and was modified at 13:14:33.
- `FloorplanFit.Desktop.dll` was last built at 13:00:09.
- The built DLL contains neither `DecreasePinchMaxTrimButton_OnClick` nor the `+ 1/2` content.

The UI is therefore running an assembly produced before the XAML change. This is not a clipping or layout defect in the current source.

## Required runtime action

Stop the current watch/application process, start the source launcher again, and wait for a successful rebuild before reopening the editor. Runtime confirmation remains pending.
