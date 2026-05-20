# Loop 2A Preview Interaction Extraction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract the preview interaction brain out of `FloorPlanPreviewControl` into a focused coordinator without changing current Loop 1 CAD-faithful review behavior.

**Architecture:** Keep `FloorPlanPreviewControl` as the Avalonia shell that owns properties, render composition, event publication, and collection observers. Create one new `PreviewInteractionCoordinator` that owns pointer press / move / release / wheel decisions plus interaction-state transitions. Do not mix this slice with observer cleanup or render decomposition.

**Tech Stack:** C# / .NET 10, Avalonia `Control` + pointer events, xUnit Desktop tests, PowerShell verification, existing preview helpers under `src/FloorplanFit.Desktop/Controls/Preview/*`.

---

## File Structure

- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs`
  - Purpose: own interaction decisions, state transitions, and explicit outcomes for pointer press / move / release / wheel.
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
  - Purpose: become a thinner Avalonia shell that delegates interaction decisions and applies outcomes.
- Create: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`
  - Purpose: pin the new interaction coordinator behavior with TDD before control delegation.
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
  - Purpose: preserve / adapt control-facing regression checks where helper ownership moves.
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 2A preview interaction extraction.md`
  - Purpose: durable implementation note for this loop slice.
- Modify: `obsidian-vault/Current State.md`
  - Purpose: record that Loop 2A extracted preview interaction coordination while leaving observer wiring and render decomposition for later.

---

## Tasks

### Task 1: Introduce the interaction coordinator seam for zoom / pan and middle-button pan start

**Files:**
- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Create: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`

- [ ] **Step 1: Write the failing coordinator tests for wheel zoom, pan drag, and middle-button pan start**

Add tests like these to `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`:

```csharp
using Avalonia;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class PreviewInteractionCoordinatorTests
{
    [Fact]
    public void CalculateWheelZoomFactor_zooms_in_and_out_with_bounds()
    {
        var zoomedIn = PreviewInteractionCoordinator.CalculateWheelZoomFactor(1d, wheelDeltaY: 1d);
        var zoomedOut = PreviewInteractionCoordinator.CalculateWheelZoomFactor(1d, wheelDeltaY: -1d);
        var clampedMinimum = PreviewInteractionCoordinator.CalculateWheelZoomFactor(0.2d, wheelDeltaY: -10d);
        var clampedMaximum = PreviewInteractionCoordinator.CalculateWheelZoomFactor(20d, wheelDeltaY: 10d);

        Assert.True(zoomedIn > 1d);
        Assert.True(zoomedOut < 1d);
        Assert.Equal(FloorPlanPreviewControl.MinimumUserZoomFactor, clampedMinimum);
        Assert.Equal(FloorPlanPreviewControl.MaximumUserZoomFactor, clampedMaximum);
    }

    [Fact]
    public void ResolvePanStateForDrag_keeps_zoom_and_adds_pointer_delta_to_pan_offset()
    {
        var startState = new FloorPlanPreviewControl.PreviewZoomState(
            ZoomFactor: 2.5d,
            PanOffset: new Vector(30d, -12d));

        var nextState = PreviewInteractionCoordinator.ResolvePanStateForDrag(
            startState,
            new Point(100d, 120d),
            new Point(145d, 90d));

        Assert.Equal(startState.ZoomFactor, nextState.ZoomFactor);
        Assert.Equal(new Vector(75d, -42d), nextState.PanOffset);
    }

    [Fact]
    public void HandlePointerPressed_prefers_middle_button_pan_before_any_left_button_hit_logic()
    {
        var request = PreviewInteractionCoordinator.CreatePointerPressedRequestForPan(
            pointerPosition: new Point(120d, 80d),
            currentZoomState: FloorPlanPreviewControl.PreviewZoomState.Default);

        var outcome = PreviewInteractionCoordinator.HandleMiddleButtonPressed(request);

        Assert.True(outcome.Handled);
        Assert.True(outcome.CapturePointer);
        Assert.True(outcome.InvalidateVisual);
        Assert.True(outcome.IsPanningPreview);
        Assert.Equal(new Point(120d, 80d), outcome.PanStartPoint);
        Assert.Equal(FloorPlanPreviewControl.PreviewZoomState.Default, outcome.PanStartZoomState);
    }
}
```

- [ ] **Step 2: Run the targeted test to verify RED**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .\.artifacts-test\desktop-loop2a-task1-red
```

Expected: FAIL because `PreviewInteractionCoordinator` and the helper APIs do not exist yet.

- [ ] **Step 3: Write the minimal coordinator seam and delegate the existing helper methods**

Create `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs` with an initial shape like:

```csharp
using Avalonia;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewInteractionCoordinator
{
    private const double UserZoomStep = 1.12d;

    internal readonly record struct MiddleButtonPanStartRequest(
        Point PointerPosition,
        FloorPlanPreviewControl.PreviewZoomState CurrentZoomState);

    internal readonly record struct MiddleButtonPanStartOutcome(
        bool Handled,
        bool CapturePointer,
        bool InvalidateVisual,
        bool IsPanningPreview,
        Point PanStartPoint,
        FloorPlanPreviewControl.PreviewZoomState PanStartZoomState);

    public static double CalculateWheelZoomFactor(double currentZoomFactor, double wheelDeltaY)
    {
        var safeCurrentZoom = double.IsFinite(currentZoomFactor) ? currentZoomFactor : 1d;
        var requestedZoom = safeCurrentZoom * Math.Pow(UserZoomStep, wheelDeltaY);
        return Math.Clamp(requestedZoom, FloorPlanPreviewControl.MinimumUserZoomFactor, FloorPlanPreviewControl.MaximumUserZoomFactor);
    }

    public static FloorPlanPreviewControl.PreviewZoomState ResolvePanStateForDrag(
        FloorPlanPreviewControl.PreviewZoomState startState,
        Point startPointerPosition,
        Point currentPointerPosition)
    {
        var pointerDelta = currentPointerPosition - startPointerPosition;
        return new FloorPlanPreviewControl.PreviewZoomState(
            startState.ZoomFactor,
            startState.PanOffset + pointerDelta);
    }

    public static FloorPlanPreviewControl.PreviewZoomState ResolveZoomStateForWheel(
        FloorPlanPreviewGeometry.PreviewViewport baseViewport,
        FloorPlanPreviewControl.PreviewZoomState currentState,
        Point pointerPosition,
        double wheelDeltaY)
    {
        var currentZoom = Math.Clamp(
            double.IsFinite(currentState.ZoomFactor) ? currentState.ZoomFactor : 1d,
            FloorPlanPreviewControl.MinimumUserZoomFactor,
            FloorPlanPreviewControl.MaximumUserZoomFactor);
        var currentViewport = baseViewport.WithUserTransform(currentZoom, currentState.PanOffset);
        var worldPoint = currentViewport.Unproject(pointerPosition);
        var nextZoom = CalculateWheelZoomFactor(currentZoom, wheelDeltaY);
        var nextViewportWithoutPan = baseViewport.WithUserTransform(nextZoom, default);
        var projectedWithoutPan = nextViewportWithoutPan.Project(worldPoint.X, worldPoint.Y);
        var nextPanOffset = new Vector(
            pointerPosition.X - projectedWithoutPan.X,
            pointerPosition.Y - projectedWithoutPan.Y);

        return new FloorPlanPreviewControl.PreviewZoomState(nextZoom, nextPanOffset);
    }

    public static MiddleButtonPanStartRequest CreatePointerPressedRequestForPan(
        Point pointerPosition,
        FloorPlanPreviewControl.PreviewZoomState currentZoomState)
        => new(pointerPosition, currentZoomState);

    public static MiddleButtonPanStartOutcome HandleMiddleButtonPressed(MiddleButtonPanStartRequest request)
        => new(
            Handled: true,
            CapturePointer: true,
            InvalidateVisual: true,
            IsPanningPreview: true,
            PanStartPoint: request.PointerPosition,
            PanStartZoomState: request.CurrentZoomState);
}
```

Then modify `FloorPlanPreviewControl.cs` so these methods delegate instead of owning the logic:

```csharp
internal static double CalculateWheelZoomFactor(double currentZoomFactor, double wheelDeltaY)
    => PreviewInteractionCoordinator.CalculateWheelZoomFactor(currentZoomFactor, wheelDeltaY);

internal static PreviewZoomState ResolveZoomStateForWheel(
    FloorPlanPreviewGeometry.PreviewViewport baseViewport,
    PreviewZoomState currentState,
    Point pointerPosition,
    double wheelDeltaY)
    => PreviewInteractionCoordinator.ResolveZoomStateForWheel(baseViewport, currentState, pointerPosition, wheelDeltaY);

internal static PreviewZoomState ResolvePanStateForDrag(
    PreviewZoomState startState,
    Point startPointerPosition,
    Point currentPointerPosition)
    => PreviewInteractionCoordinator.ResolvePanStateForDrag(startState, startPointerPosition, currentPointerPosition);
```

And rewrite the middle-button branch in `OnPointerPressed(...)` to apply the coordinator outcome instead of inlining the state transition.

- [ ] **Step 4: Run the targeted test again to verify GREEN**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .\.artifacts-test\desktop-loop2a-task1-green
```

Expected: PASS.

- [ ] **Step 5: Commit the seam**

```bash
git add tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs
git commit -m "refactor: introduce preview interaction coordinator seam"
```

---

### Task 2: Extract left-button press priority routing into the coordinator

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`

- [ ] **Step 1: Write the failing tests for left-button priority resolution**

Add tests like these:

```csharp
[Fact]
public void HandleLeftButtonPressed_prefers_dimension_handle_before_dimension_body()
{
    var handleDimension = CreateDimension("DIM-HANDLE");
    var bodyDimension = CreateDimension("DIM-BODY");
    var outcome = PreviewInteractionCoordinator.HandleLeftButtonPressed(
        new PreviewInteractionCoordinator.LeftButtonPressRequest(
            PointerPosition: new Point(320d, 160d),
            IsPinchPlacementArmed: false,
            AxisTag: Domain.FloorPlans.PinchAxisTag.Width,
            ResolveEdgeDrag: (_, _) => null,
            ResolveDimensionHandleHit: _ => new FloorPlanPreviewControl.DimensionHandleHit(
                handleDimension,
                FloorPlanPreviewControl.DimensionHandleKind.TextAnchor),
            ResolveDimensionHit: _ => new FloorPlanPreviewControl.DimensionHit(
                bodyDimension,
                FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint),
            ResolveRoomLabelHit: _ => null,
            ResolveOpeningLabelHit: _ => null,
            ResolveGeometryHit: _ => null,
            ResolveMovableArtifact: _ => null));

    Assert.True(outcome.Handled);
    Assert.True(outcome.CapturePointer);
    Assert.Equal(handleDimension.DimensionId, outcome.DimensionClickedId);
    Assert.NotNull(outcome.StartedDimensionEdit);
    Assert.Equal("DIM-HANDLE", outcome.StartedDimensionEdit.Value.BaseDimension.SourceEntityRef);
    Assert.Null(outcome.StartedArtifactMove);
}

[Fact]
public void HandleLeftButtonPressed_starts_room_label_absolute_move_before_generic_geometry_hit()
{
    var roomLabel = new RoomLabelDto(Guid.NewGuid(), "TEXT:1", "ROOM LBLS", "KITCHEN", 40m, 75m, 0.95m, null, 1);
    var outcome = PreviewInteractionCoordinator.HandleLeftButtonPressed(
        new PreviewInteractionCoordinator.LeftButtonPressRequest(
            PointerPosition: new Point(100d, 140d),
            IsPinchPlacementArmed: false,
            AxisTag: Domain.FloorPlans.PinchAxisTag.Width,
            ResolveEdgeDrag: (_, _) => null,
            ResolveDimensionHandleHit: _ => null,
            ResolveDimensionHit: _ => null,
            ResolveRoomLabelHit: _ => roomLabel,
            ResolveOpeningLabelHit: _ => null,
            ResolveGeometryHit: _ => new PreviewInteractionCoordinator.GeometryHit(Guid.NewGuid(), 0.4m),
            ResolveMovableArtifact: _ => null));

    Assert.True(outcome.Handled);
    Assert.NotNull(outcome.StartedArtifactMove);
    Assert.Equal(FloorPlanArtifactPositionMode.AbsolutePoint, outcome.StartedArtifactMove.Value.PositionMode);
    Assert.Equal(roomLabel.RoomLabelId, outcome.RoomLabelClickedId);
}
```

Include local test helpers:

```csharp
private static DimensionDto CreateDimension(string sourceKey)
    => new(
        Guid.NewGuid(),
        sourceKey,
        "DIMS",
        "DIMENSION",
        "*D1",
        "8'-0\"",
        "GeometryBlock",
        string.Empty,
        96m,
        2438.4m,
        "Inch",
        0,
        0m,
        0m,
        0m,
        0m,
        0m,
        100m,
        0m,
        0m,
        0m,
        20m,
        0m,
        0.99m,
        null,
        1);
```

- [ ] **Step 2: Run the targeted test to verify RED**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .\.artifacts-test\desktop-loop2a-task2-red
```

Expected: FAIL because `HandleLeftButtonPressed`, `LeftButtonPressRequest`, and the richer outcome model do not exist yet.

- [ ] **Step 3: Implement left-button priority routing in the coordinator and delegate from the control**

Extend `PreviewInteractionCoordinator.cs` with a request / outcome like:

```csharp
internal readonly record struct GeometryHit(Guid GeometryPathId, decimal PositionRatio);

internal readonly record struct LeftButtonPressRequest(
    Point PointerPosition,
    PinchAxisTag? AxisTag,
    bool IsPinchPlacementArmed,
    Func<Point, PinchAxisTag, FloorPlanPreviewGeometry.PreviewCompressionEdge?> ResolveEdgeDrag,
    Func<Point, FloorPlanPreviewControl.DimensionHandleHit?> ResolveDimensionHandleHit,
    Func<Point, FloorPlanPreviewControl.DimensionHit?> ResolveDimensionHit,
    Func<Point, RoomLabelDto?> ResolveRoomLabelHit,
    Func<Point, OpeningLabelDto?> ResolveOpeningLabelHit,
    Func<Point, GeometryHit?> ResolveGeometryHit,
    Func<Guid, FloorPlanPreviewControl.PreviewMovableArtifactDescriptor?> ResolveMovableArtifact);

internal readonly record struct LeftButtonPressOutcome(
    bool Handled,
    bool CapturePointer,
    bool InvalidateVisual,
    Guid? DimensionClickedId,
    Guid? RoomLabelClickedId,
    Guid? OpeningLabelClickedId,
    GeometryHit? GeometryClick,
    FloorPlanPreviewGeometry.PreviewCompressionEdge? StartedEdgeDrag,
    FloorPlanPreviewControl.PreviewArtifactMoveState? StartedArtifactMove,
    FloorPlanPreviewControl.PreviewDimensionEditState? StartedDimensionEdit);
```

In the same change, make the reusable nested interaction helper types visible to the preview namespace by changing their access modifiers in `FloorPlanPreviewControl.cs`:

```csharp
internal readonly record struct DimensionHandleHit(DimensionDto Dimension, DimensionHandleKind HandleKind);

internal readonly record struct PreviewArtifactMoveState(...);

internal readonly record struct PreviewMovableArtifactDescriptor(...);

internal readonly record struct PreviewDimensionEditState(...);
```

Implement the press priority in this exact order:

1. compression edge drag
2. dimension handle hit
3. dimension body hit
4. room label hit
5. opening label hit
6. geometry hit
7. movable-artifact start when geometry maps to one

Then simplify `OnPointerPressed(...)` in `FloorPlanPreviewControl.cs` so it:

- computes the viewport / axis tag once
- builds coordinator request delegates from the existing private helpers
- applies the returned outcome
- invokes existing events (`DimensionClicked`, `RoomLabelClicked`, `OpeningLabelClicked`, `GeometryPathClicked`)
- captures pointer and invalidates only when the outcome says to

- [ ] **Step 4: Run the targeted test again to verify GREEN**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .\.artifacts-test\desktop-loop2a-task2-green
```

Expected: PASS.

- [ ] **Step 5: Commit the press routing extraction**

```bash
git add tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs
git commit -m "refactor: delegate preview press priority"
```

---

### Task 3: Extract pointer move and release coordination for pan, preview trim, artifact drag, and dimension edit

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`

- [ ] **Step 1: Write the failing tests for move and release outcomes**

Add tests like:

```csharp
[Fact]
public void HandlePointerMoved_updates_preview_trim_while_edge_drag_is_active()
{
    var viewport = new FloorPlanPreviewGeometry.PreviewViewport(
        new Rect(0, 0, 800, 600),
        MinX: 0d,
        MinY: 0d,
        Scale: 2d,
        OffsetX: 100d,
        OffsetY: 80d);

    var state = new PreviewInteractionCoordinator.InteractionState(
        PreviewZoomState: FloorPlanPreviewControl.PreviewZoomState.Default,
        IsPanningPreview: false,
        PanStartPoint: default,
        PanStartZoomState: FloorPlanPreviewControl.PreviewZoomState.Default,
        ActiveDragEdge: FloorPlanPreviewGeometry.PreviewCompressionEdge.Right,
        DragStartPoint: new Point(420d, 200d),
        ActivePreviewTrimMm: 0m,
        ActiveArtifactMove: null,
        ActiveDimensionEdit: null);

    var outcome = PreviewInteractionCoordinator.HandlePointerMoved(
        new PreviewInteractionCoordinator.PointerMovedRequest(
            CurrentState: state,
            PointerPosition: new Point(390d, 200d),
            Viewport: viewport,
            ResolveSnappedDimensionWorldPoint: null));

    Assert.True(outcome.Handled);
    Assert.True(outcome.InvalidateVisual);
    Assert.Equal(15m, outcome.NextState.ActivePreviewTrimMm);
}

[Fact]
public void HandlePointerReleased_commits_translation_only_when_delta_is_meaningful()
{
    var move = FloorPlanPreviewControl.PreviewArtifactMoveState.ForTranslation(
        FloorPlanArtifactSourceKinds.OpeningCandidate,
        Guid.NewGuid(),
        new Point(100d, 100d),
        baseDx: 0m,
        baseDy: 0m);
    var state = PreviewInteractionCoordinator.InteractionState.ForArtifactMove(
        FloorPlanPreviewControl.PreviewZoomState.Default,
        move with { CurrentDeltaX = 12m, CurrentDeltaY = -4m });

    var outcome = PreviewInteractionCoordinator.HandlePointerReleased(
        new PreviewInteractionCoordinator.PointerReleasedRequest(
            CurrentState: state,
            PointerPosition: new Point(124d, 92d),
            Viewport: null,
            BuildEditedDimensionPreview: null));

    Assert.True(outcome.Handled);
    Assert.True(outcome.ReleasePointerCapture);
    Assert.NotNull(outcome.CommittedArtifactMove);
    Assert.Equal(12m, outcome.CommittedArtifactMove!.TranslationDx);
    Assert.Equal(-4m, outcome.CommittedArtifactMove.TranslationDy);
}
```

- [ ] **Step 2: Run the targeted test to verify RED**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .\.artifacts-test\desktop-loop2a-task3-red
```

Expected: FAIL because the move / release request-outcome APIs do not exist yet.

- [ ] **Step 3: Implement move and release coordination and delegate from the control**

Extend `PreviewInteractionCoordinator.cs` with:

```csharp
internal readonly record struct InteractionState(
    FloorPlanPreviewControl.PreviewZoomState PreviewZoomState,
    bool IsPanningPreview,
    Point PanStartPoint,
    FloorPlanPreviewControl.PreviewZoomState PanStartZoomState,
    FloorPlanPreviewGeometry.PreviewCompressionEdge? ActiveDragEdge,
    Point DragStartPoint,
    decimal ActivePreviewTrimMm,
    FloorPlanPreviewControl.PreviewArtifactMoveState? ActiveArtifactMove,
    FloorPlanPreviewControl.PreviewDimensionEditState? ActiveDimensionEdit)
{
    public static InteractionState Default { get; } = new(
        FloorPlanPreviewControl.PreviewZoomState.Default,
        false,
        default,
        FloorPlanPreviewControl.PreviewZoomState.Default,
        null,
        default,
        0m,
        null,
        null);

    public static InteractionState ForArtifactMove(
        FloorPlanPreviewControl.PreviewZoomState previewZoomState,
        FloorPlanPreviewControl.PreviewArtifactMoveState move)
        => new(previewZoomState, false, default, FloorPlanPreviewControl.PreviewZoomState.Default, null, default, 0m, move, null);
}
```

Also implement:

- `HandlePointerMoved(PointerMovedRequest request)`
- `HandlePointerReleased(PointerReleasedRequest request)`
- pure helpers for:
  - world delta calculation
  - movement commit resolution
  - preview trim calculation

Then rewrite `OnPointerMoved(...)` and `OnPointerReleased(...)` in `FloorPlanPreviewControl.cs` so they:

- build coordinator requests using existing local helpers like `GetPreviewViewport(...)`, `ResolveDimensionSnapAnchors(...)`, and `BuildEditedDimensionPreview(...)`
- update the control’s interaction fields from `outcome.NextState`
- invoke `MovableArtifactMoved` or `DimensionEdited` only when the outcome includes committed values

- [ ] **Step 4: Run the targeted test again to verify GREEN**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .\.artifacts-test\desktop-loop2a-task3-green
```

Expected: PASS.

- [ ] **Step 5: Commit the move/release extraction**

```bash
git add tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs
git commit -m "refactor: delegate preview move and release coordination"
```

---

### Task 4: Refresh regression coverage, document Loop 2A, and verify the Desktop slice

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 2A preview interaction extraction.md`

- [ ] **Step 1: Add / adjust the remaining regression checks around the control shell**

Add or keep thin shell-facing assertions in `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`, for example:

```csharp
[Fact]
public void FloorPlanPreviewControl_control_helpers_delegate_to_preview_interaction_coordinator()
{
    var zoomedIn = FloorPlanPreviewControl.CalculateWheelZoomFactor(1d, wheelDeltaY: 1d);
    var panned = FloorPlanPreviewControl.ResolvePanStateForDrag(
        new FloorPlanPreviewControl.PreviewZoomState(1.5d, new Vector(10d, 15d)),
        new Point(50d, 50d),
        new Point(70d, 80d));

    Assert.True(zoomedIn > 1d);
    Assert.Equal(new Vector(30d, 45d), panned.PanOffset);
}
```

- [ ] **Step 2: Run focused preview verification**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewInteractionCoordinatorTests|FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-loop2a-final-focused
```

Expected: PASS.

- [ ] **Step 3: Run the full Desktop suite**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop2a-full
```

Expected: PASS.

- [ ] **Step 4: Write the implementation note and update Current State**

Create `obsidian-vault/Implementation/2026-05-12 - Loop 2A preview interaction extraction.md` with content like:

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
  - interaction
  - modularization
  - loop-2
---

# Loop 2A preview interaction extraction

## What changed

Se extrajo la coordinacion de interaccion del preview a `PreviewInteractionCoordinator` para que `FloorPlanPreviewControl` deje de mezclar pointer-state transitions con render shell.

## Why

Era el hotspot estructural mas riesgoso del preview y el corte aprobado para Loop 2 fue quirurgico: primero sacar el cerebro de interaccion, despues observers o render decomposition.

## Where

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`

## Learned

- El mejor primer seam no era otro renderer, sino un coordinador que devuelve outcomes explicitos y deja los side effects de Avalonia en el control.
```

Then update `obsidian-vault/Current State.md` so `Immediate Next Steps` no longer points to Loop 2 as untouched and instead records that:

- Loop 2A extracted preview interaction coordination
- observer wiring / invalidation repetition remains for a later slice
- render composition cleanup remains for a later slice

- [ ] **Step 5: Run diff hygiene and commit the loop**

Run:

```powershell
git diff --check
git diff --stat
git status --short
```

Expected:

- `git diff --check` prints nothing
- the diff stays within the coordinator, preview control, tests, and the two Obsidian notes

Then commit:

```bash
git add -- 'tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs' 'tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs' 'src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs' 'obsidian-vault/Current State.md' 'obsidian-vault/Implementation/2026-05-12 - Loop 2A preview interaction extraction.md'
git commit -m "refactor: extract preview interaction coordinator"
```

---

## Self-review

- This plan keeps **observer wiring out of scope** on purpose.
- This plan keeps **render decomposition out of scope** on purpose.
- This plan reuses current preview helpers instead of rewriting them.
- This plan preserves TDD and verifies the full Desktop suite without building.
