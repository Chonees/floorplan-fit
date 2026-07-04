---
type: Bug
date: 2026-06-15
project: floorplan-fit
status: active
related:
  - ../Implementation/2026-06-15 - Synthetic site plans use Pointe layer appearance.md
  - ../Current State.md
tags:
  - floorplan-fit
  - loop2
  - dxf
  - autocad
  - synthetic-fixtures
---

# Synthetic site plan 05 stale locked DXF opens black

## Symptom

After regenerating the synthetic site plans, AutoCAD can still show the black/blank `Drawing1` loop when opening this exact file:

```txt
D:\PointAIData\PLANS\originalsSitePlans\SYNTH SITE PLAN 05 - LOTE TRAPEZOIDAL - alto menos 2 inch.dxf
```

## Fresh verification

`python verify_synthetic_siteplans.py` fails only for that exact-name file. AutoCAD Core Console rejects it with exit `53`; the verifier reports:

```txt
Error in APPID Table
DXF read error on line 226.
Invalid or incomplete DXF input -- drawing discarded.
ErrorStatus=53
```

A direct file check shows the stale exact-name file is still only `2242` bytes, while the valid fallback replacement is `114189` bytes:

```txt
SYNTH SITE PLAN 05 - LOTE TRAPEZOIDAL - alto menos 2 inch - AUTOCAD FIXED.dxf
```

The fixed fallback file opens/audits in AutoCAD Core Console with exit `0`.

## Root cause

The generator already writes the valid Pointe-structured DXF, but it could not overwrite the exact-name 05 file because Windows reports that file is being used by another process. So the stale invalid file remains on disk under the original name.

## Current fix path

1. Close/release the exact-name 05 DXF in AutoCAD or the process holding it.
2. Rerun `python generate_synthetic_siteplans.py`.
3. Rerun `python verify_synthetic_siteplans.py`; expected result is all generated synthetic site plans pass without needing the `- AUTOCAD FIXED` fallback.
