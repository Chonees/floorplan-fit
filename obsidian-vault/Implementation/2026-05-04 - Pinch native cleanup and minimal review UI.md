---
project: floorplan-fit
type: implementation
date: 2026-05-04
status: active
replaces:
  - "[[Implementation/2026-05-04 - Pinch curation preview prototype]]"
related:
  - "[[Current State]]"
  - "[[Inbox/2026-05-04 - Proposed MVP simplification axis-tagged pinch zones]]"
tags:
  - loop-1
  - desktop
  - application
  - infrastructure
  - pinch
  - curation
---

# Pinch native cleanup and minimal review UI

## Context

La rama prototipo anterior todav?a llevaba demasiado equipaje:

- `CuratedWall`
- metadata de wall
- `PinchGroup`
- sync autom?tico de walls staged
- UI de inspector que ya no representaba el objetivo real

La decisi?n aprobada fue hacer una **limpieza extrema en esta branch** y dejar como verdad can?nica solo lo que el fit futuro realmente necesita:

- l?neas detectadas/rechazadas
- pinch markers estrat?gicos
- eje `Width` / `Height`
- `MaxTrimMm`

## What changed

- **Contracts**
  - `FloorPlanReviewSessionDto` ahora expone `PinchMarkers` en vez de `CuratedWalls`
  - `PinchMarkerDto` ahora guarda:
    - `SourceCandidateId`
    - `GeometryPathId`
    - `AxisTag`
    - `PositionRatio`
    - `MaxTrimMm`

- **Domain**
  - `PinchMarker` pas? a ser pinch-native
  - `PinchGroup` sali? del flujo
  - `CuratedWall` sali? del flujo activo de review

- **Application**
  - `AddPinchMarkerHandler` ya no depende de `CuratedWall` ni `PinchGroup`
  - `RejectWallCandidateHandler` ahora limpia pinches por `SourceCandidateId`
  - `PublishFloorPlanCurationHandler` ahora exige al menos un pinch marker
  - se eliminaron:
    - `AcceptWallCandidateHandler`
    - `UpdateCuratedWallMetadataHandler`
    - `CreatePinchGroupHandler`
    - `SyncCuratedWallsFromCandidatesHandler`

- **Infrastructure**
  - SQLite ahora usa `pinch_markers` como payload de curado pinch-native
  - el review session reader ya no hidrata curated walls
  - `SqliteCuratedWallRepository` y `SqlitePinchGroupRepository` quedaron fuera de esta branch

- **Desktop**
  - la review UI se simplific? a:
    - `Lines`
    - `Preview`
    - `Pinch Tools`
  - acciones disponibles:
    - add pinch
    - remove pinch
    - reject line
    - publish
  - el preview ahora muestra:
    - pinch markers
    - handles visibles de `Width` / `Height`
    - preview runtime-only de compresi?n

## Why it matters

Ahora la codebase deja de mentir sobre qu? se est? curando.

Antes:
- la UI iba hacia pinches
- el core segu?a pensando en walls curadas y grupos

Ahora:
- el dato persistido refleja exactamente la intenci?n del producto
- el futuro fit engine va a leer una estructura mucho m?s directa

## Verification

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --no-restore` -> **16/16**
- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --no-restore` -> **17/17**
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --no-restore` -> **11/11**

## Tradeoff

### Pros

- branch mucho m?s limpia
- menos conceptos accidentales
- UI mucho m?s enfocada
- base m?s honesta para Loop 2

### Contras

- es una refactor agresiva y destructiva
- ya no queda el camino intermedio de curated walls en esta branch
- si despu?s queremos reintroducir sem?ntica arquitect?nica rica, habr? que dise?arla arriba de este n?cleo
