---
type: decision
date: 2026-07-21
status: implemented-uncompiled
replaces: "[[2026-07-15 - HousePlanSet export should return one folder]]"
---

# Publish JSON comparison evidence in the atomic plan-set package

## Decision

The user-facing `X-plan-set` folder publishes five artifact classes together:

- `X-floorplan.dxf` (`FloorPlan`)
- `X-electrical.dxf` (`ElectricalPlan`)
- `X-comparison.json` (`Comparison`)
- `manifest.json` (`Manifest`)
- `X-audit.txt` (`Audit`)

`X-comparison.json` is the existing final-output congruence evidence for the actual staged FloorPlan and ElectricalPlan outputs. It is not a CAD drawing and does not claim visual composition.

## Why

The package needs an explicit, inspectable comparison artifact without reviving `X-comparison.dxf`. The removed whole-file IxMilia merger downgraded AC1032 drawings into an AutoCAD-invalid AC1009 result, so a DXF comparison remains forbidden until a source-preserving AC1032 merger exists.

The existing congruence builder already owns the truthful native-coordinate Floor/Electrical verification. Reusing it avoids a second evidence model, a renderer, dependencies, and SEMINOLE-specific behavior.

## Atomicity and paths

Application records all five final package paths before verification. Infrastructure writes comparison JSON, human audit text, and package manifest into the same sibling staging directory that already contains both DXFs. Only after workspace verification/manifest publication succeeds does the existing atomic publisher rename that directory into place.

Any metadata staging, publication, or persistence failure removes/rolls back the package and preserves the canonical scratch. The package manifest lists its own final path plus final user artifact paths. Congruence calculations still read `VerificationPath`, but the user comparison copy reports the corresponding final `StoragePath`; transient staging names never survive in either user JSON document. Workspace audit output keeps its existing verification-path behavior.

## Static evidence

- Exact five-class contents and no `X-comparison.dxf`.
- Manifest artifact roles/paths all target the final package directory.
- Comparison stage is `final-output-congruence`, reports final Floor/Electrical paths, and retains nonzero/congruent metrics computed while only staged verification files exist.
- Audit text comes from `MultiSheetExportAuditDto.HumanSummary`.
- Package-artifact failure leaves no final/staging folder and does not delete owned scratch.
- No .NET/build/test/restore/watch/Desktop command ran; executable proof remains external.
