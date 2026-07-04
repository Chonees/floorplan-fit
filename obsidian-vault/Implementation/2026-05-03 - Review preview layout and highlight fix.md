---
project: floorplan-fit
type: implementation
date: 2026-05-03
tags:
  - review-ui
  - desktop
  - loop-1
related:
  - "[[Current State]]"
  - "[[Implementation/2026-05-02 - Fixed first-open review transaction crash]]"
---

# Review preview layout and highlight fix

## Context

En la review UI de Loop 1 hab?a tres problemas de UX que bloqueaban el test manual serio del curated draft:

1. el preview no ocupaba bien el panel central y daba sensaci?n de plano "flotando" sobre fondo oscuro
2. la selecci?n no comunicaba con claridad qu? l?nea se estaba curando
3. las listas quedaban r?gidas por alturas hardcodeadas y el usuario ve?a solo una cantidad limitada de items

Despu?s apareci? una necesidad UX todav?a m?s fuerte: poder tocar directamente una pared en el preview y que la UI seleccione esa l?nea o curated wall sin depender solamente de las listas laterales.

## What changed

- `ReviewFloorPlanWindow.axaml`
  - se reemplaz? layout r?gido con `StackPanel` por grids con filas `Auto/*`
  - el preview ahora ocupa todo el panel central y se apoya sobre fondo blanco
  - se agreg? `PreviewSelectionLabel` visible arriba del canvas
  - el control de preview ahora emite selecci?n por click y la ventana la delega al ViewModel
  - la lista de candidates y la lista de curated walls ahora usan scroll natural del `ListBox`
  - se eliminaron `Height="700"` del preview y `Height="180"` de curated walls

- `FloorPlanPreviewControl.cs`
  - ahora invalida render cuando cambia `HighlightGeometryPathId`
  - ahora tambi?n observa cambios de colecci?n sobre `GeometryPaths`
  - el path seleccionado se dibuja al final y con trazo m?s fuerte para que quede por encima del resto
  - ahora soporta hit-testing por click sobre segmentos proyectados y emite `GeometryPathClicked`

- `FloorPlanPreviewGeometry.cs`
  - centraliza la l?gica de viewport/proyecci?n para mantener el plano centrado con padding consistente
  - centraliza tambi?n el estilo del path resaltado vs. paths normales
  - agrega hit-testing geom?trico con tolerancia en p?xeles para detectar qu? path toc? el usuario

- `FloorPlanReviewViewModel.cs`
  - ahora expone `PreviewSelectionLabel`
  - cuando cambia la candidate seleccionada muestra `Previewing candidate: ...`
  - cuando cambia la curated wall seleccionada muestra `Previewing curated wall: ...`
  - agrega `SelectPreviewPath(...)` para sincronizar click sobre canvas con candidate y curated wall, priorizando la curated wall cuando existe

## Why it matters

Sin este ajuste, el usuario no sabe con seguridad qu? l?nea est? curando y el flujo `Extracted -> Open Review -> Curated Draft` pierde valor pr?ctico aunque el dominio y la persistencia est?n correctos.

Con click-to-select, el preview deja de ser solamente una visualizaci?n y pasa a ser una herramienta real de curado.

## Use cases unlocked

1. seleccionar `LINE:1` en la lista y ver instant?neamente esa geometr?a resaltada
2. tocar una l?nea directamente en el preview y que la lista seleccione el candidate correcto
3. tocar una wall ya aceptada y que la UI priorice la `CuratedWall` asociada
4. revisar floor plans con muchas lines sin quedar limitado por una altura fija artificial

## Verification

- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewGeometryTests|FullyQualifiedName~FloorPlanReviewViewModelTests" --no-restore` -> **8/8**
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --no-restore` -> **12/12**

## Tradeoff

Se eligi? una soluci?n estructural en XAML + control custom + helper de geometr?a, no un parche de estilos ni un tooltip superficial.

- **Pros**: layout el?stico, repaint confiable, mejor se?al visual, interacci?n directa sobre el preview y tests de regresi?n
- **Contras**: m?s c?digo Desktop y m?s responsabilidad geom?trica en la capa visual
