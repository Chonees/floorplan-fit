---
type: implementation
created: 2026-07-04
status: proposed
---

# Goal prompt - FloorPlan to Electrical pinch reprojection

## Scope
Implement only FloorPlan -> ElectricalPlan synchronization for canonical local adjustment recipes.

Do **not** include RoofPlan, Facade/Elevation, bidirectional sync, UI redesign, unrelated refactors, or destructive DXF filters in this goal.

## Core thesis
The curated FloorPlan is the only canonical geometry owner. ElectricalPlan is a dependent sheet. When the FloorPlan is adjusted to a site plan, the ElectricalPlan must receive the same approved local structural deformation through registration, not by inventing a separate adjustment engine.

## Required formula
For every safe electrical geometry point:

1. `pFloor = ElectricalToFloorRegistration(pElectrical)`
2. `pFloorAdjusted = ApplyCanonicalPinchRecipe(pFloor)`
3. `pOutput = FloorToSitePlacement(pFloorAdjusted)`

The recipe is applied in FloorPlan source coordinates, never directly in Electrical source coordinates.

## Safety contract
- Always regenerate ElectricalPlan from the original Electrical DXF + registration + canonical FloorPlan recipe.
- Preserve electrical-specific content: wiring/cables, outlets, switches, symbols, fixture blocks, room labels, dimensions, doors, arcs, ellipses, text.
- Move points according to the canonical recipe; do not globally squeeze entity internals unless the entity type is explicitly understood.
- Text/MTEXT: move insertion point; do not deform glyphs.
- INSERT blocks: move insertion and transform supported attributes safely; do not explode blocks.
- ELLIPSE: center is a point; major-axis vector is a vector, not a translated point.
- DIMENSION: move definition/control points and associated anonymous graphic block consistently.
- Unknown or crossing geometry must become `RequiresManualConfirmation`; do not guess.

## Implementation agents/tasks

### Agent 1 - Current-system mapper
Question: where does the app already persist canonical adjustment, registration, projection, export, and audit?

Tasks:
- Map the exact current flow from FloorPlan adjustment to Electrical export.
- Identify existing DTO/domain/repository/exporter boundaries.
- Report what is done, partial, missing.
- Do not modify production code.

Stop when it can name the exact files and methods that carry: canonical recipe, registration transform, projection status, DXF export, manifest/audit.

### Agent 2 - Geometry contract/TDD
Question: can one electrical point be mapped into floor coordinates, receive the same pinch, and land in output coordinates deterministically?

Tasks:
- Write red tests first for the pure formula.
- Cover right/left/top/bottom compression and multiple accumulated pinches.
- Include a point before the pinch line that must not move.
- Implement the smallest pure helper needed.

Stop when the pure function explains the math with tests and no DXF involved.

### Agent 3 - DXF integration
Question: can the exporter apply that point projection to safe DXF entities without corrupting CAD semantics?

Tasks:
- Wire the recipe-aware point projector into projected Electrical export only.
- Keep affine-only behavior for cases without local recipe.
- Preserve current fixed behavior for binary DXF, door arcs, wiring, ellipses, dimensions, and blocks.
- Add minimal exporter regression tests for representative DXF pairs/entities.

Stop when the exporter can apply recipe-aware point projection without reintroducing the old invalid/black DXF or ellipse garabato bugs.

### Agent 4 - Product workflow/audit
Question: when the FloorPlan adjustment changes, how does the user know the Electrical projection is stale and needs review/re-export?

Tasks:
- Keep current manual-confirmation safety for local compression until proven automatic.
- Ensure manifest/audit says whether Electrical used affine placement only, recipe-aware projection, or requires manual review.
- Define the next-state lifecycle: Current -> Stale -> Reprojected -> Reviewed.
- Do not implement broad stale UI unless the core geometry works first.

Stop when SEMINOLE can demonstrate an Electrical projection created from original DXF with recipe summary and review status.

### Agent 5 - Verification
Question: did we prove the actual user case, or only make tests pass?

Tasks:
- Verify SEMINOLE with a local patio/porch/floor compression.
- Open/inspect exported Electrical DXF behavior through non-build checks and available scripts.
- Compare against original: wiring remains, door arcs remain, dimensions are not invented, ellipses do not become huge circles.
- Run no build command if repo rules forbid it.

Stop only with evidence paths: manifest, exported file path, relevant test names/source checks, and known limitations.

## Anti-loop rules
- Max 3 attempts for the same repeated symptom. After 3, stop and report the architectural gap instead of patching randomly.
- Do not use destructive filters to hide CAD artifacts.
- Do not quarantine whole layers unless proven wrong by metadata and visual comparison.
- Do not touch Roof/Facade in this goal.
- Do not introduce a second independent adjustment engine for Electrical.
- Do not mark success if only affine placement works; local pinches must be represented.
- Do not claim final completion without SEMINOLE evidence.

## Definition of done
This goal is done only when:

1. FloorPlan adjustment saves a canonical recipe with local compression operations.
2. ElectricalPlan registered to that FloorPlan is regenerated from the original Electrical DXF.
3. Electrical geometry receives the same local coordinate displacement through the formula:
   `Electrical -> Floor -> Recipe -> Output`.
4. Safe entity types preserve CAD meaning.
5. Unknown/unsafe cases remain manual instead of silently corrupted.
6. SEMINOLE export proves patio/porch-style local compression affects the dependent ElectricalPlan.
7. Manifest/audit explains recipe status, confidence, warnings, and manual review needs.
8. No known regression reappears: invalid/empty DXF, missing wiring, giant ellipse/arc garabatos, broken floor-plan export.
