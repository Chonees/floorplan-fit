---
type: implementation
date: 2026-04-29
status: active
---

# Validation Results and App Control Blockers

## What was validated

### Application tests

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --verbosity minimal`
- resultado final: **4/4 passing**

### Infrastructure tests

- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --verbosity minimal`
- resultado final: **6/6 passing**

### Desktop build

- build historico validado antes del ultimo fix:
  - `dotnet build src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj --verbosity minimal`
  - resultado: **build succeeded**
- build fresco despues de hydration + AVLN3001 fix:
  - `dotnet build src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj -p:OutDir=bin\Debug\net10.0-verify\ --verbosity minimal`
  - resultado: **build succeeded**
  - resultado: **0 warnings / 0 errors**

### Desktop manual smoke test

- corrida manual realizada el **2026-04-29**
- la app abre
- el import de `SANTA-BARBARA.dxf` funciona
- el workspace se crea bajo `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace`

## Fixes applied during validation

Durante la validacion aparecieron y se corrigieron estas costuras:

- faltaba `global using Xunit;` en ambos proyectos de tests
- el test del gateway DXF estaba atado al bbox del JSON legacy y no a una validacion sana del raw DXF
- el test de integracion SQLite necesitaba limpiar pools de `SqliteConnection` para liberar `app.db` al borrar el workspace temporal
- `App.axaml.cs` tenia colision de nombre entre `Avalonia.Application` y el namespace `FloorplanFit.Application`
- `MainWindow.axaml` necesitaba `x:DataType` explicito para los compiled bindings de Avalonia
- el bug de reimport por sufijo tecnico se corrigio preservando la identidad logica desde `request.FilePath`
- la hidratacion de Library se agrego mediante query formal + reader SQLite
- `AVLN3001` se resolvio moviendo el `DataContext` de `MainWindow` fuera del constructor inyectado

## Environment blocker and resolution

### Initial blocker

El entorno frenaba la validacion por una politica de Windows / Application Control:

- `0x800711C7`
- `An Application Control policy has blocked this file`

Eso afectaba:

- `FloorplanFit.Infrastructure.Tests.dll`
- `Avalonia.Build.Tasks.dll`

### Resolution

El usuario desactivo temporalmente **Smart App Control** en Windows.

Despues de eso:

- los tests de Infrastructure pasaron
- Avalonia pudo ejecutar sus build tasks
- el siguiente bloqueo ya fue de codigo/XAML real y no de politica del SO

## Current truth

La arquitectura del slice ya no esta frenada por falta de SDK ni por Application Control.

El estado real ahora es:

- **Application** validado
- **Infrastructure** validado
- **Desktop** compila
- si la app esta abierta, el build normal de Desktop puede fallar por lock de `.dll`; por eso la verificacion fresca se hizo con `OutDir` alternativo
- **Desktop smoke test** validado
- el bug de reimport por sufijo tecnico quedo cubierto por unit test e integration test
- la hidratacion de Library desde SQLite queda implementada y verificada por tests
- `AVLN3001` ya no aparece en el build de verificacion
