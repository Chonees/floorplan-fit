# 2026-07-09 - Electrical edge-anchor fallback

## What changed
Electrical recipe-aware DXF export now computes registered dependent-sheet anchor bounds before replaying the canonical FloorPlan recipe.

## Why
The latest SEMINOLE proof showed Electrical patio/top geometry existed, but the canonical top pinch coordinates were outside the registered Electrical coordinate range because registration was identity/misaligned.

## Design
- Prefer generic wall/exterior/structural layer-family geometry as the dependent-sheet anchor bounds.
- Fall back to all modelspace coordinate geometry when those layers are unavailable.
- If a canonical edge pinch is outside those registered anchor bounds, clamp that operation to the dependent sheet's equivalent edge.
- Repeated out-of-bounds operations on the same edge are offset cumulatively so two top/right pinches stack instead of collapsing to a single delta.
- Keep the original canonical operation in audit output, and add a reason explaining the edge-anchor fallback.
- No SEMINOLE/PATIO coordinate/name hardcode.

## Verification
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `scripts/test-electrical-edge-anchor-contract.ps1`
- `git diff --check` for touched files

## Remaining runtime proof
- A fresh Desktop re-export is still required. The latest manifest on disk predates this code path, so it still reports the old 6/8 Electrical operation result.
- The verifier now rejects the old ambiguous Electrical `NoGeometryAffected` reason under `-RequireAutomatic`, forcing fresh edge-anchor observability or a specific empty-zone reason.
- Current live verifier failure against stale manifest `c9f3a538...`: `still uses the old ambiguous NoGeometryAffected reason`.

## Real-data dry run against stale SEMINOLE inputs
Without re-exporting, a read-only analysis of the latest manifest plus the original Electrical DXF predicts the new edge-anchor logic:

- Electrical registered anchor bounds from generic wall layers: `X 63.094..533.094`, `Y 31.576..961.577`.
- Top canonical operations are outside those bounds:
  - op 6: canonical `Y 1000.461` -> effective `Y 961.577`, affected bbox points `5`.
  - op 7: canonical `Y 1001.669` -> effective `Y 961.277`, affected bbox points `6`.
- Meaning: a fresh export should no longer report the two top operations as ambiguous `NoGeometryAffected`.
