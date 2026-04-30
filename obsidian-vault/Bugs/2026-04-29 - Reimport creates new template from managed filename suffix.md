---
type: bug
status: fixed
date: 2026-04-29
fixed_at: 2026-04-29
---

# Reimport creates a new template from managed filename suffix

## What happened

Si se importaba el mismo DXF mas de una vez, el sistema podia terminar creando un template nuevo con codigo derivado del nombre de la copia gestionada, por ejemplo:

- `santa-barbara`
- `santa-barbara-2`

En vez de crear una nueva version del mismo template.

## Root cause

`ManagedFileStorage` renombra la copia fisica cuando hay colision (`-2`, `-3`, etc.).
Despues `IxMiliaDxfGateway` derivaba `SuggestedName` y `OriginalFileName` desde esa copia gestionada.
Finalmente `ImportFloorPlanHandler` usaba esos valores para:

- normalizar el code del template
- nombrar el template
- guardar `original_file_name`

O sea: la identidad de dominio quedaba acoplada al nombre fisico de storage.

## Fix applied

El fix se hizo en `ImportFloorPlanHandler`.

Ahora el handler preserva la identidad logica desde la ruta fuente pedida por el usuario:

- `sourceFileName = Path.GetFileName(request.FilePath)`
- `sourceSuggestedName = Path.GetFileNameWithoutExtension(request.FilePath)`

Y usa esos valores para:

- `ImportedDocument.OriginalFileName`
- `FloorPlanCodeNormalizer.Normalize(...)`
- `FloorPlanTemplate.Name`

La copia gestionada puede seguir llamandose `SANTA-BARBARA-2.dxf`, pero el template sigue siendo `santa-barbara` y el segundo import pasa a ser `version 2` del mismo template.

## Verification

### Application

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --verbosity minimal`
- resultado final del dia: **4/4 passing**

### Infrastructure

- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --verbosity minimal`
- resultado final del dia: **6/6 passing**

Se agregaron tests especificos para el caso de reimport:

- unit test en `ImportFloorPlanHandlerTests`
- integration test real en `ImportFloorPlanIntegrationTests`

## Learned

La identidad logica del floor plan no puede depender del nombre fisico de la copia gestionada.
Storage y dominio son dos preocupaciones distintas.
