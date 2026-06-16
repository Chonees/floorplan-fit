---
type: Implementation
date: 2026-06-15
project: floorplan-fit
status: current
related:
  - ../Inbox/2026-06-15 - Synthetic site plans must use Pointe layer appearance.md
  - ../Experiments/2026-06-08 - Total-deficit setback non-fit DXF examples.md
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - synthetic-fixtures
  - dxf-layers
---

# Synthetic site plans use Pointe layer appearance

## Change

Generated synthetic site-plan DXFs now live in the same operator-facing site-plan corpus as the real Pointe sample:

```txt
D:\PointAIData\PLANS\originalsSitePlans
```

The generator creates six `SYNTH SITE PLAN *.dxf` files there.

## Layer appearance truth

The synthetic files now use the same required layer appearance metadata as `158 DAWSON STREET.dxf`:

- `0`: color `7`, linetype `Continuous`, lineweight `-3`, plot style `F`, material `98`, shadow `0`
- `TEXT`: color `6`, linetype `Continuous`, lineweight `-3`, plot style `F`, material `98`, shadow `0`
- `E`: color `4`, linetype `Continuous`, lineweight `20`, plot style `F`, material `98`, shadow `0`
- `SETBACKS`: color `31`, linetype `Continuous`, lineweight `13`, plot style `F`, material `98`, shadow `0`
- `2312-001-BM$0$C-PROP-SUBD`: color `7`, linetype `2312-001-BM$0$PHANTOM2`, lineweight `-3`, plot style `F`, material `98`, shadow `0`

Only the setback/buildable geometry changes between synthetic scenarios. This keeps the visual/CAD language Pointe-like instead of toy-fixture-like.

## Generated files

- `SYNTH SITE PLAN 01 - LOTE RECTANGULAR - ancho menos 1 inch.dxf`
- `SYNTH SITE PLAN 02 - LOTE ESQUINA CON CHAFLAN - ancho menos 2 inch.dxf`
- `SYNTH SITE PLAN 03 - SETBACK CON FILLETS - ancho menos 5 inch.dxf`
- `SYNTH SITE PLAN 04 - FRENTE CURVO CUL-DE-SAC - alto menos 1 inch.dxf`
- `SYNTH SITE PLAN 05 - LOTE TRAPEZOIDAL - alto menos 2 inch.dxf`
- `SYNTH SITE PLAN 06 - CHAFLAN Y FRENTE CURVO - alto menos 5 inch.dxf`

## Verification

- RED: `python verify_synthetic_siteplans.py` failed before generation because no `SYNTH SITE PLAN *.dxf` existed in `D:\PointAIData\PLANS\originalsSitePlans`.
- GREEN: `python generate_synthetic_siteplans.py` generated all six files in the target corpus.
- GREEN: `python verify_synthetic_siteplans.py` passed: `PASS: 6 synthetic site plan(s) match Pointe layer appearance`.

## Relevant files

- `generate_synthetic_siteplans.py` ? generator now targets the site-plan corpus and emits Pointe-compatible layer records.
- `verify_synthetic_siteplans.py` ? verifier for reference layer signatures and synthetic `SETBACKS` geometry.
- `docs/superpowers/plans/2026-06-15-synthetic-site-plan-pointe-layers.md` ? implementation plan.

## Follow-up: AutoCAD black screen required preserving Pointe DXF structure, not only layers

User reported the synthetic site plans still appeared black/blank in AutoCAD. AutoCAD Core Console reproduced the actual failure:

```txt
Error in APPID Table
DXF read error on line 226.
Invalid or incomplete DXF input -- drawing discarded.
ErrorStatus=53
```

Root cause: the first synthetic generator copied Pointe layer names/appearance metadata, but still emitted a minimal DXF with only `LTYPE`, `LAYER`, and `STYLE` tables. The real Pointe sample has the fuller table set: `VPORT`, `LTYPE`, `LAYER`, `STYLE`, `VIEW`, `UCS`, `APPID`, `DIMSTYLE`, and `BLOCK_RECORD`. In R2013 DXF, the synthetic `LINE`/`TEXT` records also need handles, model-space owner references, and subclass markers such as `AcDbEntity`, `AcDbLine`, and `AcDbText`.

Fix implemented in the generator:

- use `158 DAWSON STREET.dxf` as the structural DXF template;
- replace only the `ENTITIES` section with synthetic geometry;
- emit R2013-compatible `LINE`/`TEXT` records with handles, owner `330`, and subclass markers;
- update `$EXTMIN`, `$EXTMAX`, active `VPORT`, and `$HANDSEED`.

Verification:

- RED: `python verify_synthetic_siteplans.py` failed with missing tables and AutoCAD Core Console `Error in APPID Table`.
- GREEN selected fixed set: AutoCAD Core Console audit/open passed (`exit 0`) for files 01, 02, 03, 04, 06, and the generated fallback `SYNTH SITE PLAN 05 - LOTE TRAPEZOIDAL - alto menos 2 inch - AUTOCAD FIXED.dxf`.
- Caveat: the original `SYNTH SITE PLAN 05 - LOTE TRAPEZOIDAL - alto menos 2 inch.dxf` remained locked by AutoCAD/another process, so it could not be overwritten yet and still fails the global verifier until the lock is released.

## Follow-up: user still saw black because exact-name 05 was stale and locked

Fresh verification after the user reported AutoCAD still appeared black:

- `python verify_synthetic_siteplans.py` fails only for `SYNTH SITE PLAN 05 - LOTE TRAPEZOIDAL - alto menos 2 inch.dxf`.
- AutoCAD Core Console rejects that exact-name file with exit `53` and the same stale minimal-DXF APPID error.
- The fixed fallback file `SYNTH SITE PLAN 05 - LOTE TRAPEZOIDAL - alto menos 2 inch - AUTOCAD FIXED.dxf` opens/audits with exit `0`.
- Windows confirms the exact-name file is still locked by another process, so the generator cannot overwrite it until AutoCAD releases it.

Bug note: `../Bugs/2026-06-15 - Synthetic site plan 05 stale locked DXF opens black.md`.

## Follow-up: Dawson-style title block and short fake names

User clarified that the synthetic site plans should visually read like the Dawson reference, not like diagnostic fixtures. The generator now removes visible diagnostic/title text such as `BUILDABLE BBOX`, long deficit names, and `SYNTHETIC STREET`.

Current generated short-name files:

- `SYNTH OAK.dxf` ? visible title `101 / OAK STREET`
- `SYNTH PINE.dxf` ? visible title `214 / PINE WAY`
- `SYNTH CEDAR.dxf` ? visible title `32 / CEDAR COURT`
- `SYNTH MESA.dxf` ? visible title `8 / MESA LOOP`
- `SYNTH RIO.dxf` ? visible title `57 / RIO DRIVE`
- `SYNTH PARK.dxf` ? visible title `16 / PARK LANE`

The title block follows the `158 DAWSON STREET.dxf` pattern with the same text styles/heights used by the real file: `HOUSE`, `SITE`, `L80`, `RS`, and `ARCHITECTURAL`. It includes `SITE PLAN`, `SCALE 1'=20'`, the R.O.W. note, legal/city lines, curve-table headers/row, and survey-style bearing labels.

AutoCAD validity gotcha: literal `?` and `?` in generated `TEXT` records caused AutoCAD Core Console read failures. The generator now writes degree marks as `%%d` and `DO?A` as `DO\U+00D1A` so AutoCAD renders the intended characters without rejecting the DXF.

Verification:

- RED: `python verify_synthetic_siteplans.py` failed with missing short-name synth files.
- GREEN: `python generate_synthetic_siteplans.py` generated the six short files.
- GREEN: `python verify_synthetic_siteplans.py` passed: `PASS: 6 synthetic site plan(s) match Pointe layer and title appearance`.

## Follow-up: title block scaled up and stale-file cleanup attempted

User clarified the lower/title-block letters were still too small. The generator now uses a `TITLE_BLOCK_SCALE = 5.0`, so the Dawson-style title block keeps the same relative proportions/styles but renders much larger in the synthetic site-plan canvas.

Verified current valid output set with AutoCAD Core Console:

- `SYNTH OAK.dxf` ? pass
- `SYNTH PINE.dxf` ? pass
- `SYNTH CEDAR.dxf` ? pass
- `SYNTH MESA - AUTOCAD FIXED.dxf` ? pass while exact `SYNTH MESA.dxf` is locked
- `SYNTH RIO.dxf` ? pass
- `SYNTH PARK.dxf` ? pass

Sample title heights after scaling:

- `SITE PLAN`: `40.0`
- `HOUSE` title text: `26.015625`
- `L80` title text: `8.0`
- curve-table header text: `7.03125`

Cleanup status:

- Deleted old long-name synth files 03, 04, 05, 05 fallback, and 06.
- Could not delete old long-name 01/02 because they are locked by AutoCAD/another process.
- Could not overwrite exact `SYNTH MESA.dxf` because it is locked, so the current valid MESA output is `SYNTH MESA - AUTOCAD FIXED.dxf` until AutoCAD releases the exact file.

## Follow-up: property boundary now traces the setback shape

User clarified the outer terrain/property boundary must mirror the setback form. The previous generator created correct buildable areas but wrapped them in independent rectangles/trapezoids, which is geometrically wrong: if the setback has a chamfer/curve/fillet, the terrain has that shape too.

Verification added:

- `verify_synthetic_siteplans.py` now compares the ordered vertex shape of `2312-001-BM$0$C-PROP-SUBD` against `SETBACKS` after bbox normalization.
- RED confirmed the old bug: `PINE` property boundary had `4` segments while its setback had `5`; `CEDAR` had `4` vs `36`; `MESA` had `4` vs `27`.

Generator fix:

- Added `lot_from_setback_shape(...)`, which builds the outer property boundary as an affine expansion of the exact setback vertex sequence.
- The buildable setback bbox and deficit measurements remain unchanged; only the exterior terrain shape now matches the buildable shape.
- Survey/title labels use lot bounds instead of assuming the first four vertices are rectangle corners.

Verified current valid output set:

- `SYNTH OAK.dxf` ? property boundary matches `SETBACKS`; AutoCAD audit passed.
- `SYNTH PINE - AUTOCAD FIXED.dxf` ? property boundary matches `SETBACKS`; AutoCAD audit passed.
- `SYNTH CEDAR - AUTOCAD FIXED.dxf` ? property boundary matches `SETBACKS`; AutoCAD audit passed.
- `SYNTH MESA - AUTOCAD FIXED.dxf` ? property boundary matches `SETBACKS`; AutoCAD audit passed.
- `SYNTH RIO.dxf` ? property boundary matches `SETBACKS`; AutoCAD audit passed.
- `SYNTH PARK.dxf` ? property boundary matches `SETBACKS`; AutoCAD audit passed.

Cleanup status:

- Could not delete/overwrite stale exact `SYNTH PINE.dxf`, `SYNTH CEDAR.dxf`, `SYNTH MESA.dxf`, or old long-name synth 01/02 because AutoCAD/another process is locking them.
- Current valid PINE/CEDAR/MESA outputs are the `- AUTOCAD FIXED` files until the exact names are released.
