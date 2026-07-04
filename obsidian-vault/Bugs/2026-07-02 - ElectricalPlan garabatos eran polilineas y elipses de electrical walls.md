# 2026-07-02 - ElectricalPlan garabatos eran polilíneas y elipses de Electrical Walls

type: Bug
status: Root cause identified and mitigated in exporter
replaces:
replaced_by:

## Síntoma
Al exportar el HousePlanSet de SEMINOLE, el DXF dependiente `ElectricalPlan` abría en AutoCAD con “garabatos” / walls redondas gigantes que visualmente deformaban el plano.

## Lo que NO era
- No era necesario desvincular y volver a relacionar el ElectricalPlan con SEMINOLE.
- No era un problema de `HousePlanSet` registration/projection guardada.
- No era sólo un problema de `ARC`/`SPLINE` directos en `ENTITIES`.
- No era sólo un problema de bloques `CFANLT`.

## Causa real final
El último export inspeccionado (`TEST-BLOCK-FIX-plan-set`) ya no tenía curvas problemáticas `ARC`/`SPLINE` ni directas ni dentro de `BLOCKS`.

Lo que seguía dibujando las “Electrical Walls” redondas eran entidades DXF de otra familia:

- `LWPOLYLINE` en layer `ELECTRICAL WALLS`, color `251`: 24 entidades
- `ELLIPSE` en layer `ELECTRICAL WALLS`, color `251`: 4 entidades

AutoCAD las mostraba como curvas/paredes redondas aunque no fueran entidades `ARC`.

## Fix aplicado
La cuarentena del exporter dependiente ahora omite curvas visualmente peligrosas en ElectricalPlan:

- `ELECTRICAL WIRING`: `ARC`, `SPLINE`
- `ELECTRICAL WALLS`: `ARC`, `SPLINE`, `LWPOLYLINE`, `ELLIPSE`
- `ELECTRICAL` color `253`: `ARC`, `SPLINE`

Y preserva geometría lineal útil:

- `ELECTRICAL WALLS` `LINE`: 832 entidades conservadas en SEMINOLE

## Archivos tocados
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`

## Lección
El error fue asumir que “curva visible” equivalía a `ARC` o `SPLINE`. En DXF, AutoCAD también dibuja curvas desde `LWPOLYLINE` con muchos puntos/bulges y desde `ELLIPSE`. Para depurar visual DXF hay que auditar por familia geométrica completa: `ARC`, `SPLINE`, `LWPOLYLINE`, `ELLIPSE`, `BLOCKS`, `DIMENSION` y símbolos insertados.

## Próximo paso de prueba
Regenerar un export nuevo después de recompilar, por ejemplo `TEST-POLY-FIX`, y abrir el ElectricalPlan dentro de `TEST-POLY-FIX-plan-set`.