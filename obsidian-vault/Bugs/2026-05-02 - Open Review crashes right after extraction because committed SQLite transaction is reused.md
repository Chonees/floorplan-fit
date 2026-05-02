---
type: bug
date: 2026-05-02
status: resolved
---

# Open Review crashes right after extraction because committed SQLite transaction is reused

## Symptom

Si el usuario extrae walls y enseguida hace `Open Review`, la app puede crashear con:

- `System.InvalidOperationException: The transaction object is not associated with the same connection object as this command.`

## Verified root cause

El problema no esta en DXF ni en la extraccion en si. El problema esta en el ciclo write-then-read dentro del mismo scope de SQLite:

1. `OpenFloorPlanReviewSessionHandler` llama `StartOrResumeCurationHandler`
2. si no existe draft, `StartOrResumeCurationHandler` crea el draft y hace `unitOfWork.SaveChangesAsync()`
3. `SqliteUnitOfWork` delega a `SqliteSession.CommitAsync()`
4. `SqliteSession` commitea la transaccion actual, pero NO abre una transaccion nueva
5. en el mismo scope, `SqliteFloorPlanReviewSessionReader` crea comandos y les vuelve a asignar `session.Transaction`
6. esa transaccion ya fue commiteada y queda invalida para esos comandos, por eso SQLite tira la excepcion

## Why it only happens the first time

En la primera apertura despues de extraction normalmente todavia no existe draft, asi que hay commit y luego read en el mismo scope.

Cuando el usuario vuelve a entrar, el draft ya existe:

- `StartOrResumeCurationHandler` ya no commitea nada
- el read-model puede leer sin pegar contra esa transaccion ya cerrada

## Evidence

- Stack trace apunta a `SqliteFloorPlanReviewSessionReader.GetTemplateSummary()`
- El flujo previo pasa por `OpenFloorPlanReviewSessionHandler` y `StartOrResumeCurationHandler`
- `SqliteSession` mantiene una sola `Transaction` creada al abrir la sesion
- `CommitAsync()` la commitea pero no reemplaza esa transaccion por una nueva

## Fix

Se dejo `SqliteSession` con su semantica original de commit y se movio el fix al lugar correcto: los readers SQLite de solo lectura ya no setean `command.Transaction`.

Eso aplica a:

- `SqliteFloorPlanLibraryReader`
- `SqliteFloorPlanExtractionSourceReader`
- `SqliteFloorPlanReviewSessionReader`

## Verification

- test rojo agregado: `OpenFloorPlanReviewSessionIntegrationTests.HandleAsync_opens_review_after_creating_the_first_draft_in_the_same_sqlite_scope`
- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --no-restore` -> **16/16**
- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --no-restore` -> **17/17**
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --no-restore` -> **4/4**
