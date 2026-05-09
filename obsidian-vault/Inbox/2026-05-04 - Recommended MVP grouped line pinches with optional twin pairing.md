---
project: floorplan-fit
type: inbox
date: 2026-05-04
tags:
  - loop-1
  - curation
  - pinch-zones
  - mvp
---

# Recommended MVP: grouped line pinches with optional twin pairing

## Recommendation

For MVP, the system can reuse the current line-selectable review UI and store pinch markers directly on selected geometry lines, grouped by axis-intent (`Width` / `Height`).

## Why it fits the current codebase

- Current review selection is geometry-path based.
- `CuratedWall` currently stores a single `GeometryPathId` plus semantic metadata, not a rich wall-strip or paired-face model.
- Therefore line-level pinch authoring is a practical near-term fit for the existing Desktop/Application flow.

## Important caution

Line-level pinches alone are not sufficient when a wall is represented by two parallel CAD faces. If only one face is trimmed, the wall thickness can desynchronize.

## MVP-safe rule

Accept line-level pinches as the authoring UX, but require either:
- two matching pinches (one per face) in the same group for double-line walls, or
- a future helper that mirrors a pinch to the detected parallel mate.

## Implication

The solver may stay simple and group-based for MVP, but geometric consistency still requires paired-face awareness at least through grouping conventions.

## Refinement: no persisted anchor side for MVP

After further clarification, the recommended MVP can omit a persisted `anchor side` in curation.

Reasoning:
- The user is explicitly placing strategic pinches on the specific lines that may compress.
- If a wall is represented by two faces, the user can place one pinch on each relevant face.
- The saved intent can stay simple: pinch location on line, recortable length, and axis group (`Width` or `Height`).
- The direction of visible compression can be inferred at preview/fit time from the drag interaction or final placement logic, rather than stored as part of the curation model.

MVP implication:
- Persist only grouped line-level pinches plus recortable length and axis intent.
- Do not require wall ownership or anchor-side semantics in the first version.
- Geometric consistency depends on the user placing the needed companion pinches when a rectangular/double-line wall must compress coherently.
