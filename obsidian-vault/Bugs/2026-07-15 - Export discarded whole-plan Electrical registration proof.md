# 2026-07-15 - Export discarded whole-plan Electrical registration proof

## Status
Proof/audit code now builds externally. A verified current-version additive-schema startup failure was corrected statically; focused migration execution and fresh runtime export remain required.

## Current-version additive migration correction
An active LocalAppData database already had `PRAGMA user_version = 2` from before `whole_plan_registration_proof_json` existed. Initialization returned immediately for current-version databases, so repositories queried a column that had never been added and startup failed with SQLite Error 1.

Current-version initialization now runs the existing idempotent `EnsureSheetRegistrationsSchema` before returning. Future/unknown versions still fail closed before any repair, while fresh databases and the v1-to-v2 migration keep their existing paths. A focused realistic v2 fixture preserves its registration row, initializes twice, verifies the column, and exercises repository get/list contracts.

## Root cause
Electrical registration accepted exactly one transform only after evaluating canonical-to-transformed-Electrical coverage and residuals across the complete structural plan. The accepted `SheetRegistration` persisted only the transform and human-readable evidence summary.

Recipe-aware export later recomputed `DominantAxisAlignedOutlineSelector.Select` from the Electrical file. A locally stronger connected frame could therefore replace the already-proven whole-plan canonical frame and force manual review. The export path had discarded the conclusive evidence rather than consuming it.

## Fix
- Added the versioned typed `WholePlanRegistrationProof` with explicit `Passed`, canonical/dependent identities, canonical/dependent SHA-256 values, horizontal/vertical coverage, RMS residual, and maximum residual.
- Electrical registration maps the estimator-owned global result into the proof. Estimator acceptance and persisted-proof authority use the same versioned `WholePlanRegistrationAcceptancePolicy`; weak `Passed=true` payloads are not authoritative.
- Confirmation preserves the proof, and SQLite round-trips it through one nullable JSON column.
- The SQLite v1-to-v2 migration adds the nullable proof column before repairing identities and advancing `user_version`; a migrated-database contract covers add/read/update round-trip.
- Recipe-aware export validates projection/registration/canonical-adjustment ownership, proof identity binding, and the caller Electrical DXF SHA-256. Only a confirmed, current, passed, policy-valid, source-bound proof authorizes the canonical registered coordinate frame.
- The authoritative path keeps `OutlineNormalization = null`, leaves canonical operation coordinates unchanged, and does not select a local frame for source anchoring or final outline inference.
- Missing, legacy, failed, malformed, or unsupported-version proof remains manual-review only. `RuleSummary` text is never parsed as authority.
- Outline authorization is reported honestly as `RegistrationProofAuthorized`: bounds, residuals, anchors, and scales are null/unmeasured rather than fabricated as congruent zeroes. Measured final-output congruence, operation, segment, DXF-safety, and package gates remain mandatory.

## Safety retained
Unsupported entities, curve-crossing conversion/rejection, operation/deformation audits, DXF safety, canonical congruence dimensions, and outer atomic package publication remain in their existing paths. Proof authorization replaces only the contradictory local-frame inference.

## Source binding
The proof lives on `SheetRegistration`, which binds the canonical floor-plan version and dependent sheet identities. Canonical `FloorPlanVersion` and `ImportedDocument` records are append-only in their repositories, and managed file import copies to a unique path with `overwrite: false`; export therefore compares the canonical version identity. The dependent caller path can vary, so estimator-captured SHA-256 is rechecked immediately before export. Both source hashes remain persisted in the proof for audit/binding.

## Verification
- Focused contracts were written before production changes.
- Static constructor/schema/call-site review, PowerShell parse checks, scoped whitespace/diff checks, and generic hardcode scans passed.
- No `dotnet`, build, test, restore, watch, Desktop, commit, or push command ran.

## Related
- Implementation: [[2026-07-15 - Whole-plan Electrical proof reaches export]]
- Replaces the export-blocker portion of [[2026-07-15 - Nullable Desktop audit summary masked manual HousePlanSet result]].
