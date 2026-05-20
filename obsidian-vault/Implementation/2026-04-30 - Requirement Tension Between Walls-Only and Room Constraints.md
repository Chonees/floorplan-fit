---
type: implementation
date: 2026-04-30
status: active
---

# Requirement Tension Between Walls-Only and Room Constraints

## What was clarified

El usuario aclar? que el MVP no solo debe saber qu? walls se mueven o no.

Tambi?n necesita captar:

- medidas exactas de cada ambiente
- qu? es cada habitaci?n
- reglas del usuario sobre qu? tocar y qu? no
- l?mites como m?nimos de ba?o, dormitorios u otros espacios
- casos donde living y cocina comparten un espacio abierto

## Why this matters

La documentaci?n vigente empuja fuerte a **walls-only**:

- `TECH-STACK-ARCHITECTURE-DATAFLOW.md:120`
- `TECH-STACK-ARCHITECTURE-DATAFLOW.md:809`

Pero esta nueva aclaraci?n muestra que, para cumplir la intenci?n real del producto, el curado no puede quedarse solo en walls.

Hace falta al menos una capa sem?ntica m?nima de **espacios / ambientes + constraints**.

## Current tension

Hay una tensi?n de scope entre:

1. mantener v1 como `walls-only`
2. soportar reglas reales por ambiente como:
   - `baño mínimo tanto por tanto`
   - `no tocar este espacio`
   - `hasta acá puede crecer`
   - `living y cocina comparten el mismo espacio`

## Consequence for design

Antes de escribir la spec final de cierre de Loop 1 hay que decidir el modelo m?nimo correcto para:

- espacios curados
- ambientes abiertos o compartidos
- constraints por ambiente
- relaci?n entre walls y espacios
