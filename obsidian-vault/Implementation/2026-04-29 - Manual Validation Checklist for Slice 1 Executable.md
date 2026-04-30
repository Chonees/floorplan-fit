---
type: implementation
date: 2026-04-29
status: active
---

# Manual Validation Checklist for Slice 1 Executable

## Current manual smoke path

1. Abrir `Floorplan Fit`
2. Verificar que aparece la ventana `Floorplan Fit - Library`
3. Hacer click en `Import DXF`
4. Elegir `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`
5. Verificar que el status cambie a `Imported SANTA-BARBARA v1`
6. Verificar que aparezca un row con:
   - code: `santa-barbara`
   - name: `SANTA-BARBARA`
   - status: `Imported`
   - version: `1`
   - source unit: `inch`
7. Cerrar la app
8. Reabrir la app
9. Verificar que la fila reaparezca desde SQLite aunque sea una nueva sesion de UI
10. Verificar en filesystem que exista:
   - `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/app.db`
   - `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/library/raw-dxf/SANTA-BARBARA.dxf`

## Manual run result on this machine

Validacion manual ya ejecutada el **2026-04-29** para el smoke path base:

- la app abre
- el import de `SANTA-BARBARA.dxf` funciona
- el workspace real se crea bajo `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace`
- la confusion inicial vino de mirar `src/FloorplanFit.Application/bin/Debug/net10.0`, que no es el host de runtime de la app

## Important current limitations

- El smoke path manual basico ya fue validado.
- La rehidratacion al reabrir ahora queda implementada y verificada por tests, pero todavia conviene confirmarla visualmente en una corrida manual nueva.
- Reimportar el mismo DXF ya no crea un template nuevo por sufijo tecnico: el segundo import queda cubierto por tests como `version 2` del mismo template.
