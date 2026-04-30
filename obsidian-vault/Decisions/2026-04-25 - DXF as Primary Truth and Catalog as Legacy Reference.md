---
type: decision
date: 2026-04-25
status: accepted
---

# DXF as Primary Truth and Catalog as Legacy Reference

## Decision

Para Loop 1, la fuente primaria de verdad serán los archivos `DXF` de `PLANS/originalFloorPlans/`.

Los archivos bajo `PLANS/catalog/` quedan clasificados como **referencia legacy/comparativa**, útiles para validación técnica temprana y comparación con un pipeline previo, pero **no** como verdad canónica del dominio.

## Why

- El flujo técnico definido en `TECH-STACK-ARCHITECTURE-DATAFLOW.md` parte de `DXF crudo -> extracción -> review -> curado -> publicación`
- Los JSON actuales no representan una curación canónica: no incluyen `stable_wall_id`, `mobility_level`, `protection_level` ni `curated_walls`
- Necesitamos controlar nuestro propio pipeline de importación y extracción para que las reglas del dominio nazcan dentro del sistema y no de artefactos heredados

## Operational Rule

- `SANTA-BARBARA.dxf` será el primer caso conductor del vertical slice
- `SEMINOLE2000.dxf` será el segundo fixture para robustez/regresión
- `PLANS/catalog/*.json` puede usarse como baseline auxiliar para unidad, bounding box y comparación técnica
- El dominio y la persistencia canónica NO deben depender de `PLANS/catalog/*.json`

## Consequences

- La implementación arranca desde DXF real y genera sus propios artefactos/persistencia
- El catálogo legado no se elimina todavía; se conserva como evidencia útil mientras entendemos sus supuestos
- El sistema futuro podrá comparar salida nueva vs baseline legacy sin contaminar la verdad del dominio
