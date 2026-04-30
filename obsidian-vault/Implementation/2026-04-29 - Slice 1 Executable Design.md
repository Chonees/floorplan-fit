---
type: implementation
date: 2026-04-29
status: active
---

# Slice 1 Executable Design

## What was formalized

Se escribi? el spec t?cnico `docs/superpowers/specs/2026-04-29-slice-1-executable-design.md`.

## Core definition

El Slice 1 ejecutable queda definido como:

`thin Desktop -> import DXF real -> copy to managed storage -> hash -> persist in SQLite -> show Library item`

## Scope locked in

### Entra

- `FloorplanFit.Desktop` m?nimo
- `IDxfGateway` real
- storage gestionado en `library/raw-dxf/`
- hash real
- SQLite real
- integraci?n real del import de `SANTA-BARBARA.dxf`

### No entra

- wall extraction
- curation
- site plan
- fit engine
- export
- UI rica

## Important design rule

`IDxfGateway` y `IManagedFileStorage` quedan separados. Leer un DXF y gestionar archivos importados son responsabilidades distintas.

## Next handoff

El pr?ximo artefacto debe ser el plan de implementaci?n detallado, una vez aprobado este spec escrito.

## Correction applied during design review

Se corrigi? el orden interno del import para hacerlo m?s robusto:

`copy to managed storage -> read managed DXF -> hash managed DXF`

No conviene leer el archivo externo y reci?n despu?s copiarlo, porque eso puede desalinear metadata y archivo persistido si el original cambia entre pasos.
