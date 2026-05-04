# Review Preview Layout and Selection Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hacer que la review UI de Loop 1 centre mejor el preview, actualice el highlight en tiempo real, permita click-to-select sobre el plano y tenga listas con scroll real.

**Architecture:** El fix vive en Desktop. La lógica pura de proyección del preview se extrae para poder testearla con TDD; después el control visual la consume y la ventana se recompone con `Grid` para sacar alturas rígidas.

**Tech Stack:** Avalonia UI, CommunityToolkit.Mvvm, xUnit

---

### Task 1: Cubrir con tests la proyección del preview

**Files:**
- Create: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`

- [ ] Escribir tests RED para:
  - centrar correctamente un conjunto de segmentos en bounds disponibles
  - devolver un estilo destacado para el path seleccionado
- [ ] Correr solo esos tests y confirmar RED
- [ ] Implementar helper mínimo de geometría/estilo en Desktop
- [ ] Re-correr los tests y confirmar GREEN

### Task 2: Hacer que el preview reaccione al cambio de selección

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

- [ ] Verificar qué tests existentes cubren `HighlightGeometryPathId`
- [ ] Agregar test RED si falta uno que asegure cambio de highlight al seleccionar curated wall
- [ ] Declarar invalidación de render para props visuales del control
- [ ] Correr tests desktop relevantes y confirmar GREEN

### Task 3: Recomponer el layout de review

**Files:**
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`

- [ ] Reemplazar alturas rígidas problemáticas por layout con `Grid`
- [ ] Hacer que la columna central estire el preview correctamente
- [ ] Hacer que las listas laterales ocupen su región y scrolleen donde corresponde
- [ ] Revisar el diff para evitar cambios cosméticos innecesarios

### Task 4: Verificación final

**Files:**
- Modify: `obsidian-vault/Current State.md` (solo si el comportamiento cambia de forma relevante)
- Modify: `obsidian-vault/Implementation/2026-05-03 - Review preview layout and highlight fix.md` (crear si se implementa)

- [ ] Correr `dotnet test` filtrado de Desktop tests relevantes
- [ ] Revisar que el diff final toque solo Desktop + docs del cambio
- [ ] Documentar root cause + fix en Obsidian
