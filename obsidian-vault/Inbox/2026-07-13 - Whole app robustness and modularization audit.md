# 2026-07-13 - Whole app robustness and modularization audit

## Type
Architecture audit / Inbox

## Scope
Static, read-only audit of the current `STAGING` worktree. No build, test, restore, app execution, or production-code change was performed.

Product mapping:
- Loop 1: FloorPlan import, extraction, review, curation, publish.
- Loop 2: SitePlan intake, fit, adjustment, approval, export.
- Shared foundation: HousePlanSet relationships, registration, projection, verification, persistence, DXF, operational resilience.

## Executive verdict
The top-level layered modular monolith is the right architecture and should be kept. Project dependencies point in a healthy direction and Application already uses feature folders.

The app is nevertheless not production-safe yet. The main risk is not the number of projects; it is that integrity and business-trust rules are split among UI code, JSON files, scripts, and startup mutation logic. Stabilization must precede broad refactoring or Roof/Facade expansion.

## What is already strong
- One local-first Desktop app, one SQLite database, and explicit Domain/Application/Infrastructure/Contracts boundaries.
- Clear Loop 1 and Loop 2 product model.
- FloorPlan is canonical and dependent sheets receive its approved transformation.
- FloorPlan/Electrical has a real recipe-aware export path and a fresh width-only final-output congruence proof.
- Tests are broad by layer: Application, Infrastructure, Desktop, and PowerShell contract checks.
- Existing modularization design correctly recommends feature modules inside the current solution rather than microservices or more csproj files.

## P0 - stop before expanding features

### 1. Startup can destroy pinch-marker data
`SqliteSchemaInitializer.EnsurePinchMarkersSchema` falls back to `RecreatePinchMarkersTable`, which drops the table without preserving rows. The initializer runs on every startup.

Evidence:
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs:1141-1164`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs:1314-1319`
- `src/FloorplanFit.Desktop/Program.cs:24-28`

Required direction: unknown schema must fail closed; migrations must be numbered, transactional, and backed up before destructive steps.

### 2. Deleting a canonical FloorPlan version can leave broken HousePlanSets
The deletion use case does not check PlanSet references. Cleanup hard-deletes the version, and SQLite has no FK protecting `plan_set_versions.canonical_floor_plan_version_id`.

Evidence:
- `src/FloorplanFit.Application/FloorPlans/Library/RemoveFloorPlanVersionHandler.cs:18-24`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionCleanupService.cs:51-60,170-182`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs:70-75`

Required direction: reject deletion while referenced (`RESTRICT`) and provide an explicit replacement/archive workflow.

### 3. Sheet registration persists the wrong canonical identity
The three registration handlers pass `PlanSetVersionId` into the `CanonicalFloorPlanVersionId` constructor position. These are different domain identities.

Evidence:
- `src/FloorplanFit.Domain/PlanSets/SheetRegistration.cs:5-17`
- `src/FloorplanFit.Domain/PlanSets/PlanSetVersion.cs:32-44`
- `src/FloorplanFit.Application/PlanSets/Registration/RegisterElectricalSheetHandler.cs:64-69`
- equivalent Roof/Facade handlers contain the same pattern.

Required direction: load `PlanSetVersion`, use its real canonical FloorPlan version, validate aggregate ownership, and repair existing persisted rows.

### 4. `ReadyForExport` is decided before final output verification
`CreateMultiSheetExportAuditHandler` derives status from projected/manual counts before the manifest writer creates final congruence and DXF-safety artifacts. The PowerShell verifier is therefore stricter than the product state.

Evidence:
- `src/FloorplanFit.Application/PlanSets/ExportAudit/CreateMultiSheetExportAuditHandler.cs:111-150`
- `src/FloorplanFit.Infrastructure/Storage/PlanSetExportManifestWriter.cs:23-60`

Required direction: one typed `PlanSetVerificationReport` must be computed in-process before status. `ReadyForExport` must derive from that report. JSON and PowerShell become representations/independent smoke checks, not the business gate.

### 5. Roof/Facade can be manually confirmed without receiving canonical compression
Roof/Facade correctly fall to manual review when compression exists, but confirmation only changes status. Export builds a recipe only for Electrical, so a manual confirmation can promote unchanged Roof/Facade geometry.

Required direction: mark unsupported capabilities explicitly and keep Roof/Facade non-exportable for compression until each has a real policy and output verification. Do not pretend partial wiring is support.

### 6. Durable data lives beside the executable
`Program.cs` builds the workspace from `AppContext.BaseDirectory`, producing DB/library/audits under the build or installation directory. This risks permission failures, data loss on update, and non-portable absolute paths. No backup/restore boundary exists.

Required direction: stable user-data root such as `%LOCALAPPDATA%/FloorplanFit`, workspace-relative managed paths, one-time migration, SQLite + managed-file backup, and restore integrity validation.

## P1 - robustness foundation

### Persistence
- Introduce small numbered SQLite migrations; no ORM or migration framework is required.
- Add validated FKs and only proven indexes.
- Configure foreign keys, WAL, busy timeout, and startup integrity check centrally.
- Repair cleanup ordering: it currently deletes source rows before later geometry-path subqueries can discover their IDs.
- Make imports and exports staged and compensatable: temp -> validate -> DB/state -> atomic rename; manifest last.

### Import and extraction
- Keep Import and Extraction as separate product modules and explicit states.
- Rename `ExtractWallCandidatesHandler`; it extracts six artifact families, not only walls.
- Parse each DXF once. Six Infrastructure extractors currently each call `DxfFile.Load` for the same file.
- Persist extraction `Started`, `Completed`, and `Failed` runs with extractor version and error.
- Replace dependent-sheet use of `ReadFloorPlanAsync` with a generic DXF metadata read port.

### Desktop operational boundary
- Add one centralized UI-operation runner for user-safe errors and structured logs.
- Create screen/app lifetime cancellation tokens; Desktop currently sends `CancellationToken.None` from 43 paths.
- Stop/dispose the Host and track background cleanup.
- Make required services required. Optional DI currently lets canonical/package work silently disappear.
- Keep ViewModels as screen state; move multi-step workflows into Application.

### DXF core
- Reuse `DxfDimensionBlockPatcher` instead of maintaining the same block-patch algorithm inside `IxMiliaAdjustedDxfExporter`.
- Create one small text/binary `DxfPairCodec`; there are multiple private `DxfPair` models and parsers.
- Split `ProjectedPlanSheetDxfExporter` only by cohesive responsibility: recipe transform, safety inspection, orchestration. Do not build a generic CAD framework.

### Query performance
- Batch the review-session read model. `SqliteFloorPlanReviewSessionReader` performs per-dimension primitive queries and other N+1 lookups.
- Separate raw batched loading from projection/assembly, but do not create a repository per table.

### Observability and data collection
Separate two concepts:
- Required business audit: part of the transaction and allowed to block a state transition.
- Optional technical telemetry/logging: best effort, but failures must be logged.

Use a typed, versioned event envelope with correlation IDs such as PlanSetVersionId, CanonicalAdjustmentId, ExportId, operation duration, outcome, warning, and failure code. Keep data local-first by default.

## Minimal target module map

Keep the existing five production projects. Organize vertically inside them:

```text
FloorPlans/
  LibraryImport/
  Extraction/
  CurationReview/
  CanonicalAdjustment/

SitePlans/
  IntakeEnvelope/
  FitAdjustment/

PlanSets/
  LibraryRelationship/
  Registration/
  Projection/
  Verification/
  PackageExport/

Infrastructure/
  Persistence/Migrations/
  Dxf/Codec/
  Dxf/Extraction/
  Dxf/Transformation/
  Dxf/Verification/
  Storage/
  Observability/
```

Each workflow should expose one typed Application entry point. Differences among Electrical/Roof/Facade stay as small explicit rule functions or switches. Avoid strategies/factories/interfaces until two implementations genuinely require them.

## Hotspots verified
- `FloorPlanReviewViewModel.cs`: 2,891 lines.
- `SqliteFloorPlanReviewSessionReader.cs`: 2,358 lines.
- `SitePlanAdjustmentViewModel.cs`: 2,287 lines and a 34-parameter constructor.
- `FloorPlanPreviewControl.cs`: 2,063 lines.
- `ProjectedPlanSheetDxfExporter.cs`: 1,988 lines.
- `SqliteSchemaInitializer.cs`: 1,348 lines.
- `DimensionGeometryProjector.cs`: 1,322 lines.
- `IxMiliaAdjustedSitePlanExporter.cs`: 1,276 lines.
- `PlanSetOutlineSegmentCongruenceAuditBuilder.cs`: 1,001 lines.
- `LibraryViewModel.cs`: 932 lines.

File size is not itself the bug. The split criterion is multiple reasons to change: UI state + workflow + filesystem, SQL + projection, codec + transformation + auditing, or schema creation + migration.

## Duplication and deletion candidates
- Delete inactive Claude provider/options/tests and legacy suggester overload if provider choice is not a real requirement.
- Delegate duplicate dimension-block patching to the existing patcher.
- Consolidate the three registration pipelines and thin projection shells with one explicit typed workflow.
- Inline/delete the one-caller `ImportFloorPlanResultFactory`.
- Data-drive the 1,070-line PowerShell verifier self-test instead of hand-writing many manifest shapes.
- Remove tracked compiled outputs from `.testartifacts`; current tracked binaries are about 174 MB, while local ignored test artifacts consume tens of GB under a OneDrive workspace.
- Ignore generated `output/` and `tmp/` paths after preserving any intentional fixtures.

Potential simplification: roughly 1,200-1,800 code/script lines, no dependency addition, and about 174 MB of tracked binaries removed.

## Test and release gaps
- There is no repository CI workflow.
- Existing tests are broad, but no automated startup/failure/shutdown E2E was found.
- Add real-workspace golden cases for Loop 1 and Loop 2:
  - width-only compression
  - height-only compression
  - combined compression
  - affine-only
  - curve crossing
  - text and binary DXF
  - at least one second FloorPlan/Electrical family
  - import/extract/publish/reopen
  - cancellation and rapid double-action recovery.
- Keep one manual AutoCAD overlay/open check as release acceptance until an independent CAD validator is available.

## Change-control risk
Current `STAGING` has 30 modified tracked files, 73 untracked files, and approximately 6,485 insertions / 1,346 deletions before this note. Refactoring on top of that would mix stabilization, feature behavior, tests, scripts, and docs.

First step: checkpoint the current HousePlanSet work into reviewable logical commits and establish a green CI baseline. Do not begin a broad modularization while the baseline is this large and uncommitted.

## Recommended execution order
1. Protect data and identity: backup, stable workspace, safe migration, canonical ID repair, delete guard.
2. Put final verification inside the product and block unsupported Roof/Facade paths.
3. Establish CI and E2E golden fixtures for FloorPlan + Electrical.
4. Make import/export atomic and add UI/logging/cancellation boundaries.
5. Batch review queries and parse extraction DXF once.
6. Consolidate DXF codec and duplicated registration/projection shells.
7. Only then expand Roof and Facade with their own proven rules.

## Explicit non-goals
- No microservices.
- No rewrite.
- No extra csproj split now.
- No event sourcing or distributed job system.
- No generic CAD framework.
- No new dependency for problems already solved by .NET, SQLite, or the existing ports.

## Related
- [[Current State]]
- [[Current State - HousePlanSet Sync Done vs Missing]]
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

