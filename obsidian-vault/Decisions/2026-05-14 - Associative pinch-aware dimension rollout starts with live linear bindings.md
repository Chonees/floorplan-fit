---
type: Decision
date: 2026-05-14
project: floorplan-fit
status: current
tags:
  - architecture
  - dimensions
  - review
  - pinch
  - loops
---

# Associative pinch-aware dimension rollout starts with live linear bindings

## Decision

La implementacion de cotas asociativas **pinch-aware y fit-aware** se divide en loops con una regla central:

1. **La verdad semantica** de una cota es `A -> B` sobre geometria viva, no `X/Y` congelados.
2. **La verdad cosmetica** de una cota es su offset, texto y familia grafica authored.
3. El rollout arranca por **dimensiones lineales nativas resueltas** y recien despues incorpora persistencia semantica, export unificado y tipos no lineales.

## Why

El codigo verificado hoy ya tiene asociaciones basicas `StartAnchor/EndAnchor` y `SegmentRatio`, pero el preview todavia no reproyecta dimensiones desde `PreviewGeometry` viva, `ReactiveDimensionProjector` saltea `IsEdited`, y `MeasurableEdgeProjector` solo cubre `WallCandidate` + `OpeningCandidate`. Si intentamos cerrar “todo” de una sola vez, mezclamos wiring de preview, rebind manual, persistencia semantica, export y nuevos tipos de cota en un solo blast radius.

## Loops

### Loop 0 — Fundacion semantica
- Introducir `DimensionBindingDto` y `DimensionMeasuredSpanDto`.
- Mantener `DimensionAssociations` como projection layer de compatibilidad.
- Clasificar cada cota como `Resolved` o `Unresolved` sin estado silencioso.

### Loop 1 — Preview reactivo lineal
- Recalcular `MeasurableEdges` desde `PreviewGeometry` transformada.
- Alimentar `ReactiveDimensionProjector` durante pinch/adaptacion preview.
- Soportar `StandardLinearC`, `SplitLinear` y `LeaderLinear`.
- Las cotas no resueltas quedan authored/static.

### Loop 2 — Rebind manual y persistencia semantica
- `body drag` = cosmetico.
- `endpoint drag` = rebind semantico.
- Persistir overrides de binding en tablas paralelas a `floorplan_dimension_overrides`.

### Loop 3 — Export y reader unificados
- El mismo pipeline tipado debe servir para review, preview y adjusted DXF export.
- El reader debe resolver binding base + override semantico + projection layer de compatibilidad.

### Loop 4 — Tipos no lineales
- Incorporar `OrdinateX` y `OrdinateY`.
- Incorporar `Radius` y `Diameter`.
- Prohibido degradarlas silenciosamente a lineales.

### Loop 5 — Hardening de garantia
- Cubrir remapeo de anchors ante cambios topologicos.
- Ampliar cobertura de measurable edges si el fit futuro mueve artefactos fuera de `WallCandidate`/`OpeningCandidate`.
- Exponer auditoria `original vs computed`.

## Tradeoffs

- **A favor**: baja riesgo, permite TDD por slices y hace visible el limite exacto de cada loop.
- **En contra**: la “garantia total” llega por etapas; al principio solo cubre lineales resueltas.

## Next

El proximo paso recomendado es bajar este decision note a un plan ejecutable por loop con:
- objetivo,
- archivos tocados,
- tests RED/GREEN,
- criterio de salida,
- y riesgo explicitado para cada loop.
