# Desktop registers roof and facade sheets from library

Date: 2026-07-01
Type: Implementation
Status: Current

## What changed
- The Library `Register` action is now exposed for `ElectricalPlan`, `RoofPlan`, and `FacadeElevation` dependent sheets when they are `Unregistered / NotProjected`.
- `PlanSetSheetDto` now exposes `CanRegisterDependent`; the older `CanRegisterElectrical` remains as a compatibility alias for electrical-only checks.
- The Desktop button handler was generalized from electrical-only naming to `RegisterDependentSheetButton_OnClick`.
- ViewModel coverage now exercises roof and facade registration through the existing `RegisterDependentSheetAsync(...)` path.

## Why
The HousePlanSet goal is multi-sheet. Electrical proved the flow, but roof and facade imports also need a visible path into registration before projection/export can be meaningful.

## Boundary
- Still no transform picker/wizard.
- Default Desktop registration is intentionally low-confidence and pending confirmation.
- Roof/facade-specific geometry validation remains future UI work.

## Verification
- Added/updated RED assertions for layout wiring and roof/facade ViewModel registration behavior.
- Ran scoped `git diff --check`; exit code 0, only CRLF warnings.
- No agent-run build/test because this repository explicitly forbids build after changes.

## Related
- [[2026-07-01 - Desktop registers electrical sheet from library]]
- [[2026-07-01 - Desktop confirms dependent sheet projection]]
