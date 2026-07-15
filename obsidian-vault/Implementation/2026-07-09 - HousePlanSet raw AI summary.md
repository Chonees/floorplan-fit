---
type: Implementation
date: 2026-07-09
replaces: []
replaced_by: null
---

# HousePlanSet raw AI summary

## Problem
The UI audit panel was dumping technical strings: full manifest paths, sheet storage paths, and long `RecipeHandlingSummary` text. This made the result hard to read even though the JSON artifacts contained the necessary technical evidence.

## Change
The UI now shows a deterministic raw explanation built from audit evidence:
- what the user asked to shrink;
- what the FloorPlan shrank;
- what the ElectricalPlan received and applied;
- what Electrical did not shrink and why;
- DXF safety status;
- explicit missing-data warnings when evidence is insufficient.

The raw recipe/path detail remains in JSON artifacts instead of the visual panel.

## Files
- `CreateMultiSheetExportAuditHandler.cs` builds the stronger `HumanSummary` lines.
- `SitePlanAdjustmentViewModel.cs` no longer appends raw sheet recipe/path lines in the UI audit panel and no longer repeats full manifest paths in status/details.
- `test-plan-set-human-summary-contract.ps1` now checks for raw explanation and rejects dumping recipe/path lines in the UI audit panel.

## Verification
No build was run due AGENTS.md. Allowed checks passed:
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check` on touched files, with line-ending warnings only.
