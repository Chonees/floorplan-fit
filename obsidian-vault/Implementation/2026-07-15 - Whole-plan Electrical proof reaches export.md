# 2026-07-15 - Whole-plan Electrical proof reaches export

## Status
Implemented statically in Loop 2; external compilation and execution remain pending.

## Architecture path
1. **Domain policy** owns one version and the coverage/residual acceptance constants/predicate.
2. **Infrastructure estimator** hashes both sources around extraction, computes one unique whole-plan transform and its conclusive global metrics, and accepts through that domain policy.
3. **Application registration** creates a typed source/identity-bound proof from that estimator result and rejects any payload that is not authoritative under the same policy.
4. **Domain registration** owns the optional proof beside the canonical version/dependent sheet identities.
5. **Infrastructure persistence** stores one nullable JSON proof column, migrates v1 before advancing `user_version`, and treats malformed legacy payloads as absent.
6. **Application export** validates the projection/registration/adjustment chain, re-hashes caller Electrical bytes, then passes registration status and proof into `ProjectedPlanSheetExportRecipe`.
7. **Infrastructure DXF export** authorizes canonical coordinates only for confirmed/current/passed/policy-valid proof; otherwise it fails closed. Its outline-stage audit is `RegistrationProofAuthorized` with unmeasured values left null.

## TDD contracts
- estimator result retains all four global metrics for a unique passing transform;
- registration maps those metrics into the proof;
- confirmation preserves the same proof;
- SQLite add/update/read round-trips the proof;
- export handler passes status and proof;
- migrated SQLite v1 databases gain and round-trip the proof column;
- ownership or dependent-byte hash mismatch fails manual before exporter invocation;
- parsed DXF vertices prove a stronger interior decoy frame cannot displace the canonical outer frame;
- missing or policy-invalid proof throws manual review and creates no output.

## Deliberate limits
- No service, factory, dependency, threshold copy, text parsing, site-specific case, or path-specific rule was added.
- Non-Electrical and legacy registrations retain `null` proof.
- SHA-256 uses `System.Security.Cryptography` directly; no service or dependency was added.
- Canonical storage is append-only at the application/repository boundary (`FloorPlanVersion`/`ImportedDocument` add-only, managed copy `overwrite: false`), so canonical version identity is compared. If in-place replacement is ever introduced, canonical bytes must also be re-read at export.

## Safety tradeoff
The exporter no longer lets a local outline selector overrule valid whole-plan registration. That removes contradictory local evidence, but makes the persisted proof plus source binding a trust boundary. The boundary is fail-closed on ownership, source hash, confirmation, policy version, pass status, coverage/residual policy, malformed JSON, and invalid dimensions. Outline authorization is explicitly not a measured congruence claim; measured entity/deformation/segment/final-output/DXF/package guards still execute.

## Verification
Static inspection only. No prohibited executable command was run.

## Fresh compile correction
- `ProjectedPlanSheetOutlineCongruenceAuditDto.AnchorX` and `AnchorY` are nullable string labels, so proof authorization verifies absence with `is null`; `HasValue` applies only to the nullable decimal scale fields.
- The segment audit builder's internal `Point` is a reference record. Its `TryReadPoint` false branch now uses the conventional `null!` out sentinel; callers consume the point only when the method returns true, so no fake coordinate is introduced.

## Related
- Bug: [[2026-07-15 - Export discarded whole-plan Electrical registration proof]]
