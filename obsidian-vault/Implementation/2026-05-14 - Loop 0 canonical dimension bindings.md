---
created: 2026-05-14
project: floorplan-fit
type: implementation
status: active
replaces:
replaced_by:
---

# Loop 0 canonical dimension bindings

## What
Implemented the first typed semantic layer for native dimensions by introducing canonical `DimensionBindingDto` + `DimensionMeasuredSpanDto`, a new `DimensionBindingProjector`, and review-session wiring that now exposes `DimensionBindings` alongside the legacy `DimensionAssociations`.

## Why
The pinch-aware rollout needed a canonical answer to “what is this dimension measuring?” before wiring live preview adaptation. Loop 0 had to separate semantic binding truth from the existing authored-geometry/render truth without breaking current Desktop compatibility.

## Where
- `src/FloorplanFit.Contracts/FloorPlans/DimensionBindingDto.cs`
- `src/FloorplanFit.Contracts/FloorPlans/DimensionMeasuredSpanDto.cs`
- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionBindingProjector.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/DimensionBindingProjectorTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Review/FloorPlanReviewSessionReaderIntegrationTests.cs`

## Verified behavior
1. Resolved linear dimensions now project to a canonical binding with:
   - `BindingKind = LinearSpan`
   - two anchors
   - a measured span containing axis tag, scalar coordinates, and orientation
2. Partial or ambiguous linear associations no longer fail silently; they become explicit unresolved bindings with preserved notes/confidence.
3. Non-linear kinds such as `Radius` are classified as typed bindings already, but remain unresolved with an explicit “not yet supported” note until their later loops arrive.
4. `SqliteFloorPlanReviewSessionReader` now returns `DimensionBindings` for review sessions without removing the old `DimensionAssociations` compatibility path.

## Verified tests
- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter DimensionBindingProjectorTests`
- `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter FloorPlanReviewSessionReaderIntegrationTests`

## Learned
- Loop 0 can stay low-risk if it only introduces semantic truth and compatibility projection, without touching active preview rendering yet.
- Running two `dotnet test` commands in parallel against projects that build the same application assembly can create a file lock (`CS2012`); sequential verification avoids that infra-level false negative.
