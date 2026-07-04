# Sheet registration records quality data events

## What changed
- `RegisterElectricalSheetHandler`, `RegisterRoofSheetHandler`, and `RegisterFacadeElevationSheetHandler` now emit best-effort `SheetRegistrationQualityMeasured` events after creating a sheet registration.
- Registration-quality payloads include plan-set id, dependent sheet id, canonical floor-plan version id, method, confidence, status, warning, and rule summary when present.
- The event uses `AggregateType = SheetRegistration` and `AggregateId = registration.Id`.

## Why
The HousePlanSet goal requires DataCollection for both registration and projection quality. Projection-time telemetry already exists; this adds the missing registration-time measurement point.

## Boundary
- Telemetry remains best-effort and never blocks registration persistence.
- No new analytics service, registration engine, or per-sheet fit engine was introduced.
- Registration handlers remain sheet-specific because their rules differ: electrical whole-sheet similarity, roof overhang preservation, facade/elevation horizontal reference with vertical preservation.

## Verification
- Test-first RED: registration tests now expect `SheetRegistrationQualityMeasured` events with method, confidence, status, warning/rule summary payload.
- GREEN: all three registration handlers emit the event through existing `IPlanSetAuditEventRepository`.
- Desktop DI already registers `IPlanSetAuditEventRepository`, so runtime can inject it.
- `git diff --check` passed for touched files.
- No `dotnet test` and no `dotnet build` were run due repository rule.