---
type: implementation
date: 2026-04-30
status: active
---

# Loop 1 Curated Walls and Spaces Design

## What

Se formaliz? el dise?o t?cnico del cierre de Loop 1 en:

- `docs/superpowers/specs/2026-04-30-loop-1-curated-walls-and-spaces-design.md`

## Core decision

Loop 1 cierra con:

- walls curadas exactas
- spaces curados m?nimos
- constraints b?sicas
- draft / publish / versi?n activa

## Important boundary

La creaci?n de paredes nuevas no entra como verdad base del curado inicial.

Eso nace primero en Loop 2 como propuesta de adaptaci?n.

## Why

El usuario aclar? que el MVP necesita:

- medidas exactas por ambiente
- identidad de habitaciones
- casos open-plan
- reglas como m?nimos de ba?o o espacios que no se tocan

Eso obliga a ir m?s all? de `walls-only`, pero sin ir todav?a a un editor CAD complejo.

## Result

La spec deja definido:

- modelo can?nico de curations
- curated walls
- curated spaces
- groups / joins
- structured constraints
- relaci?n entre Loop 1 y Loop 2
