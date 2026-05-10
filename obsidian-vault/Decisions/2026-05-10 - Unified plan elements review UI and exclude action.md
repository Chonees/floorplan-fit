---
project: floorplan-fit
type: decision
date: 2026-05-10
status: active
related:
  - "[[Current State]]"
  - "[[Implementation/2026-05-04 - Pinch native cleanup and minimal review UI]]"
  - "[[Implementation/2026-05-07 - Room label candidates extracted into review]]"
tags:
  - loop-1
  - desktop
  - curation
  - review-ui
---

# Unified plan elements review UI and exclude action

## Decision

La review de Loop 1 pasa a un layout más intuitivo:

- **izquierda:** `Plan Elements`
- **centro:** `Preview`
- **derecha:** `Selected Item + Pinch Tools`

Todos los artifacts curables del plano viven en el panel izquierdo:

- lines
- rooms
- openings
- opening labels
- fixed elements
- protected details

La acción visible para el usuario se unifica como:

- `Exclude from Curation`

## Why

La UI actual obliga al usuario a repartir la atención entre dos paneles de artifacts y a aprender verbos distintos (`Reject` para lines, `Remove` para el resto).

Eso mete fricción justo en el loop que tiene que sentirse como curado técnico rápido y auditable.

## Semantic rule

La regla UX aprobada es:

> todo entra incluido por defecto y el humano excluye falsos positivos

La UI deja de exponer diferencias internas de persistencia.

## Implementation note

La acción visible se unifica, pero el backend no tiene por qué usar exactamente la misma semántica para todos los tipos en este slice:

- walls siguen usando **reject** para preservar auditoría
- openings / opening labels / fixed elements / protected details pueden seguir usando remove por debajo
- room labels necesitan sumar una vía nueva de exclusión

## Consequence

La review queda más enseñable, más densa en información útil y menos ruidosa visualmente, sin mezclar el rediseño con un correction backbone completo en la misma entrega.
