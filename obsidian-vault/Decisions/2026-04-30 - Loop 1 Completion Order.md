---
type: decision
date: 2026-04-30
status: active
---

# Loop 1 Completion Order

## Decision

Para cerrar Loop 1 se aprob? este orden de trabajo:

1. **dominio + aplicaci?n de WallCandidate y extracci?n**
2. **review/curation + persistencia**
3. **publish de versi?n activa can?nica**
4. **UI de review/curado apoyada sobre esa base**

## Why

El repo ya tiene un slice ejecutable de importaci?n, storage, DXF y SQLite, pero todav?a no tiene el coraz?n del curado.

Arrancar por dominio/aplicaci?n primero evita construir una UI linda sobre reglas inexistentes o inestables.

## Consequence

La UI ya no define el modelo.

Primero se estabiliza:

- qu? es un `WallCandidate`
- c?mo se acepta/rechaza
- c?mo se transforma en curado persistido
- qu? significa publicar una versi?n activa

Reci?n despu?s se monta la experiencia visual encima.
