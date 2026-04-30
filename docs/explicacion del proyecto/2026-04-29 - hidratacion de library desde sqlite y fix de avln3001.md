# 2026-04-29 - hidratacion de library desde sqlite y fix de AVLN3001

## Problema

Quedaban dos pendientes del Slice 1:

1. la app persistia imports pero la Library no se repoblaba al reabrir
2. Avalonia emitia la advertencia `AVLN3001` por la forma en que `MainWindow` se construia

## Solucion

### Library

Se agrego una query formal en Application y un reader SQLite en Infrastructure.

Eso permite que `LibraryViewModel.LoadAsync()` vuelva a cargar la lista real desde DB al iniciar.

### AVLN3001

Se saco la inyeccion del `LibraryViewModel` por constructor de `MainWindow`.

Ahora:

- `MainWindow` tiene constructor publico sin parametros
- `App.axaml.cs` crea la ventana
- `App.axaml.cs` asigna `DataContext`

## Archivos tocados

- `src/FloorplanFit.Application/Abstractions/IFloorPlanLibraryReader.cs`
- `src/FloorplanFit.Application/FloorPlans/Library/GetFloorPlanLibraryHandler.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `src/FloorplanFit.Desktop/App.axaml.cs`
- tests nuevos de Application e Infrastructure

## Verificacion

- `Application.Tests` -> **4/4 passing**
- `Infrastructure.Tests` -> **6/6 passing**
- `Desktop build verify` -> **0 warnings / 0 errors**

## Siguiente validacion util

Cerrar y reabrir la app para confirmar visualmente que la Library ya reaparece desde SQLite al iniciar.
