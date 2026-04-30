# 2026-04-29 - checklist manual del Slice 1 ejecutable

## Smoke test actual

1. Abrir la app.
2. Ver que la ventana `Floorplan Fit - Library` aparece sin crash.
3. Importar `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`.
4. Verificar que la UI muestre:
   - `Imported SANTA-BARBARA v1`
   - fila con code `santa-barbara`
   - status `Imported`
   - version `1`
   - source unit `inch`
5. Verificar que exista `workspace/app.db`.
6. Verificar que exista la copia gestionada en `workspace/library/raw-dxf/`.

## Resultado ya confirmado en esta maquina

La corrida manual ya fue hecha y salio bien para el smoke test basico:

- la app abre
- importa `SANTA-BARBARA.dxf`
- crea `workspace`
- persiste archivos y DB

La confusion inicial fue revisar `bin/Debug/net10.0` de `FloorplanFit.Application` en vez del de `FloorplanFit.Desktop`.

## Limitaciones actuales

- La lista de Library todavia no se hidrata desde SQLite al iniciar.
- Reimportar el mismo DXF ya no deberia crear un segundo template por sufijo tecnico; el segundo import queda protegido por tests como nueva version del mismo template.

