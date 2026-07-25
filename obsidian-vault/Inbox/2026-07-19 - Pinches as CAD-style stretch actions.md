---
type: inbox
status: accepted
date: 2026-07-19
project: FloorplanFit
area: Loop 1 compression
replaces: "[[2026-07-19 - Closed pinch scope needs explicit confirmation]]"
replaced_by: "[[2026-07-19 - Adopt CAD-style pinch stretch actions]]"
---

# Pinches as CAD-style stretch actions

## Problem

The current engine applies coordinate-threshold transforms to every supported point after a pinch. It does not know which vertices belong to the intended wall span, which entities must move rigidly, or that two parallel wall faces represent one wall. This can stretch unrelated geometry, double-count a reduction, and alter wall thickness.

## Researched model

AutoCAD's STRETCH behavior is selection-set and vertex aware: selected endpoints/vertices are stretched, while objects fully inside the crossing selection move rigidly. Dynamic-block stretch actions persist a linear parameter/key point plus the exact selection set affected by that action. Geometric and dimensional constraints preserve design intent such as coincidence, parallelism, and exact distances.

## Proposed FloorplanFit model

The user's paired markers already provide the explicit target selection: one marker on each face of the same wall, at the same cut station. Do not add a separate wall-pair inference flow, whole-building topology graph, or general selection-set authoring UI.

Treat each paired pinch group as one persisted, versioned CAD-style stretch action:

- axis and direction;
- anchor side and exact displacement;
- the two exact marked wall-face spans that may shorten;
- synchronized paired wall faces, counted once rather than as additive markers;
- one closing-side rigid translation for complete entities;
- maximum capacity and half-inch input policy;
- invariants and audit result.

The pair's shared station is the center of the removable strip. The requested delta is applied once to the pair: both marked faces shorten identically, complete entities on the closing side translate rigidly, and fixed-side entities stay unchanged. An unmarked entity crossing the cut is never silently stretched; it blocks the operation for review. No whole-building topology graph is required.

## Safety contract

- Only the two exact marked wall-face spans may change length.
- Every entity in the rigid move set receives exactly one identical translation vector.
- Paired wall faces receive the same displacement and preserve thickness.
- Unselected entities remain coordinate-identical.
- Requested reduction equals measured output reduction.
- An unmarked or unsupported entity crossing the cut fails closed before export.
- Preview, canonical FloorPlan export, and dependent Electrical projection replay the same persisted recipe-v2 action through one pure engine.

## Compatibility with the current canonical-to-Electrical pipeline

The existing pipeline is reusable. Canonical adjustments already persist versioned recipe JSON; confirmed Electrical registration already maps Electrical coordinates into canonical FloorPlan coordinates and binds proof to the dependent source SHA-256; the export handler already resolves registration, canonical adjustment, manual-review gating, and projected export.

The minimum replacement is limited to the payload and deformation execution:

- keep HousePlanSet, registration, proof, projection, package, manifest, and audit orchestration;
- keep reading historical `v1` recipes;
- write `v2` actions into the existing recipe JSON, requiring no new SQLite table for the minimum design;
- replace coordinate-threshold `ApplyRecipe` logic with entity/vertex-aware action resolution;
- make Preview, canonical FloorPlan DXF, and Electrical DXF use one shared pure engine.

In short: retain the pipeline; replace its deformation engine.

## Product decision pending

Accepted by the user on 2026-07-19. Decision: [[2026-07-19 - Adopt CAD-style pinch stretch actions]].
