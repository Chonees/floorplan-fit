# Buildable area is architect-provided site truth

Date: 2026-06-28
Type: Decision
Scope: Loop 2 Adjust-to-Site-Plan setup

## Decision
In Adjust-to-Site-Plan, the architect/user enters the buildable area dimensions from the site plan / AutoCAD truth. The app must not suggest or infer those buildable dimensions from the selected house footprint.

## Product Meaning
- The selected house/floor plan has its own exact architectural size, already known by the architect/drafting team from AutoCAD.
- The site plan/buildable envelope has its own exact allowed area, also known from AutoCAD/setbacks.
- The software's job is to compare those two exact truths coherently and report whether the house fits, what deficit exists, and what authorized adjustments can resolve it.

## UX Implication
The Adjust setup dialog should not frame `39` and `77.5` as suggested values for the buildable area just because SEMINOLE is that size. If values are shown, they must be clearly labeled as either:
- selected house footprint/reference, or
- buildable area entered by the architect.

## Non-goal
Do not present raw bounding boxes, structural-footprint heuristics, or approximate extraction diagnostics as construction-grade architectural truth.
