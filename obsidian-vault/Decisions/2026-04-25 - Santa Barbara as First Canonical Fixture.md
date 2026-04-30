---
type: decision
date: 2026-04-25
status: accepted
---

# Santa Barbara as First Canonical Fixture

## Decision

El primer vertical slice del Loop 1 se implementa tomando `SANTA-BARBARA.dxf` como floor plan canónico inicial de trabajo.

## Why

- El sistema necesita arrancar con un floor plan reusable, no con un site plan variable
- `SANTA-BARBARA` tiene un catálogo JSON mucho más chico y un DXF más liviano que `SEMINOLE2000`, lo que reduce complejidad inicial
- Permite validar importación, unidades, persistencia y modelado canónico antes de atacar casos más pesados

## Consequences

- El primer flujo real será `import DXF -> normalización -> persistencia -> library entry`
- `SEMINOLE2000` queda como segundo fixture para robustecer el pipeline una vez estabilizada la base
- El site plan se reserva para Loop 2, cuando ya exista curación canónica reutilizable
