# 2026-06-09 - Auto-fit width deficit used rendered outliers

## Type
Bug

## Symptom
In a `DEFICIT TOTAL - alto menos 2 inches` case, the UI showed valid Height candidates but also reported a large Width deficit (`77.459"`) with zero Width capacity. The user correctly pointed out that this case should not be missing width.

## Root cause
The preview projection centers the floor plan using structural wall-candidate placement geometry, but the auto-fit fact builder was called with all projected floor-plan geometry paths.

That meant rendered non-structural geometry such as fixtures/details/outliers could expand the measured bounds and create a false Width deficit.

## Fix
- Reused the wall-candidate placement geometry ids after projection.
- Added `ResolveAutoFitGeometryPaths(...)` so the fit diagnostic uses structural placement geometry, matching the centering basis.
- `AutoFitSuggestionFactBuilder.Build(...)` now receives that structural geometry subset from `SitePlanAdjustmentPreviewProjector.Build(...)`.
- Corrected stale XAML copy from Claude to OpenAI.

## Verification
- RED: `Auto_fit_facts_use_structural_placement_geometry_not_fixture_outliers` failed because the resolver did not exist / fit facts had no structural subset.
- GREEN: the test now proves structural width stays `0"` while height deficit remains `2"` even when a fixture outlier extends width by `77.459"`.
- Focused tests passed:
  - Application auto-fit: 6/6
  - Desktop site-plan/service/XAML: 17/17

## Product rule
For Loop 2 fit diagnostics, the measured footprint must use the published structural placement envelope, not every rendered CAD artifact. Rendered artifacts still display, but they must not create false fit deficits.
