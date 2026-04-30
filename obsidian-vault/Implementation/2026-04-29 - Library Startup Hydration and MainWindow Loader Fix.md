---
type: implementation
date: 2026-04-29
status: active
---

# Library Startup Hydration and MainWindow Loader Fix

## What changed

Se resolvieron dos pendientes del Slice 1 ejecutable:

1. la Library ahora puede hidratarse desde SQLite al iniciar
2. la advertencia `AVLN3001` de Avalonia desaparecio al desacoplar la ventana de la inyeccion por constructor

## Architecture

### Application

Se agrego una lectura formal de Library:

- `IFloorPlanLibraryReader`
- `GetFloorPlanLibraryHandler`

La UI ya no tiene que saber leer SQLite directamente.

### Infrastructure

Se agrego:

- `SqliteFloorPlanLibraryReader`

Este reader recompone la Library actual desde:

- `floorplan_templates`
- `floorplan_versions`
- `imported_documents`
- `measurement_contexts`

## Desktop

### `LibraryViewModel`

Ahora tiene:

- `LoadAsync()` para hidratar la lista al abrir
- `RefreshItemsAsync()` para repoblar la lista desde la query formal

Despues de importar, la UI ya no agrega solo el item en memoria: refresca la lista desde persistencia.

### `MainWindow`

Ahora expone constructor publico sin parametros.

La ventana:

- ya no recibe `LibraryViewModel` por constructor
- carga la Library al evento `Opened`
- usa `DataContext` para ejecutar `ImportAsync()` y `LoadAsync()`

### `App.axaml.cs`

Ahora crea la ventana y le asigna el `DataContext` desde DI.

Eso elimina la causa probable de `AVLN3001` y deja el wiring de Desktop mas sano.

## Verification

### Application

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --verbosity minimal`
- resultado: **4/4 passing**

### Infrastructure

- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --verbosity minimal`
- resultado: **6/6 passing**

### Desktop

- `dotnet build src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj -p:OutDir=bin\Debug\net10.0-verify\ --verbosity minimal`
- resultado: **build succeeded**
- resultado: **0 warnings / 0 errors**

## Honest current truth

A nivel tecnico:

- `AVLN3001` queda resuelto
- la hidratacion de Library desde SQLite queda implementada y verificada por tests/integracion

A nivel manual de UI:

- todavia conviene reabrir la app y confirmar visualmente que el item reaparece al inicio, aunque la costura tecnica ya quedo cubierta
