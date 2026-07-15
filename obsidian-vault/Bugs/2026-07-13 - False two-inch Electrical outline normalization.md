---
type: Bugs
status: open
date: 2026-07-13
replaces: "[[2026-07-13 - Fresh final output congruence proof passed]]"
replaced_by: null
---

# False two-inch Electrical outline normalization

## Problem

The original SEMINOLE FloorPlan and ElectricalPlan dominant exterior walls have the same `468" x 930"` footprint after translation, but the pipeline reported the Electrical source as `470"` wide, applied a false horizontal normalization, and later declared the adjusted outputs congruent.

## Verified source geometry

App-managed import snapshots used by the pipeline:

- Floor dominant vertical edges: `X=94.574189` and `X=562.574189`; width `468"`.
- Electrical dominant vertical edges: `X=64.094337` and `X=532.094337`; width `468"`.
- Source translation between those dominant footprints: `+30.479852"` X and approximately `+63.802325"` Y; scale is `1`.

The Electrical layer also contains short one-inch-outboard fragments:

- `X=63.094337`: four segments, total support `168"`.
- `X=533.094337`: two segments, total support `72"`.

Those fragments made the selected Electrical envelope `470"` even though the dominant wall pair is `468"`.

## Root cause

- `ProjectedPlanSheetDxfExporter.TryCollectRegisteredAnchorBounds` takes raw min/max coordinates from every entity on WALL/EXTERIOR/STRUCT layers.
- `PlanSetOutlineSegmentCongruenceAuditBuilder.SupportedBounds` accepts every coordinate with cumulative support at least `24"` and then takes the outermost min/max.
- Both paths therefore choose the same short fringe fragments instead of the dominant exterior wall pair.

The exporter then applied false normalization `scaleX=0.9957446808508176` before replaying the canonical width recipe.

## False-green proof

- Final Floor dominant wall width: `464.400000"`.
- Final Electrical dominant wall width: `463.308510"`.
- Real dominant-wall mismatch: Electrical is approximately `1.091490"` too narrow.
- Final Electrical fringe envelope: `464.400000"`.

The final audit compared the same incorrect fringe envelope and returned `FinalOutputCongruent`, masking the dominant-wall mismatch the user saw in AutoCAD.

## Required correction

Select and compare dominant corresponding exterior wall pairs/anchors, preserve scale `1` when those pairs agree, and expose both selected and rejected edge candidates with support evidence. A translation-invariant bbox equality must not be sufficient for automatic approval.

## 2026-07-13 - Selector review found a remaining false-rectangle path

The first dominant-pair implementation rejected X/Y candidates whose shared-support bands were completely outside the opposite axis span. Static adversarial review found that this is necessary but not sufficient: interior vertical and horizontal stubs can overlap each other's broad spans without touching at any corner, so independent axis selections can still synthesize a rectangle that does not exist in the DXF.

Required fail-closed rule: the selected vertical and horizontal edge pairs must prove actual cross-axis connectivity/coherent corner support, not merely span overlap. A focused RED fixture must cover non-touching interior stubs before the selector is changed.

The same review found two adjacent audit defects that must be fixed under RED coverage:

- `SOLID` vertices cannot be traversed in raw DXF tag order. AutoCAD defines point 3 opposite point 2 and point 4 opposite point 1, so the perimeter is `1 -> 2 -> 4 -> 3`; raw `1 -> 2 -> 3 -> 4` introduces diagonals and drops two axis-aligned sides. `3DFACE` ordering must remain unchanged.
- When `VerificationPath` overrides `StoragePath`, the final audit currently reads the verification file but reports the storage path. Provenance must name the file actually inspected.

Follow-up review proved that fixing only the successful congruence branch was insufficient: every `InsufficientFinalOutput` branch and the exception path still discarded the resolved Floor verification path and reverted Electrical provenance to `StoragePath`. The resolved paths must be carried through success, insufficient-data, and exception artifacts alike.

## 2026-07-13 - Insufficient-output provenance repaired statically

- A focused Infrastructure RED uses distinct Floor/Electrical `StoragePath` and `VerificationPath` files, then reaches the no-comparable-structural-segments branch after both verification DXFs are read.
- `BuildFinalOutputSheet` now resolves both inspected paths before its `try`; all five insufficient-data returns and the catch forward them to `InsufficientFinalOutput`.
- `InsufficientFinalOutput` now emits those resolved paths while preserving its reason, status, and verification-gating semantics.
- Scoped `git diff --check` exits `0`. No `.NET` command was run, so executable proof remains pending.
