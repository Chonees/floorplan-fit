---
type: experiment
date: 2026-06-16
topic: ponytail-whole-app-audit
status: analysis
replaces: Experiments/2026-06-16 - Ponytail as simplicity guard for Floorplan Fit.md
---

# Ponytail whole-app audit

## Scope

Read-only audit of the full Floorplan Fit app using Ponytail as a simplicity lens: YAGNI, deletion over addition, standard/native features first, no speculative abstractions, and one focused runnable check for non-trivial logic.

## Evidence snapshot

- Projects: `Domain`, `Contracts`, `Application`, `Infrastructure`, `Desktop`, plus three test projects.
- Production C# size excluding `bin/obj`:
  - `src/FloorplanFit.Infrastructure`: 55 files, ~14.9k lines.
  - `src/FloorplanFit.Desktop`: 49 files, ~12.6k lines.
  - `src/FloorplanFit.Application`: 115 files, ~6.4k lines.
  - `src/FloorplanFit.Domain`: 47 files, ~2.3k lines.
  - `src/FloorplanFit.Contracts`: 33 files, ~0.7k lines.
- Largest production files:
  - `FloorPlanReviewViewModel.cs`: ~2.3k lines.
  - `SqliteFloorPlanReviewSessionReader.cs`: ~2.2k lines.
  - `FloorPlanPreviewControl.cs`: ~1.7k lines.
  - `SitePlanAdjustmentViewModel.cs`: ~1.4k lines.
  - `DimensionGeometryProjector.cs`: ~1.2k lines.
  - `IxMiliaAdjustedSitePlanExporter.cs`: ~1.1k lines.
- Dependencies are lean: Avalonia, CommunityToolkit.Mvvm, Microsoft hosting/logging, IxMilia.Dxf, Microsoft.Data.Sqlite, xUnit/coverlet/test SDK.
- Application does not take real dependencies on Infrastructure/Avalonia/IxMilia; the only `ixmilia` hit in Application is a string version label.
- `.testartifacts` contains many ignored isolated test-output directories and measured around 31GB during audit.

## Findings

### Good foundations

- The high-level project dependency direction is mostly healthy: Application depends on Domain/Contracts; Infrastructure implements Application ports; Desktop composes.
- Dependencies are not bloated. Do not add geometry/rendering libraries unless a focused test proves current primitives cannot cover the case.
- The newly extracted Loop 2 modules (`SitePlanAdjustmentFitAnalyzer`, `ChangedDimensionDetector`, `AdjustedDimensionImpactResolver`) are aligned with Ponytail: small, pure-ish, and testable.

### Main complexity hotspots

1. Desktop still owns too much orchestration/rendering logic.
   - `FloorPlanReviewViewModel.cs`, `FloorPlanPreviewControl.cs`, and `SitePlanAdjustmentViewModel.cs` are the highest-risk files for accidental regressions.
   - Preferred fix is not generic layering; extract behavior only when it is pure, named, and separately testable.

2. Persistence has one very large read model.
   - `SqliteFloorPlanReviewSessionReader.cs` mixes summary resolution, extraction reads, override lineage, primitive hydration, deduplication, and curated artifact construction.
   - Preferred fix is package-private helper classes/functions grouped by read concern, not repository-per-table explosion.

3. DXF export/extraction files are large because IxMilia mapping is verbose and CAD correctness is fragile.
   - Do not simplify away DXF metadata preservation or AutoCAD compatibility checks.
   - Extract repeated primitive transform/map logic only when tests prove equivalence.

4. The root contains one-off investigation scripts.
   - `analyze_centering.py`, `analyze_floorplan_bbox.py`, `analyze_session_bbox.py`, and `analyze_setback.py` look like forensic scripts, not app surface.
   - Move to `scripts/forensics/` or document them; delete if obsolete.

5. Test output artifacts are a workspace-health issue.
   - `.testartifacts` is ignored but huge; establish a cleanup command or convention before it silently eats disk.

## Ponytail rules for this app

- Keep ports that protect Application from Infrastructure; do not delete them merely because they have one implementation.
- Avoid new internal interfaces/factories until a second real implementation exists.
- Extract pure geometry/fit/export behavior from ViewModels before adding new UI state.
- Prefer boring static/internal modules plus focused tests over framework-heavy architecture.
- Mark deliberate shortcuts only with an upgrade trigger, e.g. `ponytail: axis-aligned only; upgrade when diagonal interval binding is product truth`.

## Recommended order

1. Clean workspace artifacts and document isolated `dotnet test --output .testartifacts/<name>` cleanup.
2. Apply `ponytail-review` manually to the current Adjust-to-Site-Plan diff.
3. Extract the next Loop 2 pure modules:
   - `AutoFitCompressionTransformBuilder`
   - `AutoFitCompressionProjector`
   - `AdjustedSitePlanPlacementBuilder`
   - `AutoFitSuggestionTextFormatter`
4. Split `SqliteFloorPlanReviewSessionReader` by read concern only after Loop 2 stabilizes.
5. Move/delete forensic root scripts after confirming which are still useful.

