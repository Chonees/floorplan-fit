# 2026-04-29 - checklist manual del Slice 1 ejecutable

> **Nota de actualizaci?n (2026-05-02):** este documento naci? como checklist del 29/04, pero se actualiza para no dejar drift con el runtime actual. Donde cambi? la app, se deja explicitado.

## Smoke test actual

1. Abrir la app.
2. Ver que la ventana `Floorplan Fit - Library` aparece sin crash.
3. Si ya hab?a imports previos, verificar que la Library hidrata filas desde SQLite al iniciar.
4. Importar `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`.
5. Verificar que la UI muestre:
   - fila con code `santa-barbara`
   - name `SANTA-BARBARA`
   - status `Extracted` porque el import actual auto-dispara extracci?n
   - version `1` si el workspace est? limpio
   - source unit `inch`
6. Verificar que exista `workspace/app.db`.
7. Verificar que exista la copia gestionada en `workspace/library/raw-dxf/`.
8. Seleccionar el item e ir a `Open Review`.
9. Verificar que la review abre en el primer intento y muestra wall candidates sin crash.

## Resultado hist?rico confirmado en esta m?quina (2026-04-29)

La corrida manual original del smoke test b?sico ya hab?a dado bien para ese corte:

- la app abr?a
- importaba `SANTA-BARBARA.dxf`
- creaba `workspace`
- persist?a archivos y DB

La confusi?n inicial fue revisar `bin/Debug/net10.0` de `FloorplanFit.Application` en vez del de `FloorplanFit.Desktop`.

## Verdad actual

- La Library **s?** hidrata desde SQLite al iniciar.
- El import actual deja el floor plan en estado **`Extracted`**, no `Imported`, porque la extracci?n corre autom?ticamente despu?s de guardar el DXF.
- El crash del primer `Open Review` despu?s de extraction qued? corregido el **2026-05-02**.

## Limitaciones actuales

- Para validar versionado/reimport limpio, hay que usar siempre `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`, no la copia gestionada dentro de `workspace/library/raw-dxf/`.
- Reimportar el mismo DXF queda protegido por tests como nueva versi?n del mismo template.
- Loop 2 todav?a no tiene implementaci?n real de site plan, envelope, fit engine ni proposals.
