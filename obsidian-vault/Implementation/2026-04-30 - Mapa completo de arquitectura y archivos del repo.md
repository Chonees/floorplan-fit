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

## Actualizaci?n 2026-05-02

- el mapa can?nico de `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md` fue **revalidado** contra `git ls-files`
- la cobertura qued? sincronizada a **211 archivos trackeados**
- se incorporaron los archivos nuevos agregados desde la versi?n inicial del mapa
- cada entrada ahora explica:
  - **misi?n**
  - **importancia**
  - **use case**

### Por qu? importa

Antes el mapa serv?a muy bien para **ubicar** archivos, pero no tanto para entender por qu? cada pieza era imprescindible.

Ahora el documento no solo te dice **qu? es cada archivo**, sino tambi?n:

- qu? se perder?a si no existiera
- en qu? momento real del producto, del runtime o del mantenimiento aparece su valor

Eso lo convierte en una herramienta mucho m?s fuerte para:

- onboarding
- auditor?a de arquitectura
- debugging conceptual
- detectar r?pido qu? capa sostiene cada responsabilidad

## Actualización 2026-05-03

- el mapa canónico incorporó los archivos nuevos del fix de review UI
- la cobertura visible del documento pasó a **218 archivos relevantes**: **211 trackeados** más **7 archivos nuevos del working tree**
- se agregaron entradas siguiendo el mismo patrón de redacción: **misión**, **importancia** y **use case**

## Actualizaci?n 2026-05-04

- se limpiaron del repo archivos de estado visual local de Obsidian (`app.json`, `appearance.json`, `graph.json`, `workspace.json`)
- se eliminaron docs duplicadas de launcher/shortcut local que no agregaban verdad de producto frente a Obsidian
- se eliminaron meta-notas viejas que solo anunciaban otras docs ya superseded por el mapa can?nico
- la cobertura visible del mapa qued? en **209 archivos relevantes del working tree**: **202 versionados presentes** m?s **7 archivos nuevos del working tree**
