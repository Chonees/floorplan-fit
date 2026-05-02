---
type: implementation
date: 2026-04-30
status: active
---

# Mapa completo de arquitectura y archivos del repo

## Qu? se hizo

Se cre? una nueva fuente can?nica en `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md` que cubre **todos los archivos trackeados por git** del repositorio al 2026-04-30 y adem?s incorpora los documentos nuevos creados en esta misma pasada.

## Para qu? sirve

- navegar el repo sin perderse
- entender la arquitectura por capas y por ?reas
- saber qu? misi?n cumple cada archivo
- auditar r?pidamente si una zona del sistema ya existe o todav?a est? vac?a

## Alcance exacto

- cobertura verificada contra `git ls-files`
- base versionada cubierta: **201 archivos trackeados**
- documentos nuevos sumados en esta pasada: **2**
- total cubierto en esta pasada: **203 archivos**
- excluye `bin/`, `obj/`, caches y directorios no versionados porque no son fuente can?nica del producto

## Uso recomendado

1. leer `MVP-UX.md` y `TECH-STACK-ARCHITECTURE-DATAFLOW.md`
2. abrir el mapa completo de archivos
3. bajar luego al ?rea exacta (`src/`, `tests/`, `docs/`, `obsidian-vault/`) seg?n la duda puntual

## Relaci?n con Current State

`Current State.md` ahora referencia este mapa como fuente exhaustiva de navegaci?n del repo.
