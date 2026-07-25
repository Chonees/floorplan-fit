---
type: implementation
status: active
date: 2026-07-20
project: FloorplanFit
area: Loop 2 Electrical export composition
implements: "[[2026-07-20 - Replace pinches with parametric adaptation profiles]]"
---

# Electrical export still uses dependent source as full document

## Verified current behavior

`ExportProjectedPlanSheetHandler` calls `IProjectedPlanSheetExporter` with the dependent Electrical source path. `ProjectedPlanSheetDxfExporter` clones and transforms that complete document. The adjusted canonical FloorPlan export is not an input to the recipe, so current Electrical output is still a deformed copy of the imported Electrical sheet rather than `adjusted canonical ArchitecturalBase + ElectricalOverlay`.

Layer matching for `WALL`, `EXTERIOR`, or `STRUCT` is currently registration evidence only. It does not control output ownership; therefore `ELECTRICAL WALLS`, devices, wires, dimensions, and dependent title content can all remain in the exported Electrical document.

## Smallest safe seam

1. Add the canonical Floor export path to `ProjectedPlanSheetExportRecipe`.
2. Preserve the existing dependent-sheet projection as an intermediate overlay projection because it already owns registration and action replay.
3. Compose final output after projection:
   - base: all entities/resources from adjusted canonical Floor export;
   - overlay: only explicitly supported Electrical entities plus required layer/block dependency closure;
   - exclude dependent architectural background and dependent title/sheet content.
4. Fail closed for unknown coordinate-bearing overlay entities, unsupported wires/bulges, missing or external block definitions, incompatible resource collisions, and ambiguous background/overlay classification.

The canonical Floor exporter itself should remain untouched.

## First RED contract

`ExportWithAuditAsync_uses_adjusted_canonical_floor_as_base_and_adds_only_electrical_overlay` must prove that the canonical adjusted wall and canonical title survive, `ELECTRICAL WALLS` does not, an Electrical device is projected to the adjusted position, and the dependent title block is absent.

## Collision warning

`ExportProjectedPlanSheetHandler.cs`, `ProjectedPlanSheetDxfExporter.cs`, and its tests already contain staged prior work. New changes must be surgical and preserve those edits.

## 2026-07-20 implementation progress

- `ProjectedPlanSheetExportRecipe` now carries the adjusted canonical Floor export path.
- The Application handler now builds the composition recipe even when Electrical has zero compression operations; composition is an output-ownership requirement, not a compression special case.
- Infrastructure currently fails closed before creating an output directory/file when canonical-base composition is requested.
- The first composition contract is RED and specifies canonical wall/title preservation, dependent wall/title exclusion, projected Electrical device retention, resource closure, and canonical source immutability.
- The safe next step is **not** a raw `ENTITIES` splice. DXF composition must preserve/remap owners, handles, `BLOCK_RECORD`, nested blocks, layers/styles/linetypes, collisions, and `$HANDSEED`.
- Reimplementing that graph logic locally would duplicate private machinery already present in `IxMiliaAdjustedSitePlanExporter`; the next dependency-ready slice is to extract/wrap one shared tested DXF composition primitive and reuse it from both exporters.

## 2026-07-21 bounded canonical composition

- The composer now uses the adjusted canonical FloorPlan document as the architectural base and imports only supported entities on `ELECTRICAL*` layers, excluding `ELECTRICAL WALLS`.
- Imported blocks, owner chains, handles, `ATTRIB`/`SEQEND`, HATCH associations and standard source-database `ACAD_REACTORS`/`ACAD_DIMASSOC` metadata are handled explicitly; unknown references fail closed before publication.
- A static audit of the actual SEMINOLE sources found 907 overlay records and seven required block definitions. Every required LTYPE, DIMSTYLE, APPID and LAYER already exists in the canonical FloorPlan.
- The only real missing resource is text style `ROMANS`. The bounded fix imports a referenced missing STYLE record from the Electrical source into the canonical STYLE table; it does not introduce a generic symbol-table importer.
- Package publication is atomic: an automatic Electrical failure cannot leave a Floor-only HousePlanSet.
- Runtime/AutoCAD validity remains unclaimed until the external build/test/export sequence is run.
