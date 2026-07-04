# HousePlanSet Phase 6 facade elevation projection implemented

## What
Implemented the first Phase 6 facade/elevation registration and projection slice.

## Why
Facade/elevation sheets are vertical drawings. They relate to the canonical floor plan through horizontal references, but their vertical scale/height truth should not be silently rescaled by the floor-plan fit.

## Current truth
- Commits:
  - `fb56b4f` (`docs: plan facade elevation projection rules`)
  - `9dd0d3d` (`feat: support facade elevation projection rules`)
  - `ff0d5a7` (`docs: record facade elevation bridge`)
- New registration method: `FacadeHorizontalReference`.
- New projection method: `FacadeHorizontalPreservingVerticals`.
- Registration stores `RuleSummary = PreserveVertical=true;HorizontalReference=<name>`.
- Projection composes canonical horizontal scale/offset into the facade/elevation horizontal reference.
- Projection intentionally emits rotation `0` and `TranslateY = 0` so vertical values remain sheet-native.
- Export readiness requires confirmed registration, confidence >= `0.8`, zero canonical compression steps, and the vertical-preservation rule.

## Boundaries
- No opening/window extraction.
- No height calibration.
- No facade/elevation DXF rewrite/export.
- No Desktop UI behavior beyond DI registration.
- No independent facade/elevation fit engine.

## Verification
- Test-first RED evidence: facade production handler files were absent before implementation (`Test-Path` returned `False` for both registration and projection handlers).
- Scoped `git diff --check` and `git diff --cached --check` passed.
- No `dotnet test` and no `dotnet build` were run due repository rule.
