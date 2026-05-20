---
type: Implementation
date: 2026-05-08
status: active
tags:
  - architecture
  - documentation
  - loop1
replaces: [[Implementation/2026-04-30 - Mapa completo de arquitectura y archivos del repo]]
---

# Architecture map refreshed for CAD-faithful curation

## What

Updated `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md` against the current working tree.

## Why

The repository moved from the older curated-wall-centric review model to a CAD-faithful Loop 1 curation surface with pinch-native shrink zones and separated artifact families.

## Where

- `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`
- `obsidian-vault/Current State.md`

## Learned

- The current map covers 320 relevant present files: 302 versioned present files plus 18 relevant untracked files.
- The active architecture families are wall candidates, pinch groups/markers, room labels, opening candidates/labels, fixed plan components, protected detail assemblies, and future dimensions.
- New CAD artifact families must keep a full lifecycle: profile/extractor, detected model, domain/persistence, DTO, preview layer, selection/curation action, and tests.
