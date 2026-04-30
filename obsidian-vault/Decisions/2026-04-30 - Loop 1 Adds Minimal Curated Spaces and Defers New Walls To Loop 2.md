---
type: decision
date: 2026-04-30
status: active
---

# Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2

## Decision

Se ajusta el dise?o de cierre de Loop 1:

- Loop 1 ya no queda en `walls-only` puro
- agrega una capa m?nima de **curated spaces / curated rooms**
- agrega **constraints b?sicas por ambiente**
- deja la **creaci?n de paredes nuevas** como parte de las propuestas de adaptaci?n de **Loop 2**

## What enters Loop 1 now

### Walls curadas

- geometr?a exacta en planta
- largo exacto
- espesor
- tipo de pared / assembly como metadata ?til (`2x4`, `2x6`, etc.)
- movilidad / protecci?n / rol / grupo

### Spaces curados m?nimos

- identidad del ambiente
- geometr?a o boundary del espacio
- ?rea exacta
- dimensiones m?nimas derivables
- tipo de ambiente
- reglas b?sicas del usuario

### Constraints b?sicas por ambiente

- no tocar
- tocar hasta cierto l?mite
- m?nimo ?rea
- m?nimo ancho / profundidad

## What stays out of Loop 1

- solver avanzado de ambientes
- sem?ntica ultra rica de arquitectura
- edici?n CAD compleja
- creaci?n de paredes nuevas como parte del curado base

## Why

La aclaraci?n del usuario mostr? que solo curar walls no alcanza para expresar reglas reales como:

- ba?o m?nimo tanto por tanto
- preservar un ambiente
- living + cocina como espacio abierto

Pero al mismo tiempo, crear paredes nuevas pertenece mejor a la adaptaci?n por lote, o sea a Loop 2.

## Consequence

Una propuesta de Loop 2 puede m?s adelante promoverse a nueva versi?n base de Loop 1, pero primero nace como adaptaci?n espec?fica del caso.
