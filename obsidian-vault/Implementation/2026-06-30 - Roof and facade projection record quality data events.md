# Roof and facade projection record quality data events

## What changed
- `ProjectRoofSheetAdjustmentHandler` now emits a best-effort `SheetAdjustmentProjectionQualityMeasured` event after creating a roof projection.
- `ProjectFacadeElevationSheetAdjustmentHandler` now emits the same event after creating a facade/elevation projection.
- Roof/facade payloads include plan-set id, dependent sheet id, registration id, canonical adjustment id, method, confidence, status, warning, compression step count, and `ruleSummary`.

## Why
The HousePlanSet architecture needs DataCollection across all dependent sheet projection types. Electrical already records projection quality; roof and facade/elevation now have parity, including their sheet-specific rule summaries.

## Boundary
- Telemetry remains best-effort and never blocks projection persistence.
- No new analytics service, projection engine, or per-sheet fit engine was introduced.
- The three handlers still use their own projection rules: electrical whole-sheet similarity, roof overhang preservation, facade horizontal reference with vertical preservation.

## Verification
- Test-first RED: roof/facade tests now expect `SheetAdjustmentProjectionQualityMeasured` events with method, confidence, status, and rule summary payload.
- GREEN: both handlers emit the event through the existing `IPlanSetAuditEventRepository`.
- `git diff --check` passed for touched projection handlers/tests.
- No `dotnet test` and no `dotnet build` were run due repository rule.