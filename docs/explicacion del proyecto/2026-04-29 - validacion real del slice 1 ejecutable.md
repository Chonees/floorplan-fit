# 2026-04-29 - validacion real del slice 1 ejecutable

> **Nota de actualizaci?n (2026-05-02):** este documento queda como snapshot hist?rico del 29/04. Los conteos `4/4`, `6/6` y el paso de `dotnet build` pertenecen a ese corte. La verificaci?n vigente hoy est? en `obsidian-vault/Current State.md` y usa `dotnet test` por la regla actual del repo.

## Objetivo

Cerrar la validaci?n local del Slice 1 ejecutable sobre esta m?quina, en el estado real de ese d?a:

- tests de Application
- tests de Infrastructure
- build de Desktop

## Evidencia ejecutada

> **Nota de sincronizaci?n (2026-04-29):** este documento refleja el estado final de ese d?a, despu?s del fix de reimport, la hidrataci?n de Library desde SQLite y la resoluci?n de `AVLN3001`.

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
dotnet build src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj -p:OutDir=bin\Debug\net10.0-verify\ --verbosity minimal
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
2. El test del gateway DXF depend?a del bbox del JSON legacy y no del DXF raw real.
3. El test de integraci?n SQLite dejaba `app.db` bloqueado hasta limpiar pools de `SqliteConnection`.
4. `App.axaml.cs` ten?a colisi?n entre `Avalonia.Application` y el namespace `FloorplanFit.Application`.
5. `MainWindow.axaml` necesitaba `x:DataType` para compiled bindings de Avalonia.

## Bloqueo de entorno y resoluci?n

Antes de la validaci?n final, Windows Smart App Control estaba bloqueando:

- `FloorplanFit.Infrastructure.Tests.dll`
- `Avalonia.Build.Tasks.dll`

Eso se resolvi? desactivando temporalmente Smart App Control en esta m?quina.

## Verdad de ese momento

El Slice 1 ejecutable ya no estaba solo authored: para ese corte tambi?n quedaba **validado localmente** en esta m?quina a nivel de:

- Application
- Infrastructure
- Desktop compile

`AVLN3001` ya no era un abierto: hab?a quedado resuelto en el build fresco de verificaci?n.

## Verdad actual (2026-05-02)

- La verificaci?n vigente ya no usa `dotnet build` como paso normal de trabajo por la regla actual del repo `never build after changes`.
- El import actual auto-dispara extracci?n y deja el item en `Extracted`.
- La Library s? hidrata desde SQLite al iniciar.
- El primer `Open Review` despu?s de extraction ya no crashea.
- El green vigente es: Application **16/16**, Infrastructure **17/17**, Desktop **4/4**.
