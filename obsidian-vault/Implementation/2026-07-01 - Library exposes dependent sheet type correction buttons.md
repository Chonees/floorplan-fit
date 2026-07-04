# 2026-07-01 - Library exposes dependent sheet type correction buttons

- The selected HousePlanSet sheet list now exposes `To Electrical`, `To Roof`, and `To Facade` actions for unregistered/not-projected dependent sheets.
- `PlanSetSheetDto` now gates correction buttons with `CanCorrectSheetType`, `CanCorrectToElectrical`, `CanCorrectToRoof`, and `CanCorrectToFacade`.
- `LibraryViewModel.CorrectDependentSheetTypeAsync(...)` calls `CorrectPlanSheetTypeHandler`, preserves selected floor-plan version, refreshes the selected plan-set sheets, and reports the changed type.
- `MainWindow.axaml.cs` wires the three correction buttons through one helper.
- Boundary: no wizard/modal yet; registered/projected sheets still cannot be corrected from this path.
- Verification: scoped `git diff --check` returned exit code 0 with CRLF warnings only; `rg`/`Select-String` verified buttons, bindings, code-behind, ViewModel method, DTO gates, docs, and tests. No agent-run build/test due repo rule.
