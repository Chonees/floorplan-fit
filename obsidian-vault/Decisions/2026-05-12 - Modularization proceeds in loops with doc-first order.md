---
type: Decision
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - modularization
  - refactor
  - documentation
---

# Modularization proceeds in loops with doc-first order

## Decision

El saneamiento arquitectonico del repo no se va a hacer como un big-bang ni como "partamos los archivos grandes y vemos".

Se hace en **loops verticales**, con **documentacion canonica primero** y despues refactor por responsabilidades.

## Ordered loops

1. **Loop 0** - verdad canonica y mapa de arquitectura
2. **Loop 1** - sistema visual compartido
3. **Loop 2** - composicion e interaccion del preview
4. **Loop 3** - descomposicion de `FloorPlanReviewViewModel`
5. **Loop 4** - cleanup final, naming y ownership

## Why

- el mapa canonico esta desactualizado despues del milestone de native dimensions
- la modularizacion del preview mejoro, pero `FloorPlanPreviewControl.cs` y `FloorPlanReviewViewModel.cs` siguen demasiado grandes
- el sistema visual sigue parcialmente tokenizado y parcialmente hardcodeado
- si se refactoriza codigo sin verdad documental actualizada, el repo queda mas lindo de leer por dentro pero menos confiable como arquitectura compartida

## Rule

Cada loop tiene que dejar:

- una frontera de responsabilidades mas clara
- una codebase mas legible
- documentacion alineada con el estado real

## Consequence

El refactor deja de ser "limpieza cosmetica" y pasa a ser un programa de arquitectura con orden, trazabilidad y puntos claros de verificacion.
