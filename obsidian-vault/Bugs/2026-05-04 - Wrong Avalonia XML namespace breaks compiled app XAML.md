---
project: floorplan-fit
type: bug
date: 2026-05-04
status: fixed
tags:
  - desktop
  - avalonia
  - xaml
  - runtime
related:
  - "[[Current State]]"
  - "[[Implementation/2026-05-04 - Pinch curation preview prototype]]"
---

# Wrong Avalonia XML namespace breaks compiled app XAML

## Symptom

`dotnet watch run --project src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj` llegaba a compilar, pero al arrancar explotaba con:

`Avalonia.Markup.Xaml.XamlLoadException: No precompiled XAML found for FloorplanFit.Desktop.App`

El stack trace apuntaba a:

- `src/FloorplanFit.Desktop/App.axaml.cs`
- `App.Initialize()`

## Root cause

El problema NO estaba en `App.axaml`.

La causa real era que `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml` hab?a quedado con el namespace ra?z equivocado:

- incorrecto: `https://github.com/avalonia`
- correcto: `https://github.com/avaloniaui`

Ese error romp?a la emisi?n esperada de XAML compilado en el assembly Desktop y la app terminaba fallando en la primera carga XAML, que justo era `App.Initialize()`.

## Fix

Se corrigi? el root namespace de `ReviewFloorPlanWindow.axaml` a:

`<Window xmlns="https://github.com/avaloniaui" ... >`

Adem?s se agreg? un test de regresi?n:

- `tests/FloorplanFit.Desktop.Tests/Layout/AppXamlInitializationTests.cs`

## Verification

- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --filter FullyQualifiedName~AppXamlInitializationTests --no-restore` -> **1/1**
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --no-restore` -> **15/15**

## Learned

Cuando Avalonia dice que falta XAML precompilado en `App`, no necesariamente significa que `App.axaml` est? mal. Puede ser drift o invalidez en OTRO `.axaml` del mismo assembly.
