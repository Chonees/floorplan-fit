---
type: decision
date: 2026-04-25
status: accepted
---

# Floorplan Teaching Skill Always On

## Decision

`floorplan-fit-teaching-mode` pasa a ser una guía default del repositorio y debe cargarse junto con `superpowers:using-superpowers` cuando se trabaja en Floorplan Fit.

## Why

- El repositorio tiene una intención pedagógica fuerte, no solo de ejecución
- El skill ya existía, pero estaba definido como condicional a pedidos explícitos de enseñanza
- Eso lo dejaba por debajo del comportamiento siempre-on que hoy impone Superpowers

## What changed

- `AGENTS.md` ahora lo trata como skill siempre activo a nivel de instrucciones del repo
- `skills/floorplan-fit-teaching-mode/SKILL.md` ahora define profundidad baseline y profundidad exhaustiva
- La profundidad exhaustiva se reserva para pedidos explícitos de aprendizaje paso a paso

## Consequences

- Toda implementación, refactor o bugfix en este repo debe explicarse con loop de producto + capa arquitectónica
- Cuando el usuario pida enseñanza exhaustiva, la respuesta debe expandirse sin cambiar la lógica base del skill
- Para que quede al mismo nivel operativo que otros skills globales, conviene instalarlo también en `~/.codex/skills/`
