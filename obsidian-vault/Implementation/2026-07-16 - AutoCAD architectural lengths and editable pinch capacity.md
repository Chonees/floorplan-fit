---
project: floorplan-fit
type: implementation
date: 2026-07-16
status: static-ready-runtime-pending
related:
  - "[[Inbox/2026-07-16 - Professional architectural units and editable pinch capacity]]"
  - "[[Inbox/2026-07-16 - Architectural feet-inch input convention]]"
  - "[[Current State]]"
---

# AutoCAD architectural lengths and editable pinch capacity

## Product loop

Loop 1 authoring now accepts professional US architectural lengths while preserving the existing decimal inputs, and a Draft pinch can update its maximum trim without delete/recreate.

## Implemented boundary

- Site width/height accepts bare decimal feet and explicit architectural input such as `39'-0"`.
- Pinch create/edit accepts bare decimal inches and architectural input such as `6 1/2"` or `1'-2"`.
- Supported architectural fractional denominators match AutoCAD through `1/256"`.
- Feet/inches carry and fraction reduction are handled in presentation; exact numeric values remain persisted.
- AutoCAD feet-inch forms with an omitted terminal inch quote are accepted when feet are explicit.
- Ambiguous calculator subtraction forms such as `5' -9"` and `5'- 9"` are rejected instead of silently misread.
- Current-culture and invariant signed/scientific bare decimals remain supported without enabling thousands grouping.
- New pinch input no longer rounds the converted millimeter value to three decimals.
- `PinchMarkerDto` and `ArticulationBandProjector` no longer round exact derived capacity prematurely.

## Existing-pinch edit

- Selecting a pinch exposes its canonical architectural capacity and `Editar capacidad`.
- Save performs an identity-preserving Domain mutation and SQLite `UPDATE`; no marker is deleted or recreated.
- Application verifies the owning curation exists, is `Draft`, and owns the marker.
- SQLite requires exactly one affected row.
- A successful save refreshes markers and articulation bands while restoring selection.
- Saving an unchanged prefilled display is a no-op so display precision cannot snap an exact stored value.
- Published curation remains read-only and must follow the existing published-to-Draft edit flow.

## Architecture

- **Desktop / Presentation:** `ArchitecturalLengthText`, site dialog, review UI/view model/coordinator.
- **Application:** `UpdatePinchMarkerMaxTrimHandler`, repository contract, exact band projection.
- **Domain:** identity-preserving `PinchMarker.UpdateMaxTrim`.
- **Infrastructure:** exact invariant decimal SQLite update.
- **Contracts:** exact derived `MaxTrimInches`.

No schema migration, formatted-string persistence, dependency, DXF-unit change, or Loop 2 capacity-semantic redesign was introduced.

## Verification

- Focused parser, ViewModel, Application, projection, layout, and SQLite tests were written.
- Both changed Avalonia XAML files parse as XML.
- `git diff --check -- src tests` passes.
- Two bounded static-review rounds found no remaining concrete blocker in scope.
- `.NET` build/tests/runtime remain external and pending under repository execution rules.

## 2026-07-17 - Half-inch interactive stepping

- `ArchitecturalLengthText.TryAdjustInches` now applies a signed delta to exact total inches before canonical formatting; overflow and non-positive results fail safely.
- Simulated site width and height expose fixed `- 1/2"` / `+ 1/2"` controls with bare values interpreted as feet.
- Existing pinch-capacity editing exposes the same controls with bare values interpreted as inches; adjustment remains unsaved until `Guardar` and does not change marker identity.
- The countdown crosses a foot boundary correctly: `39'-0"` -> `38'-11 1/2"` -> `38'-11"` -> `38'-10 1/2"`.
- Invalid input remains unchanged; successful recovery replaces stale validation with a save-pending message.
- Pinch editing controls carry explicit automation names, and layout tests structurally parse their XAML.
- Final bounded static review reports no P1/P2 findings. Runtime compilation and interaction proof remain external.
