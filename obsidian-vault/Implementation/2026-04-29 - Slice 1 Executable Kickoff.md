---
type: implementation
date: 2026-04-29
status: active
---

# Slice 1 Executable Kickoff

## Chosen direction

- Se empieza por el enfoque recomendado senior: `FloorplanFit.Desktop` m?nimo + pipeline real.
- El DXF importado se copiar? a `library/raw-dxf/`.
- Los `PLANS/catalog/*.json` se conservan como or?culo legacy/comparativo durante este slice.

## Working rule requested by the user

- Documentar cada paso importante en `docs/` y `obsidian-vault/` a medida que avancemos.

## Immediate next design target

Definir con precisi?n el Slice 1 ejecutable de punta a punta:

`select/import DXF -> read metadata/units -> copy to managed storage -> hash -> persist in SQLite -> create template/version -> show library item in thin desktop`

## Milestone 1 completed

Se implement? la primera correcci?n estructural en Application:

- se agreg? `IManagedFileStorage`
- `ImportFloorPlanHandler` ahora importa as?:
  - copia primero el DXF al storage gestionado
  - lee metadata/fingerprint desde la copia gestionada
  - calcula hash desde la copia gestionada
  - persiste `ImportedDocument.storage_path` con esa ruta gestionada

## Why this matters

Esto evita una inconsistencia sutil pero grave: que metadata y hash describan un archivo externo que podr?a cambiar antes de que la app institucionalice su propia copia.

## Milestone 2 completed

Tambien ya se escribio la infraestructura concreta del import:

- workspace administrado
- copia de DXF a `library/raw-dxf/`
- hash SHA-256 real
- lectura DXF real con `IxMilia.Dxf`
- schema SQLite
- repositorios SQLite
- unit of work SQLite
- tests de infraestructura escritos antes de esa implementacion

## Current truth after milestone 2

El Slice 1 ya no depende solo de dobles de prueba en Application.

Ahora el repo contiene el pipeline concreto pensado para:

`managed copy -> read DXF real -> hash real -> persist in SQLite -> return library item`

La verificacion ejecutable queda diferida hasta tener SDK.
