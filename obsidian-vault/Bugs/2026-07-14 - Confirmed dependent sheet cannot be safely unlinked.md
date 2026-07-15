---
type: Bugs
status: resolved
date: 2026-07-14
---

# Confirmed dependent sheet cannot be safely unlinked

## Symptom

The unlink cross is hidden for a confirmed ElectricalPlan, including `Confirmed + ReadyForExport`, preventing a fresh registration test.

## Root cause

`CanUnlink` intentionally permits only `Unregistered` or `Rejected` sheets. The Desktop method directly deletes the `plan_sheets` row. Confirmed registrations and projections are separate records without foreign-key cascade, so simply showing the cross would orphan workflow records.

## Correct fix

An Application unlink operation must delete all projections, then all registrations, then the dependent sheet in one transaction. Imported source documents and historical export/audit snapshots remain untouched.

## Implemented

`UnlinkPlanSheetHandler` now owns the transaction. The UI exposes unlink for every noncanonical sheet, while the handler protects canonical sheets and deletes every projection, every registration, and then the sheet before one commit. The Desktop view model no longer deletes rows directly.
