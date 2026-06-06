# Current State

## 2026-05-16
- Review ahora permite **Eliminar franja**.
- El delete de franja hace cascade manual sobre:
  - puntos de medida
  - interval bindings manuales de cotas
- La selecciÃ³n local de franja/puntos se limpia despuÃ©s del refresh para no dejar UI colgando.
- Fix extra: guardar "quÃ© mide" ya no crashea si el refresh limpia selecciÃ³n de franja/puntos antes del replay.
- UX hardening: si una franja tiene exactamente 2 puntos y ya hay una cota seleccionada, el sistema autocompleta punto inicial/final y puede habilitar Guardar quÃ© mide sin obligar a elegir ambos combos manualmente.
- Preview: el zoom mÃ¡ximo subiÃ³ de 6x a 20x para permitir curado mÃ¡s preciso.
- Preview: el zoom mÃ¡ximo volviÃ³ a subir y ahora quedÃ³ en 40x.
- Preview: el zoom mÃ¡ximo volviÃ³ a subir y ahora quedÃ³ en 80x.

- Measurement interval discovery: cross-wall start/end nodes are accepted inside one corridor, but reactive rebuild still uses raw 2D node points, so total width/height only works robustly when both endpoints are already axis-aligned.

- Product semantics: a true diagonal A->B measure is not equivalent to a Width/Height interval. Pinches and articulation bands are axis-aligned, so diagonal support would need explicit angle-aware semantics instead of reusing the same corridor model unchanged.

- Measurement intervals: cross-wall Width/Height bindings are now projected back to the corridor axis before reactive rebuild, so total width/height no longer turns into a diagonal when clicks are misaligned.
- Preview overlay: the active interval line is now axis-aligned; nodes still stay at their real clicked geometry positions.
- Guard rail: Guardar quÃ© mide is disabled for FreeAngle dimensions (and axis-mismatched corridor selections) with Spanish guidance in the ViewModel.
- Preview: ahora hay un switch visual en el panel Preview para mostrar u ocultar todas las cotas del canvas.
- Cuando el switch oculta cotas, tambiÃ©n se apaga el hit-testing de dimensions para que no queden invisibles pero clickeables.

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

## 2026-05-20 - Fit tools moved into a dedicated workbench column (superseded)
- UX problem verified: the Fit/pinch/measurement flow lived inside the same inspector scroll as generic selection tools, forcing repeated scroll-touch-scroll loops for mm, pinches, corridors, nodes, and dimension bindings.
- Fix applied: the Fit tool now opens a dedicated `FitWorkbench` in the right column, while generic inspector tools use `GeneralInspectorContent`.
- The right work area is now wider (`ColumnDefinitions="320,*,420,72"`) and the Fit workflow is split into bounded panels: quick mm/group/action controls, Pinches, Bandas, Franjas, Nodos, and Cota vinculada.
- ViewModel state now exposes `IsGeneralInspectorSelected` so the UI can hide the generic inspector while the Fit workbench is active.
- Verification: focused Desktop tests passed 62/62 with the review layout, VM architecture, measurement-binding VM, review VM, and XAML initialization slices.
- Superseded the same day because the user rejected the workbench/column as too console-like.

## 2026-05-20 - Fit tools now use a canvas-first icon palette
- Current truth: the `FitWorkbench` column has been removed. Fit actions now live in `FitToolPalette` directly above the preview canvas.
- The main review layout is back to a lighter `340,*,320,72` column split; the inspector remains contextual information, not the primary Fit operation surface.
- The Fit palette exposes compact context controls (group, selected group, axis, mm, corridor, selected corridor, A/B nodes) and icon tools for Crear grupo, Marcar pinch, Crear franja, Elegir nodo, Vincular cota, Quitar pinch, and Eliminar franja.
- Active tools use the existing visual language: pinch and node buttons reuse `Button.tool` plus `Classes.tool-active` bound to `IsPinchPlacementArmed` / `IsMeasurementNodePlacementArmed`.
- Verification: the new RED/green layout and architecture tests passed; focused Desktop slice passed 62/62.

## 2026-05-20 - Fit palette simplified after visual QA
- Follow-up visual QA showed the first icon palette was still too busy: inputs, dropdowns, help text, and disabled buttons could overlap and feel non-intuitive.
- Current truth: the top `FitToolPalette` is now minimal and canvas-first: title, axis, mm, and only direct icon actions (Marcar pinch, Crear franja, Elegir nodo, Quitar pinch).
- Existing/created objects now live in the right inspector panel under `FitExistingPanel`: Pinches existentes, Franjas existentes, Nodos existentes, Bandas, and Cota vinculada.
- The grey/no-op Vincular cota icon was removed from the top toolbar; binding actions now live in the right panel with context, A/B node selectors, summaries, and enabled/disabled state.
- Verification: layout/architecture tests passed 12/12; focused Desktop slice passed 62/62.

## 2026-05-20 - Fit right panel reduced to existing pinches and nodes (superseded)
- Follow-up UX correction: the right panel is no longer a full Fit management surface.
- `Crear franja` now opens a `Flyout` popup from the toolbar icon and asks for `Nombre de franja`; the name is no longer typed in the right panel.
- `FitExistingPanel` now only shows two dropdowns:
  - `Pinche existente` bound to all `PinchMarkers`.
  - `Nodo existente` bound to all `MeasurementNodes`.
- User-facing group controls were removed from this screen. Pinch groups remain an internal model detail for grouping pinches into articulation bands, but they are no longer presented as a primary UX concept here.
- Verification: layout/architecture tests passed 12/12; focused Desktop slice passed 62/62.
- Replaced by: Fit right panel restored node A/B binding controls. The two-dropdown-only panel removed too much of the dimension-binding workflow.

## 2026-05-20 - Fit right panel restored node A/B binding controls
- Current truth: the canvas-first Fit toolbar remains minimal, and `Crear franja` still opens a Flyout popup from the toolbar icon.
- The right `FitExistingPanel` is no longer two-dropdown-only. It now restores the useful slower workflow:
  - `Pinche existente` dropdown for existing pinch markers.
  - `Nodos existentes` navigator using `MeasurementNodeOptions`, with real operator labels like `Patio-Width - Nodo A` plus source/axis/coordinate details.
  - `Nodo A` and `Nodo B` selectors backed by the selected corridor's node options.
  - `Cota vinculada` summary plus `Guardar que mide` and `Volver a medida fija`.
- Selecting an existing node now selects its measurement corridor in the ViewModel, so the A/B selectors and save-enabled state stay coherent without exposing group/corridor management as a console.
- User-facing group controls remain removed from this screen; groups are still an internal grouping model.
- Verification: RED confirmed missing node-option properties; focused Desktop slice passed 63/63 with layout, architecture, measurement-binding ViewModel, review ViewModel, and XAML initialization tests.

## 2026-05-21 - Fit right panel lists A-B groups
- Current truth: the Fit right panel no longer lists individual measurement nodes as separate choices.
- `Nodos existentes` was renamed to `Grupos de A y B`.
- The panel now lists `MeasurementNodePairOptions`: one selectable A/B group per measurement corridor with at least two nodes.
- Selecting a group sets `SelectedMeasurementCorridor`, `SelectedMeasurementNode`, `SelectedMeasurementStartNode`, and `SelectedMeasurementEndNode` together, so `Guardar que mide` still works without choosing A and B in separate dropdowns.
- Superseded detail: the first A/B group version displayed A and B only as read-only summaries. Latest UX restores a scoped `Nodo del grupo` dropdown.
- Verification: RED confirmed missing pair-option properties; focused Desktop slice passed 63/63.

## 2026-05-21 - Fit A-B groups keep scoped endpoint dropdown (superseded)
- Current truth: `Grupos de A y B` remains the main list; the panel does not go back to a global node list.
- A scoped `Nodo del grupo` dropdown was restored under the selected A/B group.
- That dropdown only lists the selected group's endpoints and labels them `A` / `B` with details like source, axis, coordinate, and line ratio; it no longer shows only a raw coordinate.
- Selecting endpoint A or B updates `SelectedMeasurementNode` for navigation/highlight without changing the saved A/B relationship (`SelectedMeasurementStartNode` and `SelectedMeasurementEndNode`).
- Verification: RED confirmed missing endpoint-option properties; focused Desktop slice passed 63/63.
- Superseded by: Fit restores manual A/B assignment inside selected group. Endpoint-only navigation was not the old A/B assignment workflow.

## 2026-05-21 - A-B endpoint dropdown clarification accepted
- Clarification: the user originally meant the old two lower A/B controls used while adding new nodes, where the operator could choose which newly placed node related to which endpoint.
- Current UX with `Grupos de A y B` plus scoped `Nodo del grupo` dropdown is accepted by the user despite the earlier misunderstanding.
- Decision: leave the current implementation as-is unless the user explicitly asks to bring back the old lower A/B assignment controls.

## 2026-05-21 - Fit restores manual A-B assignment inside group
- Current truth: `Grupos de A y B` is a corridor/group selector and is visible even before any nodes exist, so the operator can select the group, place node 1, place node 2, then assign A/B.
- The group selector now uses `MeasurementNodeGroupOptions`, not pair-only options; it no longer disappears when the selected group has fewer than 2 nodes.
- `Nodo del grupo` lists the nodes inside the selected group for navigation/highlight.
- The lower A and B controls are dropdowns again. They are scoped to the selected group and let the operator decide which node is A and which node is B before pressing `Guardar que mide`.
- Node option labels now show `Nodo 1`, `Nodo 2`, etc. plus source/axis/coordinate/line details, not only a raw coordinate.
- Verification: RED confirmed missing group/manual A-B option properties; focused Desktop slice passed 63/63.

## 2026-05-21 - Fit supports single-node groups and franja deletion
- Current truth: `Grupo A/B` lists every measurement corridor/group even when it has 0 or 1 node, so the operator can return to a partially-authored franja after restarting the app and keep adding nodes.
- With a one-node group selected, `Elegir nodo` can still be armed; the status message guides the operator to click valid preview geometry and place the next measurement node.
- The right panel now exposes `Eliminar franja`, enabled when a measurement group/corridor is selected. It calls the existing cascade delete path (`RemoveSelectedMeasurementCorridorAsync`) that removes the franja and clears related nodes/bindings/selection.
- The lower A and B dropdowns remain scoped to the selected group and continue to assign the saved measurement relationship manually.
- Verification: RED confirmed missing delete button; focused Desktop slice passed 63/63.

## 2026-05-21 - Fit hides group names and shows selected franja nodes
- Current truth: the Fit toolbar no longer asks for `Nombre de franja`; clicking `Crear franja` creates the next internal franja automatically from the selected preview line.
- The right panel keeps `Grupos de A y B` visible as the selector, but no longer exposes the internal corridor/group name (`Patio-Width`, etc.). Options are neutral `Franja 1`, `Franja 2`, with axis + node count details.
- Selecting a franja now shows `Nodos de esta franja` as a visible list, not only a hidden/dropdown navigation affordance.
- `Eliminar franja` remains directly available for the selected franja and uses the cascade delete path for corridor + nodes + manual interval bindings.
- Lower A/B dropdowns remain scoped to the selected franja so the operator can still decide which placed node is A and which is B before `Guardar qu? mide`.
- Supersedes the user-facing copy from the prior `Grupo A/B` / `Nodo del grupo` iteration; the model still uses corridors internally, but the UI talks in franjas and nodes.
- Verification: RED confirmed missing no-name creation/list UI; focused Desktop slice passed 63/63 with review layout, architecture, measurement-binding ViewModel, review ViewModel, and XAML initialization tests.

## 2026-05-21 - Fit franja selection restores visual A-B overlay
- Bug verified: selecting a `Grupos de A y B` franja without also selecting a dimension did select the corridor/group, but `TryAutoAssignMeasurementEndpoints` refused to assign A/B unless `SelectedDimension` was non-null.
- Consequence: the preview could show no A/B interval line for the selected franja, and selecting a node felt like the visual relation disappeared.
- Fix: endpoint auto-assignment now only requires a selected franja/corridor with exactly two nodes; it does not require a selected dimension.
- Selecting a node in `Nodos de esta franja` changes only `SelectedMeasurementNode` for navigation/highlight and keeps selected franja + A/B endpoints alive.
- Verification: new RED test `Measurement_group_selection_without_dimension_keeps_preview_nodes_and_interval_active` failed with null start/end; after the fix it passed. Focused Desktop slice passed 68/68 including measurement preview renderer tests.

## 2026-05-21 - Fit selection survives ComboBox refresh nulls
- Bug verified: Avalonia selection controls can transiently push `SelectedItem = null` when their `ItemsSource` refreshes. The previous Fit setters treated that null as a real user clear, wiping the selected franja/node/A/B endpoints.
- Consequence: after selecting a `Grupos de A y B` franja, the dropdown could appear blank and the preview could lose selected node/start/end ids, so nodes/lines were not highlighted.
- Fix: the group/node/A/B option setters now ignore transient null values; explicit delete/restore flows still clear the underlying `SelectedMeasurementCorridor` / nodes directly.
- Fix: the preview now binds to explicit ViewModel id properties (`SelectedMeasurementCorridorId`, `SelectedMeasurementNodeId`, `SelectedMeasurementStartNodeId`, `SelectedMeasurementEndNodeId`) instead of nested nullable paths like `SelectedMeasurementStartNode.NodeId`.
- Verification: RED compile/test confirmed missing explicit ids and null-clearing behavior; focused Desktop slice passed 68/68.

## 2026-05-21 - Fit franja dropdown uses stable option instances
- Bug verified: `MeasurementNodeGroupOptions` and `SelectedMeasurementGroupNodeOptions` were computed properties that recreated new option record instances on every getter call.
- Consequence: Avalonia `ComboBox/ListBox.SelectedItem` could point at an object that was value-equal but not the same instance as the current `ItemsSource` item, so selecting a franja could leave the dropdown blank and make navigation/highlight feel broken.
- Fix: group and selected-node option lists are now cached/stable between data refreshes. Caches invalidate only when the underlying review session data changes or when the selected franja changes for node options.
- The previous null-guard fix was necessary but not sufficient; stable option identity is the missing piece for Avalonia selected-item controls.
- Verification: RED test asserted `Assert.Same` between selected options and current options; it failed before caching and passed after. Focused Desktop slice passed 68/68.

## 2026-05-21 - Publish stores Fit relationships but still lacks a hard readiness contract
- Verified again against current code: publishing does **not** export or flatten Fit data; it marks the draft curation as `Published` and sets `floorplan_templates.active_published_curation_id` to that same curation id.
- Therefore the already-saved Fit rows under that `floorplan_curation_id` remain the published source of truth: `pinch_groups`, `pinch_markers`, `measurement_corridors`, `measurement_nodes`, and `floorplan_dimension_interval_bindings`.
- A dimension becomes a curated node-bound measure only after `Guardar qué mide` creates/upserts a `DimensionIntervalBinding` with `BindingStatus = ManualVerified`, pointing to corridor + start/end nodes.
- The reactive preview consumes the same model: preview geometry is compressed by pinches, `ArticulationBandProjector` builds bands from pinch groups/markers, and `DimensionIntervalReactiveProjector` only adjusts dimensions with `ManualVerified` bindings whose corridor axis matches the active pinch band.
- Important gap remains: `PublishFloorPlanCurationHandler` still only gates on “at least one pinch marker”; it does not validate corridor completeness, node count, binding existence/coherence, or fit-readiness. A dedicated Fit Readiness / Published Curation Contract validator is still needed before trusting published data as Loop 2 input.
- Additional caveat: when opening a new draft based on a published curation, the reader applies lineage for generic overrides, but measurement corridors/nodes/bindings are read only from the active draft curation id. If published Fit semantics must carry into the next edit draft, measurement data needs explicit inheritance/copy support.

## 2026-05-21 - Franja axis editing is not implemented yet
- Verified current code: a measurement corridor/franja gets its `AxisTag` only when it is created from the toolbar `Eje` value via `AddMeasurementCorridorHandler`.
- There is no `UpdateMeasurementCorridor` handler/repository method/UI control today, so selecting an existing franja and changing it from `Width` to `Height` is not currently supported.
- Safe implementation would need more than updating the corridor row: recompute corridor band coordinates from its guide geometry, recompute each node `AxisCoordinate` from its stored real anchor/position for the new axis, and update/re-save any affected `DimensionIntervalBinding` interval coordinates or warn the user before changing linked data.

## 2026-05-22 - Existing Fit franjas can change Width/Height
- Current truth: a selected measurement franja can now be changed between `Width` and `Height` from the Fit right panel via `Tipo de franja` + `Cambiar tipo`.
- The change is semantic, not cosmetic: `ChangeMeasurementCorridorAxisHandler` updates the corridor axis, recalculates corridor band coordinates, recalculates each node `AxisCoordinate` for the new axis, and re-upserts affected `DimensionIntervalBinding` interval coordinates.
- This solves the common curation mistake where nodes were placed under a Width franja but the relation was actually Height, without forcing the operator to delete/recreate all nodes.
- Verification: Application curation tests passed 3/3, Infrastructure measurement persistence tests passed 3/3, and Desktop focused Fit/review tests passed 70/70.

## 2026-05-22 - Fit pinch groups are explicit and handles only show when usable
- Current truth: the green articulation-band rectangle is no longer painted in the preview. Selected franja guides, nodes, A/B interval lines, pinches, and dimensions still render.
- Compression handles now require a selected pinch group with at least one marker on the active axis. If a Height group has no Height markers, the Height handles stay hidden instead of appearing and doing nothing.
- The Fit right panel now navigates pinches by `Grupos de pinches`: select the group, create an automatic `Ajuste N` group, and see `Pinches de este grupo`. The old global `Pinche existente` dropdown was removed.
- `Quitar pinch` is disabled unless a specific marker is selected, so there is no grey/no-op remove action.
- Verification: RED tests covered missing handle gating, selected group marker list, no green band render call, and new XAML structure. Desktop test project passed 183/183 via `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --artifacts-path .testartifacts\dotnet-test-artifacts-pinch-ux-desktop-all`.

## 2026-05-22 - Worklog format preference
- User clarified that 14-day worklog summaries should be numbered from Day 1 to Day 14, with Day 1 starting today and Day 14 ending fourteen days back, instead of only using calendar-date headings.
- Output language for worklog titles/focus lines remains English.

## 2026-06-01 - Dimension text override prevents reactive number update
- Verified root cause: node-bound dimensions can recompute geometry and `MeasurementSourceUnits`, but visible text stays static when `RawTextOverride` is present and not exactly `<>`.
- Current behavior is conservative: literal CAD overrides are preserved to avoid corrupting labels like `VERIFY`/`EQ`/notes.
- Missing product case: mixed AutoCAD placeholder overrides such as `<> TO CL. OF EXH. VENT` should regenerate only the numeric token while preserving suffix text.
- Follow-up: add a failing test for placeholder-plus-suffix override, then implement token-aware dimension display text projection.
- See bug note: `Bugs/2026-06-01 - Dimension text override prevents reactive number update.md`.

## 2026-06-01 - Dimension placeholder text overrides fixed
- Fixed: node-bound native dimensions whose DXF text override contains the CAD measurement placeholder `<>` now recalculate the numeric value while preserving surrounding technical text.
- Example: `<> TO CL. OF EXH. VENT` now becomes the new measured value plus `TO CL. OF EXH. VENT` during reactive pinch preview or endpoint editing.
- Literal overrides without `<>` remain protected (`VERIFY`, `EQ`, `TYP.`, etc.) and are not overwritten.
- Implementation: shared `DimensionDisplayTextFormatter` is used by both `DimensionGeometryProjector` and `NativeDimensionEditor`, avoiding divergent Application/Desktop behavior.
- Verification: Application dimension reactive slice passed 21/21; Desktop native dimension slice passed 16/16. `git diff --check` exited 0, with only existing line-ending warnings.

## 2026-06-01 - Height preview handles select compatible group
- Fixed: changing the Fit toolbar axis to `Height` now selects a compatible Height pinch group when one exists, preferring a group that already has a Height marker.
- Root cause: preview handles require `SelectedPinchGroupId + SelectedPinchAxis` to point to a group with markers on that axis; previously the axis could switch to Height while the selected group stayed Width, so the renderer correctly hid handles due to no preview driver.
- Implementation: `FloorPlanReviewViewModel.OnSelectedPinchAxisChanged` now resolves a matching pinch group for the active axis.
- Verification: RED/green test added in `FloorPlanReviewViewModelTests`; focused Desktop slice passed 98/98. `git diff --check` exited 0 with only line-ending warnings.

## 2026-06-01 - Height compression handles made visually visible
- Verified second root cause after the selected-group fix: Height handles could be calculated but painted nearly black over the dark preview workspace, measuring only 1.16:1 contrast.
- Current truth: compression handles use selection green stroke plus translucent green fill, matching the inspector copy that says to drag the green handle.
- Renderer gating remains intact: handles are still shown only when a selected pinch group has a marker for the active axis and pinch placement is not armed.
- Verification: RED contrast test failed first, then `PreviewSemanticPaletteTests` passed 3/3 and the focused Desktop preview/ViewModel slice passed 101/101.

## 2026-06-01 - Height handle fix corrected after user verification
- Correction: the prior visual-contrast/color hypothesis was not the real symptom; it was reverted. User clarified Width/right-left handles still appeared while Height/top-bottom did not.
- Verified root cause: Fit had two axis states. `SelectedPinchAxis` drives preview handles, while `SelectedMeasurementCorridorAxis` only drove the franja editor. Changing the franja axis to Height could leave the preview axis on Width.
- Current truth: changing `SelectedMeasurementCorridorAxis` now syncs into `SelectedPinchAxis`, which then resolves a compatible Height pinch group when available.
- Important gating remains: top/bottom Height handles still require a selected Height pinch group with at least one Height pinch marker; the local app DB currently has a Height group with 0 markers and Width groups with existing markers.
- Verification: RED axis-sync test failed first; focused Desktop ViewModel/Preview slice passed 99/99 after the fix.

## 2026-06-01 - Height handles require Height pinches and hint now says so
- Verified from screenshots and local DB: selected `Ajuste 3` is a Height pinch group, but it currently has 0 pinch markers. Existing markers are on Width groups.
- Current truth: top/bottom Height handles are not drawn for a Height group with no Height pinches, because the renderer requires a selected group + matching marker before exposing a draggable preview handle.
- UX correction: the interaction hint no longer promises “drag top/bottom handle” when the selected group has no driver. It now tells the operator to mark at least one pinch for that axis first.
- Verification: RED hint test failed first; focused Desktop ViewModel/Preview slice passed 100/100 after the fix.

## 2026-06-01 - Cross-axis bound dimensions follow pinch preview
- Fixed: manual-verified dimensions bound to the opposite axis no longer stay behind during pinch preview.
- Same-axis behavior is unchanged: Width adjustments recalculate Width-bound dimension spans/text, and Height adjustments recalculate Height-bound dimension spans/text.
- New cross-axis behavior: when a Width adjustment moves geometry, Height-bound dimensions translate horizontally with their live nodes but keep their height value/text; when a Height adjustment moves geometry, Width-bound dimensions translate vertically but keep their width value/text.
- Implementation detail: `DimensionIntervalReactiveProjector` no longer returns opposite-axis bindings untouched. It resolves their live node deltas and delegates to `DimensionGeometryProjector.TranslateAssociatedDimensionFromAnchorDeltas`, which applies only the active-axis average translation to the entire authored CAD dimension primitive set.
- Verification: RED/green projector regression added; Application tests passed 83/83; focused Desktop preview slice passed 69/69. `git diff --check` exited 0 with only line-ending warnings.

## 2026-06-01 - Preview CAD grid density doubled
- The Floorplan Review preview CAD grid now targets 16 px minor-grid spacing instead of 32 px, giving roughly double the background grid density for finer visual alignment.
- The existing CAD palette and renderer remain unchanged; only `CadViewportContext` chooses a denser 1/2/5 world spacing.
- At the tested preview scale, minor grid spacing changes from 20 to 10 world units and major grid spacing from 100 to 50; snap tolerance remains unchanged.
- Verification: RED/green Desktop preview test added; focused preview/snap slice passed 64/64.

## 2026-06-01 - Fit franja nodes can be removed individually
- Current truth: the Fit right panel now exposes `Eliminar nodo` inside `Nodos de esta franja`, enabled only when a node in the selected franja is selected.
- Deleting a node is granular: it removes that `measurement_nodes` row and only `floorplan_dimension_interval_bindings` rows that reference the node as A/start or B/end.
- The franja/corridor remains, unrelated nodes remain, and unrelated dimension bindings remain.
- After refresh, the ViewModel keeps the selected franja when it still exists, clears the deleted node, and only clears A/B endpoint selection if the deleted node was one of those endpoints.
- Verification: Application curation slice passed 4/4; Desktop Fit ViewModel/layout slice passed 23/23; Infrastructure measurement persistence slice passed 4/4; `git diff --check` exited 0.

## 2026-06-01 - Selecting a bound dimension opens its Fit franja
- Current truth: selecting/touching a dimension with a saved `DimensionIntervalBinding` now selects the bound franja/corridor in the right Fit panel.
- The ViewModel restores the binding's corridor, start node, and end node, so `Grupos de A y B`, `Nodos de esta franja`, and the A/B selectors align with the selected cota.
- Unbound dimensions do not force a franja; they keep the existing manual-assignment flow using the currently selected franja.
- Verification: RED/green ViewModel regression added; MeasurementBinding ViewModel slice passed 19/19; DimensionEditing/FloorPlanReview ViewModel slice passed 54/54; `git diff --check` exited 0.

## 2026-06-01 - Width left-edge bound dimensions keep segment-local node identity
- Fixed: in Width mode, reducing from the left handle toward the right no longer lets bound cotas/nodes drift because of global path-ratio re-sampling.
- Root cause: `DimensionIntervalReactiveProjector` resolved saved nodes by `PositionRatio` against the compressed preview path. If earlier segments changed length, the same global ratio could point to the wrong live location.
- Current truth: the projector now receives source/original geometry plus preview geometry, resolves the node on the original segment/local ratio, then maps that same segment/local ratio onto the preview path. Global ratio remains only as fallback.
- `FloorPlanPreviewControl` passes authored `GeometryPaths` as source geometry for reactive pinch preview.
- Verification: RED/green regression added; DimensionIntervalReactiveProjector suite passed 14/14; Application tests passed 85/85; Desktop native preview slice passed 16/16 via temporary `OutDir` because the running desktop app locked normal output DLLs; `git diff --check` exited 0.

## 2026-06-03 - Opening published Seminole hides Fit data behind empty draft
- Verified against local `SEMINOLE2000` workspace DB: the published curation still has the Fit data (`11` pinch groups, `1` pinch marker, `93` measurement corridors/franjas, `187` nodes, `87` dimension interval bindings).
- Reopening created a new draft curation based on the published curation, but that draft has `0` pinch groups, `0` pinch markers, `0` franjas/nodes, and `0` dimension interval bindings.
- Root cause: `StartOrResumeCurationHandler` creates the new empty draft after publish, and `SqliteFloorPlanReviewSessionReader` reads Fit structures only from `ActiveCurationId` instead of inheriting/copying the published Fit state.
- Product impact: publish did not delete the data; the review/edit session is looking at an empty post-publish draft for Fit objects.
- Recommended MVP fix: copy Fit rows from published curation into the new draft when the draft is created, because current mutation handlers already assume rows belong to the active curation id.
- See bug note: `Bugs/2026-06-03 - Opening published Seminole hides Fit data behind empty draft.md`.

## 2026-06-03 - Review opens latest published curation by default
- Product decision accepted: when a floor plan has an active published curation, opening Review must show the latest published curation, not auto-create/show a new draft.
- Fix applied: `OpenFloorPlanReviewSessionHandler` now returns the active published session with `DraftCurationId = Guid.Empty` instead of calling `StartOrResumeCurationHandler`.
- Fix applied: `SqliteFloorPlanReviewSessionReader` now reports `Published` before `Curated Draft` when `active_published_curation_id` points to the selected version, and `GetCurationContext` prefers that published curation over any stale draft.
- Result: stale empty post-publish drafts no longer hide published pinch groups, pinches, franjas, nodes, or dimension interval bindings.
- Verification: Application tests passed 86/86; Infrastructure tests passed 77/77; `git diff --check` exited 0 with only line-ending warnings.
- See decision note: `Decisions/2026-06-03 - Review opens the latest published curation by default.md`.
- See bug note: `Bugs/2026-06-03 - Opening published Seminole hides Fit data behind empty draft.md`.

## 2026-06-03 - Published curation edit mode is explicit
- Current truth: Review still opens the latest active published curation by default and does not create a draft just from opening the plan.
- New UX: the header now exposes **Editar** when the session is published/read-only; `Publish Curation` is enabled only when an editable draft exists.
- Clicking **Editar** creates/resumes a draft from the active published curation and copies Fit-owned rows into it: pinch groups, pinch markers, measurement corridors/franjas, measurement nodes, and dimension interval bindings.
- During edit mode, refresh reads by draft curation id so Crear franja / pinches / nodes mutate the draft instead of snapping back to the published view.
- After publishing that draft, refresh sees `Published` and clears `DraftCurationId` back to `Guid.Empty`, returning the session to read-only published mode.
- Verification: Application tests passed 87/87; Infrastructure tests passed 78/78; Desktop tests passed 192/192 via temporary artifacts path; `git diff --check` exited 0 with only LFâ†’CRLF warnings.
- See decision note: `Decisions/2026-06-03 - Edit published curation creates an explicit draft copy.md`.
- See implementation note: `Implementation/2026-06-03 - Published curation edit flow creates copied draft.md`.

## 2026-06-03 - Editar published copy fixed for drafts with existing non-Fit data
- Verified against local SEMINOLE2000 DB: the published curation still had 11 pinch groups, 1 pinch marker, 93 franjas, 187 nodes, and 87 interval bindings; the edit draft had 0 Fit rows but 1 dimension override.
- Root cause fixed: `SqliteFloorPlanCurationDataCloneService` no longer lets unrelated/simple curation rows block copying Fit-owned rows into the edit draft.
- Transaction boundary fixed: `EditPublishedFloorPlanCurationHandler` now owns create/resume draft + clone + session read + commit, instead of delegating draft creation to `StartOrResumeCurationHandler` and leaving clone persistence ambiguous.
- Verification after fix: Application tests passed 87/87; Infrastructure tests passed 80/80; Desktop tests passed 192/192; `git diff --check` exited 0 with only LFâ†’CRLF warnings.
- See bug note: `Bugs/2026-06-03 - Editar published curation opened empty draft when draft had non-Fit data.md`.

## 2026-06-04 - Fit pinch groups can be deleted
- Current truth: the Fit right panel now exposes `Eliminar grupo de pinches` under `Grupos de pinches`, enabled when an editable draft group is selected.
- Deleting a group is structural: it removes all `pinch_markers` for that group first, then removes the `pinch_groups` row.
- The Application layer owns this in `RemovePinchGroupHandler`, with draft/ownership validation matching the franja-style safety model.
- The Desktop ViewModel stops pinch-placement mode, refreshes the session, and clears selected group/marker after deletion.
- Verification: Application handler, SQLite repository, Desktop ViewModel, layout, and DI focused tests passed. Desktop tests used isolated `--artifacts-path` because a running `FloorplanFit.Desktop` process locked normal output DLLs.
- See bug note: `Bugs/2026-06-04 - Pinch groups could not be deleted from Fit panel.md`.
- See implementation note: `Implementation/2026-06-04 - Fit pinch groups can be removed with their pinches.md`.

## 2026-06-04 - Publish no longer crashes when Fit pinches are missing
- Verified root cause: `PublishFloorPlanCurationHandler` correctly throws when a draft has zero `PinchMarker` rows, but Desktop let that exception escape through the Avalonia `async void` click handler, crashing the dispatcher/dotnet watch.
- Current truth: `CanPublishCuration` now requires both an editable draft and at least one pinch marker in the current session.
- If publish is attempted without pinches, Desktop shows `Agregá al menos un pinche antes de publicar.` instead of crashing.
- The Application publish guard remains intact; Desktop now mirrors/handles the validation at the UX boundary.
- Verification: Application publish handler tests passed 2/2; focused Desktop publish/edit/group-delete tests passed 4/4 with isolated `--artifacts-path`; `git diff --check` exited 0 with only line-ending warnings.
- See bug note: `Bugs/2026-06-04 - Publish without pinches crashed Desktop dispatcher.md`.
- See implementation note: `Implementation/2026-06-04 - Publish validation stays in Application but Desktop handles it.md`.

## 2026-06-04 - Publish button stays enabled for editable drafts
- Correction to the earlier publish-crash fix: `Publish Curation` is now enabled whenever Review is editing a draft (`DraftCurationId != Guid.Empty`), not only when the ViewModel projection currently shows pinch markers.
- Missing-pinch validation stays in `PublishFloorPlanCurationHandler`; Desktop catches that Application validation and shows `Agregá al menos un pinche antes de publicar.` instead of crashing.
- Root cause of the follow-up bug: gating the button on `PinchMarkers.Count` coupled the UX to a potentially stale session projection, so persisted pinches could exist while the button still looked disabled.
- Verification: RED/green Desktop publish tests passed 2/2; broader FloorPlan Review ViewModel slice passed 58/58; `git diff --check` exited 0 with only line-ending warnings.
- See bug note: `Bugs/2026-06-04 - Publish stayed disabled after pinches were placed.md`.
- See implementation note: `Implementation/2026-06-04 - Publish button stays enabled for editable drafts.md`.

## 2026-06-05 - Adjusted DXF exports go to Desktop exports
- Current truth: adjusted DXF exports now write to `C:\Users\lucas\OneDrive\Escritorio\exports`, not the Debug executable workspace.
- The internal workspace still owns DB/raw DXF storage; only `LibraryAdjustedDxfDirectory` is overridden for Desktop composition.
- Review export status now shows the full managed path so the operator can find the file.
- Cleanup done: old `.dxf` files were deleted from `src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\library\adjusted-dxf`; remaining count is 0.
## 2026-06-05 - Adjusted DXF export validity repaired
- Root cause verified: the adjusted DXF was not empty, but `ezdxf` rejected it because `Layout1` lost its required `BLOCK_RECORD` reference after IxMilia reserialized the file.
- Exporter fix: after `DxfFile.Save(...)`, `IxMiliaAdjustedDxfExporter` repairs layout block-record references, symbol-table record owners, block/entity owners, and modelspace entity owners.
- Important DXF rule captured: group code `330` is not always the owner; the repair only changes/inserts the owner before the first `100 AcDb...` subclass marker so hatch/internal references are not overwritten.
- Data cleanup: deleted the invalid `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted.dxf` and cleared `last_exported_at_utc` for 89 overrides in the active Seminole draft so the next export can regenerate the file.
- Safety: DB backup created at `src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\app.db.backup-before-adjusted-dxf-reexport-20260605-153651`.
## 2026-06-05 - Adjusted DXF export switched to surgical source-preserving patch
- Correction: the earlier "ownership handles repaired" fix was incomplete. AutoCAD still opened the adjusted DXF as an empty `Drawing1`, and `ezdxf.audit()` still reported `4607` fixes.
- Verified root cause: whole-file IxMilia reserialization damaged CAD metadata beyond layout owners: it dropped `ACDSDATA` and corrupted DIMSTYLE/style references while preserving enough syntax to look like a DXF.
- Current truth: adjusted export now preserves the original source DXF text/sections/handles and patches only the dirty native dimension entity plus its existing geometry-block primitives by source handle.
- Real export check: `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted-2.dxf` loads in `ezdxf`, keeps `ACDSDATA`, has `modelspace_count=3944`, and audits with `errors=0`, `fixes=0`.
- Supersedes: `Implementation/2026-06-05 - Adjusted DXF export repairs DXF ownership handles.md`.

## 2026-06-05 - Adjusted DXF duplicate dimension text fixed
- Follow-up visual QA: AutoCAD opened the surgical export, but some dimensions appeared duplicated.
- Root cause: exporter inserted DXF group code `1` text overrides into dimensions that did not have a source override, while also patching the dimension geometry block text. AutoCAD could render both.
- Current truth: `IxMiliaAdjustedDxfExporter` only updates group code `1` if the source DIMENSION record already had it; it never creates a new native text override for block-rendered dimensions.
- Patched current user file: `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted.dxf` now has 46 DIMENSION text overrides, matching the source DXF, and audits with `errors=0`, `fixes=0`.
- Note: `SEMINOLE2000-adjusted-2.dxf` remained locked by AutoCAD during cleanup and still has the old duplicated text override count.

## 2026-06-05 - Adjusted DXF preview/AutoCAD dimension mismatch fixed
- Follow-up visual QA: after duplicate text was fixed, AutoCAD still rendered some dimensions differently than Floorplan Fit preview, with rotated/displaced labels.
- Root cause: exporter patched both the anonymous dimension geometry block and native DIMENSION definition fields (`10/20/30`, `11/21/31`, `13/23/33`, `14/24/34`, `42`). AutoCAD can interpret/regenerate from native fields, diverging from our preview's style-preserving block primitives.
- Current truth: adjusted DXF export is WYSIWYG/block-authoritative. It leaves native DIMENSION definition fields unchanged and patches only existing geometry-block primitives plus existing text overrides.
- Patched current file: `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted.dxf` now has `native_definition_diffs=0`, `dimension_group1=46` matching source, and audits with `errors=0`, `fixes=0`.

## 2026-06-05 - Adjusted DXF source duplicate twins synchronized
- Follow-up visual QA: many dimensions still looked duplicated, but not all.
- Root cause verified: the source SEMINOLE2000 DXF already contains many exact-overlap duplicate DIMENSION pairs (for example `*D169` and `*D498`). They look like one cota in the original because the twins sit exactly on top of each other.
- Export bug: when only one twin was dirty/adjusted, the exporter moved that twin's geometry block and left the source twin at the original position, making the duplicate visible.
- Current truth: exporter detects source DIMENSION twins by stable native signature and patches duplicate geometry blocks together by ordinal primitive order.
- Patched current file: `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted.dxf` audits with `errors=0`, `fixes=0`, and has `remaining_changed_duplicate_text_position_mismatches=0`.

## 2026-06-05 - Adjusted DXF dimension duplication remains unresolved
- Correction: user visual QA still shows duplicated dimensions and some text orientation changes in AutoCAD, so the previous "fixed" DXF export notes are superseded.
- Cleanup done: failed exporter/test changes were reverted in `IxMiliaAdjustedDxfExporter.cs` and `IxMiliaAdjustedDxfExporterTests.cs`; those two paths currently have no diff.
- Export location remains: adjusted DXF output still targets `C:\Users\lucas\OneDrive\Escritorio\exports`.
- Stale files remain locked by AutoCAD/another process: `SEMINOLE2000-adjusted-2.dxf`, `SEMINOLE2000-adjusted-3.dxf`, and `SEMINOLE2000-adjusted-4.dxf`.
- Verified evidence remains useful but not conclusive: source `SEMINOLE2000.dxf` has 164 exact-overlap DIMENSION twin groups / 328 twinned DIMENSION entities.
- Next required step: debug one selected bad dimension/handle with controlled one-variable exports before implementing any new patch.
- See active bug note: `Bugs/2026-06-05 - Adjusted DXF dimension duplication unresolved after rollback.md`.
## 2026-06-05 - Adjusted DXF twin sync and native metadata preservation
- Current technical truth: adjusted DXF export now treats existing geometry blocks as the visual authority and preserves protected native DIMENSION metadata from the source after IxMilia serialization.
- Root cause 1 verified by RED: source SEMINOLE2000 has exact DIMENSION twins; adjusting only `*D169` left twin `*D498` at the old text/position, making a duplicate visible.
- Fix 1: exporter computes exact native DIMENSION twin groups and rewrites all twin geometry blocks together.
- Root cause 2 verified by RED: the exporter/IxMilia serialization introduced changed native DIMENSION metadata (`1`, missing `42`, and protected coordinate/rotation fields), which can make AutoCAD render/regenerate differently from the preview.
- Fix 2: after `DxfFile.Save`, exporter restores protected native DIMENSION group values from the source record by geometry block name; visual adjustments stay in the dimension geometry block primitives.
- Verification: `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaAdjustedDxfExporterTests"` passed 3/3; `git diff --check` exited 0 with LF-to-CRLF warnings only.
- Not yet visually verified: old `SEMINOLE2000-adjusted-2/3/4.dxf` files are still locked by AutoCAD/another process, so AutoCAD QA must use a fresh export after closing old files.
## 2026-06-05 - Adjusted DXF export no longer crashes when there are no dirty dimensions
- Crash verified from user log: Desktop click path let `InvalidOperationException: No dirty native dimensions are available to export.` escape through Avalonia `async void`, killing `dotnet watch` with code `-532462766`.
- Root cause: Application exported only `IsEdited && IsDirty` dimensions; after a previous export, edited dimensions could be clean, so the user could not generate a fresh DXF to validate exporter fixes.
- Product correction: Adjusted DXF export now exports all edited native dimensions (`IsEdited`), not only dirty ones. Export means “write current adjusted state”, not “write only never-exported deltas”.
- Desktop boundary fix: `FloorPlanReviewViewModel.ExportAdjustedDxfAsync` catches Application validation exceptions and shows `No hay cotas modificadas para exportar.` instead of crashing the Dispatcher.
- Verification: Application `ExportAdjustedDxfHandlerTests` passed 5/5; Desktop `DimensionEditingFloorPlanReviewViewModelTests` passed 6/6 using isolated artifacts because running `FloorplanFit.Desktop (9808)` locked normal output DLLs; `git diff --check` exited 0 with LF-to-CRLF warnings only.
## 2026-06-05 - Adjusted DXF now suppresses exact duplicate DIMENSION twins
- Correction to prior fix: synchronizing exact source DIMENSION twins was insufficient because the exported DXF still rendered both entities. If two identical cotas are one over the other, aligning them does not remove the visual duplication.
- Verified on fresh export `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted.dxf`: it still had 328 native DIMENSION entities and 164 exact duplicate signature groups.
- Exporter fix: after restoring protected native DIMENSION metadata, it deduplicates exact native DIMENSION records by stable signature and emits only the first entity from each duplicate group.
- Regression changed: `ExportAsync_suppresses_exact_source_duplicate_dimension_twins` now expects `*D169` to remain adjusted and `*D498` to be absent from extracted output.
- Verification: `IxMiliaAdjustedDxfExporterTests` passed 3/3; `git diff --check` exited 0 with LF-to-CRLF warnings only.
- Diagnostic file created for visual QA: `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted-dedup-diagnostic.dxf`, verified with 164 DIMENSION entities and 0 duplicate signature groups.
## 2026-06-05 - Adjusted DXF export is source-preserving again to avoid AutoCAD blank Drawing1
- User visual QA showed the deduplicated adjusted export opened with the old black-screen/blank-Drawing1 AutoCAD behavior.
- Verified root cause: fresh `SEMINOLE2000-adjusted.dxf` was still produced through whole-file IxMilia serialization; it lost `ACDSDATA` and `ezdxf.readfile` failed with `required BLOCK_RECORD #0 for layout 'Layout1' does not exist`.
- Exporter architecture correction: `IxMiliaAdjustedDxfExporter` no longer calls `DxfFile.Save` for adjusted exports. It reads the original DXF text as Latin-1 pairs, patches only geometry-block primitives for edited dimensions/twins, deduplicates exact native DIMENSION records, removes DIMASSOC objects owned by removed dimensions, removes dictionary entries that referenced those DIMASSOC objects, and writes the source-preserved DXF back out.
- Verification: RED proved `ACDSDATA` was missing before the fix; exporter tests now pass 4/4. The generated diagnostic `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted-source-preserving-diagnostic.dxf` has `ACDSDATA`, 164 DIMENSION records, 0 duplicate dimension signature groups, and `ezdxf.audit()` reports `errors=0`, `fixes=0`.
- Open this diagnostic/fresh source-preserved export in AutoCAD instead of the older `SEMINOLE2000-adjusted.dxf`, which still lacks `ACDSDATA` until regenerated after dotnet watch restarts.