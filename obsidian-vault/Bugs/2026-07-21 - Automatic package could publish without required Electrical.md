---
type: bug
status: fixed-statically
date: 2026-07-21
project: FloorplanFit
area: Loop 2 HousePlanSet export
---

# Automatic package could publish without required Electrical

## Root cause

`ExportMultiSheetPlanSetPackageHandler` caught `ProjectedPlanSheetManualReviewRequiredException` for every dependent sheet. In automatic discovery that converted a required Electrical failure into an audit entry and allowed a Floor-only directory to be published.

## Fix

> Superseded by [[#Reopened gap]]: the exception path is atomic, but automatic discovery filters out a latest non-ready Electrical projection before reaching that path.

Auto-discovered `ElectricalPlan` is now required and atomic: its manual-review exception propagates through `AtomicDirectoryPublisher`, which prevents final publication and cleans staging. Explicit legacy projection selections keep the existing manual-audit path.

## Evidence

- Focused contract proves there is no final package or staging directory after the failure.
- The original FloorPlan source remains present.
- The actionable Electrical reason is persisted at `DependentSheetGeneration` and rethrown.
- Static diff check passes; repository policy leaves `.NET` execution to the external handoff.

## Files

- `src/FloorplanFit.Application/PlanSets/Export/ExportMultiSheetPlanSetPackageHandler.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Export/ExportMultiSheetPlanSetPackageHandlerTests.cs`

## Reopened gap

`ResolveExportableProjectionsAsync` selects each sheet's latest projection and then keeps only `ReadyForExport`. If the required Electrical sheet's latest projection is `RequiresManualConfirmation`, it disappears from `exportableProjections`; no Electrical export is attempted, so the atomic exception guard never runs and a Floor-only package can still be published.

The existing test `HandleAsync_without_explicit_projection_ids_uses_latest_projection_per_sheet` explicitly accepts that result. Automatic discovery must instead fail closed when its required Electrical projection is absent or not `ReadyForExport`; explicit legacy projection-ID requests may retain manual-audit behavior.

## Reopened gap resolved — 2026-07-21

A universal auto-discovery fail-closed rule was rejected with evidence: the legacy Desktop manual flow (`ConfirmManualPlanSetProjectionsAndReExportAsync`, proven by `SitePlanAdjustmentPreviewProjectorTests.ConfirmManualPlanSetProjectionsAndReExportAsync_reuses_existing_canonical_adjustment`) also uses empty `DependentProjectionIds` and requires the intermediate manual-audit package so the operator can confirm and re-export. Making every automatic discovery atomic would have dead-ended that preserved flow.

The implemented closure is an explicit request invariant:

- `ExportMultiSheetPlanSetPackageRequest.RequireReadyElectricalPlan` (Contracts) declares that automatic discovery requires the latest `ElectricalPlan` projection in `ReadyForExport` before any staging.
- `ResolveExportableProjectionsAsync` became `ResolveRequestedProjectionsAsync`: automatic discovery no longer silently filters non-ready latest projections; the export loop still skips them as audit-only entries, which the automatic audit ignores because it rediscovers all sheets itself.
- `EnsureRequiredElectricalProjectionIsReadyAsync` runs inside the staging callback at `DependentSheetGeneration`: a non-ready latest Electrical throws `ProjectedPlanSheetManualReviewRequiredException` with the sheet name, status, and stored warning; a missing/unclassifiable Electrical throws the absence reason. `AtomicDirectoryPublisher` cleans staging, no final package appears, the loose Floor source is preserved, and the failure is persisted.
- `SitePlanAdjustmentViewModel` sets `RequireReadyElectricalPlan = isCommissionedAutoFit` on both package call sites, so the commissioned daily route is all-or-nothing while the legacy manual confirm-then-re-export route keeps its intermediate audit package.

### Static contracts

- `HandleAsync_requiring_ready_electrical_fails_closed_when_latest_electrical_is_not_ready`: no final package, no staging, Floor source preserved, failure persisted at `DependentSheetGeneration`, warning text propagated, zero dependent exports attempted.
- `HandleAsync_requiring_ready_electrical_fails_closed_when_no_electrical_projection_exists`: a ready non-Electrical sheet cannot satisfy the requirement and nothing is exported before the gate.
- `HandleAsync_requiring_ready_electrical_publishes_when_latest_electrical_is_ready`: the gate does not over-block a ready Electrical.
- `HandleAsync_without_required_electrical_flag_keeps_latest_manual_projection_in_audit` (renamed complicit test): legacy flag-off discovery keeps the manual-audit result.
- Desktop `ExportAdjustedSitePlanAsync_commissioned_fails_closed_without_package_when_electrical_requires_manual_confirmation`: rigid commissioned fit + manual Electrical → no `-plan-set` directory, no staging sibling, canonical DXF preserved, no dependent export, actionable reason in `AutoFitSuggestionStatus`, `LastPlanSetExportAudit` null.

Scoped `git diff --check` passed (line-ending warnings only). Repository policy reserves compile/test execution for the external handoff.
