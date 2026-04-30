---
type: decision
date: 2026-04-25
status: accepted
---

# Project Identity and Knowledge Stack

## Decision

Se fija `floorplan-fit` como identidad canónica del proyecto.

## Why

- Ya existe el repo `Chonees/floorplan-fit`
- Engram Cloud es project-scoped
- Mantener nombres distintos fragmenta memoria y contexto

## Also decided

- Obsidian es el sink principal de conocimiento visible del proyecto
- Engram sigue como memoria operativa/sombra para sesiones y agentes
- Engram Cloud se usará para este proyecto solamente, no como backend del dominio del producto

## Consequences

- Toda nueva documentación durable debería aterrizar primero en este vault
- `Current State.md` pasa a ser el documento de verdad operativa rápida
