---
type: implementation
date: 2026-05-02
status: active
---

# Fixed first-open review transaction crash

## Problem

El primer `Open Review` despues de extraction podia crear el draft y luego crashear en el read-model de review por reutilizar una transaccion SQLite ya commiteada.

## Chosen fix

No se toco la semantica global de `SqliteSession` para evitar side effects de locking en tests y runtime.

En cambio, se corrigio el boundary correcto:

- los readers SQLite de solo lectura ahora crean comandos sin `command.Transaction`

Archivos tocados:

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanExtractionSourceReader.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Review/OpenFloorPlanReviewSessionIntegrationTests.cs`

## Why this is the right layer

Porque el problema no era "falta una transaccion nueva", sino que un read-model de solo lectura no deberia depender de una transaccion scoped ya cerrada para poder hidratar UI.

## Verification

- regression test nuevo verde para el primer open de review
- Application **16/16**
- Infrastructure **17/17**
- Desktop **4/4**
