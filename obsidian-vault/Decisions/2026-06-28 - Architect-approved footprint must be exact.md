# Architect-approved footprint must be exact

Date: 2026-06-28
Type: Decision
Scope: Loop 2 Adjust-to-Site-Plan sizing UX

## Decision
For user-facing Adjust-to-Site-Plan setup, the important number is the architect-approved official plan footprint, e.g. "SEMINOLE with patio is X by Y", not a raw extracted bounding box presented as if it were the construction truth.

## Rationale
Architectural drawings are used for construction and PDF/AutoCAD review; the app must not imply approximate or heuristic sizing is acceptable for construction-grade decisions.

## Current Gap
The app can accept exact manual buildable dimensions, and SEMINOLE's verified with-patio fit size is known (`39'0" x 77'6"`), but the setup dialog does not yet display/prefill an explicit architect-approved selected-plan footprint as a first-class source of truth.

## Implication
Next UX improvement should show the selected floor plan's official/curated footprint in the Adjust setup dialog, with exact feet/inches display and clear override semantics.
