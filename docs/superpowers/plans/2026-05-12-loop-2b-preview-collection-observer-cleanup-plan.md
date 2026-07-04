# Loop 2B Preview Collection Observer Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract preview collection observer wiring out of `FloorPlanPreviewControl` into a focused hub without changing existing Loop 1 CAD-faithful preview behavior.

**Architecture:** Keep `FloorPlanPreviewControl` as the Avalonia shell that owns styled properties, interaction state, render composition, and UI side effects. Introduce one `PreviewCollectionObserverHub` that owns collection subscription bookkeeping, replace-on-property-change behavior, and shared invalidation callback bridging. Do not mix this slice with render decomposition or `FloorPlanReviewViewModel`.

**Tech Stack:** C# / .NET 10, Avalonia `Control`, `INotifyCollectionChanged`, xUnit Desktop tests, PowerShell verification, existing preview helpers under `src/FloorplanFit.Desktop/Controls/Preview/*`.

---

## File Structure

- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs`
  - Purpose: own preview collection observer lifecycle and invalidation callback bridging.
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
  - Purpose: delegate observer-plumbing lifecycle to the hub and stop owning repeated attach/detach handlers inline.
- Create: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs`
  - Purpose: pin attach / replace / detach / invalidate behavior with TDD before integration.
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
  - Purpose: preserve thin shell-facing regression checks after the hub extraction.
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 2B preview collection observer cleanup.md`
  - Purpose: durable implementation note for this loop slice.
- Modify: `obsidian-vault/Current State.md`
  - Purpose: record that Loop 2B extracted preview observer wiring while render cleanup remains for a later slice.

---

## Tasks

### Task 1: Introduce hub-facing tests for observer lifecycle

**Files:**
- Create: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs`

- [ ] **Step 1: Write the failing hub tests for attach, replace, and detach-all**

Add tests like these to `tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs`:

```csharp
using System.Collections.ObjectModel;
using FloorplanFit.Desktop.Controls.Preview;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class PreviewCollectionObserverHubTests
{
    [Fact]
    public void AttachAll_subscribes_observable_collections_and_invalidates_on_change()
    {
        var invalidations = 0;
        var hub = new PreviewCollectionObserverHub(() => invalidations++);
        var geometry = new ObservableCollection<int>();

        hub.AttachAll(new PreviewCollectionObserverHub.PreviewObservedCollections(
            GeometryPaths: geometry,
            PinchMarkers: null,
            RoomLabels: null,
            WallCandidates: null,
            OpeningCandidates: null,
            OpeningLabels: null,
            Dimensions: null,
            DimensionAssociations: null,
            FixedPlanComponents: null,
            ProtectedDetailAssemblies: null,
            CuratedPlanArtifacts: null));

        geometry.Add(1);

        Assert.Equal(1, invalidations);
    }

    [Fact]
    public void Replace_detaches_previous_collection_before_subscribing_new_one()
    {
        var invalidations = 0;
        var hub = new PreviewCollectionObserverHub(() => invalidations++);
        var oldCollection = new ObservableCollection<int>();
        var newCollection = new ObservableCollection<int>();

        hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, oldCollection);
        hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, newCollection);

        oldCollection.Add(1);
        newCollection.Add(1);

        Assert.Equal(1, invalidations);
    }

    [Fact]
    public void DetachAll_removes_all_active_subscriptions()
    {
        var invalidations = 0;
        var hub = new PreviewCollectionObserverHub(() => invalidations++);
        var geometry = new ObservableCollection<int>();

        hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, geometry);
        hub.DetachAll();
        geometry.Add(1);

        Assert.Equal(0, invalidations);
    }
}
```

- [ ] **Step 2: Run the targeted test to verify RED**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewCollectionObserverHubTests" --artifacts-path .\.artifacts-test\desktop-loop2b-task1-red
```

Expected: FAIL because `PreviewCollectionObserverHub` does not exist yet.

---

### Task 2: Implement the hub minimally and turn the tests GREEN

**Files:**
- Create: `src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs`

- [ ] **Step 1: Write the minimal hub implementation**

Create `src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs` with a shape like:

```csharp
using System.Collections;
using System.Collections.Specialized;

namespace FloorplanFit.Desktop.Controls.Preview;

internal sealed class PreviewCollectionObserverHub
{
    private readonly Action invalidateVisual;
    private readonly Dictionary<PreviewObservedCollectionSlot, INotifyCollectionChanged> observed = [];

    internal enum PreviewObservedCollectionSlot
    {
        GeometryPaths,
        PinchMarkers,
        RoomLabels,
        WallCandidates,
        OpeningCandidates,
        OpeningLabels,
        Dimensions,
        DimensionAssociations,
        FixedPlanComponents,
        ProtectedDetailAssemblies,
        CuratedPlanArtifacts
    }

    internal readonly record struct PreviewObservedCollections(
        IEnumerable? GeometryPaths,
        IEnumerable? PinchMarkers,
        IEnumerable? RoomLabels,
        IEnumerable? WallCandidates,
        IEnumerable? OpeningCandidates,
        IEnumerable? OpeningLabels,
        IEnumerable? Dimensions,
        IEnumerable? DimensionAssociations,
        IEnumerable? FixedPlanComponents,
        IEnumerable? ProtectedDetailAssemblies,
        IEnumerable? CuratedPlanArtifacts);

    public PreviewCollectionObserverHub(Action invalidateVisual)
    {
        this.invalidateVisual = invalidateVisual;
    }

    public void AttachAll(PreviewObservedCollections collections)
    {
        Replace(PreviewObservedCollectionSlot.GeometryPaths, collections.GeometryPaths);
        Replace(PreviewObservedCollectionSlot.PinchMarkers, collections.PinchMarkers);
        Replace(PreviewObservedCollectionSlot.RoomLabels, collections.RoomLabels);
        Replace(PreviewObservedCollectionSlot.WallCandidates, collections.WallCandidates);
        Replace(PreviewObservedCollectionSlot.OpeningCandidates, collections.OpeningCandidates);
        Replace(PreviewObservedCollectionSlot.OpeningLabels, collections.OpeningLabels);
        Replace(PreviewObservedCollectionSlot.Dimensions, collections.Dimensions);
        Replace(PreviewObservedCollectionSlot.DimensionAssociations, collections.DimensionAssociations);
        Replace(PreviewObservedCollectionSlot.FixedPlanComponents, collections.FixedPlanComponents);
        Replace(PreviewObservedCollectionSlot.ProtectedDetailAssemblies, collections.ProtectedDetailAssemblies);
        Replace(PreviewObservedCollectionSlot.CuratedPlanArtifacts, collections.CuratedPlanArtifacts);
    }

    public void Replace(PreviewObservedCollectionSlot slot, IEnumerable? value)
    {
        if (observed.TryGetValue(slot, out var existing) && ReferenceEquals(existing, value))
        {
            return;
        }

        Detach(slot);
        if (value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observed[slot] = notifyCollectionChanged;
        notifyCollectionChanged.CollectionChanged += OnObservedCollectionChanged;
    }

    public void DetachAll()
    {
        foreach (var slot in observed.Keys.ToArray())
        {
            Detach(slot);
        }
    }

    private void Detach(PreviewObservedCollectionSlot slot)
    {
        if (!observed.TryGetValue(slot, out var existing))
        {
            return;
        }

        existing.CollectionChanged -= OnObservedCollectionChanged;
        observed.Remove(slot);
    }

    private void OnObservedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        invalidateVisual();
    }
}
```

- [ ] **Step 2: Add the remaining green-path tests**

Extend `PreviewCollectionObserverHubTests.cs` with:

```csharp
[Fact]
public void Replace_ignores_non_observable_lists_without_throwing()
{
    var invalidations = 0;
    var hub = new PreviewCollectionObserverHub(() => invalidations++);

    hub.Replace(
        PreviewCollectionObserverHub.PreviewObservedCollectionSlot.RoomLabels,
        new[] { 1, 2, 3 });

    Assert.Equal(0, invalidations);
}

[Fact]
public void Replace_same_instance_does_not_double_subscribe()
{
    var invalidations = 0;
    var hub = new PreviewCollectionObserverHub(() => invalidations++);
    var geometry = new ObservableCollection<int>();

    hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, geometry);
    hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, geometry);
    geometry.Add(1);

    Assert.Equal(1, invalidations);
}
```

- [ ] **Step 3: Run the targeted tests again to verify GREEN**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewCollectionObserverHubTests" --artifacts-path .\.artifacts-test\desktop-loop2b-task2-green
```

Expected: PASS.

- [ ] **Step 4: Commit the hub seam**

```bash
git add -- 'tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs'
git commit -m "refactor: add preview collection observer hub"
```

---

### Task 3: Delegate `FloorPlanPreviewControl` observer plumbing to the hub

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`

- [ ] **Step 1: Write the failing shell-facing regression test**

Add a source-facing assertion to `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`:

```csharp
[Fact]
public void FloorPlanPreviewControl_sources_collection_observer_lifecycle_through_the_hub()
{
    var controlPath = Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "FloorplanFit.Desktop", "Controls", "FloorPlanPreviewControl.cs");
    var source = File.ReadAllText(controlPath);

    Assert.Contains("PreviewCollectionObserverHub", source, StringComparison.Ordinal);
    Assert.DoesNotContain("private void GeometryPathsCollectionChanged(", source, StringComparison.Ordinal);
    Assert.DoesNotContain("private void RoomLabelsCollectionChanged(", source, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run the focused control test to verify RED**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-loop2b-task3-red
```

Expected: FAIL because the control still owns the legacy per-collection callbacks.

- [ ] **Step 3: Delegate attach/detach/replace wiring from the control**

Refactor `FloorPlanPreviewControl.cs` so it:

- adds a field like:

```csharp
private readonly PreviewCollectionObserverHub collectionObserverHub;
```

- initializes it in the constructor:

```csharp
public FloorPlanPreviewControl()
{
    collectionObserverHub = new PreviewCollectionObserverHub(InvalidateVisual);
}
```

- captures current collections through a helper:

```csharp
private PreviewCollectionObserverHub.PreviewObservedCollections CaptureObservedCollections()
    => new(
        GeometryPaths,
        PinchMarkers,
        RoomLabels,
        WallCandidates,
        OpeningCandidates,
        OpeningLabels,
        Dimensions,
        DimensionAssociations,
        FixedPlanComponents,
        ProtectedDetailAssemblies,
        CuratedPlanArtifacts);
```

- changes visual tree lifecycle to:

```csharp
protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnAttachedToVisualTree(e);
    collectionObserverHub.AttachAll(CaptureObservedCollections());
}

protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    collectionObserverHub.DetachAll();
    base.OnDetachedFromVisualTree(e);
}
```

- changes each property-change handler to call one shared helper:

```csharp
private void ReplaceObservedCollection<T>(
    PreviewCollectionObserverHub.PreviewObservedCollectionSlot slot,
    IReadOnlyList<T>? oldValue,
    IReadOnlyList<T>? newValue)
{
    if (!ReferenceEquals(oldValue, newValue))
    {
        collectionObserverHub.Replace(slot, newValue);
    }

    InvalidateVisual();
}
```

- removes the legacy `Attach...CollectionObserver`, `Detach...CollectionObserver`, and `...CollectionChanged(...)` methods once the hub owns them.

- [ ] **Step 4: Run the focused control test again to verify GREEN**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~PreviewCollectionObserverHubTests" --artifacts-path .\.artifacts-test\desktop-loop2b-task3-green
```

Expected: PASS.

- [ ] **Step 5: Commit the control delegation slice**

```bash
git add -- 'tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs' 'tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs' 'src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs'
git commit -m "refactor: delegate preview observer wiring"
```

---

### Task 4: Refresh docs, verify the Desktop slice, and close Loop 2B

**Files:**
- Modify: `obsidian-vault/Current State.md`
- Create: `obsidian-vault/Implementation/2026-05-12 - Loop 2B preview collection observer cleanup.md`
- Modify: `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs` (only if the final shell assertions need adjustment)

- [ ] **Step 1: Run focused preview verification**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewCollectionObserverHubTests|FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .\.artifacts-test\desktop-loop2b-final-focused
```

Expected: PASS.

- [ ] **Step 2: Run the full Desktop suite**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop2b-full
```

Expected: PASS.

- [ ] **Step 3: Write the implementation note and update Current State**

Create `obsidian-vault/Implementation/2026-05-12 - Loop 2B preview collection observer cleanup.md` with content like:

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
  - observer
  - modularization
  - loop-2
---

# Loop 2B preview collection observer cleanup

## What changed

Se extrajo la plomeria de colecciones observables del preview a `PreviewCollectionObserverHub`, dejando a `FloorPlanPreviewControl` mas cerca de un shell Avalonia real.

## Why

Despues de Loop 2A, el siguiente hotspot verificado era la repeticion de attach / detach / invalidate en el control. Este slice limpia esa responsabilidad sin mezclarla con render decomposition.

## Where

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`

## Learned

- La frontera correcta era un hub stateful chico para observer lifecycle, no un simple `partial` cosmetico.
- El preview shell sigue grande, pero ahora la repeticion mecanica de suscripciones deja de vivir inline.
```

Then update `obsidian-vault/Current State.md` so it records that:

- Loop 2B extracted preview observer wiring
- `FloorPlanPreviewControl` now delegates collection lifecycle to `PreviewCollectionObserverHub`
- render composition cleanup remains for a later slice

- [ ] **Step 4: Run diff hygiene and commit the loop**

Run:

```powershell
git diff --check
git diff --stat
git status --short
```

Expected:

- `git diff --check` prints nothing
- the diff stays within the hub, preview control, tests, and the two Obsidian notes

Then commit:

```bash
git add -- 'tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs' 'tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs' 'src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs' 'src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs' 'obsidian-vault/Current State.md' 'obsidian-vault/Implementation/2026-05-12 - Loop 2B preview collection observer cleanup.md'
git commit -m "refactor: clean preview observer wiring"
```

---

## Self-review

- This plan keeps **render decomposition out of scope** on purpose.
- This plan keeps **`FloorPlanReviewViewModel` out of scope** on purpose.
- This plan reuses the current Loop 2A shell/coordinator split instead of reopening interaction behavior.
- This plan targets a real verified hotspot: observer lifecycle repetition, not a cosmetic file split.
