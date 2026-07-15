---
type: Implementation
status: static-green
date: 2026-07-13
replaces: []
replaced_by: null
---

# Desktop automatic Electrical registration boundary

## What changed

- Desktop composition registers the stateless `DxfElectricalFloorRegistrationEstimator` as the singleton implementation of `IElectricalFloorRegistrationEstimator`.
- `LibraryViewModel.RegisterDependentSheetAsync` sends only `PlanSetVersionId` and `SheetId` into the Electrical Application request. Its legacy transform/confidence parameters remain solely because Roof and Facade share the same Desktop method.
- Inconclusive Electrical estimates are translated into a concise `StatusMessage` containing `requires manual confirmation` and the estimator evidence; the ViewModel returns `null` without changing Roof or Facade exception behavior.
- `MainWindow` invokes Electrical registration directly and returns before constructing `RegistrationTransformDialog`. Roof and Facade still use the existing dialog and all of its values.

## Architecture boundary

This is Loop 2 Desktop work. Desktop selects the interaction path and composes the Infrastructure estimator, while Application owns Electrical geometry estimation behind the identifier-only request boundary.

## Evidence

- The existing Desktop REDs were read before implementation.
- Scoped `git diff --check` passed for the three owned Desktop files.
- No `dotnet`, build, test, restore, watch, Desktop, or runtime command was run; executable proof remains external.

## Relevant files

- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
