# 2026-07-03 - Goal prompt finite para canonical adjustment recipe

## Tipo
Implementation / Prompt pattern

## Contexto
El objetivo del proyecto es que el FloorPlan curado produzca una `AdjustmentRecipe` canónica y que Electrical/Roof/Facade se registren contra ese FloorPlan para recibir la misma cirugía geométrica aprobada, sin crear cuatro motores de ajuste separados.

## Decisión
El prompt debe operar como una meta finita, phase-gated y evidence-driven. No debe pedir "hacer todo"; debe parar cuando exista la primera ruta demostrable:

`FloorPlan adjustment -> AdjustmentRecipe guardada -> dependent sheet projection consume la recipe -> export report/manifest indica qué aplicó y qué quedó para review`.

## Guardrails anti-loop
- Máximo 3 intentos fallidos sobre el mismo síntoma.
- No repetir explicación sin nueva evidencia.
- No implementar antes de investigar CAD workflow real y mapearlo al código actual.
- No build si el repo lo prohíbe.
- Cada fase debe producir artefacto: research note, mapa actual, diseño, task list, test, implementación o verificación.
- Si falta runtime/manual smoke, reportar bloqueo concreto y comando/acción exacta para destrabar.

## Macro + micro obligatorios
- Macro: cómo un arquitecto/CAD ubica una casa en setbacks, qué significa global placement, local stretch/compression y overlay/XREF.
- Micro: qué archivos/DTOs/handlers/repos/exporters/tests de Floorplan Fit participan y qué riesgo tiene cada cambio.

## Uso recomendado
Usar este prompt para iniciar o continuar la meta, no para abrir una refactorización total. La implementación debe avanzar por slices chicos, empezando por report-first antes de deformar DXF dependientes.
