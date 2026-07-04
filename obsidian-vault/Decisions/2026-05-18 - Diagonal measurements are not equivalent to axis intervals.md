> Superseded on 2026-05-20. Replaced by: $replacement. The active dimension interval rebuild path now uses the raw A/B node line; corridor/pinch band overlap remains axis-tagged.\n\n# 2026-05-18 - Diagonal measurements are not equivalent to axis intervals

## What
A diagonal A->B measurement is not semantically equivalent to a horizontal or vertical interval in the pinch/corridor model.

## Why
Pinches and articulation bands are axis-aligned (`Width` / `Height`). A true diagonal length changes according to both axis components and therefore depends on angle. Treating it like a simple width/height interval would conflate two different measurement semantics.

## Product implication
For the current corridor model, the robust first-class concept is an axis interval (horizontal or vertical). If diagonal measurements are ever supported, they need explicit angle-aware semantics rather than being treated as the same thing as width/height spans.

