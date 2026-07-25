---
type: bug
status: fixed-statically
date: 2026-07-21
project: FloorplanFit
area: Infrastructure canonical Floor DXF replay
replaces: "[[Current State#2026-07-21 - Commissioned readiness now requires explicit auxiliary coverage]]"
---

# Canonical Floor replay could not resolve auxiliary DXF roles

## Root cause

CadStretch v2 replay assigned stable raw source references only to structural wall `LINE`/`LWPOLYLINE` entities. Persisted commissioned roles for openings, labels, dimensions, and protected inserts therefore could not be resolved. The replay path also rejected persisted `Fixed`, selected only the first duplicate role, and could silently infer a raw role without proving that it matched the commissioned role.

## Fix

The canonical Floor exporter now keeps structural and auxiliary source identities separate. It resolves opening `LINE`/`ARC`/straight `LWPOLYLINE`, `TEXT`/`MTEXT`, handle-backed `DIMENSION`, and commissioned `INSERT` identities. INSERT identity and complete transformed bounds reuse the existing fixed-component extractor rather than introducing another DXF model.

Before any output is created, replay rejects missing, duplicate, ambiguous, unsupported, aliased, crossing, deforming, or source-role-mismatched auxiliary evidence. `Fixed` produces no raw pair edits. `RigidMove` translates every coordinate pair exactly once while leaving scale, radius, text height, block name, and orientation payload untouched; zero-delta axes are not rewritten.

## Scope

- Canonical FloorPlan exporter only; Electrical composition is unchanged.
- No Roof/Facade behavior, Pinches fallback, SEMINOLE hardcode, generic arbitrary-DXF transformer, DTO change, or new dependency.
- Unsupported grouped/protected source identities remain fail-closed.

## Evidence

- Focused static contracts cover a supported INSERT opening, fixed TEXT payload preservation, exact rigid INSERT translation, auxiliary Stretch rejection, full INSERT-bounds crossing rejection, missing/duplicate/unsupported identities, duplicate raw DIMENSION identity, and persisted/raw role mismatch.
- `git diff --check` passes for the two implementation files.
- Repository policy forbids local `.NET` execution; compile and focused test execution remain external.

## Contradictory evidence — 2026-07-21

The broad "identities are separate" claim was too strong. Structural target resolution correctly reads only `StructuralSourceEntityRef`, but persisted auxiliary resolution still calls `SourceReferenceMatchesEntity`, which accepts **either** `StructuralSourceEntityRef` or `AuxiliarySourceEntityRef`. A textual `LINE:n` collision can therefore resolve through the wrong namespace. Microsteps `2.1–2.4` reopen only that bounded identity/DIMENSION proof; the already-covered INSERT/TEXT/fail-closed behavior remains valid.

## Resolution of the contradictory evidence — 2026-07-21

Persisted auxiliary roles now compare only against `AuxiliarySourceEntityRef`; structural target spans compare only against `StructuralSourceEntityRef`. The existing extraction identities remain unchanged. A focused global/structural `LINE:3` collision rejects before output, and a handle-backed `DIMENSION` `RigidMove` contract proves exact one-time coordinate translation with non-coordinate payload preservation. This closes only microsteps `2.1-2.4`; accepted wall-host identity remains a separate open concern in [[2026-07-21 - Opening commissioning confuses geometry with wall host]].

## Files

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaAdjustedSitePlanExporter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaAdjustedSitePlanExporterTests.cs`
