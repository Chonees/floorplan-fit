---
type: Implementation
date: 2026-07-01
replaces:
  - [[2026-07-01 - Next gap is registration rejection recovery]]
replaced_by: null
---

# Library rejects dependent sheet registrations

## What
The Library can now reject a pending dependent-sheet registration and recover the sheet for correction, unlink, or re-registration.

## Why
A user can register the wrong dependent sheet or register it with the wrong assumed alignment. Without a reject/unregister action, the row stayed `PendingConfirmation` and the UI no longer allowed correction/unlink/re-register recovery.

## Changed
- Added `RejectSheetRegistrationRequest` and `RejectSheetRegistrationHandler`.
- Added Desktop DI registration for `RejectSheetRegistrationHandler`.
- Added `Reject Reg` button on pending/not-projected dependent sheet rows.
- Added `LibraryViewModel.RejectDependentSheetRegistrationAsync(...)`.
- Added `PlanSetSheetDto.CanRejectRegistration`.
- Treat `Rejected / NotProjected` dependent sheets as recoverable for unlink, type correction, and re-register.
- Updated sheet type correction to ignore rejected historical registrations while still blocking pending/confirmed registrations.

## Boundary
No transform wizard or registration editor was added. Rejection is a small recovery action; confirmed registrations remain protected.
