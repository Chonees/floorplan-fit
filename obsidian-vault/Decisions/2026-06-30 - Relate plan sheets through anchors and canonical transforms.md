---
type: decision
date: 2026-06-30
topic: architecture/sheet-relationship-graph
replaces: 2026-06-30 - House plan set adjustment model
---
# 2026-06-30 - Relate plan sheets through anchors and canonical transforms

## Decision
Related house sheets must be connected through an explicit sheet relationship/registration model, not by assuming every DXF sheet shares the same geometry or can be blindly scaled.

## Why
A house plan set contains floor, electrical, roof, and facade/elevation sheets. They are related, but the relationship is different by sheet type: electrical is usually an overlay of floor plan elements, roof can include overhang/ridge/eave rules, and facades/elevations are projected views that share horizontal references but not the same 2D coordinate system.

## Model
- The curated floor plan defines the canonical house coordinate frame and adjustment transform.
- Each dependent sheet stores a `SheetRegistration` into that canonical frame.
- Registration is built from anchors, not whole-drawing assumptions: footprint corners, exterior wall lines, dimension corridors, room labels, openings/windows/doors, electrical symbols, roof eaves/ridges, and facade grid/opening centers.
- The fit engine produces one canonical adjustment transform; dependent sheets receive a sheet-specific projection of that transform.

## Confidence rule
The system may propose registration automatically, but must persist confidence, anchor matches, warnings, and manual confirmations. If the relation is ambiguous, the product should ask for a small calibration/confirmation instead of guessing silently.

## Minimal-first path
Start with whole-sheet similarity registration for electrical sheets, then add piecewise/pinch projection and roof/facade-specific rules only when needed. Do not build four independent fit engines.
