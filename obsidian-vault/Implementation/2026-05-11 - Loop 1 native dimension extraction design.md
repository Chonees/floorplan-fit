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
  - review
---

# Loop 1 native dimension extraction design

## What changed

Se defini? el dise?o del primer slice de dimensiones para Loop 1.

## Design summary

- extraer `DIMENSION` nativas de floor plans
- excluir `ELECTRICAL WIRING`
- resolver `DisplayText` primero desde el bloque de geometr?a renderizado (`MTEXT` / `TEXT`), luego desde `DIMENSION.dxf.text`, y solo al final desde fallback generado
- persistir measurement en unidades fuente y en mil?metros, m?s tipo, layer y anchors base
- exponer una secci?n read-only `Dimensions` en Review
- dejar fuera por ahora overlay en canvas, exclusi?n/edici?n y scraping de medidas del site plan por `TEXT/MTEXT`

## Why

El usuario pidi? ver en la pr?xima extracci?n todas las dimensiones ?tiles con el texto de cu?nto miden. La validaci?n sobre `SEMINOLE2000` mostr? que eso exige una extracci?n CAD-faithful del texto renderizado, no solo contar cotas ni leer el campo `DIMENSION.dxf.text`.

## Where

- `docs/superpowers/specs/2026-05-11-loop1-native-dimension-extraction-design.md`
- `obsidian-vault/Decisions/2026-05-11 - Initial native dimension extraction excludes electrical wiring.md`
- `obsidian-vault/Current State.md`
