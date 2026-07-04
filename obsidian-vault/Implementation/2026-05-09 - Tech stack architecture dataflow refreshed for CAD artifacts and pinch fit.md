---
type: Implementation
date: 2026-05-09
status: active
tags:
  - architecture
  - documentation
  - loop1
  - loop2
---

# Tech stack architecture/dataflow refreshed for CAD artifacts and pinch fit

## What

Updated `TECH-STACK-ARCHITECTURE-DATAFLOW.md` so the canonical technical source matches the current architecture direction.

## Why

The previous version still described the old wall-only / curated-wall fit model, including `CuratedWalls`, overlay against curated walls, `Load curated walls`, and wall-only scope. That no longer matches the CAD-faithful curation + pinch-native adaptation model.

## Where

- `TECH-STACK-ARCHITECTURE-DATAFLOW.md`
- `obsidian-vault/Current State.md`

## Learned

- The technical source now treats Loop 1 as extraction by CAD artifact family plus persisted curation.
- The future fit engine should consume a published curation, active artifacts, pinch groups, pinch markers, project overrides, and future dimensions.
- Wall candidates remain important, but they are not the only input and not the complete curation contract.
