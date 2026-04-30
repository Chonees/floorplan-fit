---
type: decision
date: 2026-04-29
status: accepted
---

# Thin Desktop Included In Executable Slice 1

## Decision

El Slice 1 ejecutable va a incluir un `FloorplanFit.Desktop` **m?nimo y ultrafino**, en lugar de quedar solo como backend/integraci?n.

## Why

- Valida el recorrido real del producto de punta a punta.
- Obliga a cerrar la costura entre Desktop, Application e Infrastructure desde el comienzo.
- Da una salida visible sin meter UI pesada prematuramente.
- Es el mejor equilibrio entre calidad arquitect?nica y avance demostrable.

## Operational Rule

- El Desktop inicial solo debe cubrir el flujo m?nimo de Library + Import.
- No se habilita todav?a wall extraction, curation, site plan, fit engine ni UI rica.
- El foco del slice es `import real -> persistencia real -> library entry visible`.

## Consequences

- Habr? que crear el proyecto `src/FloorplanFit.Desktop` antes de dar por ejecutable el slice.
- La UI del slice ser? deliberadamente m?nima.
- La robustez se mide por el flujo real, no por polish visual.
