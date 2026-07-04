---
type: Implementation
date: 2026-05-11
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop1
  - dimensions
  - dxf
  - site-plan
---

# DXF dimension source audit across sample plans

## What changed

Se audit? de d?nde salen las dimensiones en los DXF de muestra actuales.

## Why

Antes de implementar la familia CAD de dimensiones hab?a que verificar si el repo pod?a depender solo de entidades `DIMENSION` nativas o si tambi?n necesitaba leer medidas escritas como texto.

## Current behavior

- `SANTA-BARBARA.dxf` contiene **114** entidades `DIMENSION`.
- `SEMINOLE2000.dxf` contiene **328** entidades `DIMENSION`.
- `158 DAWSON STREET.dxf` contiene **0** entidades `DIMENSION`.
- En `158 DAWSON STREET.dxf`, las medidas visibles relevantes hoy viven como `TEXT` / `MTEXT` (por ejemplo bearings y longitudes como `49.12`, `49.16`, `325.00`, `24.63`, `008?40'03"` y `N16?44'49"W`).

## Consequence

- Para **floor plans**, la familia de dimensiones puede arrancar leyendo entidades `DIMENSION` nativas.
- Para **site plans**, si queremos auditar frontage, setbacks, cuerdas/radios o bearings del sample actual, tambi?n vamos a necesitar una estrategia para texto de medici?n, no solo `DIMENSION`.

## Where

- `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`
- `PLANS/originalFloorPlans/SEMINOLE2000.dxf`
- `PLANS/originalsSitePlans/158 DAWSON STREET.dxf`
- `obsidian-vault/Current State.md`

## Verification

- Script Python ad-hoc con `ezdxf` para contar tipos de entidad en modelspace y muestrear textos de medici?n.

## Gotchas

- "Extraer dimensiones" NO significa lo mismo en todos los planos del repo.
- Si implementamos solo `DIMENSION`, Loop 1 quedar? bien encaminado para floor plans, pero Loop 2 seguir? ciego frente al site plan semilla actual.
