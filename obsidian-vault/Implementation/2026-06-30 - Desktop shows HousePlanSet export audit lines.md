# Desktop shows HousePlanSet export audit lines

## What changed
- Added `SitePlanAdjustmentViewModel.PlanSetExportAuditLines` as the minimal UI-facing read model for the latest HousePlanSet package audit.
- After package export, the view model now formats each exported/audited sheet with sheet kind, status, confidence, warning, and storage path when present.
- `SitePlanAdjustmentWindow.axaml` shows those audit lines in the existing Adjust-to-Site-Plan sidebar.

## Why
The success criterion is not only exporting a package; the user must know which sheets were projected automatically, which require manual confirmation/missing projection, and with what confidence. Keeping it in `LastPlanSetExportAudit` only was too hidden.

## Boundary
- No new modal or full audit screen yet.
- No new package/audit service.
- No per-sheet fit engine.
- This is a minimal visible audit readout; a richer review UI can come later if real usage needs it.

## Verification
- Test-first RED: Desktop test now expects ElectricalPlan `ProjectedAutomatically` with `94%` confidence and RoofPlan `MissingProjection` in `PlanSetExportAuditLines`.
- GREEN: `SitePlanAdjustmentViewModel` populates the lines and XAML binds them in the sidebar.
- `git diff --check` passed for touched files.
- No `dotnet test` and no `dotnet build` were run due repository rule.