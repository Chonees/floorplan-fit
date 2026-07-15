# Fase 5 package rename preceded verification gate

## Links
- `replaces`: [[2026-07-13 - P0 hardening baseline and gates#Fase 5 - publicacion atomica]]
- `replaced_by`: current implementation in [[Current State#2026-07-13 - Fase 5 publication order gate corrected]]

## What
`AtomicDirectoryPublisher.PublishAsync` moved dependent DXFs from sibling staging to the final user `PackageDirectory` before `BuildVerificationReport` and workspace manifest publication.

## Why it was wrong
The explicit reliability gate is stage -> verify -> publish workspace audits and manifest (manifest last) -> rename the user package -> persist success. Publishing the user directory first exposed an unverified package and forced later rollback instead of preventing early visibility.

## Fix
- Added a minimal atomic completion callback that keeps staging private until the audit handler requests the rename.
- Added non-serialized `VerificationPath` DTO state so DXF readers inspect staging while manifest/DB keep logical final paths.
- Moved user rename between workspace publication and success persistence.
- Added compensation for staging, newly published user package, and workspace publication while preserving any pre-existing final.
- Strengthened `scripts/test-p0-atomic-package-contract.ps1` and the focused package test to assert the real order.

## Evidence
- RED: `P0 RED: atomic publication still has no callback boundary between staging and final rename.`
- GREEN: Fase 5, Fase 4, Fase 3, manifest self-check, and `git diff --check` all exit `0`.
- No `dotnet`, build, test, restore, watch, app, commit, or push command was run.
