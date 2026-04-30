---
type: decision
date: 2026-04-29
status: accepted
---

# Copy Imported DXFs Into Managed Workspace

## Decision

En el Slice 1 ejecutable, cuando el usuario importe un floor plan DXF, la aplicaci?n debe **copiar el archivo a un workspace administrado** por el sistema (por ejemplo `library/raw-dxf/`) y persistir esa ruta administrada como `storage_path`.

## Why

- La app necesita trazabilidad y control sobre los archivos que institucionaliza.
- Depender de una ruta externa original deja el sistema fr?gil frente a movimientos, renombres o borrados fuera de la aplicaci?n.
- `TECH-STACK-ARCHITECTURE-DATAFLOW.md` ya define una estructura runtime con filesystem gestionado por la app.
- Para calidad senior y robusta, el sistema debe poseer los activos que convierte en objetos reutilizables.

## Operational Rule

- La ruta elegida por el usuario es solo fuente de importaci?n.
- El archivo persistido por el sistema debe vivir dentro del workspace administrado.
- El hash debe corresponder al contenido realmente copiado.
- `ImportedDocument.storage_path` debe apuntar a la copia administrada, no a la ruta externa original.

## Consequences

- El Slice 1 necesita un servicio concreto de filesystem/copia.
- Los tests de integraci?n deber?n verificar tambi?n la copia del archivo, no solo metadatos en memoria.
- El sistema queda mejor preparado para auditor?a, export y reutilizaci?n futura.
