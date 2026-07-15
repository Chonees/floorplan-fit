# 2026-07-09 - Electrical patio top compression missed by identity registration

## Type
Bug / architecture discovery

## Current evidence
- Latest runtime export `c9f3a5381daa4b64ab64165de74a4577/manifest.json` is `ReadyForExport` and the Electrical sheet is `ProjectedAutomatically`.
- The canonical FloorPlan recipe has 8 operations.
- FloorPlan applied 8/8 operations.
- Electrical applied 6/8 operations.
- The two missed Electrical operations are top vertical compressions:
  - `VerticalCompression Top @1000.460736 delta 0.300`
  - `VerticalCompression Top @1001.669227 delta 0.300`
- Electrical reported `NoGeometryAffected` for both.

## Root cause hypothesis with proof
The Electrical patio geometry exists, but its coordinates are not registered into the FloorPlan coordinate system before deciding whether a canonical top pinch applies.

Proof from local DXF inspection:
- FloorPlan `SEMINOLE2000-12.dxf` bbox max Y is about `1064.675`; patio label is around `Y 982..993`.
- Electrical `ELECTRICAL PLAN SEMINOLE 2000-12.dxf` bbox max Y is about `961.577`; patio label is around `Y 918..929`.
- The canonical top pinch lines are at `Y 1000.460736` and `Y 1001.669227`.
- Current `sheet_registrations.transform_json` for the Electrical sheet is identity: `Scale=1`, `RotationDegrees=0`, `TranslateX=0`, `TranslateY=0`.

Therefore the exporter asks: “does Electrical geometry have floor-registered `Y >= 1000`?” Because registration is identity and Electrical max Y is only about `961.6`, the answer is no, even though the patio exists.

## Product meaning
This is not a Seminole-only issue. For N FloorPlans and N ElectricalPlans with the same drawing conventions but different sizes/room variations, dependent sheets need a real Electrical -> FloorPlan registration strategy. Whole-sheet identity is not enough.

## Next correct direction
- Do not hardcode `PATIO` or Seminole coordinates.
- Improve dependent-sheet registration so Electrical geometry is mapped into canonical FloorPlan coordinates using geometry anchors/zones.
- Add audit that distinguishes:
  - true empty area,
  - nearby electrical geometry outside the pinch,
  - registration mismatch suspected,
  - unsupported/layer-filtered geometry.

