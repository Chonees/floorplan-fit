---
type: Bug
date: 2026-05-13
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - dimensions
  - review-session
  - duplicate
---

# Review session surfaced duplicate native dimensions when the DXF contained twin DIMENSION entities

## What happened

Some DXFs contain two native `DIMENSION` entities with different handles / anonymous block names but the exact same visible authored geometry. When one of those twins was edited, the other one stayed behind in preview, so moving a dimension upward revealed a black “calco” below it.

## Root cause

`SqliteFloorPlanReviewSessionReader.GetDimensions(...)` returned every extracted dimension row after overlaying overrides, but it never collapsed duplicate twins that represented the same visible CAD dimension.

That meant the session could contain:

- one edited twin (for example handle `3ACC`)
- one untouched twin with identical base geometry (for example handle `1689`)

and both were rendered as independent dimensions.

## Fix

The review-session reader now groups dimensions by a base authored-geometry signature and keeps only one visible dimension per duplicate cluster.

Selection rule:

1. prefer the edited/overridden twin
2. otherwise prefer the later / higher-priority extracted twin

This fixes the preview symptom without changing schema.
