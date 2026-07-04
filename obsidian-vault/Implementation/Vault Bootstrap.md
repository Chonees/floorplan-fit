---
type: implementation
date: 2026-04-25
status: active
---

# Vault Bootstrap

## Qué se creó

- `obsidian-vault/Home.md`
- `obsidian-vault/Current State.md`
- `obsidian-vault/Decisions/`
- `obsidian-vault/Implementation/`
- `obsidian-vault/Experiments/`
- `obsidian-vault/Bugs/`
- `obsidian-vault/Inbox/`

## Criterio de uso

- `Current State.md`: verdad actual y estado operativo
- `Decisions/`: decisiones de arquitectura, tooling y workflow
- `Implementation/`: ejecución concreta, estructura, pasos y avances
- `Experiments/`: pruebas y validaciones
- `Bugs/`: incidentes, causas raíz, fixes
- `Inbox/`: capturas rápidas que después se refinan

## Siguiente uso recomendado

Las próximas notas útiles serían:

1. estructura inicial de solución `.NET`
2. strategy note para `WallExtraction`
3. note de setup para Engram Cloud local

## Politica vigente de configuracion del vault

- el conocimiento del proyecto vive en las notas del vault, no en el layout personal de la UI
- `app.json`, `appearance.json`, `graph.json` y `workspace.json` ya no se versionan porque representan estado visual/local y generan churn sin valor de producto
- si hace falta compartir una configuracion estable del vault, solo conviene versionar piezas realmente compartidas como plugins core
