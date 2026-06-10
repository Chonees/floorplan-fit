---
type: experiment
project: floorplan-fit
date: 2026-06-08
topic_key: experiments/synthetic-setback-non-fit-dxf-examples
status: superseded
replaced_by: Experiments/2026-06-08 - Corrected synthetic setback non-fit DXF examples.md
---

# Synthetic setback non-fit DXF examples

## What

Created synthetic DXF examples in the operator-facing exports folder to visually validate exact setback non-fit cases by 1 inch and 2 inches.

Folder:

```txt
C:\Users\lucas\OneDrive\Escritorio\exports
```

## Files

- `setback ejemplo 01 - largo excede 1 inch - lado derecho.dxf`
- `setback ejemplo 02 - largo excede 2 inches - lado izquierdo.dxf`
- `setback ejemplo 03 - ancho excede 1 inch - lado superior.dxf`
- `setback ejemplo 04 - ancho excede 2 inches - lado inferior.dxf`
- `setback ejemplos - overview.svg`
- `setback ejemplos - README.txt`

## Geometry contract

- DXF units: inches (`$INSUNITS = 1`).
- Common buildable setback rectangle: `(20,20)` to `(240,160)`, i.e. `220" x 140"`.
- Layer convention:
  - `PROPERTY`: grey reference/property boundary.
  - `SETBACK`: orange buildable area; detected by the current site-plan preview reader because the layer contains `SETBACK`.
  - `FLOORPLAN_OVERFLOW`: red footprint that does not fit.
  - `OVERFLOW_STRIP`: red exact violating strip.

## Verified cases

- Example 01: overflow `1"` to the right, testing length/right-side failure.
- Example 02: overflow `2"` to the left, testing length/left-side failure.
- Example 03: overflow `1"` to the top, testing width/top-side failure.
- Example 04: overflow `2"` to the bottom, testing width/bottom-side failure.

## Verification

A filesystem/parser check confirmed all files exist, each DXF has `$INSUNITS = 1`, each has `SETBACK` and `FLOORPLAN_OVERFLOW` line geometry, and the measured overflow tuples are exact:

```txt
Example 01 L/R/B/T = 0/1/0/0
Example 02 L/R/B/T = 2/0/0/0
Example 03 L/R/B/T = 0/0/0/1
Example 04 L/R/B/T = 0/0/2/0
```

## Why it matters

These files isolate Loop 2 fit semantics without relying on a noisy real site plan. They are useful for visual QA of setback detection, placement, and future auto-fit diagnostics.


## Superseded

This first batch was superseded because it used arbitrary horizontal rectangles (`220" x 140"`) instead of preserving the real SEMINOLE2000 wall-footprint ratio (`483.786" x 930"`, height/width `1.922338`). Use the corrected note/files instead:

- [[2026-06-08 - Corrected synthetic setback non-fit DXF examples]]
