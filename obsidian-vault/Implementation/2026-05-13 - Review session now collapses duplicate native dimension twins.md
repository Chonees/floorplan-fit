---
created: 2026-05-13
updated: 2026-05-13
project: floorplan-fit
type: implementation
status: fixed
replaces:
replaced_by:
---

# Review session now collapses duplicate native dimension twins by base authored geometry

## What

Implemented a surgical fix in the infrastructure review-session reader so native dimensions that are exact authored twins only surface once in preview/curation.

## Why

Real DXFs in the current workspace contain pairs of native `DIMENSION` entities with different handles / anonymous block names but the same visible geometry. Editing one handle exposed the unedited twin as a duplicate underneath, which made the preview look like it was rendering a “calco”.

## Where

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Review/DimensionOverrideReviewSessionIntegrationTests.cs`

## Implementation

- `GetDimensions(...)` now keeps both the raw extracted/base dimension and the resolved/override-applied dimension while reading the session.
- After loading, the reader collapses duplicate clusters using a base authored-geometry signature built from the original primitives / render metadata instead of the post-edit geometry.
- Cluster selection prefers:
  1. edited dimensions
  2. otherwise the later / higher-priority extracted twin
- This is intentional: if one twin has an override, that is the one the user actually touched and the preview must keep it.

## Result

- moving an edited dimension no longer reveals an untouched duplicate under it
- current duplicated extraction rows are hidden at session-read time
- no schema migration and no preview-layer hacks were needed

## Verification

- RED -> GREEN focused test:
  - `dotnet test .\\tests\\FloorplanFit.Infrastructure.Tests\\FloorplanFit.Infrastructure.Tests.csproj --filter FullyQualifiedName~DimensionOverrideReviewSessionIntegrationTests.GetByTemplateAsync_overlays_manual_dimension_snapshot_and_export_state --no-restore`
  - **PASS** after the fix
- Review infrastructure slice:
  - `dotnet test .\\tests\\FloorplanFit.Infrastructure.Tests\\FloorplanFit.Infrastructure.Tests.csproj --filter FullyQualifiedName~FloorplanFit.Infrastructure.Tests.Review --no-restore`
  - **8/8 PASS**

## Tradeoff

This fix is intentionally surgical in the review-session layer. The raw DXF / extracted corpus can still contain twin `DIMENSION` entities; if we later want to eliminate them earlier in the pipeline or propagate dedupe semantics into adjusted-DXF export, that should be a separate follow-up.
