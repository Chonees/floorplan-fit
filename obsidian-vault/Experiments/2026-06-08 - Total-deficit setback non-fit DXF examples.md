---
type: experiment
project: floorplan-fit
date: 2026-06-08
topic_key: experiments/total-deficit-setback-non-fit-dxf-examples
status: active
replaces: Experiments/2026-06-08 - Corrected synthetic setback non-fit DXF examples.md
---

# Total-deficit setback non-fit DXF examples

## What changed

Regenerated the setback examples again after clarifying the intended semantics:

> "No entra por 1 inch de ancho" means the setback/buildable area is **1 inch smaller in total width** than the floor-plan footprint, not that the floor-plan overflows by 1 inch on a single side.

The corrected semantic model is total centered deficit:

- Width deficit `1"`: setback width = footprint width - `1"`, overflow `0.5"` left + `0.5"` right.
- Width deficit `2"`: setback width = footprint width - `2"`, overflow `1"` left + `1"` right.
- Height/length deficit `1"`: setback height = footprint height - `1"`, overflow `0.5"` bottom + `0.5"` top.
- Height/length deficit `2"`: setback height = footprint height - `2"`, overflow `1"` bottom + `1"` top.

## Geometry basis

The examples still preserve the real SEMINOLE2000 wall-footprint proportion:

```txt
footprint width  = 483.786"
footprint height = 930.000"
height / width   = 1.922338
```

## Files

Folder:

```txt
C:\Users\lucas\OneDrive\Escritorio\exports
```

Current files:

- `setback ejemplo 01 - DEFICIT TOTAL - ancho menos 1 inch.dxf`
- `setback ejemplo 02 - DEFICIT TOTAL - ancho menos 2 inches.dxf`
- `setback ejemplo 03 - DEFICIT TOTAL - alto menos 1 inch.dxf`
- `setback ejemplo 04 - DEFICIT TOTAL - alto menos 2 inches.dxf`
- `setback ejemplos - overview.svg`
- `setback ejemplos - README.txt`

## Verified dimensions

```txt
Example 01: footprint 483.786 x 930; setback 482.786 x 930; deficit W/H = 1/0; overflow L/R/B/T = 0.5/0.5/0/0
Example 02: footprint 483.786 x 930; setback 481.786 x 930; deficit W/H = 2/0; overflow L/R/B/T = 1/1/0/0
Example 03: footprint 483.786 x 930; setback 483.786 x 929; deficit W/H = 0/1; overflow L/R/B/T = 0/0/0.5/0.5
Example 04: footprint 483.786 x 930; setback 483.786 x 928; deficit W/H = 0/2; overflow L/R/B/T = 0/0/1/1
```

## Root cause of prior misunderstanding

The previous corrected batch fixed the floor-plan aspect ratio but still modeled a side-specific overflow. That was useful for lateral failure diagnostics but not for the user's requested total width/height deficit examples.

## Design decision

The setback is centered inside the reference footprint to make total dimensional deficit visually unambiguous. If future tests need side-specific failures, create a separate lateral-overflow set instead of mixing semantics.
