# 2026-04-29 - bugfix de versionado al reimportar el mismo dxf

## Problema

Reimportar el mismo DXF podia generar un template nuevo con sufijo tecnico (`santa-barbara-2`) porque el sistema estaba tomando la identidad del floor plan desde el nombre fisico de la copia gestionada.

## Solucion

Se desacoplo:

- identidad logica del floor plan
- nombre fisico de almacenamiento

La identidad ahora se preserva desde `request.FilePath` en `ImportFloorPlanHandler`.

## Archivos tocados

- `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Imports/ImportFloorPlanIntegrationTests.cs`

## Verificacion

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --verbosity minimal` -> **4/4 passing**
- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --verbosity minimal` -> **6/6 passing**
- `dotnet build src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj -p:OutDir=bin\\Debug\\net10.0-verify\\ --verbosity minimal` -> **build succeeded**

## Nota de contexto

Los conteos crecieron despues porque al mismo corte se agregaron:

- el caso de uso `GetFloorPlanLibraryHandler`
- el reader SQLite de Library
- tests nuevos para rehidratacion y lectura de Library

## Nota importante

El build normal de Desktop puede fallar si la app esta abierta, no por error de compilacion sino por lock de archivos `.dll` en `bin/Debug/net10.0`.
