---
type: bug
date: 2026-06-10
topic: loop2-autofit-suggestion-ui-localization
---
# Auto-fit suggestion copy and cards were not localized

## Symptom
In Adjust to Site Plan, OpenAI-ranked fit options appeared with English copy such as `Reduce height by...`, large cards, clipped buttons, and a suggestion area that visually crowded the preview.

## Root cause
`AutoFitSuggestionOptionViewModel` displayed `AutoFitSuggestionPlan.Summary` and `FormatPlanDetails(...)` directly, so OpenAI/deterministic English explanation text leaked into the Desktop UI. The AXAML option strip also used oversized cards (`560px` wide / `128px` max height) inside a `280px` suggestion section, which made the panel look unprofessional and visually pushed the preview.

## Fix
- Desktop now treats OpenAI as a ranker only for visible UI copy: card title/details are generated locally from validated plan steps.
- Suggestion status, summary, candidate axes, apply labels, warnings, and plan details are Spanish/Rioplatense-friendly.
- Option cards are compact (`360x96`) inside a `220px` fixed suggestion section with a `104px` horizontal scroller.
- The OpenAI button now reads `Sugerir ajuste (OpenAI)`.

## Verification
- Focused Desktop test slice passed: `SuggestAutoFitPlanAsync|ApplyAutoFitPlan_reduces_only|ApplyAutoFitPlan_respects_split_option_groups_on_opposite|Site_plan_adjustment_window` = 10/10.
- Broader Desktop focused tests passed: `SitePlanAdjustmentPreviewProjectorTests|AppXamlInitializationTests` = 27/27.
- `git diff --check` exited 0 with LF-to-CRLF warnings only.

## Product impact
Loop 2 keeps the human-in-the-loop choice professional: OpenAI can rank options, but the operator sees concise Spanish cards that fit in the reserved panel and do not crowd the preview canvas.

## Follow-up: side-space layout and provider label
The first compact strip still stacked the cards below the status text, so on a 220px fixed panel the cards could be clipped vertically even though there was horizontal space available. The layout now uses a two-area header: status/details in a fixed left column and option cards in the right-side horizontal strip. Visible provider copy was shortened from `OpenAI` to `AI`.

## Follow-up verification
- RED/GREEN side-layout/copy test slice passed: 7/7 with isolated `--artifacts-path`.
- Broader Desktop focused tests passed: `SitePlanAdjustmentPreviewProjectorTests|AppXamlInitializationTests` = 27/27.
- `git diff --check` exited 0 with LF-to-CRLF warnings only.

## Follow-up: full preview plus sidebar
The side-strip still consumed vertical space above the preview and the old window header/title/subtitle/status reduced the drawing area. The Adjust to Site Plan window now removes those body header bindings and uses a two-column shell: preview fills the left side, while all controls, fit facts, AI action, and selectable options live in a right sidebar. Option cards are vertically scrollable and stretch to the sidebar width instead of being clipped in a fixed-height top strip.

## Follow-up verification: preview/sidebar shell
- RED/GREEN layout-only tests passed: 4/4.
- Broader Desktop focused tests passed: `SitePlanAdjustmentPreviewProjectorTests|AppXamlInitializationTests` = 27/27 using isolated `--artifacts-path`.
- `git diff --check` exited 0 with LF-to-CRLF warnings only.

## Follow-up: sidebar copy and clipping polish
After moving the controls into the right sidebar, the remaining context line still said `Centered in buildable area...`, the `Mover plano` button appeared clipped as `Mov`, and option card text could still feel squeezed. Root cause: `SitePlanAdjustmentPreviewProjector.Build(...)` still supplied the old English preview/status strings, the sidebar button reused the global `Button.tool` style with fixed `Width="48"`, and each option card kept a two-column layout where the `Aplicar` button consumed text width. The sidebar now uses Spanish context copy, a dedicated `Button.sidebar-action` style with `MinWidth="120"`, and option cards with a vertical three-row layout plus full-width Apply button.

## Follow-up verification: sidebar copy/clipping
- RED/GREEN sidebar polish tests passed: 3/3.
- Broader Desktop focused tests passed: `SitePlanAdjustmentPreviewProjectorTests|AppXamlInitializationTests` = 28/28 using isolated `--artifacts-path`.
- `git diff --check` exited 0 with LF-to-CRLF warnings only.
