# Loop 2C Preview Render Composition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract preview render composition and layer orchestration out of `FloorPlanPreviewControl` into a focused render scene + composer without changing existing Loop 1 CAD-faithful preview behavior.

**Architecture:** Keep `FloorPlanPreviewControl` as the Avalonia shell that owns state, helper methods, and scene preparation. Introduce one immutable `PreviewRenderScene` snapshot plus one `PreviewRenderComposer` that owns layer order, base-geometry rendering, and the curated-vs-detected artifact branch. Do not mix this slice with render-helper rewrites or `FloorPlanReviewViewModel`.

**Tech Stack:** C# / .NET 10, Avalonia `DrawingContext`, xUnit Desktop tests, PowerShell verification, existing preview renderers under `src/FloorplanFit.Desktop/Controls/Preview/*`.

---

## File Structure

- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs`
  - Purpose: immutable render-only snapshot of prepared preview data.
- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs`
  - Purpose: own preview layer order, base geometry strokes, and artifact-branch orchestration.
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
  - Purpose: build the render scene and delegate composition to the composer.
- Create: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs`
  - Purpose: pin ordering and branch-selection behavior with TDD before integration.
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
  - Purpose: preserve shell-facing regression checks after render delegation.
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 2C preview render composition.md`
  - Purpose: durable implementation note for this slice.
- Modify: `obsidian-vault/Current State.md`
  - Purpose: record that Loop 2C extracted render composition while scene-preparation helpers still live in the shell.

---

## Tasks

### Task 1: Introduce composer-facing tests for base-path ordering and artifact branch selection

**Files:**
- Create: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs`

- [ ] **Step 1: Write the failing composer tests**

Add tests like these to `tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs`:

```csharp
using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls.Preview;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class PreviewRenderComposerTests
{
    [Fact]
    public void ResolveOrderedBasePaths_excludes_detected_artifact_paths_and_keeps_highlighted_path_last()
    {
        var wallPathId = Guid.NewGuid();
        var highlightedWallPathId = Guid.NewGuid();
        var openingPathId = Guid.NewGuid();
        var componentPathId = Guid.NewGuid();

        GeometryPathDto[] geometry =
        [
            new(highlightedWallPathId, false, [new GeometrySegmentDto(highlightedWallPathId, 1, 0m, 10m, 50m, 10m)]),
            new(openingPathId, false, [new GeometrySegmentDto(openingPathId, 1, 0m, 20m, 50m, 20m)]),
            new(componentPathId, false, [new GeometrySegmentDto(componentPathId, 1, 0m, 30m, 50m, 30m)]),
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 50m, 0m)])
        ];
        var artifactIndex = PreviewArtifactGeometryIndex.Create(
            [new OpeningCandidateDto(Guid.NewGuid(), "LINE:1", "DOORS", "Door", "LINE", openingPathId, 0.95m, null, 1)],
            [new FixedPlanComponentDto(Guid.NewGuid(), "INSERT:1", "FIXTURES", "Toilet", "INSERT", "TOILET1", [componentPathId], 0.95m, null, 1)]);

        var ordered = PreviewRenderComposer.ResolveOrderedBasePaths(
            geometry,
            artifactIndex,
            highlightGeometryPathId: highlightedWallPathId);

        Assert.Equal([wallPathId, highlightedWallPathId], ordered.Select(path => path.Id).ToArray());
    }

    [Fact]
    public void ResolveArtifactLayerMode_prefers_curated_branch_when_curated_artifacts_exist()
    {
        var scene = CreateScene(
            curatedPlanArtifacts:
            [
                new CuratedPlanArtifactDto(
                    Guid.NewGuid(),
                    "OpeningCandidate",
                    Guid.NewGuid(),
                    "opening",
                    "opening",
                    "#FF455668",
                    0m,
                    0m,
                    [Guid.NewGuid()],
                    null,
                    1)
            ]);

        Assert.Equal(
            PreviewRenderComposer.PreviewArtifactLayerMode.Curated,
            PreviewRenderComposer.ResolveArtifactLayerMode(scene));
    }
}
```

- [ ] **Step 2: Run the targeted test to verify RED**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewRenderComposerTests" --artifacts-path .\.artifacts-test\desktop-loop2c-task1-red
```

Expected: FAIL because `PreviewRenderScene` / `PreviewRenderComposer` do not exist yet.

---

### Task 2: Implement the render scene and composer minimally and turn the tests GREEN

**Files:**
- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs`
- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs`

- [ ] **Step 1: Write the minimal render scene snapshot**

Create `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs` with a shape like:

```csharp
using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;

namespace FloorplanFit.Desktop.Controls.Preview;

internal sealed record PreviewRenderScene(
    Rect Bounds,
    PinchAxisTag? AxisTag,
    bool IsPinchPlacementArmed,
    FloorPlanPreviewGeometry.PreviewViewport? Viewport,
    IReadOnlyList<GeometryPathDto> PreviewGeometry,
    IReadOnlyList<RoomLabelDto> RoomLabels,
    IReadOnlyList<OpeningLabelDto> OpeningLabels,
    IReadOnlyList<DimensionDto> Dimensions,
    PreviewArtifactGeometryIndex ArtifactIndex,
    IReadOnlyList<OpeningCandidateDto>? OpeningCandidates,
    IReadOnlyList<FixedPlanComponentDto>? FixedPlanComponents,
    IReadOnlyList<ProtectedDetailAssemblyDto>? ProtectedDetailAssemblies,
    IReadOnlyList<CuratedPlanArtifactDto>? CuratedPlanArtifacts,
    IReadOnlyList<PinchMarkerDto>? PinchMarkers,
    Guid? HighlightGeometryPathId,
    Guid? HighlightRoomLabelId,
    Guid? HighlightOpeningLabelId,
    Guid? HighlightDimensionId,
    Guid? PreviewPinchGroupId,
    string? PreviewAxisTag,
    FloorPlanPreviewControl.DimensionHandleKind? ActiveDimensionHandleKind)
{
    public bool HasGeometry => PreviewGeometry.Count > 0;

    public bool HasCuratedArtifacts => CuratedPlanArtifacts is { Count: > 0 };
}
```

- [ ] **Step 2: Write the minimal composer implementation**

Create `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs` with a shape like:

```csharp
using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewRenderComposer
{
    internal enum PreviewArtifactLayerMode
    {
        Detected,
        Curated
    }

    public static void Render(DrawingContext context, PreviewRenderScene scene)
    {
        PreviewWorkspaceRenderer.Render(context, scene.Bounds, scene.Viewport);
        context.DrawRectangle(new Pen(Brushes.Gainsboro, 1), scene.Bounds.Deflate(0.5));

        if (scene.AxisTag is { } axisTag)
        {
            CompressionHandlePreviewLayerRenderer.Render(context, scene.Bounds, axisTag, scene.IsPinchPlacementArmed);
        }

        if (!scene.HasGeometry || scene.Viewport is not { } viewport)
        {
            return;
        }

        foreach (var path in ResolveOrderedBasePaths(scene.PreviewGeometry, scene.ArtifactIndex, scene.HighlightGeometryPathId))
        {
            var style = FloorPlanPreviewGeometry.GetPathStyle(path.Id, scene.HighlightGeometryPathId);
            var pen = new Pen(new SolidColorBrush(style.Color), style.Thickness);

            foreach (var segment in path.Segments)
            {
                context.DrawLine(
                    pen,
                    viewport.Project(segment.StartX, segment.StartY),
                    viewport.Project(segment.EndX, segment.EndY));
            }
        }

        if (ResolveArtifactLayerMode(scene) == PreviewArtifactLayerMode.Curated)
        {
            CuratedArtifactPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.PreviewGeometry,
                scene.CuratedPlanArtifacts,
                scene.HighlightGeometryPathId);
        }
        else
        {
            OpeningPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.PreviewGeometry,
                scene.OpeningCandidates,
                scene.ArtifactIndex.OpeningGeometryPathIds,
                scene.HighlightGeometryPathId);
            FixedPlanComponentPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.PreviewGeometry,
                scene.FixedPlanComponents,
                scene.ArtifactIndex.FixedPlanComponentGeometryPathIds,
                scene.HighlightGeometryPathId);
            ProtectedDetailPreviewLayerRenderer.Render(
                context,
                viewport,
                scene.PreviewGeometry,
                scene.ProtectedDetailAssemblies,
                scene.ArtifactIndex.ProtectedDetailGeometryPathIds,
                scene.HighlightGeometryPathId);
        }

        DimensionPreviewLayerRenderer.Render(context, viewport, scene.Dimensions, scene.HighlightDimensionId);
        CadTextPreviewLayerRenderer.RenderRoomLabels(context, viewport, scene.RoomLabels, scene.HighlightRoomLabelId);
        CadTextPreviewLayerRenderer.RenderOpeningLabels(context, viewport, scene.OpeningLabels, scene.HighlightOpeningLabelId);
        CadTextPreviewLayerRenderer.RenderDimensions(context, viewport, scene.Dimensions, scene.HighlightDimensionId);
        PinchMarkerPreviewLayerRenderer.Render(
            context,
            viewport,
            scene.PreviewGeometry,
            scene.PinchMarkers,
            scene.PreviewPinchGroupId,
            scene.PreviewAxisTag);
        DimensionPreviewLayerRenderer.RenderHandles(
            context,
            viewport,
            scene.Dimensions,
            scene.HighlightDimensionId,
            scene.ActiveDimensionHandleKind);
    }

    internal static IReadOnlyList<GeometryPathDto> ResolveOrderedBasePaths(
        IReadOnlyList<GeometryPathDto> previewGeometry,
        PreviewArtifactGeometryIndex artifactIndex,
        Guid? highlightGeometryPathId)
    {
        return previewGeometry
            .Where(path => !artifactIndex.OpeningGeometryPathIds.Contains(path.Id))
            .Where(path => !artifactIndex.FixedPlanComponentGeometryPathIds.Contains(path.Id))
            .Where(path => !artifactIndex.ProtectedDetailGeometryPathIds.Contains(path.Id))
            .OrderBy(path => FloorPlanPreviewGeometry.GetPathStyle(path.Id, highlightGeometryPathId).IsHighlighted)
            .ToArray();
    }

    internal static PreviewArtifactLayerMode ResolveArtifactLayerMode(PreviewRenderScene scene)
    {
        return scene.HasCuratedArtifacts
            ? PreviewArtifactLayerMode.Curated
            : PreviewArtifactLayerMode.Detected;
    }
}
```

- [ ] **Step 3: Add the missing helper in the test file**

Extend `PreviewRenderComposerTests.cs` with a helper like:

```csharp
private static PreviewRenderScene CreateScene(IReadOnlyList<CuratedPlanArtifactDto>? curatedPlanArtifacts = null)
{
    return new PreviewRenderScene(
        Bounds: new Rect(0, 0, 800, 600),
        AxisTag: null,
        IsPinchPlacementArmed: false,
        Viewport: null,
        PreviewGeometry: [],
        RoomLabels: [],
        OpeningLabels: [],
        Dimensions: [],
        ArtifactIndex: PreviewArtifactGeometryIndex.Create(openingCandidates: null, fixedPlanComponents: null),
        OpeningCandidates: [],
        FixedPlanComponents: [],
        ProtectedDetailAssemblies: [],
        CuratedPlanArtifacts: curatedPlanArtifacts,
        PinchMarkers: [],
        HighlightGeometryPathId: null,
        HighlightRoomLabelId: null,
        HighlightOpeningLabelId: null,
        HighlightDimensionId: null,
        PreviewPinchGroupId: null,
        PreviewAxisTag: null,
        ActiveDimensionHandleKind: null);
}
```

- [ ] **Step 4: Run the targeted tests again to verify GREEN**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewRenderComposerTests" --artifacts-path .\.artifacts-test\desktop-loop2c-task2-green
```

Expected: PASS.

- [ ] **Step 5: Commit the scene/composer seam**

```bash
git add -- 'tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs'
git commit -m "refactor: add preview render composer"
```

---

### Task 3: Delegate `FloorPlanPreviewControl.Render(...)` to the composer

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`

- [ ] **Step 1: Write the failing shell-facing regression test**

Add a source-facing assertion to `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`:

```csharp
[Fact]
public void FloorPlanPreviewControl_sources_preview_render_through_the_composer()
{
    var controlPath = Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "FloorplanFit.Desktop", "Controls", "FloorPlanPreviewControl.cs");
    var source = File.ReadAllText(controlPath);

    Assert.Contains("PreviewRenderComposer.Render(context, scene);", source, StringComparison.Ordinal);
    Assert.DoesNotContain("context.DrawLine(", source, StringComparison.Ordinal);
    Assert.DoesNotContain("CuratedArtifactPreviewLayerRenderer.Render(", source, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run the focused control test to verify RED**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-loop2c-task3-red
```

Expected: FAIL because `Render(...)` still composes layers inline in the control.

- [ ] **Step 3: Delegate render composition from the control**

Refactor `FloorPlanPreviewControl.cs` so `Render(...)` becomes a thin shell:

```csharp
public override void Render(DrawingContext context)
{
    base.Render(context);

    var bounds = GetLocalRenderBounds(Bounds);
    var axisTag = ParseAxisTag();
    var viewport = GetPreviewViewport(axisTag);
    var scene = BuildRenderScene(bounds, axisTag, viewport);

    PreviewRenderComposer.Render(context, scene);
}
```

Add a helper like:

```csharp
private PreviewRenderScene BuildRenderScene(
    Rect bounds,
    PinchAxisTag? axisTag,
    FloorPlanPreviewGeometry.PreviewViewport? viewport)
{
    var previewGeometry = BuildPreviewGeometry(axisTag);
    previewGeometry = ApplyActiveArtifactMoveToGeometry(previewGeometry);
    var roomLabels = BuildRenderedRoomLabels();
    var openingLabels = BuildRenderedOpeningLabels();
    var dimensions = BuildRenderedDimensions();
    var artifactIndex = CuratedPlanArtifacts is { Count: > 0 }
        ? PreviewArtifactGeometryIndex.Create(CuratedPlanArtifacts)
        : PreviewArtifactGeometryIndex.Create(OpeningCandidates, FixedPlanComponents, ProtectedDetailAssemblies);

    return new PreviewRenderScene(
        Bounds: bounds,
        AxisTag: axisTag,
        IsPinchPlacementArmed: IsPinchPlacementArmed,
        Viewport: viewport,
        PreviewGeometry: previewGeometry,
        RoomLabels: roomLabels,
        OpeningLabels: openingLabels,
        Dimensions: dimensions,
        ArtifactIndex: artifactIndex,
        OpeningCandidates: OpeningCandidates,
        FixedPlanComponents: FixedPlanComponents,
        ProtectedDetailAssemblies: ProtectedDetailAssemblies,
        CuratedPlanArtifacts: CuratedPlanArtifacts,
        PinchMarkers: PinchMarkers,
        HighlightGeometryPathId: HighlightGeometryPathId,
        HighlightRoomLabelId: HighlightRoomLabelId,
        HighlightOpeningLabelId: HighlightOpeningLabelId,
        HighlightDimensionId: HighlightDimensionId,
        PreviewPinchGroupId: PreviewPinchGroupId,
        PreviewAxisTag: PreviewAxisTag,
        ActiveDimensionHandleKind: activeDimensionEdit?.HandleKind);
}
```

- [ ] **Step 4: Run the focused control/composer tests again to verify GREEN**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~PreviewRenderComposerTests" --artifacts-path .\.artifacts-test\desktop-loop2c-task3-green
```

Expected: PASS.

- [ ] **Step 5: Commit the control delegation slice**

```bash
git add -- 'tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs' 'tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs' 'src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs'
git commit -m "refactor: delegate preview render composition"
```

---

### Task 4: Refresh docs, verify the Desktop slice, and close Loop 2C

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 2C preview render composition.md`
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs` (only if the final shell assertions need adjustment)

- [ ] **Step 1: Run focused preview verification**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewRenderComposerTests|FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~PreviewInteractionCoordinatorTests|FullyQualifiedName~PreviewCollectionObserverHubTests" --artifacts-path .\.artifacts-test\desktop-loop2c-final-focused
```

Expected: PASS.

- [ ] **Step 2: Run the full Desktop suite**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop2c-full
```

Expected: PASS.

- [ ] **Step 3: Write the implementation note and update Current State**

Create `obsidian-vault/Implementation/2026-05-12 - Loop 2C preview render composition.md` with content like:

```md
---
type: Implementation
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - desktop
  - preview
  - render
  - modularization
  - loop-2
---

# Loop 2C preview render composition

## What changed

Se extrajo la composicion de layers del preview a `PreviewRenderComposer`, usando una snapshot `PreviewRenderScene` para que `FloorPlanPreviewControl` deje de mezclar shell Avalonia con layer ordering y branch render logic.

## Why

Despues de Loop 2A y Loop 2B, el hotspot real que quedaba en el preview shell era `Render(...)`. Este slice limpia el orden de layers sin reescribir todavia los helpers de scene preparation.

## Where

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`

## Learned

- La frontera correcta para este slice es composer + scene snapshot, no una reescritura completa de render helpers.
- Los helpers de scene preparation pueden quedarse en el control por ahora sin contaminar el ownership del layer order.
```

Then update `obsidian-vault/Current State.md` so it records that:

- Loop 2C extracted preview render composition
- `FloorPlanPreviewControl` now delegates layer ordering to `PreviewRenderComposer`
- scene preparation still remains in the control for now
- Loop 3 becomes the next sane move

- [ ] **Step 4: Run diff hygiene and commit the loop**

Run:

```powershell
git diff --check
git diff --stat
git status --short
```

Expected:

- `git diff --check` prints nothing
- the diff stays within the composer, scene, preview control, tests, and the two Obsidian notes

Then commit:

```bash
git add -- 'tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs' 'tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs' 'src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs' 'obsidian-vault/Current State.md' 'obsidian-vault/Implementation/2026-05-12 - Loop 2C preview render composition.md'
git commit -m "refactor: extract preview render composition"
```

---

## Self-review

- This plan keeps **scene-preparation helper extraction out of scope** on purpose.
- This plan keeps **`FloorPlanReviewViewModel` out of scope** on purpose.
- This plan reuses the current preview renderers instead of rewriting them.
- This plan targets a real verified hotspot: render-layer orchestration, not interaction or observer wiring.
