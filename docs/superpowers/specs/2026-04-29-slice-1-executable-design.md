# Slice 1 Executable Design

**Goal:** Turn a real floor plan DXF (`SANTA-BARBARA.dxf`) into a persisted, versioned library asset through a thin desktop entrypoint, real DXF reading, managed file storage, real hashing, and SQLite persistence.

## Relationship to previous design

This design builds on `docs/superpowers/specs/2026-04-25-slice-1-import-foundation-design.md`.

The earlier design defined the architectural foundation of Slice 1. This document narrows the target to an **executable end-to-end slice** with a thin Desktop shell and concrete Infrastructure behind the existing Application abstractions.

## Product scope

- **Product loop:** Loop 1 ? floor plan curation foundation
- **Architecture layers involved:** Desktop, Application, Domain, Infrastructure, Contracts

The output of this slice is not wall extraction or curation yet. The output is a real, institutionalized floor plan import that the system owns and can show in a minimal Library.

## In scope

This slice includes:

1. A minimal `FloorplanFit.Desktop` application shell
2. A minimal Library screen that can trigger floor plan import
3. Real import of `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`
4. Real DXF metadata/unit reading through Infrastructure
5. Copying the imported file into app-managed storage (`library/raw-dxf/`)
6. Real SHA-256 hashing of the managed copy
7. Real SQLite persistence for:
   - `measurement_contexts`
   - `imported_documents`
   - `floorplan_templates`
   - `floorplan_versions`
8. Creation of the first visible Library entry
9. Real integration verification of the import pipeline

## Out of scope

This slice does not include:

- wall extraction
- extracted wall candidates
- wall review UI
- curated walls
- publish curation
- site plan import
- envelope extraction
- fit engine
- export
- rich desktop UI polish

## Primary truth and oracle policy

- **Primary truth:** `PLANS/originalFloorPlans/*.dxf`
- **Legacy oracle only:** `PLANS/catalog/*.json`

The executable slice must read from the original DXF. Existing JSON catalog files remain comparison artifacts only. They may help verify stable metadata such as unit, footprint bounding box shape, or expected naming, but they are not product truth.

## End-to-end flow

The executable flow is:

`Desktop import action -> ImportFloorPlanHandler -> IDxfGateway (real) -> IManagedFileStorage (real) -> IFileHashService (real) -> SQLite repositories -> UnitOfWork -> Library response -> Desktop list refresh`

More explicitly:

1. The user opens the thin Desktop app.
2. The user chooses a floor plan DXF to import.
3. Desktop passes the selected path into `ImportFloorPlanRequest`.
4. `ImportFloorPlanHandler` asks `IManagedFileStorage` to copy the source file into app-managed storage.
5. `ImportFloorPlanHandler` asks `IDxfGateway` to read DXF metadata and initial geometric fingerprint from the managed copy.
6. `ImportFloorPlanHandler` asks `IFileHashService` to hash the managed copy.
7. The handler creates `MeasurementContext`, `ImportedDocument`, `FloorPlanTemplate`, and `FloorPlanVersion` domain objects.
8. SQLite repositories persist the objects inside one transaction.
9. The handler returns a `FloorPlanLibraryItemDto`.
10. Desktop refreshes the Library and shows the imported item.

## Managed storage policy

The original user-selected path is import input only.

The application must copy the DXF into an app-managed workspace before persisting the import. `ImportedDocument.storage_path` must point to the managed copy, not the external original path. DXF metadata reading, geometric fingerprinting, and hashing must all operate on that managed copy so the persisted records describe the exact file the app now owns.

Initial runtime structure for this slice:

```text
/workspace
  app.db

  /library
    /raw-dxf
```

## Solution shape for this slice

```text
/src
  FloorplanFit.Desktop          <-- new
  FloorplanFit.Application
  FloorplanFit.Domain
  FloorplanFit.Infrastructure
  FloorplanFit.Contracts

/tests
  FloorplanFit.Application.Tests
  FloorplanFit.Infrastructure.Tests   <-- new, minimal
```

## Layer responsibilities

### Desktop

Responsible for:

- app bootstrap
- Library screen/view model
- import trigger
- showing imported items and status

Desktop does not parse DXF, touch SQLite directly, or compute hashes.

### Application

Responsible for:

- orchestrating the import use case
- coordinating ports for DXF read, managed file copy, hashing, repositories, and transaction boundary
- mapping persisted entities back to Library-facing response DTOs

### Domain

Responsible for:

- `MeasurementContext`
- `ImportedDocument`
- `FloorPlanTemplate`
- `FloorPlanVersion`
- invariants of those objects only

### Infrastructure

Responsible for:

- real DXF reading via IxMilia.Dxf behind `IDxfGateway`
- file copy into managed storage
- SHA-256 hashing
- SQLite schema creation and repository implementations
- transaction handling

### Contracts

Responsible for:

- import request/response contracts
- Library item DTO

## Required new/adjusted abstractions

The current Application layer already has most of the right boundaries. One new abstraction should be introduced:

### `IManagedFileStorage`

Purpose:

- accept the user-selected source path
- copy the file into app-managed storage
- return the managed destination path

This must remain separate from `IDxfGateway` because file storage responsibility is different from DXF interpretation responsibility.

## Concrete infrastructure components

The executable slice should add these concrete pieces:

- `ManagedFileStorage`
- `IxMiliaDxfGateway`
- `Sha256FileHashService`
- SQLite schema initializer
- SQLite connection factory or equivalent simple bootstrap component
- SQLite repositories for measurement contexts, imported documents, floor plan templates, and floor plan versions
- SQLite unit of work / transaction wrapper

## Minimal Desktop shape

The initial `FloorplanFit.Desktop` project should stay deliberately thin.

Minimum responsibilities:

- app startup
- DI/host bootstrap
- main window
- Library view model
- import action
- refresh of the imported item list

The Desktop slice should prove the product stitch, not visual polish.

## Persistence model for this slice

### `measurement_contexts`

Minimum columns:

- `id`
- `source_unit`
- `to_millimeters_factor`
- `linear_tolerance_mm`
- `angular_tolerance_deg`
- `created_at_utc`

### `imported_documents`

Minimum columns:

- `id`
- `document_type`
- `original_file_name`
- `storage_path`
- `sha256`
- `dxf_version`
- `measurement_context_id`
- `imported_at_utc`

### `floorplan_templates`

Minimum columns:

- `id`
- `code`
- `name`
- `current_version_id`
- `is_active`

### `floorplan_versions`

Minimum columns:

- `id`
- `floorplan_template_id`
- `imported_document_id`
- `geometry_fingerprint`
- `version_number`
- `created_at_utc`

## Versioning rule

If a floor plan with the same normalized code already exists, the slice should create a new `FloorPlanVersion` and advance `current_version_id` on the template. The first executable path only needs to prove version `1` cleanly using `SANTA-BARBARA`, but the persistence design must not block future version increments.

## Testing strategy

### 1. Existing Application unit test stays

The current `ImportFloorPlanHandlerTests` remains valuable for fast orchestration checks with in-memory fakes.

### 2. Add Infrastructure integration tests

The executable slice should add a minimal Infrastructure test project that verifies:

- DXF metadata can be read from the real `SANTA-BARBARA.dxf`
- managed storage copies the DXF into a temp workspace
- SHA-256 hash is produced for the managed copy
- SQLite repositories persist the import objects correctly

### 3. Add end-to-end import integration test

A real integration test should exercise:

- temp workspace
- temp SQLite database
- real `IxMiliaDxfGateway`
- real `ManagedFileStorage`
- real `Sha256FileHashService`
- real SQLite repositories
- `ImportFloorPlanHandler`

Expected outcome:

- one imported library item returned
- one managed DXF copy exists
- one measurement context row exists
- one imported document row exists
- one template row exists
- one version row exists

### 4. Desktop verification

The slice requires manual verification that the thin Desktop app can import and display the item. Automated Desktop UI testing is not required in this slice.

## Acceptance criteria

The slice is complete only when all of these are true:

1. `FloorplanFit.Desktop` starts successfully in a correct `.NET 10 SDK` environment.
2. The app can import `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`.
3. The DXF is copied into `library/raw-dxf/` under app-managed storage.
4. The managed copy is hashed.
5. SQLite contains persisted measurement context, imported document, template, and version rows.
6. The Library shows the imported item with status `Imported` and version `1`.
7. At least one real integration test covers the import path.

## Environment prerequisite

This design assumes a correct environment with **.NET 10 SDK** available. The current local machine still lacks any .NET SDK, so implementation may be authored now but cannot be compiled or executed locally until the environment is fixed.

## Risks and mitigation

### Risk: overbuilding Desktop too early

Mitigation:

- keep Desktop to one screen and one action
- defer all non-import UI behavior

### Risk: mixing DXF parsing with filesystem concerns

Mitigation:

- keep `IDxfGateway` and `IManagedFileStorage` separate

### Risk: treating legacy JSON as truth

Mitigation:

- only compare against legacy catalog when useful
- never make runtime persistence depend on those JSONs

### Risk: widening the slice into extraction or curation

Mitigation:

- treat this slice as import institutionalization only
- reject any feature that crosses into wall extraction or site plan work

## Summary

The correct executable Slice 1 is a thin but real product path:

`thin Desktop -> real import -> managed file copy -> hash -> SQLite persistence -> Library item`

That is the first robust milestone. It is intentionally small, but it proves the system owns a floor plan asset end to end and prepares the codebase for the next Loop 1 slices.
