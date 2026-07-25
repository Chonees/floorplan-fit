---
type: decision
status: proposed
date: 2026-07-20
project: FloorplanFit
area: Loop 1 site fitting and Loop 2 dependent-plan reconciliation
replaces: "[[2026-07-19 - Adopt CAD-style pinch stretch actions]]"
---

# Replace pinches with parametric adaptation profiles

## Runtime truth

Both pinch models are rejected as the primary house-adaptation abstraction:

- V1 interpreted a marker as a global coordinate threshold and changed unrelated geometry.
- V2 tried to infer the intended closing component from raw DXF connectivity, but live Width/Height behavior remained unacceptable.
- A point or pair of points does not encode enough architectural intent: it does not identify the design variable, fixed anchor, affected region, hosted objects, protected constraints, or dependent-discipline reconciliation policy.

No production code was changed as part of this research. This note records the recommended replacement pending explicit acceptance and design.

## Research-backed recommendation

Use a **Parametric Adaptation Profile** curated once per canonical house instead of more pinch heuristics.

Each adjustable design variable must declare:

- architectural name, such as `PatioDepth`, `PorchDepth`, `LivingBayWidth`, or `GarageStorageWidth`;
- axis, current value, permitted minimum and maximum, and preferred reduction order;
- one fixed anchor and one moving boundary;
- one or more explicit crossing regions identifying which endpoints stretch and which complete objects move rigidly;
- hosted elements that must move or rehost with their wall;
- protected constraints that must remain unchanged;
- discipline-specific reconciliation rules and manual-review triggers.

The site-fit flow should:

1. Build the legal/site **Buildable Envelope** from the surveyed lot, zoning district, setbacks, easements, access, drainage, parking, and applicable overlays.
2. Try rigid placement alternatives first: translate, rotate, mirror, and approved orientation variants.
3. Calculate the exact unresolved width/depth deficit.
4. Allocate that deficit only among approved design variables without passing their limits or violating constraints.
5. Emit one semantic `CanonicalBuildingChangeSet`, then let FloorPlan, ElectricalPlan, RoofPlan, and Facade adapters reconcile their own entities.
6. Fail closed when the deficit cannot be resolved safely; do not scale the entire house or guess from raw line topology.

## Why this matches professional CAD/BIM practice

- El Paso standards depend on the parcel's zoning/use; setbacks are not one universal rectangle. The City also identifies topography, access, drainage, circulation, orientation, and spacing as site-design concerns.
- AutoCAD `STRETCH` uses crossing windows or polygons and moves only selected endpoints/vertices; fully enclosed objects move rigidly. The **selection region**, not a point, carries the edit intent.
- AutoCAD/Revit parametric constraints preserve geometric and dimensional relationships while design values change.
- Referenced drawings keep disciplines coordinated with the latest architectural base, but Autodesk explicitly warns that blindly stretching MEP geometry can break connectivity. Electrical devices and circuits therefore need rehost/reroute semantics, not copied vertex deformation.

## Minimum viable implementation boundary

Start with axis-aligned rectangular adaptation regions and human-authored variables. Do **not** begin with a generic whole-building topology solver or arbitrary polygons. Layer conventions may propose candidates, but a curator must confirm design intent.

## Automation boundary

Automation means **commission once per canonical house, fit automatically on every site**. The end user must never perform CAD `STRETCH` operations.

During one-time house commissioning, the system extracts candidates from known layers and geometry, then a curator confirms a small control model: adjustable variables, fixed anchors, affected entity sets, wall/opening hosts, limits, priorities, and invariants. That profile is compiled once. At site-fit runtime the engine only calculates exact width/depth deficits, allocates them among approved variables, and replays deterministic precompiled effects; it does not rediscover topology.

For the minimum FloorPlan model, preserve the source DXF and add only a semantic control overlay:

- wall runs and exterior footprint;
- openings hosted to walls with immutable width and hinge/orientation metadata;
- room/bay variables with current/minimum values;
- per-variable entity roles: stretch endpoint, rigid translate, rehost/regenerate, or protected;
- postconditions for footprint delta, wall thickness, opening sizes, collisions, and untouched geometry.

The first allocator should be deterministic priority/capacity filling. Add a general optimizer only if real profiles contain interacting variables that this cannot solve.

### Role partition inside a commissioned profile

- `Stretch` is a geometric role. It must bind an exact source entity, geometry path, segment and selected vertices; incomplete bindings fail closed.
- `Fixed` and `RigidMove` may also describe source-only annotations. In that case they retain a stable source entity reference but deliberately have no geometry path, segment or vertex indices.
- Source-only annotations are excluded from geometric preview replay and remain available to the downstream DXF reconciliation adapter. They are not malformed stretches and must not make an otherwise valid commissioned profile fail readiness.
- A source-only role carrying segment or vertex metadata is contradictory and therefore invalid.

## Electrical consequence

Treat an Electrical drawing as `ArchitecturalBase + ElectricalOverlay`, not as a second independent house geometry that must be deformed forever.

- Use its architectural copy only for one-time registration and host discovery.
- Export the adjusted canonical FloorPlan architecture as the authoritative base.
- Project only Electrical overlay entities: wall-hosted outlets/switches follow canonical walls; room-hosted lights follow room anchors; immutable blocks keep their size/rotation; annotations move or regenerate by host.
- Persist circuit connectivity separately from wire curves. After device anchors move, regenerate safe wire graphics from the same connections; unsupported routes fail closed to explicit manual review rather than stretching arcs blindly.

This makes final Floor/Electrical structural congruence true by construction instead of an after-the-fact normalization target.

## Product experience

Keep two clearly separated experiences:

1. **House commissioning (advanced, once per model):** import/curate FloorPlan, auto-detect walls/openings/rooms, review suggested adjustable variables and protected cores, register Electrical by visual overlay, validate, then publish the HousePlanSet as `Auto-fit ready`.
2. **Site fitting (daily operator flow):** select an `Auto-fit ready` house, load a Site Plan, let the system derive the buildable envelope and exact deficit, review one recommended safe scenario (optionally a small number of approved alternatives), compare Floor/Electrical before and after, then confirm and export.

The daily UI must expose architectural outcomes, not CAD mechanics. Show named reductions, preserved elements, rehosted Electrical devices, unresolved/manual items, and a color overlay: unchanged, resized, rigidly moved, rehosted, or blocked. Do not expose pinch markers, entity IDs, raw vertices, crossing windows, or STRETCH controls.

If no safe profile solution exists, block export with a concise explanation and approved next actions; never silently deform or present an unsafe result as automatic.

Recommended modules:

- `SiteFeasibility`: parcel constraints, buildable envelope, rigid placement, deficit.
- `HouseAdaptation`: profiles, variables, constraints, allocation, canonical change set.
- `FloorPlanProjection`: authoritative architectural application.
- `DisciplineReconciliation`: Electrical/Roof/Facade adapters.
- `ValidationAndAudit`: before/after dimensions, invariant checks, unresolved/manual items, DXF safety.

## Sources

- City of El Paso Planning & Inspections FAQ: https://www.elpasotexas.gov/planning-and-inspections/faqs/
- El Paso Chapter 20.12 density and dimensional standards: https://library.municode.com/TX/El%20Paso/codes/code_of_ordinances?nodeId=TIT20ZO_CH20.12DEDIST
- City adopted codes: https://www.elpasotexas.gov/assets/Documents/CoEP/Planning-and-Inspections/Applications/Building-Permit-Applications/Adopted-Codes-and-Amendments.pdf
- Autodesk AutoCAD STRETCH: https://help.autodesk.com/cloudhelp/2021/ENU/AutoCAD-Core/files/GUID-F000A502-D39E-4D31-A8E2-4A626473FB72.htm
- Autodesk parametric drawing and constraints: https://help.autodesk.com/cloudhelp/2023/ENU/AutoCAD-LT/files/GUID-899E008D-B422-4DF2-AC8D-1A4F5701ED4E.htm
- Autodesk Xrefs: https://help.autodesk.com/cloudhelp/2027/ENG/AutoCAD-Core/files/GUID-A987D2FF-45BD-474E-99C1-E6316A42F667.htm
- Autodesk AutoCAD MEP STRETCH warning: https://help.autodesk.com/cloudhelp/2025/ENU/AutoCAD-MEP/files/GUID-0E5BEC88-F60F-4FD2-9C43-9B31516744F0.htm
