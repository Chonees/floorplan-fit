# HousePlanSet design spec refreshed as-built

## What changed
Updated the main HousePlanSet modularization spec so its current-state evidence matches the actual working tree after the implementation slices.

## Why
The design file still described HousePlanSet as future work, while the repo now contains the backbone modules. That drift would mislead future implementation sessions and make it easier to accidentally return to a floor-plan-only model.

## Updated
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`
- Added an as-built 2026-07-01 evidence section.
- Added a phase status matrix for phases 1-7.

## Current truth
The HousePlanSet backbone is partially implemented: explicit HousePlanSet/PlanSetVersion identity, multiple sheets, classification/import, registration/projection per sheet type, canonical adjustment persistence, package export/audit, manual confirmation handlers, and DataCollection quality reporting exist. The remaining major product gap is final visible UX for importing/registering/reviewing dependent sheets and validating deeper sheet-specific rules against real roof/facade examples.

## Verification
- Searched the updated spec for stale phrases such as `no first-class`, `found no`, placeholders, and TODO markers.
- `git diff --check` passed for the updated spec.
- No `dotnet test` or `dotnet build` was run due repository rule.
