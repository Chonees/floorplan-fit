---
type: bug
status: open
date: 2026-07-14
---

# Dominant outline independent-axis selection rejected valid frames

## Root cause

After the actual SEMINOLE DXF entity extraction policy completed, runtime reached the dominant structural outline selector. That selector chooses vertical and horizontal edge pairs independently, then validates four-corner support afterward. This ordering discards valid connected frames that rank lower on either independent axis.

## Evidence

Read-only source/DXF reconstruction found **365 canonical** and **1,033 electrical** connected horizontal/vertical frame candidates. Geometry is therefore present; the failure is in selection rather than missing geometry.

## Correct fix

Construct candidates only from edge pairs with connected four-corner support, then compare compatible frames using the existing uniform-scale and residual gates. Keep this generic: do **not** substitute a bounding-box fallback, relax tolerance, or introduce SEMINOLE-specific rules.

## Validation status

Runtime proof remains pending implementation and retry.
