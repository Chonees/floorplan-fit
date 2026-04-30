---
type: implementation
date: 2026-04-29
status: active
---

# Desktop Dev Watch Launcher

## What changed

Se agrego un launcher de desarrollo continuo:

- `scripts/dev-desktop.bat`
- acceso directo de escritorio: `C:\Users\lucas\OneDrive\Escritorio\Floorplan Fit Dev Watch.lnk`

## Why

Durante la validacion manual aparecio un problema operativo, no de arquitectura:

- el acceso directo del escritorio apuntaba al `.exe` de `bin/Debug/net10.0`
- ese `.exe` podia quedar viejo respecto del codigo mas reciente
- entonces el usuario podia abrir la app y creer que un fix no existia, cuando en realidad estaba corriendo un binario stale

## What the launcher does

El `.bat`:

1. se posiciona en la raiz del repo
2. muestra un encabezado claro de modo desarrollo
3. detecta si `FloorplanFit.Desktop.exe` ya esta corriendo y avisa sobre posibles locks de DLL
4. ejecuta:

- `dotnet watch --non-interactive run --project "src\FloorplanFit.Desktop\FloorplanFit.Desktop.csproj"`

Ademas, se dejo un acceso directo nuevo en el escritorio que apunta a ese `.bat`, para no depender del `.exe` compilado.

## Why this is the recommended path

Para desarrollo iterativo, este launcher es mejor que abrir el `.exe` directo porque:

- corre siempre contra el codigo fuente actual
- recompila y reinicia segun corresponda
- evita confusion entre outputs `Debug` distintos
- funciona como equivalente practico del "modo Vite" para este repo desktop

## Recommended usage

- cerrar cualquier instancia vieja de `FloorplanFit.Desktop.exe`
- ejecutar `scripts/dev-desktop.bat`
- dejar la consola abierta mientras se desarrolla
- usar el acceso directo del `.exe` solo para smoke tests manuales puntuales, no para iterar cambios

## Honest current truth

Este cambio estandariza el flujo de desarrollo, pero NO reemplaza automaticamente el acceso directo viejo del escritorio.

El shortcut existente sigue apuntando al `.exe` compilado y puede quedar desfasado del ultimo codigo.
