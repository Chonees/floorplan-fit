# Slice 1 Import Foundation Design

**Goal:** Convert a real floor plan DXF into a persisted library asset with basic identity, measurement context, and versioning.

## Scope

This first slice covers:

- reading `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`
- capturing source measurement data
- persisting the imported document
- creating the reusable floor plan entry
- creating the first version for that floor plan
- returning a minimal library item

This slice does **not** include wall extraction, wall review, curation, publication, site plan work, or fit generation.

## Primary Truth

- **Primary truth:** `PLANS/originalFloorPlans/*.dxf`
- **Legacy comparative reference only:** `PLANS/catalog/*.json`

The system must generate its own import and persistence flow from DXF files. Existing catalog JSON files can help us compare metadata, but they are not domain truth.

## Architecture

### Contracts

- `ImportFloorPlanRequest`
- `ImportFloorPlanResponse`
- `FloorPlanLibraryItemDto`

### Domain

- `MeasurementContext`
- `ImportedDocument`
- `FloorPlanTemplate`
- `FloorPlanVersion`

### Application

- `ImportFloorPlanHandler`
- `FloorPlanCodeNormalizer`
- `ImportFloorPlanResultFactory`
- interfaces for DXF reading, hashing, repositories, and transaction boundary

### Infrastructure

- concrete DXF adapter later
- concrete SQLite repositories later

## Data Flow

`DXF path -> read document metadata -> build measurement context -> hash file -> create imported document -> create/update floor plan -> create version -> persist -> return library item`

## Error Handling

The slice should move toward explicit failures for:

- missing file
- unreadable DXF
- unit not detected
- persistence failure

This first implementation pass focuses on the happy path shape and file boundaries first.

## Test Strategy

1. Application test first for the import use case using in-memory fakes
2. Infrastructure tests later when a real SDK is available locally
3. `SANTA-BARBARA` as the first guiding fixture
4. `SEMINOLE2000` as the regression/robustness fixture after the happy path is stable

## Environment Note

The current machine has the .NET runtime installed but **no .NET SDK**. That means we can scaffold files and write tests/code, but we cannot compile or run the red/green loop locally until the SDK is installed.
