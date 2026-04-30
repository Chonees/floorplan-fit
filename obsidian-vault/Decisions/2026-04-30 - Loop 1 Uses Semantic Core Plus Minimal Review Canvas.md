---
type: decision
date: 2026-04-30
status: active
---

# Loop 1 Uses Semantic Core Plus Minimal Review Canvas

## Decision

El cierre serio de Loop 1 se va a construir con un **core sem?ntico primero** y un **canvas m?nimo real** para review.

No se arranca por un editor visual rico tipo CAD.

Tampoco se cierra el loop con listas ciegas sin contexto espacial.

## Shape

La primera versi?n de cierre de Loop 1 debe permitir:

1. importar un floor plan
2. extraer wall candidates
3. revisar candidates visualmente
4. curar walls aceptadas con metadata b?sica
5. guardar draft
6. publicar una versi?n activa reutilizable

## Why

El repo ya tiene importaci?n, DXF, hash, SQLite y Desktop b?sico.

El mayor riesgo ahora no es de infraestructura sino de modelo.

Por eso primero hay que estabilizar:

- qu? es una wall candidate
- qu? significa aceptarla o rechazarla
- qu? datos forman una curated wall
- qu? significa publicar una versi?n can?nica

Pero el review de walls necesita contexto geom?trico real, as? que un canvas m?nimo s? entra en el primer cierre.

## Consequence

Se posterga para un slice posterior:

- merge visual avanzado
- split visual avanzado
- tooling de edici?n tipo CAD
- manipulaciones geom?tricas complejas

El primer cierre busca **valor de producto real** sin explotar la complejidad visual.
