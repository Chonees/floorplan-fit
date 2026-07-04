---
type: implementation
date: 2026-06-30
topic: architecture/modularization
---
# 2026-06-30 - Modularization assessment prompt

## What
Verified the current Floorplan Fit solution before proposing modularization: it is already a layered modular monolith (`Desktop`, `Application`, `Domain`, `Infrastructure`, `Contracts`), but important responsibilities are still grouped too broadly around `FloorPlans` and several large Desktop/Application/Infrastructure files.

## Evidence
- Solution projects: `FloorplanFit.Desktop`, `FloorplanFit.Application`, `FloorplanFit.Domain`, `FloorplanFit.Infrastructure`, `FloorplanFit.Contracts`.
- Application namespaces already split into `Import`, `Extraction`, `Curation`, `Review`, `Library`, and `SitePlanAdjustment` under `FloorPlans`.
- Architecture doc already states the intended flow: raw DXF -> CAD-family extraction -> persisted curation -> site-plan envelope -> deterministic fit -> export/audit.
- Largest files indicate extraction/curation/preview/site-plan adjustment are doing too much in a few places: `FloorPlanReviewViewModel.cs`, `FloorPlanPreviewControl.cs`, `SitePlanAdjustmentViewModel.cs`, `SqliteSchemaInitializer.cs`, `DimensionGeometryProjector.cs`, and IxMilia DXF adapters.

## Current architectural reading
The next modularization should not create random projects first. It should define vertical product modules by lifecycle and data ownership, then move code only when a boundary is proven by use case, data, and test seam.

Candidate modules:
1. Import and managed documents.
2. CAD extraction by artifact family.
3. Floor-plan curation and publishable truth.
4. Measurement/dimensions and interval bindings.
5. Preview/review interaction shell.
6. Site-plan import/envelope extraction.
7. Fit/adjustment proposals.
8. Export/audit.
9. Workspace/storage/security runtime foundation.
10. Telemetry/data collection for extraction quality and user corrections.

## Boundary rule
Each module should own: use cases, ports, DTOs, persistence tables/read models, tests, and UI coordination entry points. Shared code must be tiny and boring: units, geometry primitives, IDs, clock, storage abstractions.
