---
type: inbox
project: floorplan-fit
date: 2026-06-08
topic_key: requests/auto-fit-suggestion-engine-named-pinch-groups
status: active
---

# Auto fit suggestion engine with named pinch groups request

## User intent

The user wants Loop 2 to understand whether a site-plan fit failure needs height adjustment, width adjustment, or both, then build a named, human-readable adjustment plan from the curated pinch groups.

## Desired behavior

- The system measures the current floor-plan overlay against the setback/buildable area.
- If the overlay does not fit, it computes deficits separately:
  - width deficit
  - height deficit
- It chooses compatible pinch groups by axis:
  - height deficit uses `Height` pinch groups
  - width deficit uses `Width` pinch groups
- It names the groups in the suggestion, using user-facing group names such as `H-01 Pasillo central` or `W-01 Garage`.
- It proposes a plan like:
  - reduce `H-01` by X inches
  - reduce `H-02` by Y inches
  - reduce `W-01` by Z inches
- The plan should be human-in-the-loop: user can review/apply rather than the app silently modifying the plan.
- Applying the plan should adjust the curated floor-plan geometry and any related manually verified dimensions/cotas.

## Important design constraint

The recommendation is to keep geometry/fitting deterministic and auditable. An LLM may help explain or label a plan, but should not be the core solver deciding geometry because the fit must be exact, repeatable, and testable.

## Current code facts already verified

- `PinchGroupDto` has `Name`, `AxisTag`, and `SortOrder`.
- `PinchMarkerDto` has `MaxTrimMm`; current capacity is marker-based.
- `ArticulationBandProjector` currently sums marker `MaxTrimMm` into a band capacity.
- `Adjust to Site Plan` currently projects/centers/moves the floor plan but does not yet run an auto-fit suggestion engine.
- Reactive dimension adjustment exists in the review/preview path for `ManualVerified` interval bindings, but it needs to be wired into Loop 2 auto-fit application.

## Open design issue

If the product intends "max 2 inches per group", capacity should be modeled or resolved at the group/band level. Today two markers with `2"` each may imply `4"` total capacity if summed.
