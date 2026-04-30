---
type: implementation
date: 2026-04-29
status: active
---

# Desktop Shortcut Created

## What

Se creo un acceso directo local en el escritorio del usuario:

- `C:\Users\lucas\OneDrive\Escritorio\Floorplan Fit.lnk`

## Target

El acceso directo apunta al ejecutable ya compilado del Desktop slice:

- `src/FloorplanFit.Desktop/bin/Debug/net10.0/FloorplanFit.Desktop.exe`

## Why

Sirve para abrir mas rapido la app durante la validacion manual del Slice 1 ejecutable, sin tener que escribir el comando cada vez.

## Tradeoff

Este acceso directo depende del binario actual en `Debug/net10.0`.

- ventaja: lanzamiento inmediato
- desventaja: si cambia la ubicacion del output o se limpia `bin/`, el shortcut puede quedar roto

Si eso pasa, la alternativa mas robusta seria crear un launcher script estable y apuntar el acceso directo a ese script.
