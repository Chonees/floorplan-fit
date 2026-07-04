---
type: bug
date: 2026-06-09
topic: loop2-option-cards-not-visible
replaced_by: [[Implementation/2026-06-09 - Selectable auto-fit plan options]]
---
# Auto-fit option cards did not render as clickable options

## Symptom
Adjust to Site Plan showed `OpenAI ranked deterministic fit options` and `3 fit options available`, but no clickable option cards appeared below the summary.

## Root cause
The ViewModel state was correct; the rendering problem was in the XAML item template. The option card template used a parent `DataContext.ApplyAutoFitPlanCommand` `RelativeSource` binding with compiled binding disabled. This deviated from the existing repo pattern, where buttons inside `ItemsControl` templates use code-behind click handlers.

## Fix
Changed the Apply button inside the auto-fit option template to use `Click="ApplyAutoFitOptionButton_OnClick"` and added a code-behind handler that reads the button's option DataContext and calls `SitePlanAdjustmentViewModel.ApplyAutoFitPlan(option)`.

## Verification
- RED: `AppXamlInitializationTests.Site_plan_adjustment_window_reuses_preview_ux_without_fit_tools` failed until the option template used the click handler and removed the parent command binding.
- GREEN: same test passed.
- Focused Desktop SitePlanAdjustment/XAML/registration tests passed 18/18.
- Application auto-fit tests passed 8/8.
- Infrastructure OpenAI/Claude tests passed 6/6.
- `git diff --check` passed with LF-to-CRLF warnings only.
