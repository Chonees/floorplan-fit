---
type: Bugs
date: 2026-07-13
replaces: []
replaced_by: null
---

# Canonical extraction lineage missing from latest manifest

## Problem

The latest HousePlanSet manifest identifies the adjusted canonical output, but it does not expose the complete lineage back to the imported canonical DXF and the extraction run that produced the curated geometry.

This makes two different artifacts easy to confuse:

- canonical source/extraction: `SEMINOLE2000.dxf`
- latest adjusted canonical export: `fresh final test.dxf`

## Verified lineage

- Canonical template: `seminole2000` / `SEMINOLE2000`
- Canonical FloorPlan version: `937291f9-3904-489a-907a-abab8bc8355a`
- Original imported filename: `SEMINOLE2000.dxf`
- Managed filename: `SEMINOLE2000-21.dxf`
- Source SHA-256: `2C8ACFBE120668844D803A0ACA482B8B75C8AA72A9536D35EB86C62C9C80C8A4`
- Last successful extraction: `c2496c35-ad07-4846-a68c-6bcc34027d69`
- Extractor/status/time: `ixmilia-line-segments` / `Completed` / `2026-05-20T04:27:55.7853173Z`
- Later ignored recovery run: `fc119d23-fe8f-4323-ac5e-8c9b849aee9a`
- Latest canonical adjustment: `67d4f5e5-675e-44bb-9f7a-c29384a2e7b6`
- Latest adjusted canonical output: `C:\Users\lucas\Downloads\fresh final test.dxf`
- Latest manifest: `b4e8192e70a146f3910a4fc77988b028\manifest.json`

## Storage verification

The old build-workspace copy and the LocalAppData copy of `SEMINOLE2000-21.dxf` both exist, have length `3,092,023` bytes, and have the same SHA-256. There is no observed source-content drift, but the database lineage still records the old build-relative path instead of the durable LocalAppData path.

## Missing observability

One audit record should connect, without querying SQLite manually:

`original filename -> managed source path/hash -> extraction run -> active curation -> canonical version -> adjustment -> final export`

Until that exists, the final manifest proves the output but not the full canonical provenance in one place.
