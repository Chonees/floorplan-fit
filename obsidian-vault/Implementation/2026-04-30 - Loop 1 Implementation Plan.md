---
type: implementation
date: 2026-04-30
status: active
---

# Loop 1 Implementation Plan

## Scope

- extraction de wall candidates
- curated walls exactas
- curated spaces mínimos
- constraints básicas por wall/space
- draft / publish / active curation
- review UI mínima apoyada en el modelo correcto

## Why

Este es el primer cierre serio de Loop 1 y deja la base reusable que después necesita Loop 2 para overlay, fit y propuestas.

## Execution Notes

- El plan ejecutable vive en `docs/superpowers/plans/2026-04-30-loop-1-curated-walls-and-spaces-implementation.md`
- La ejecución debe respetar TDD estricto: test rojo -> código mínimo -> test verde
- Bajo la regla actual del repo `never build after changes`, la verificación durante implementación queda limitada a `dotnet test`
- `scripts/dev-desktop.bat` usa `dotnet watch run`, así que queda diferido como path de runtime hasta tener autorización explícita para una pasada separada

## Ordered Milestones

1. dominio semántico base y estados de Library
2. puertos Application + lifecycle handlers
3. schema SQLite + repositorios de curación
4. extracción real de wall candidates
5. review/metadata/publish de curated walls
6. curated spaces mínimos + constraints por ambiente
7. Library states + review screen mínima
