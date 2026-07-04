# Recipe handling summary reaches package manifest

## What
The canonical adjustment recipe reporting slice now includes manifest coverage: dependent sheet export metadata must serialize `RecipeHandlingSummary` so the package report can show what local recipe operation was detected and why it still requires review before DXF deformation.

## Why
The first demonstrable AdjustmentRecipe route must stop at a reportable loop, not at hidden in-memory state:
FloorPlan adjustment -> recipe derived from placement/compression -> dependent projection consumes recipe -> export/report says applied vs review.

## Where
- `tests/FloorplanFit.Infrastructure.Tests/PlanSets/PlanSetExportManifestWriterTests.cs`

## Verification
- `git diff --check` exited 0 with CRLF warnings only.
- `rg -n "RecipeHandlingSummary|AdjustmentRecipeSummaryDto|recipe_handling_summary|HorizontalCompression|VerticalCompression" src tests docs obsidian-vault` found the route across contracts, projectors, persistence, export audit, desktop UI, and tests.
- Build/test intentionally not run because this repo says never build after changes.

## Out of scope
This still does not deform ElectricalPlan geometry with local compression. That is the next slice and needs entity policy/anchors to avoid breaking DXF output.
