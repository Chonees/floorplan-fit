---
type: implementation
date: 2026-04-29
status: active
---

# Reimport Versioning Bug Fixed

## What

Se corrigio el bug del Slice 1 ejecutable donde reimportar el mismo DXF podia crear un template nuevo con sufijo tecnico (`santa-barbara-2`) en vez de crear `version 2` del mismo template.

## Where

- `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Imports/ImportFloorPlanIntegrationTests.cs`

## Why

La identidad de dominio estaba saliendo del nombre de la copia gestionada en `library/raw-dxf/`, no del archivo fuente que el usuario eligio importar.
Eso mezclaba storage fisico con identidad logica del floor plan.

## Result

Ahora:

- la copia gestionada puede tener sufijo fisico (`-2`, `-3`)
- el template sigue siendo el mismo (`santa-barbara`)
- el segundo import pasa a `version 2`

## Verification

- `FloorplanFit.Application.Tests`: **4/4 passing**
- `FloorplanFit.Infrastructure.Tests`: **6/6 passing**
- `FloorplanFit.Desktop` compila en build de verificacion a `net10.0-verify` incluso con la app abierta

## Extra discovery

Durante la nueva integracion aparecio una costura de laboratorio: reusar una misma `SqliteSession` despues de `CommitAsync` no modela el runtime real de Desktop.
El flujo real abre un scope/sesion por import, asi que el test de reimport se ajusto a esa verdad del producto.
