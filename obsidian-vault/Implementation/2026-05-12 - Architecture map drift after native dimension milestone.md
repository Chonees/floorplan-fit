---
type: Implementation
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - modularization
  - documentation
  - dimensions
---

# Architecture map drift after native dimension milestone

## What changed

Se audito el estado real de modularizacion y documentacion despues del milestone fuerte de `native dimensions` del 2026-05-11.

## Verified findings

- La modularizacion mejoro, sobre todo en `src/FloorplanFit.Desktop/Controls/Preview/*`, pero todavia NO estamos en el ideal de **un archivo = una responsabilidad**.
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` sigue concentrando demasiada orquestacion con **1610 lineas**.
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` sigue demasiado grande para una sola pieza con **1851 lineas**.
- La modularizacion visual tambien esta a mitad de camino: `src/FloorplanFit.Desktop/App.axaml` centraliza brushes base, pero sigue mezclando multiples hex inline en estilos concretos.
- El mapa exhaustivo `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md` quedo desactualizado para la realidad actual: su frontmatter dice `last_verified: 2026-05-09`, su actualizacion 2026-05-08 todavia dice que las dimensiones CAD estan pendientes, y no cubre archivos ya presentes como `IxMiliaAdjustedDxfExporter.cs`, `SaveFloorPlanDimensionOverrideHandler.cs`, `SqliteFloorPlanDimensionOverrideRepository.cs` y `NativeDimensionEditor.cs`.

## Why

Antes de seguir limpiando colores, iconografia, features o capas de preview, hacia falta verificar si la base documental y la modularizacion real seguian alineadas con el repo vivo.

## Where

- `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`
- `obsidian-vault/Current State.md`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/App.axaml`
- `src/FloorplanFit.Desktop/Controls/Preview/`

## Learned

- El repo ya salio de la etapa de mega-control monolitico puro, pero el refactor todavia esta incompleto.
- La ola de dimensiones movio tanto la arquitectura que el mapa exhaustivo ya no alcanza como fuente confiable sin una revalidacion nueva.
- Si queremos limpiar de verdad colores, iconos y features, primero conviene refrescar el mapa y despues atacar los hotspots grandes (`FloorPlanPreviewControl`, `FloorPlanReviewViewModel`, tokens visuales).
