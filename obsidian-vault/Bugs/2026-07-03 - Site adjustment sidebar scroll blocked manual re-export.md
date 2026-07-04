# 2026-07-03 - Site adjustment sidebar scroll blocked manual re-export

## Tipo
Bugfix

## Síntoma
After SEMINOLE TEST9 export, the right sidebar in `SitePlanAdjustmentWindow` did not expose enough scroll to reach `Confirmar manuales + re-exportar`. That made the user unable to confirm the ElectricalPlan projection and re-export the package.

The user also expected a `TEST9-plan-set` folder with the electrical DXF immediately after `Exportar DXF`, but the manifest correctly showed:

- `ElectricalPlan: RequiresManualConfirmation`
- `Warning: Canonical compression steps require electrical review before export.`

## Root cause
The sidebar used a fixed Grid layout:

`RowDefinitions="Auto,Auto,Auto,*"`

Only the auto-fit options list had its own `ScrollViewer`. Long status/audit text expanded the upper `Auto` rows, pushed action buttons down, and the overall sidebar had no scroll surface.

## Fix
`SitePlanAdjustmentWindow.axaml` now wraps the whole sidebar content in `AdjustmentSidebarScroller`, a vertical `ScrollViewer`, and uses a simple vertical `StackPanel`. This makes status text, export buttons, manual confirmation, and fit options reachable together.

## Test/guard
`AppXamlInitializationTests.Site_plan_adjustment_window_keeps_the_sidebar_scrollable` now asserts:

- `AdjustmentSidebarScroller` exists;
- vertical scroll is auto;
- horizontal scroll is disabled;
- the old fixed `RowDefinitions="Auto,Auto,Auto,*"` is gone.

## Product behavior clarified
First export writes the canonical FloorPlan and manifest/report. If dependent ElectricalPlan has local compression, it remains `RequiresManualConfirmation` and no electrical DXF is written to the selected package folder yet. After pressing `Confirmar manuales + re-exportar`, the package exporter can write the projected ElectricalPlan into the plan-set folder.
