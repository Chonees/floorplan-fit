---
type: inbox
status: captured
date: 2026-05-06
project: floorplan-fit
area: Loop 1 curation
---

# Requirement to bring wall assemblies and room names into curation

## What

The user wants Loop 1 curation to bring in:

- which walls are `2x4`
- which walls are `2x6`
- room names such as living, bathroom, kitchen, bedrooms, etc.

## Current verified state

- The active visual wall geometry currently comes from automatic DXF extraction of wall-like entities, not from a curated wall model.
- `ExtractedWallCandidate` already has `ThicknessMm`, but the current `IxMiliaWallExtractor` sets wall thickness to `null`.
- `SpaceType` exists in Domain, but there is no active room extraction/persistence flow in the current review UI.
- The DXFs contain room-label layers such as `ROOM LBLS`.
- `SEMINOLE2000.dxf` contains wall notes such as `6" WALL`.
- `PLANS/catalog/seminole-2000.json` has room names and polygons, but `PLANS/catalog/` is currently legacy/reference, not the canonical active source.

## Suggested direction

Use DXF as primary truth:

1. Parse room label text from `ROOM LBLS` / text layers.
2. Persist extracted room labels/regions as reviewable room candidates.
3. Extract wall assembly hints from notes like `6" WALL` and/or infer thickness from paired wall geometry.
4. Let the review UI confirm or override `2x4` / `2x6` and room names.

Catalog JSON can be used as fallback/oracle during migration, but should not silently become the canonical source.
