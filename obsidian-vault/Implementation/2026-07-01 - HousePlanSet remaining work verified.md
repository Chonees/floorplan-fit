# HousePlanSet remaining work verified

## Current truth
The HousePlanSet backbone exists: import/classification handlers, explicit HousePlanSet and PlanSetVersion identity, canonical floor-plan adjustment persistence, dependent sheet registration/projection, package export/audit, and DataCollection quality reporting.

## Remaining senior gaps
- Manual confirmation is modeled but not actionable yet: registrations have `PendingConfirmation`/`Confirmed`, projections have `RequiresManualConfirmation`/`ReadyForExport`, but there is no Confirm/Reject use case and the PlanSet repositories expose only Add/Get/List, not Update.
- Dependent sheet onboarding is still mostly Application-layer: Desktop DI registers import/classification/registration/projection handlers, but there is no visible end-to-end UX for importing electrical/roof/facade sheets, classifying them, registering them, and projecting them after canonical adjustment.
- Registration inputs are still explicit transform values; anchor picking/title-block extraction/automatic geometric matching remain future work.
- Package export correctly auto-exports latest ready projections and audits manual/missing sheets, but the user-facing review/approval loop for those manual/missing sheets is still pending.
- Projection rules exist per sheet kind, but roof overhang and facade/elevation scaling need validation with real dependent sheets before adding more geometry-specific code.

## Evidence checked
- `src/FloorplanFit.Application/PlanSets/*` contains modules for Adjustment, Classification, DataCollection, Export, ExportAudit, Import, Library, Projection, and Registration.
- `ISheetRegistrationRepository` has `AddAsync` and `GetByIdAsync`; `ISheetAdjustmentProjectionRepository` has `AddAsync`, `GetByIdAsync`, and list-by-plan-set/canonical-adjustment only.
- No `ConfirmSheet...`, `ApproveSheet...`, or `RejectSheet...` Application use case exists in the PlanSets module.
- Desktop references package export/audit output, but dependent sheet import/registration/projection references appear only in DI/tests, not in visible ViewModel workflow.

## Verification
Static inspection only. No `dotnet test` and no `dotnet build` were run due repository rule.