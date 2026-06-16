---
type: Inbox
date: 2026-06-15
project: floorplan-fit
status: implemented
related:
  - ../Experiments/2026-06-08 - Total-deficit setback non-fit DXF examples.md
  - ../Implementation/2026-06-07 - Library adjust to site plan preview.md
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - synthetic-fixtures
  - dxf-layers
---

# Synthetic site plans must use Pointe layer appearance

## User request

Synthetic site plans created by us should live/use the `D:\PointAIData\PLANS\originalsSitePlans` site-plan corpus and look like Pointe Homes site plans. They should keep the same CAD layer names, colors, linetypes, and visual appearance; only the setback/buildable geometry changes per scenario.

## Verified current evidence

Real Pointe sample:

- `D:\PointAIData\PLANS\originalsSitePlans\158 DAWSON STREET.dxf`
- Layer table:
  - `0`: color `7`, linetype `Continuous`, lineweight `-3`, plot style `F`, material `98`, shadow `0`
  - `TEXT`: color `6`, linetype `Continuous`, lineweight `-3`, plot style `F`, material `98`, shadow `0`
  - `E`: color `4`, linetype `Continuous`, lineweight `20`, plot style `F`, material `98`, shadow `0`
  - `SETBACKS`: color `31`, linetype `Continuous`, lineweight `13`, plot style `F`, material `98`, shadow `0`
  - `2312-001-BM$0$C-PROP-SUBD`: color `7`, linetype `2312-001-BM$0$PHANTOM2`, lineweight `-3`, plot style `F`, material `98`, shadow `0`

Current synthetic generator:

- `generate_synthetic_siteplans.py`
- currently writes to `C:\Users\lucas\OneDrive\Escritorio\exports\site plans sinteticos`, not to `D:\PointAIData\PLANS\originalsSitePlans`
- currently creates the same layer names and some colors/linetype names, but omits the real layer metadata tail (`370`, `390`, `347`, `348`) that AutoCAD/Pointe-style files include.

## Proposed direction

Update the synthetic site-plan generator so every synthetic DXF uses a real Pointe-style layer table/template. Only setback/buildable geometry varies between scenarios; layer appearance and CAD metadata stay fixed.

## Implemented

Implemented in `generate_synthetic_siteplans.py` and verified with `verify_synthetic_siteplans.py`.

Generated six `SYNTH SITE PLAN *.dxf` files into `D:\PointAIData\PLANS\originalsSitePlans`, all matching the required Pointe layer appearance metadata from `158 DAWSON STREET.dxf`.
