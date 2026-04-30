---
type: implementation
date: 2026-04-29
status: active
---

# Exhaustive File Explanation Synced

## What changed

Se resincronizo el documento:

- `docs/explicacion del proyecto/2026-04-29 - explicacion exhaustiva de archivos del slice 1 ejecutable.md`

## Why

La version anterior del documento habia quedado en una foto intermedia del dia.

Todavia describia partes viejas como:

- `MainWindow` con constructor inyectado
- `LibraryViewModel` agregando items solo en memoria
- ausencia de la query formal de Library

Y ademas no cubria el launcher dev nuevo:

- `scripts/dev-desktop.bat`

## What the synced version now covers

- `IFloorPlanLibraryReader`
- `GetFloorPlanLibraryHandler`
- `SqliteFloorPlanLibraryReader`
- `MainWindow` con carga en `Opened`
- `LibraryViewModel` persisted-first
- reimport del mismo DXF como `version 2`
- launcher dev `.bat`
- shortcut dev y shortcut smoke
- mapa de documentacion actual

## Honest current truth

La deuda documental especifica de “explicacion archivo por archivo” ya no existe en este corte.
