# HousePlanSet Phase 6 implementation plan written

## What
Created and committed the Phase 6 plan for facade/elevation registration and projection.

## Why
Facade/elevation sheets cannot be treated like floor/electrical overlays: they are vertical views with horizontal correspondence to the canonical floor plan. The plan keeps the canonical floor-plan adjustment as the single source of truth while preserving facade vertical scale/height semantics by default.

## Current truth
- Commit: `fb56b4f` (`docs: plan facade elevation projection rules`).
- Plan file: `docs/superpowers/plans/2026-06-30-house-plan-set-facade-elevation.md`.
- Intended registration method: `FacadeHorizontalReference`.
- Intended projection method: `FacadeHorizontalPreservingVerticals`.
- Rule summary: `PreserveVertical=true;HorizontalReference=<name>`.
- Projection should compose canonical horizontal scale/offset only; rotation and `TranslateY` stay `0`.

## Boundaries
- No automatic opening/window extraction.
- No real elevation height calibration yet.
- No facade DXF rewrite/export.
- No independent facade/elevation fit engine.
- No Desktop UI behavior.

## Verification
- Scoped `git diff --check` and `git diff --cached --check` passed for the plan file.
- No `dotnet build` was run.
