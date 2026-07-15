---
type: bug
status: fixed-static
created: 2026-07-13
replaces: "[[2026-07-13 - Executable P0 proof is red#DXF exporter regressions]]"
replaced_by:
---

# Projected DXF binary and manual-review regressions

## Root causes

- Recipe-crossing `ARC`/`CIRCLE` conversion ran inside referenced anonymous `DIMENSION` blocks before the later safety guard could reject those curves for manual review.
- The binary reader assumed every group code was a two-byte `Int16`. A pre-R13 stream begins with one-byte code `0` followed immediately by `SECTION`, so the first two bytes were misread as group code `21248`.
- The binary writer always emitted two-byte group codes, so a reader-only workaround would still have changed and corrupted the source container format.
- The HEADER assertion helper inspected only the first pair after a variable, and three focused tests expected the broad base exception instead of the typed manual-review exception.

## Fix

- Circular-curve pre-conversion now applies only to top-level `ENTITIES`; referenced `DIMENSION` blocks remain untouched until `ThrowIfUnsupportedRecipeCurveCrossings` rejects a crossing curve with `ProjectedPlanSheetManualReviewRequiredException`.
- Binary format detection validates the first required `0/SECTION` pair and distinguishes pre-R13 one-byte codes from R13+ two-byte codes.
- Legacy code `255` is handled as an escape followed by an `Int16`, while groups `290-299` keep their one-byte boolean payload regardless of group-code framing; output otherwise uses the same group-code representation as its source.
- Fixed-length binary reads reject short payloads, including declared binary chunks, and parsing now requires the terminal `0/EOF` pair instead of accepting physical stream end.
- HEADER assertions scan the complete variable payload until the next code `9` or `0`.
- ELLIPSE, unsupported LEADER, and referenced DIMENSION-block curve tests now assert the typed manual-review exception while preserving their messages and output-absence checks.

## Evidence

- Static inspection confirms `21248` was not added as a recognized group code.
- The existing AutoCAD binary-corruption regression remains unchanged.
- Focused regressions cover a truncated declared binary chunk and a legacy binary stream missing its terminal `0/EOF` pair.
- `git diff --check` exits `0` for the two owned files.
- No build, test, restore, watch, or application command was run, per task constraints; executable proof remains pending.

## Files

- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`
