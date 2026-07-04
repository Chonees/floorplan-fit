# HousePlanSet export package manifest implemented

## What
Extended Phase 7 so each multi-sheet export audit writes a physical JSON package manifest.

## Why
An export audit without a package artifact is still only internal state. The workflow needs a concrete manifest path before UI/package export and dependent-sheet DXF rewriting can be wired safely.

## Current truth
- Commits:
  - `ab988ce` (`docs: plan export package manifest`)
  - `72ab90a` (`feat: write export package manifest`)
  - `ae9aa60` (`docs: record export package manifest bridge`)
- New port: `IPlanSetExportManifestWriter`.
- New infrastructure writer: `PlanSetExportManifestWriter`.
- Manifest output path: `<workspace>/exports/plan-sets/<exportId>/manifest.json`.
- `CreateMultiSheetExportAuditHandler` writes the manifest, persists `PackageManifestPath`, and returns it in `MultiSheetExportAuditDto`.
- SQLite `plan_set_exports` now has `package_manifest_path`.
- The manifest records the existing audit DTO: canonical/dependent sheet status, automatic/manual/missing state, confidence, warnings, projection methods, and rule summaries.

## Boundaries
- No zip archive.
- No dependent-sheet DXF copy.
- No dependent-sheet geometry rewrite.
- No Desktop UI.
- No per-sheet fit engine.

## Verification
- Test-first RED evidence: `IPlanSetExportManifestWriter.cs` did not exist and `MultiSheetExportAuditDto` had no `PackageManifestPath`.
- Scoped `git diff --check` and `git diff --cached --check` passed.
- No `dotnet test` and no `dotnet build` were run due repository rule.
