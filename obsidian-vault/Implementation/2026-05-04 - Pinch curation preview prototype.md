---
project: floorplan-fit
type: implementation
date: 2026-05-04
status: superseded
replaced_by: [[Implementation/2026-05-04 - Pinch native cleanup and minimal review UI]]
tags:
  - loop-1
  - desktop
  - curation
  - pinch
  - prototype
related:
  - "[[Current State]]"
  - "[[Inbox/2026-05-04 - Pinch zones for non-scaling floor plan compression]]"
  - "[[Implementation/2026-05-03 - Review preview layout and highlight fix]]"
---

# Pinch curation preview prototype

> **Superseded on 2026-05-04:** esta nota describe el prototipo intermedio con `PinchGroup` + soporte en `CuratedWall`. La verdad actual de la branch vive en [[Implementation/2026-05-04 - Pinch native cleanup and minimal review UI]] y en [[Current State]].

## Context

El problema nuevo de producto ya no era solo "aceptar o rechazar walls", sino dejar curado **donde se puede pellizcar el floor plan** para que m?s adelante, ante un site plan que no entra, el sistema sepa recortar solo lo m?nimo indispensable.

La simplificaci?n elegida para MVP fue:

- seguir authorando sobre la geometr?a detectada l?nea por l?nea
- agrupar pinches por eje `Width` / `Height`
- guardar en el curado la ubicaci?n relativa del pinch y su `MaxTrimMm`
- mostrar un preview de compresi?n al arrastrar desde el borde correspondiente

## What changed

- **Domain / Contracts**
  - se agregaron `PinchAxisTag`, `PinchGroup` y `PinchMarker`
  - el `FloorPlanReviewSessionDto` ahora expone `PinchGroups` y `PinchMarkers`

- **Application**
  - `OpenFloorPlanReviewSessionHandler` ahora auto-stagea `CuratedWall` desde los candidates no rechazados
  - se agregaron handlers para:
    - crear grupos de pinches
    - agregar pinch markers
    - remover pinch markers
    - sincronizar staged curated walls con candidates
  - rechazar un candidate ahora tambi?n limpia los pinch markers asociados

- **Infrastructure**
  - SQLite ahora persiste `pinch_groups` y `pinch_markers`
  - el review session reader hidrata grupos, markers y la geometr?a que necesitan
  - `SqliteSession.CommitAsync` reinicia la transacci?n activa para permitir varios `SaveChangesAsync` en una misma sesi?n sin reusar una transacci?n ya commiteada

- **Desktop**
  - la review window fue reducida a una UI pinch-first
  - el preview ahora soporta:
    - hit-testing con `PositionRatio` sobre la l?nea
    - render de pinch markers
    - drag desde borde para simular compresi?n por grupo activo
  - el ViewModel ahora maneja el estado de grupos, markers y pinch placement armado

## Why it matters

Esto mueve Loop 1 un paso m?s cerca del objetivo real de negocio:

> curar una vez el floor plan y reutilizarlo muchas veces, incluso cuando haya que hacerlo entrar en envelopes distintos sin escalarlo completo.

En vez de pedirle al fit futuro que adivine "d?nde recortar", el usuario deja esa intenci?n guardada desde el curado.

## Verification

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~SyncCuratedWallsFromCandidatesHandlerTests|FullyQualifiedName~AddPinchMarkerHandlerTests|FullyQualifiedName~RejectWallCandidateHandlerTests|FullyQualifiedName~OpenFloorPlanReviewSessionHandlerTests|FullyQualifiedName~GetFloorPlanReviewSessionHandlerTests" --no-restore` -> **6/6**
- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests|FullyQualifiedName~OpenFloorPlanReviewSessionIntegrationTests" --no-restore` -> **2/2**
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~FloorPlanPreviewGeometryTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests" --no-restore` -> **11/11**

## Tradeoff

Se eligi? una persistencia y UX **line-based + grouped pinches** en vez de modelar desde ya walls arquitect?nicas completas con spans y junction graph rico.

- **Pros**: MVP m?s r?pido, reutiliza la review UI actual, authoring directo y solver futuro m?s simple
- **Contras**: la consistencia entre las dos caras de una pared doble queda m?s del lado del usuario que del dominio; seguramente haga falta refinarlo si el prototipo demuestra valor
