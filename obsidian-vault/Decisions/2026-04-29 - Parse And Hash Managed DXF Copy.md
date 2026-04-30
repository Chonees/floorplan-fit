---
type: decision
date: 2026-04-29
status: accepted
---

# Parse And Hash Managed DXF Copy

## Decision

En el Slice 1 ejecutable, despu?s de que el usuario elige un DXF, la aplicaci?n debe **copiar primero** el archivo al workspace administrado y luego leer/hashear **esa copia gestionada**, no la ruta externa original.

## Why

- Garantiza que metadata DXF, fingerprint y hash correspondan exactamente al archivo institucionalizado por la app.
- Evita inconsistencias si el archivo original cambia, se mueve o se reemplaza entre lectura y copia.
- Refuerza la regla de que la app debe operar sobre activos que controla.

## Operational Rule

- `IManagedFileStorage` corre antes de `IDxfGateway` y `IFileHashService`.
- `IDxfGateway` debe leer la copia gestionada.
- `IFileHashService` debe hashear la copia gestionada.
- `ImportedDocument.storage_path` sigue apuntando a la copia gestionada.
