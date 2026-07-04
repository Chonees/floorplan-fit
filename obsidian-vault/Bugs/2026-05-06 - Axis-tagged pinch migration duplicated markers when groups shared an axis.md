---
type: bug
status: fixed
date: 2026-05-06
project: floorplan-fit
area: Loop 1 curation persistence
---

# Axis-tagged pinch migration duplicated markers when groups shared an axis

## What

Starting the desktop app failed during SQLite initialization with:

```txt
SQLite Error 19: 'UNIQUE constraint failed: pinch_markers.id'
```

## Why

The migration from flat axis-tagged pinch markers (`source_candidate_id + axis_tag`) to grouped markers joined legacy markers to `pinch_groups` by `floorplan_curation_id + axis_tag`.

If an existing database already had more than one group with the same axis in the same curation, a single legacy marker matched multiple groups. SQLite then attempted to insert the same marker id more than once into the new `pinch_markers` table.

## Fix

The migration now resolves one deterministic target group per `curation + axis`, ordered by `sort_order ASC, id ASC`, before inserting migrated markers.

## Where

- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs`

## Verification

- Red: `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter FullyQualifiedName~InitializeAsync_migrates_axis_tagged_pinch_markers_when_existing_groups_share_the_same_axis --no-restore` reproduced the same `UNIQUE constraint failed: pinch_markers.id`.
- Green: same targeted test passed after scoping the migration join to one selected group.
- Regression: `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --no-restore` passed `21/21`.
