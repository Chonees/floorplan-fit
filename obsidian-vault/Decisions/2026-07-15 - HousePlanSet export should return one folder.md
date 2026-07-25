---
type: decision
date: 2026-07-15
status: superseded
replaced_by: "[[2026-07-21 - Publish JSON comparison evidence in the atomic plan-set package]]"
---

# HousePlanSet export should return one folder

The one-folder and no-comparison-DXF decisions remain valid. The temporary decision to defer every comparison artifact is superseded by a truthful non-DXF JSON comparison derived from final-output congruence evidence.

The final user-facing export should be a single `X-plan-set` folder containing named technical outputs such as `X-floorplan.dxf` and `X-electrical.dxf`.

Implemented without compilation on 2026-07-15: the initial canonical adjustment first persists the actual scratch `X.dxf`. After atomic package publication, the same audit unit-of-work transaction updates that adjustment to final `X-plan-set/X-floorplan.dxf` together with export/audit persistence. Any post-publication update/add/commit failure now explicitly rolls back both workspace publication and the live unit-of-work transaction before the atomic package rollback completes, leaving the committed scratch path intact. Only after complete success may the explicitly owned scratch file be deleted. Canonical verification still reads staging, manifest/storage artifacts use final paths, and manual re-export preserves its package-owned source while targeting sibling `X-confirmed-plan-set`.

`X-comparison.dxf` remains disabled. The previous whole-file IxMilia composition produced an AutoCAD-invalid AC1009 file. Re-enable only with a source-preserving AC1032 merger validated by AutoCAD.

User confirmed on 2026-07-15 that the comparison artifact is deferred for now; the accepted current output is the folder containing only the valid FloorPlan and ElectricalPlan DXFs.
