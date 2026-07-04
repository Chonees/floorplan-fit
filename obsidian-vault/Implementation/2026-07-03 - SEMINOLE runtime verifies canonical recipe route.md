# 2026-07-03 - SEMINOLE runtime verifies canonical recipe route

## Tipo
Implementation / Verification

## Contexto
La definición de terminado de la meta era una primera ruta demostrable, no el sistema perfecto:

`FloorPlan adjustment -> AdjustmentRecipe guardada -> dependent sheet projection consume la recipe -> export report indica qué aplicó y qué dejó para review`.

## Evidencia runtime
El usuario re-exportó SEMINOLE como `TEST9` después de ajustar contra setback.

Verifier ejecutado:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1"
```

Resultado:
- Manifest: `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/exports/plan-sets/d4a30be0569049a197d4514c39f4d501/manifest.json`
- Status: `RequiresManualConfirmation`
- CanonicalSheets: `1`
- ElectricalSheets: `1`
- RecipeSheets: `1`
- CompressionRecipeSheets: `1`
- RecipeHandlingSummary includes four `VerticalCompression` operations.

## Evidence from manifest
- `CanonicalFloorPlan` exported to `D:\PointAIData\PLANS\original electrical plans\TEST9.dxf`.
- `ElectricalPlan` has projection `94cc57d8-11a7-41d5-896b-af32a2d7119c`.
- Electrical projection status is `RequiresManualConfirmation`.
- Warning: `Canonical compression steps require electrical review before export.`
- Recipe summary reports the local compression operations and explicitly says review is required before DXF deformation.

## Evidence from SQLite
Queried `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/app.db` with Python stdlib:

- canonical adjustment: `afaa1253-bc0a-463a-9862-7e090af9d4f7`
- saved recipe operations: `4`
- projection: `94cc57d8-11a7-41d5-896b-af32a2d7119c`
- projection status: `RequiresManualConfirmation`
- projection compression steps: `4`

## Conclusion
The first demonstrable route exists. This does not implement automatic electrical rerouting/deformation. Correct next slice, if requested later: entity-aware replay of local compression on safe Electrical entities, while wiring curves remain quarantined/reviewed.
