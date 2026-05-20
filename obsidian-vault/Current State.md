# Current State

## 2026-05-16
- Review ahora permite **Eliminar franja**.
- El delete de franja hace cascade manual sobre:
  - puntos de medida
  - interval bindings manuales de cotas
- La selección local de franja/puntos se limpia después del refresh para no dejar UI colgando.
- Fix extra: guardar "qué mide" ya no crashea si el refresh limpia selección de franja/puntos antes del replay.
- UX hardening: si una franja tiene exactamente 2 puntos y ya hay una cota seleccionada, el sistema autocompleta punto inicial/final y puede habilitar Guardar qué mide sin obligar a elegir ambos combos manualmente.
- Preview: el zoom máximo subió de 6x a 20x para permitir curado más preciso.
- Preview: el zoom máximo volvió a subir y ahora quedó en 40x.
- Preview: el zoom máximo volvió a subir y ahora quedó en 80x.

- Measurement interval discovery: cross-wall start/end nodes are accepted inside one corridor, but reactive rebuild still uses raw 2D node points, so total width/height only works robustly when both endpoints are already axis-aligned.

- Product semantics: a true diagonal A->B measure is not equivalent to a Width/Height interval. Pinches and articulation bands are axis-aligned, so diagonal support would need explicit angle-aware semantics instead of reusing the same corridor model unchanged.

- Measurement intervals: cross-wall Width/Height bindings are now projected back to the corridor axis before reactive rebuild, so total width/height no longer turns into a diagonal when clicks are misaligned.
- Preview overlay: the active interval line is now axis-aligned; nodes still stay at their real clicked geometry positions.
- Guard rail: Guardar qué mide is disabled for FreeAngle dimensions (and axis-mismatched corridor selections) with Spanish guidance in the ViewModel.
- Preview: ahora hay un switch visual en el panel Preview para mostrar u ocultar todas las cotas del canvas.
- Cuando el switch oculta cotas, también se apaga el hit-testing de dimensions para que no queden invisibles pero clickeables.

## 2026-05-19
- Preview dimensions: las cotas que ya tienen una relacion manual con nodos (`DimensionIntervalBindingDto`) ahora se dibujan con color celeste tanto en geometria como en texto.
- La seleccion sigue ganando prioridad visual: una cota vinculada y seleccionada se ve con el highlight verde existente.
- Verificacion focalizada: `PreviewRenderComposerTests`, `FloorPlanPreviewControlTests`, `NativeDimensionPreviewControlTests` y `MeasurementBindingPreviewLayerRendererTests` pasan usando `dotnet test ... --artifacts-path .testartifacts\dotnet-test-artifacts` para evitar el lock del ejecutable desktop abierto.

## 2026-05-19 - Publish readiness concern
- Architecture finding: current `PublishFloorPlanCurationHandler` publishes by flipping curation status and setting `floorplan_templates.active_published_curation_id`; its hard gate is currently only that at least one pinch marker exists.
- Risk: that is not enough to guarantee the published floor plan is fit-ready for future site-plan auto-adjustment.
- Recommended next architecture slice: add a Fit Readiness / Published Curation Contract validator before publish so Loop 2 receives complete, coherent, auditable structured data.
- Follow-up verification: corridor/node/dimension interval binding and pinch group/marker persistence are structurally correct. Focused Application tests passed 7/7 and Infrastructure persistence tests passed 2/2. Remaining issue is not whether this raw data is saved, but whether publish validates that it is complete enough for future site-plan auto-fit.
- Limitation verified: measurement corridor interval math is currently axis-aligned (`Width`/`Height`) and not angle-aware. Angled/diagonal dimensions are classified as `FreeAngle` and kept authored/static; no proportional reduction along an inclined corridor vector is implemented yet.

## 2026-05-20 - Measurement node coordinate UX finding
- Verified: measurement node AxisCoordinate is computed from the selected corridor axis. Height uses the clicked point Y; Width uses the clicked point X.
- Consequence: two nodes on the same vertical wall can show the same Coordenada eje under a Width corridor even when PositionRatio differs. The points are different; the displayed coordinate is the corridor-axis coordinate, not A-to-B distance along the wall.
- UX risk: node cards should clarify axis coordinate vs real anchor (AnchorX/AnchorY) and warn when the selected axis makes the interval collapse to zero.

## 2026-05-20 - Raw A-B interval bindings restored
- Current truth changed: manual-verified dimension interval bindings rebuild dimensions from the real two-node A/B line again, not from an axis-projected corridor baseline.
- Any selected dimension, including free-angle dimensions, can be associated to the selected corridor's two nodes if the nodes belong to that corridor.
- The preview measurement overlay now draws the raw A/B line between nodes.
- `AxisCoordinate` can still repeat for two points on the same vertical/horizontal wall depending on corridor axis; that value is not the A/B geometry. The actual node location is recovered from `GeometryPathId + PositionRatio` and the path geometry.
- Supersedes the 2026-05-18/2026-05-19 axis-only interpretation for dimension interval rebuilding. Corridor/pinch band overlap remains axis-tagged for now.

## 2026-05-20 - Codex skill removed
- User explicitly requested removing floorplan-fit-teaching-mode from the local Codex skills system.
- Verified deletion: C:\Users\lucas\.codex\skills\floorplan-fit-teaching-mode no longer exists.
- Future sessions should not rely on that skill being available from disk.

## 2026-05-20 - Bound dimensions follow live nodes without visual deformation
- Current truth changed again: manual-verified dimension interval bindings still resolve live node positions from `GeometryPathId + PositionRatio`, but linear dimensions no longer visually rebuild as raw skewable A/B segments.
- Bound dimensions on the active articulation axis now recompute from live nodes even when their saved interval does **not** overlap the selected pinch band; this prevents node-bound dimensions from staying behind when both anchors translate together.
- Linear dimensions preserve authored CAD appearance by projecting the live A/B delta onto the original dimension axis. Horizontal stays horizontal, vertical stays vertical, and free-angle keeps its authored angle.
- The previous "raw A-B interval bindings restored" note is superseded for visual rebuild semantics: nodes remain source of truth, but the displayed dimension geometry is style-preserving.

## 2026-05-20 - Dimension style preservation hardened for reversed/split shapes
- Follow-up from visual QA: moving all bound dimensions was not enough; split/reversed dimensions and dimensions drawn on any side could still deform if only simple first/second/third line primitives were rebuilt.
- Reactive associated linear dimensions now transform the whole primitive set in local dimension coordinates, preserving authored shape while changing the measured span along the authored axis.
- Binding node order no longer forces visual definition-point order; live nodes are sorted along the authored dimension axis so reversed dimensions keep their CAD orientation.
- Verification: DimensionIntervalReactiveProjectorTests now cover a reversed split dimension below the measured wall; Application focused slice passed 21/21 and Desktop focused slice passed 86/86.

## 2026-05-20 - Corrected dimension binding semantics after visual QA
- Current truth changed: the previous "bound dimensions follow live nodes" implementation is still semantically wrong for the intended curation model.
- Verified root cause: `DimensionIntervalReactiveProjector` resolves saved measurement nodes against adjusted preview geometry, then calls `DimensionGeometryProjector.RebuildAssociatedDimension`; that method maps the entire dimension primitive set to the live A/B node span.
- Product rule clarified: the A/B node line is measurement/trim metadata, not the visual transform driver for the CAD dimension. The dimension should preserve its authored position/geometry/side, and only the affected measurement span/value should reduce according to the articulation band.
- Consequence: tests that assert live-node anchoring passed, but they encode the wrong behavior for the desired UX and must be replaced before another implementation attempt.
- See bug note: `Bugs/2026-05-20 - Bound dimensions deform because live nodes drive visual transform.md`.

## 2026-05-20 - Bound dimensions ignore node normal drift
- Fix applied: `DimensionGeometryProjector` now uses live A/B nodes for the dimension's authored-axis span only; it no longer applies the live node perpendicular/normal offset to the cota geometry.
- Practical result: a horizontal cota can shorten/extend along X from the adjusted nodes, but it stays on its original Y/baseline/side; vertical cotas analogously stay on their original X side.
- Regression coverage added: `Project_uses_live_node_axis_span_without_pulling_the_dimension_to_the_node_normal_position`.
- Verification: Application focused slice passed 22/22; Desktop focused preview/review slice passed 86/86.

## 2026-05-20 - Bound dimensions are delta-based, not absolute-node anchored
- Current truth changed again: even "node axis span without normal drift" was still too literal because it could place a cota at the absolute live node coordinates.
- Correct model implemented: each saved measurement node contributes its movement delta from authored anchor point to live preview point. The dimension applies those deltas to its own authored endpoints along its authored axis.
- Practical result: if the A/B relation shrinks by 20 units, a dimension authored as `l-------l` becomes `l----l`; it does **not** jump to the yellow/node line, does **not** change height/side, and does **not** rotate.
- Regression coverage added: `Project_applies_live_node_delta_instead_of_anchoring_dimension_to_absolute_node_coordinates`.
- Verification: Application focused slice passed 23/23; Desktop focused preview/review slice passed 86/86.

## 2026-05-20 - Bound dimensions use visual cota axis instead of definition-point axis
- Follow-up visual QA showed dimensions could still tilt/lift because the projector used `DefPoint -> DefPoint2` as the transform axis.
- Verified root cause: in real CAD dimensions, definition points can be feature/extension origins and are not guaranteed to be parallel to the visible cota line.
- Fix applied: `DimensionGeometryProjector.ResolveAxis` now prefers the visual axis from terminal inserts, then longest drawn dimension primitive, and only falls back to definition points/angle.
- Regression coverage added: `Project_preserves_visual_dimension_axis_when_definition_points_are_not_parallel_to_the_cota_line`.
- Verification: Application focused slice passed 24/24; Desktop focused preview/review slice passed 86/86.
