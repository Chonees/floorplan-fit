---
type: Decision
date: 2026-05-07
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop1
  - dxf
  - extraction
  - curation
---

# Extraction pipeline targets Pointe floorplans with profile-backed conventions

## What we verified

The current extraction pipeline is not hardcoded to one floor plan file name like `SEMINOLE2000`, but several extractor rules are CAD conventions observed in the current Pointe Homes DXFs.

## Current pipeline shape

- Generic parts:
  - DXF file path is dynamic.
  - Extraction runs are persisted per floor plan/template/version.
  - Review DTOs, SQLite persistence, preview layers, selection, removal, and publish flow are intended to support many floor plans.
- Convention-specific parts now centralized in `DxfExtractionProfile.PointeHomes`:
  - Walls: layers containing `WALL`, excluding `ELECTRICAL` for physical thickness inference.
  - Rooms: layer `ROOM LBLS` plus room-name keyword filtering.
  - Openings: geometry layers `DOORS`, `WIN`, `WINS`; label layers `DOORTEXT`, `WINDWS LBLS`; regex for model/size labels.
  - Fixed components: layers `FIXTURES`, `CABS`, `CABS-FLOORPLAN`; block-name heuristics such as `TOILET`, `STOVE`, `DISHWASHER`, `WASH`, `DRY`, `SINK`, `TUB`, `SHOWER`.

## Decision

The product should remain built for **N Pointe Homes floor plans**, not for only the current plan. The CAD convention logic belongs in a profile/rule layer, currently implemented as `DxfExtractionProfile.PointeHomes`.

## Why

Different Pointe Homes floor plans may use different layers, block names, or annotation conventions. If those rules stay buried inside extractors, every new plan variation becomes code churn and the system becomes fragile.

## Intended direction

- Keep extractors generic in mechanics: read DXF entities, transform geometry, preserve visual metadata.
- Keep recognition rules in profile/config:
  - layer aliases
  - block-name aliases
  - artifact kind mapping
  - label filters
  - false-positive filters
  - scoring/confidence rules
- Keep manual curation per floor plan as the final source of truth.
- Later, allow admin corrections to feed reusable profile rules when the same mistake appears across many Pointe plans.

## Related files

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaWallExtractor.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaRoomLabelExtractor.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaOpeningExtractor.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaFixedPlanComponentExtractor.cs`
- `src/FloorplanFit.Infrastructure/Dxf/DxfExtractionProfile.cs`
