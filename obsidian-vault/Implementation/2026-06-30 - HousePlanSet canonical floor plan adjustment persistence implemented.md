# 2026-06-30 - HousePlanSet canonical floor plan adjustment persistence implemented

## Status
Implemented the minimal durable CanonicalFloorPlanAdjustment module.

## What changed
- Added domain entity `CanonicalFloorPlanAdjustment`.
- Added contracts `RecordCanonicalFloorPlanAdjustmentRequest` and `RecordCanonicalFloorPlanAdjustmentResponse`.
- Added Application port `ICanonicalFloorPlanAdjustmentRepository`.
- Added use case `RecordCanonicalFloorPlanAdjustmentHandler`.
- Added SQLite repository `SqliteCanonicalFloorPlanAdjustmentRepository`.
- Added SQLite table `canonical_floor_plan_adjustments` and schema ensure logic.
- Wired repository and handler in Desktop DI.
- Added Application and Infrastructure tests for recording/persisting the canonical adjustment.

## Why it matters
Before this slice, `CanonicalAdjustmentId` existed only as a Guid passed around by projection/export code. That was not senior: Desktop/UI would have had to invent adjustment ids. Now the canonical floor-plan adjustment can be recorded as a durable product artifact before dependent projections/export package use it.

## Boundary
- This records the canonical adjustment and placement JSON; it does not yet automatically invoke dependent projection handlers from Desktop.
- No per-sheet fit engine was introduced.

## Verification
- RED evidence: handler/repository/entity/table did not exist before the tests.
- `git diff --check` passed for touched files; no `dotnet test` or `dotnet build` was run due repo rule.
