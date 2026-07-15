---
type: Decisions
date: 2026-07-13
replaces: []
replaced_by: null
---

# PointAIData DXFs are comparison authority

For original-vs-adjusted FloorPlan/ElectricalPlan comparisons, use the raw fixtures under `D:\PointAIData\PLANS` as the authority:

- FloorPlan: `D:\PointAIData\PLANS\originalFloorPlans\SEMINOLE2000.dxf`
- ElectricalPlan: `D:\PointAIData\PLANS\original electrical plans\ELECTRICAL PLAN SEMINOLE 2000.dxf`

The prior measurements used the app-managed import snapshots under `%LOCALAPPDATA%\FloorplanFit\workspace\library\raw-dxf`.

- The FloorPlan snapshot is byte-identical to the D: original: `3,092,023` bytes and SHA-256 `2C8ACFBE120668844D803A0ACA482B8B75C8AA72A9536D35EB86C62C9C80C8A4`.
- The Electrical snapshot and D: original both report `569,896` bytes, but the D: file was locked by another process, so an independent hash comparison is still pending.

Do not treat matching names or sizes as proof of byte identity.
