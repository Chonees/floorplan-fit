---
type: bug
date: 2026-06-10
topic: loop2-autofit-fractional-preview-stability
---
# Auto-fit fractional reductions were visually hidden and suggestions pushed preview

## Symptom
When the required fit was fractional, such as `0.5"`, the geometry could be reduced correctly but the related dimension still displayed the old rounded whole-inch text. That made the preview look like it had merely shifted or scaled without a real dimension update.

A second visual issue came from the suggestion panel: the fit-options area only had `MaxHeight`, so when plan cards appeared it could expand and push the preview canvas down, creating a false visual impression of floor-plan movement.

## Root cause
- `DimensionDisplayTextFormatter.FormatArchitecturalInches(...)` rounded all architectural inch measurements to whole inches.
- `SitePlanAdjustmentWindow.axaml` bounded the suggestion area with `MaxHeight="280"` but did not reserve that height before options appeared.

## Fix
- Architectural display now formats clean fractional inch measurements to the nearest 1/16" and simplifies the fraction, so `123.5"` renders as `10'-3 1/2"`.
- Noisy/non-clean fractional values still round to whole inches to avoid visual clutter on diagonal/irrational measurements.
- The Loop 2 suggestion panel now reserves `Height="280"` as well as `MaxHeight="280"`, so displaying plan cards does not push the preview canvas.

## Verification
- RED/GREEN Application tests prove `123.5"` and placeholder text render as `10'-3 1/2"`.
- RED/GREEN Desktop test proves a 0.5" auto-fit reduction updates the bound dimension from `10'-4"` to `10'-3 1/2"` and marks it changed.
- RED/GREEN XAML test proves the suggestion panel reserves fixed height before options appear.
- Focused verification passed: Application Review tests 40/40; Desktop SitePlanAdjustment/XAML/Preview tests 81/81; `git diff --check` exited 0 with LF-to-CRLF warnings only.

## Product impact
Loop 2 fit plans no longer rely on visual illusions. The canvas remains stable when suggestions appear, and exact fractional reductions become visible in the cota text instead of being hidden by whole-inch rounding.
