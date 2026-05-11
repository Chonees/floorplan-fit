---
project: floorplan-fit
type: decision
date: 2026-05-11
status: active
related:
  - "[[Current State]]"
  - "[[Implementation/2026-05-11 - DXF dimension source audit across sample plans]]"
tags:
  - loop-1
  - dimensions
  - dxf
  - extraction
---

# Initial native dimension extraction excludes electrical wiring

## Decision

El primer slice de extracci?n de dimensiones para Loop 1 va a leer **todas las entidades `DIMENSION` nativas de los floor plans**, excepto las que vivan en layer **`ELECTRICAL WIRING`**.

El alcance inicial incluye, por cada dimensi?n:

- measurement calculado desde geometr?a
- display text
  - texto renderizado real del bloque de geometr?a (`MTEXT`/`TEXT`) cuando exista
  - fallback al override `DIMENSION.dxf.text` si aplica
  - generaci?n desde la medida solo como ?ltimo recurso
- dimtype
- layer
- angle / oblique angle
- defpoint / defpoint2 / defpoint3

## Why

`SEMINOLE2000.dxf` ya trae 328 entidades `DIMENSION`, pero 2 viven en `ELECTRICAL WIRING` y el usuario confirm? que esa familia no hace falta todav?a.

El resto de las dimensiones nativas s? da valor inmediato para empezar a auditar medidas reales del floor plan sin mezclar todav?a el problema aparte de textos de site plan o anotaciones no estructurales. Adem?s, la verificaci?n sobre `SEMINOLE2000.dxf` mostr? que muchas cotas con `DIMENSION.dxf.text = ''` igual exponen su texto visible real dentro del bloque de geometr?a (`*D...`) como `MTEXT`, por ejemplo `5'-8"`, `10'-2"` y `10'-4"`. Eso significa que para ser CAD-faithful no alcanza con mirar solo el campo `text` ni con regenerar el n?mero a ciegas desde la medida.

## Consequences

- El primer extractor de dimensiones queda enfocado en el scope arquitect?nicamente correcto para Loop 1.
- La cobertura inicial prioriza floor plans sobre site plan text dimensions.
- Si m?s adelante se quiere incluir wiring, eso entra como ampliaci?n expl?cita del profile o de la pol?tica de filtrado.
