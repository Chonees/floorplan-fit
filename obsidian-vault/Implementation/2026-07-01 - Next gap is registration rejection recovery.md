---
type: Implementation
date: 2026-07-01
replaces: []
replaced_by: [[2026-07-01 - Library rejects dependent sheet registrations]]
---

# Next gap is registration rejection recovery

## What
Static inspection showed the next useful HousePlanSet slice is recovery from an incorrect dependent-sheet registration.

## Why
The Library now lets the user unlink or correct a dependent sheet only while it is `Unregistered / NotProjected`. Once the user clicks `Register`, the row becomes `PendingConfirmation`; there is a domain enum value `Rejected`, and confirmation correctly refuses rejected registrations, but Desktop currently exposes `Confirm` only and no visible reject/unregister recovery action.

## Evidence
- `SheetRegistrationStatus` already contains `PendingConfirmation`, `Confirmed`, and `Rejected`.
- `ConfirmSheetRegistrationHandler` guards rejected registrations: rejected registrations cannot be confirmed.
- `PlanSetSheetDto.CanUnlink`, `CanRegisterDependent`, and `CanCorrectSheetType` currently require `RegistrationStatus == "Unregistered"`.
- `MainWindow.axaml` exposes `Register`, `Confirm`, `Confirm Projection`, sheet type correction, and unlink; no `Reject`/`Unregister` registration action is exposed for pending registrations.

## Recommended next slice
Add a minimal `RejectSheetRegistration` / `Unregister` recovery action for pending dependent sheet registrations, then make rejected dependent sheets eligible to be corrected, unlinked, or registered again.

## Boundary
Do not add a visual transform wizard yet. Keep this as a small recovery UX so mistakes do not trap the user in a stale registration state.

## Superseded
This gap was closed by [[2026-07-01 - Library rejects dependent sheet registrations]].
