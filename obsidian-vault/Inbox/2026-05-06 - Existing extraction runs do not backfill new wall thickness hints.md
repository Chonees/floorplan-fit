---
type: inbox
project: floorplan-fit
date: 2026-05-06
topic_key: gotchas/extraction-runs-not-backfilled
status: captured
---

# Existing extraction runs do not backfill new wall thickness hints

## Context

After adding geometry-based 2x4 / 2x6 inference, the user reported that every wall line still showed `Thickness unknown`.

## Verification

The local Desktop SQLite database showed the latest `SEMINOLE2000` extraction run was still from `2026-05-04T20:07:36.9517232Z` and all 738 candidates had `thickness_mm = NULL`.

## Meaning

The extractor code works for new extraction runs, but previously persisted `extracted_wall_candidates` are immutable historical rows. They are not backfilled when extractor behavior changes.

## Operational fix

To see new assembly hints, rerun extraction after restarting the app, then reopen Review:

1. restart Desktop / `dotnet watch`
2. select the floor plan
3. click `Extract Walls`
4. close and reopen `Open Review`

## Future UX improvement

Consider versioning the extractor string or showing a warning when the latest extraction was produced by an older extractor behavior.
