# 2026-07-03 - Latest manifest verifier exposes stale runtime gap

## Tipo
Implementation / Verification

## Contexto
La meta canónica exige probar la ruta observable:

`FloorPlan adjustment -> AdjustmentRecipe guardada -> dependent sheet projection consume la recipe -> export report/manifest indica qué aplicó y qué queda para review`.

El código ya propaga `RecipeHandlingSummary` hacia el manifest, pero la evidencia runtime todavía depende de re-exportar un HousePlanSet con la app corriendo el código actual.

## Cambio
Se agregó `scripts/verify-latest-plan-set-recipe-manifest.ps1` como smoke check liviano, sin build, para inspeccionar el último `manifest.json` exportado por la app.

El script valida:
- existe un `manifest.json` bajo `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/exports/plan-sets`;
- hay al menos una hoja `CanonicalFloorPlan`;
- hay al menos una hoja `ElectricalPlan`;
- al menos una hoja tiene `RecipeHandlingSummary` no vacío.

## Evidencia actual
Último manifest inspeccionado:

`src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/exports/plan-sets/67732acc8c47484d848acf4eb36ff8a7/manifest.json`

Resultado:
- `Status`: `ReadyForExport`
- hojas: `CanonicalFloorPlan`, `ElectricalPlan`
- `HasRecipeHandlingSummary`: `False`
- el verifier falla con: `has no RecipeHandlingSummary. Re-export after the latest recipe changes.`

## Interpretación
Esto NO contradice el cambio de código; muestra que el manifest disponible es stale/pre-change o no fue generado por la ruta nueva. La meta sigue incompleta porque falta smoke runtime con SEMINOLE re-exportado usando el código actual.

## Próximo paso
Reiniciar la app, re-exportar el paquete SEMINOLE actual y correr:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1"
```

Criterio de aceptación: el script debe devolver `CanonicalSheets >= 1`, `ElectricalSheets >= 1`, `RecipeSheets >= 1` y una `RecipeHandlingSummary` que mencione la operación local/review.

## 2026-07-03 update - verifier now requires compression recipe by default

The verifier was tightened: a non-empty `RecipeHandlingSummary` is not enough for this goal because an affine-only export does not prove the patio/porch/living compression route. By default the script now requires `HorizontalCompression` or `VerticalCompression` in at least one sheet summary. Use `-AllowAffineOnly` only for a narrower affine smoke.

Fresh evidence:
- Synthetic compression manifest passes.
- Synthetic affine-only manifest fails without `-AllowAffineOnly` and passes with `-AllowAffineOnly`.
- Current latest runtime manifest still fails earlier because it has no `RecipeHandlingSummary` at all.

## 2026-07-03 update - verifier self-check added and StrictMode bug fixed

Added `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` as the smallest runnable check for the manifest verifier. It creates two temporary manifests:
- compression case: `ElectricalPlan` with `HorizontalCompression` in `RecipeHandlingSummary` must pass;
- affine-only case: must fail by default and pass with `-AllowAffineOnly`.

While writing the check, `Set-StrictMode` exposed a verifier bug: the verifier read `RecipeHandlingSummary` directly on every sheet, but `CanonicalFloorPlan` rows do not have that property. The verifier now uses safe JSON property access via `Get-JsonString`.

Fresh evidence:
- `powershell -NoProfile -ExecutionPolicy Bypass -File "scripts\test-verify-latest-plan-set-recipe-manifest.ps1"` -> `manifest verifier self-check passed`.
- `git diff --check` -> exit 0, LF/CRLF warnings only.
- Current latest runtime manifest still fails because it has no `RecipeHandlingSummary`; SEMINOLE re-export is still required.
