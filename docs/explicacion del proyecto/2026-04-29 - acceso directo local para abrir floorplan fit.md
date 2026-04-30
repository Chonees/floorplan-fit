# 2026-04-29 - acceso directo local para abrir Floorplan Fit

## Que se hizo

Se creo un acceso directo en el escritorio del usuario:

- `C:\Users\lucas\OneDrive\Escritorio\Floorplan Fit.lnk`

## A que apunta

Apunta al ejecutable local ya compilado:

- `src/FloorplanFit.Desktop/bin/Debug/net10.0/FloorplanFit.Desktop.exe`

## Para que sirve

Acelera la validacion manual del Slice 1 ejecutable.

En vez de correr `dotnet run` cada vez, el usuario puede abrir la app con doble click.

## Riesgo conocido

Como apunta al output actual de `Debug`, puede romperse si cambia la carpeta de salida o si se limpia `bin/`.
