---
type: implementation
date: 2026-04-30
status: active
replaces:
  - [[Implementation/2026-04-29 - Repository Audit Status]]
---

# Status Audit and Documentation Drift

## What was verified

- `git log --oneline` ya no muestra solo `ca19d38`; el repo est? en `330737a` (`feat: implement executable slice 1 import pipeline`)
- `git branch -vv` muestra `main` y `feat/loop1-wall-candidate-extraction` apuntando a ese mismo commit
- `git status --short` est? limpio
- El c?digo real contradice el audit viejo:
  - existe `FloorplanFit.Desktop`
  - existen adaptadores reales DXF / filesystem / SQLite
  - el bloqueo de “sin SDK” ya no aplica
  - la Library tiene carga inicial desde SQLite mediante `LibraryViewModel.LoadAsync()` + `MainWindow_OnOpened()`
- La prueba de lectura de Library desde SQLite existe en `tests/FloorplanFit.Infrastructure.Tests/Imports/FloorPlanLibraryReaderIntegrationTests.cs`

## Important drift found

La nota `2026-04-29 - Repository Audit Status.md` qued? vieja y hoy ya no representa la verdad actual del repo.

Tambi?n hay drift parcial en el checklist manual del slice:

- el checklist dice que la Library “todav?a no se hidrata desde SQLite al iniciar”
- el c?digo actual s? incluye esa ruta
- lo que segu?a faltando era una revalidaci?n manual limpia desde el launcher correcto

## Workspace reality

El workspace local usado por Desktop en:

- `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/app.db`

contiene hoy estos templates:

- `santa-barbara`
- `santa-barbara-2`
- `santa-barbara-3`

con `version 1` en cada caso.

## Interpretation

Eso NO contradice por s? solo el bugfix de reimport.

La evidencia de la DB muestra que los `original_file_name` guardados son:

- `SANTA-BARBARA.dxf`
- `SANTA-BARBARA-2.dxf`
- `SANTA-BARBARA-3.dxf`

o sea: en esas corridas manuales se importaron copias gestionadas/stale artifacts con sufijo en el nombre de origen, no necesariamente el mismo archivo original de `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`.

## Why this matters

Los tests automatizados siguen respaldando la regla correcta:

- mismo archivo original elegido por el usuario -> mismo template -> nueva versi?n

Pero el workspace manual actual ya no sirve como evidencia limpia de ese comportamiento porque est? contaminado por imports de archivos gestionados.

## Next manual truth pass

1. abrir desde `scripts/dev-desktop.bat`
2. usar workspace limpio o controlado
3. reimportar siempre `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`
4. verificar hidraci?n al reiniciar UI
