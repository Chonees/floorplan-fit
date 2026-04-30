---
type: implementation
date: 2026-04-30
status: active
---

# Loop 1 Implementation Plan

## Scope

- extraction de wall candidates
- curated walls exactas
- curated spaces mÃ­nimos
- constraints bÃ¡sicas por wall/space
- draft / publish / active curation
- review UI mÃ­nima apoyada en el modelo correcto

## Why

Este es el primer cierre serio de Loop 1 y deja la base reusable que despuÃ©s necesita Loop 2 para overlay, fit y propuestas.

## Execution Notes

- El plan ejecutable vive en `docs/superpowers/plans/2026-04-30-loop-1-curated-walls-and-spaces-implementation.md`
- La ejecuciÃ³n debe respetar TDD estricto: test rojo -> cÃ³digo mÃ­nimo -> test verde
- Bajo la regla actual del repo `never build after changes`, la verificaciÃ³n durante implementaciÃ³n queda limitada a `dotnet test`
- `scripts/dev-desktop.bat` usa `dotnet watch run`, asÃ­ que queda diferido como path de runtime hasta tener autorizaciÃ³n explÃ­cita para una pasada separada
- Gotcha verificado al arrancar: no correr `dotnet test` de distintos proyectos en paralelo dentro del mismo worktree porque se pisan `obj/bin` y aparece `CS2012` por file lock; la baseline correcta es secuencial

## Ordered Milestones

1. dominio semÃ¡ntico base y estados de Library
2. puertos Application + lifecycle handlers
3. schema SQLite + repositorios de curaciÃ³n
4. extracciÃ³n real de wall candidates
5. review/metadata/publish de curated walls
6. curated spaces mÃ­nimos + constraints por ambiente
7. Library states + review screen mÃ­nima

## Progress

- **Task 1 complete**: núcleo semántico base implementado en eat/loop1-curation-impl
- Evidencia RED: FloorPlanCurationTests falló primero por tipos faltantes (FloorPlanCuration, FloorPlanCurationStatus, ActivePublishedCurationId)
- Evidencia GREEN: FloorPlanCurationTests pasó **2/2** y FloorplanFit.Application.Tests quedó en **6/6**
- Gotcha adicional verificado: cuando trabajamos dentro del worktree, las ediciones de archivos deben ejecutarse apuntando explícitamente al path del worktree; si no, el cambio cae en el workspace principal y rompe el aislamiento
- **Task 2 partial**: slice inicial de Application implementado con extracción y publish
- Evidencia GREEN adicional: ExtractWallCandidatesHandlerTests pasó **1/1**, PublishFloorPlanCurationHandlerTests pasó **1/1** y FloorplanFit.Application.Tests quedó en **8/8**
- Estado honesto: todavía faltan StartOrResumeCuration, AcceptWallCandidate, RejectWallCandidate y UpdateCuratedWallMetadata para considerar cerrada la capa Application de Loop 1
