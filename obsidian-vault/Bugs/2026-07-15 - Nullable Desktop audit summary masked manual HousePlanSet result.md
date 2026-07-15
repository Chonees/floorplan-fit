# 2026-07-15 - Nullable Desktop audit summary masked manual HousePlanSet result

## Status
Fixed statically; external compile and runtime proof remain required.

- `replaces`: [[2026-07-15 - Nullable quality confidence crashed HousePlanSet export]]

## Authoritative runtime evidence
- Active DB: `C:\Users\lucas\AppData\Local\FloorplanFit\workspace\app.db`.
- Canonical adjustment `144b78d2-a41c-41da-88ea-bb5d464634cd` points to `D:\PointAIData\test adjust\1.dxf`.
- Manifest `exports/plan-sets/07f2352e44504bb2a510feee5117fe5f/manifest.json` exists with `RequiresManualConfirmation`.
- Electrical `StoragePath` is null because projected export took the manual-review path; verification correctly reports the missing Electrical output.

## Exact root cause
Package creation succeeded in its intended blocked/manual state. Desktop then called `SitePlanAdjustmentViewModel.BuildPlanSetExportAuditLines`. Its `GetJsonDecimal` and `GetJsonInt` helpers invoked `JsonElement.TryGetDecimal` / `TryGetInt32` on JSON `null`. Those APIs throw `InvalidOperationException` unless the element is a Number. Nullable values in `final-output-congruence-audit.json` and `outline-segment-congruence-audit.json` therefore crashed post-package summary rendering and the outer UI catch mislabeled the completed `RequiresManualConfirmation` result as a package export failure.

## Fix
- `GetJsonDecimal` and `GetJsonInt` now require `ValueKind == Number` before calling `TryGet...`.
- Missing/null retains the existing semantics: decimal returns `null`, integer returns `0`.
- No broad catch was added.
- A focused Desktop regression loads nullable final/segment audit fields, verifies summary generation does not throw, preserves missing-data text, and surfaces `HousePlanSet NO listo: MissingExpectedOutput` rather than the generic Number/Null failure.
- The speculative Application/package changes from the superseded RCA were removed manually; pre-existing atomic package work remains.

## Verification
- Static source and scoped whitespace checks only.
- No `dotnet`, build, test, restore, watch, or Desktop command ran.

