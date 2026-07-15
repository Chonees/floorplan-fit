# 2026-07-15 - Nullable quality confidence crashed HousePlanSet export

## Status
Superseded by [[2026-07-15 - Nullable Desktop audit summary masked manual HousePlanSet result]].

- `replaced_by`: [[2026-07-15 - Nullable Desktop audit summary masked manual HousePlanSet result]]

## Runtime symptom
A fresh Loop 2 export wrote the standalone canonical DXF but did not produce the Electrical DXF/package. The surfaced exception was: `The requested operation requires an element of type 'Number', but the target element has type 'Null'.`

## Root-cause boundary
The only production `JsonElement` numeric accessor in the package/audit path was `GetPlanSetQualityReportHandler.ReadDecimal`, which called `JsonElement.GetDecimal()` for the optional quality-event `confidence` field. That is the only checked-in call matching the runtime exception signature.

Important evidence gap: the pre-change checked-in function already tested `ValueKind == Null` before calling `GetDecimal`, so the current source cannot statically reproduce a Null reaching that call. The runtime was therefore using stale/different compiled code, or the missing stack trace points outside the checked-in source. This patch removes the raw accessor entirely and locks the intended nullable contract, but external rebuild/runtime proof is still required before calling the incident conclusively closed.

## Fix
- `GetPlanSetQualityReportHandler.MapSignal` now reads optional `confidence` as `decimal?`: missing or JSON `null` maps to `null`; a number must parse as `decimal`; every other shape throws a contextual `JsonException` naming the event and property.
- Required `method`/`source` and `status` fields now reject missing, null, non-string, or blank values instead of silently becoming empty strings.
- Optional string fields accept only string or null.
- Malformed quality JSON is no longer swallowed by package audit construction.
- Package orchestration records one `Failed` export with no manifest path and removes staging/final package files when malformed audit JSON aborts publication. A marker on already-persisted failures prevents duplicate failure rows.

## Regression coverage
- Nullable `confidence` maps to `null`.
- The same nullable payload with required `status: null` fails with an actionable event/property error rather than the raw `JsonElement` Number/Null exception.
- A malformed quality payload during HousePlanSet publication removes staging and persists a `Failed` package state.

## Safety boundary
The canonical standalone DXF remains outside package rollback by design. No registration estimator or visual-overlay code changed.

## Verification
- Scoped `git diff --check` and static source inspection only.
- No `dotnet`, build, test, restore, watch, or Desktop command was run.
