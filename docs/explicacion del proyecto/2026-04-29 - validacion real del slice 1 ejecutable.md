# 2026-04-29 - validacion real del slice 1 ejecutable

## Objetivo

Cerrar la validacion local del Slice 1 ejecutable sobre esta maquina:

- tests de Application
- tests de Infrastructure
- build de Desktop

## Evidencia ejecutada

> **Nota de sincronizacion (2026-04-29):** este documento refleja el estado final del dia, despues del fix de reimport, la hidratacion de Library desde SQLite y la resolucion de `AVLN3001`.

### Application

Comando:

```powershell
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --verbosity minimal
```

Resultado:

- **4/4 passing**

### Infrastructure

Comando:

```powershell
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --verbosity minimal
```

Resultado:

- **6/6 passing**

### Desktop

Comando:

```powershell
dotnet build src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj -p:OutDir=bin\\Debug\\net10.0-verify\\ --verbosity minimal
```

Resultado:

- **build succeeded**
- **0 warnings / 0 errors**

### Smoke test manual

Resultado:

- la app abre
- el import de `SANTA-BARBARA.dxf` funciona
- el workspace se crea bajo `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace`

## Problemas reales encontrados y corregidos

1. Faltaba `global using Xunit;` en ambos proyectos de tests.
2. El test del gateway DXF dependia del bbox del JSON legacy y no del DXF raw real.
3. El test de integracion SQLite dejaba `app.db` bloqueado hasta limpiar pools de `SqliteConnection`.
4. `App.axaml.cs` tenia colision entre `Avalonia.Application` y el namespace `FloorplanFit.Application`.
5. `MainWindow.axaml` necesitaba `x:DataType` para compiled bindings de Avalonia.

## Bloqueo de entorno y resolucion

Antes de la validacion final, Windows Smart App Control estaba bloqueando:

- `FloorplanFit.Infrastructure.Tests.dll`
- `Avalonia.Build.Tasks.dll`

Eso se resolvio desactivando temporalmente Smart App Control en esta maquina.

## Verdad actual

El Slice 1 ejecutable ya no esta solo authored: ahora tambien queda **validado localmente** en esta maquina a nivel de:

- Application
- Infrastructure
- Desktop compile

`AVLN3001` ya no es un abierto: quedo resuelto en el build fresco de verificacion.

Lo que sigue abierto a nivel manual es revalidar la rehidratacion de Library desde el launcher correcto:

- `scripts/dev-desktop.bat`
- `Floorplan Fit Dev Watch.lnk`
