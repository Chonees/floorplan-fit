---
type: bug
status: fixed-statically
date: 2026-07-21
project: FloorplanFit
area: Loop 2 Electrical projection
---

# Electrical overlay lacks host and wire reconciliation

## Current evidence

`ProjectedPlanSheetDxfExporter.ComposeCanonicalArchitectureWithElectricalOverlay` correctly replaces the duplicate Electrical architectural background with the adjusted canonical FloorPlan, but `SelectElectricalOverlayRecords` currently imports supported records from `ELECTRICAL*` layers as raw overlay records.

There is no current production evidence that devices follow persisted wall/room hosts, that supported wires regenerate from connectivity, or that an unknown route is rejected specifically because its connectivity is unsupported. Entity-type support and CAD-resource closure are not equivalent to discipline reconciliation.

## Required closure

- Persist or consume the smallest already-available device host/connectivity evidence.
- Move supported devices through that evidence without changing count or size.
- Regenerate only routes with known connectivity.
- Fail closed with an actionable reason for unsupported routes.
- Cover the final composed DXF, not only an intermediate projection.

This is a P0 acceptance gap in [[2026-07-20 - Finite goal for automatic site fitting UX]], not a request for generic BIM.

## Resolution — 2026-07-21 (static)

- Consumption seam: `ProjectedPlanSheetExportRecipe.OverlayReconciliation` with `ElectricalDeviceHostBinding` (exactly one `Wall`|`Room` host per supported `INSERT`, `HostSourceEntityRef`, precompiled delta validated against the recipe axis) and `ElectricalWireRouteBinding` (LINE carrier + both endpoints bound to commissioned devices). No new table, no proximity inference.
- `ProjectedPlanSheetDxfExporter.ReconcileElectricalOverlayDiscipline` triggers only for locally-deforming recipes: devices move by their host's evidence delta (never by cut-line comparison), supported wires regenerate from final device endpoints preserving layer/payload, and every unsupported case — unbound device, duplicate/stale binding, non-LINE route, undeclared carrier, endpoint without binding, axis-mismatched delta — rejects as `UnsupportedWireRoute`/binding reason before any output file exists. Affine-only recipes keep the proven rigid whole-sheet path.
- 12 focused contracts on the final composed DXF cover movement-by-evidence, fixed devices crossing cuts, regeneration, fixed routes, and every rejection branch.

## Remaining producer seam

`ExportProjectedPlanSheetHandler` still builds recipes with `OverlayReconciliation = null`: one-time commissioning does not yet author device-host or wire-route evidence. Until it does, every locally-deforming commissioned Electrical export fails closed atomically (no package, exact reason) — the designed safety behavior. Authoring that evidence during commissioning is the next slice; room-hosted devices additionally await a real room-anchor producer.
