# Unified Review UI Inline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved Loop 1 review redesign so all curable plan artifacts live under `Plan Elements`, the right panel becomes `Selected Item + Pinch Tools`, and every artifact can be excluded through one unified UI action.

**Architecture:** Keep the CAD-faithful preview and existing type-specific persistence semantics, but unify the user-facing curation flow in Desktop. Add the missing room-label exclusion path in Application/Infrastructure, then route one `Exclude from Curation` action through the ViewModel based on the selected artifact type.

**Tech Stack:** C#, .NET 10, Avalonia UI, MVVM, xUnit, SQLite

---

### Task 1: Add room-label exclusion backend

**Files:**
- Modify: `src/FloorplanFit.Application/Abstractions/IExtractedRoomLabelRepository.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/RemoveRoomLabelHandler.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedRoomLabelRepository.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/RemoveOpeningArtifactHandlerTests.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs`

- [ ] **Step 1: Write the failing Application test**

Add a room-label test next to the existing remove-handler tests:

```csharp
[Fact]
public async Task RemoveRoomLabelHandler_removes_room_label_and_saves_changes()
{
    var label = new ExtractedRoomLabel(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "TEXT:1",
        "ROOM LBLS",
        "KITCHEN",
        125m,
        784m,
        0.95m,
        null,
        1);
    var repository = new InMemoryRoomLabelRepository([label]);
    var unitOfWork = new FakeUnitOfWork();
    var handler = new RemoveRoomLabelHandler(repository, unitOfWork);

    await handler.HandleAsync(label.Id, CancellationToken.None);

    Assert.Empty(repository.Items);
    Assert.True(unitOfWork.SaveChangesCalled);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~RemoveRoomLabelHandler_removes_room_label_and_saves_changes" --artifacts-path .\.artifacts-test\app-room-label-red
```

Expected: FAIL because `RemoveRoomLabelHandler` and/or `IExtractedRoomLabelRepository.RemoveAsync` do not exist yet.

- [ ] **Step 3: Write the failing Infrastructure test**

Add a repository integration test:

```csharp
[Fact]
public async Task ExtractedRoomLabelRepository_removes_room_labels_so_false_positives_do_not_persist()
{
    // seed one label, remove it, reload, assert empty
}
```

- [ ] **Step 4: Run test to verify it fails**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~ExtractedRoomLabelRepository_removes_room_labels_so_false_positives_do_not_persist" --artifacts-path .\.artifacts-test\infra-room-label-red
```

Expected: FAIL because the repository has no remove method yet.

- [ ] **Step 5: Write the minimal implementation**

Implement:

```csharp
public interface IExtractedRoomLabelRepository
{
    Task AddRangeAsync(IReadOnlyList<ExtractedRoomLabel> labels, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExtractedRoomLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken);
    Task RemoveAsync(Guid roomLabelId, CancellationToken cancellationToken);
}
```

```csharp
public sealed class RemoveRoomLabelHandler
{
    private readonly IExtractedRoomLabelRepository extractedRoomLabelRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveRoomLabelHandler(IExtractedRoomLabelRepository extractedRoomLabelRepository, IUnitOfWork unitOfWork)
    {
        this.extractedRoomLabelRepository = extractedRoomLabelRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid roomLabelId, CancellationToken cancellationToken)
    {
        await extractedRoomLabelRepository.RemoveAsync(roomLabelId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

```csharp
public Task RemoveAsync(Guid roomLabelId, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    using var command = CreateCommand(
        """
        DELETE FROM extracted_room_labels
        WHERE id = $id
        """);
    command.Parameters.AddWithValue("$id", roomLabelId.ToString());
    command.ExecuteNonQuery();

    return Task.CompletedTask;
}
```

- [ ] **Step 6: Run tests to verify green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~RemoveRoomLabelHandler_removes_room_label_and_saves_changes" --artifacts-path .\.artifacts-test\app-room-label-green
dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~ExtractedRoomLabelRepository_removes_room_labels_so_false_positives_do_not_persist" --artifacts-path .\.artifacts-test\infra-room-label-green
```

Expected: PASS.

---

### Task 2: Add unified exclusion behavior to the review ViewModel

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

- [ ] **Step 1: Write failing ViewModel tests**

Add tests for:

```csharp
[Fact]
public async Task ExcludeSelectedArtifactAsync_removes_selected_room_label()
```

```csharp
[Fact]
public async Task ExcludeSelectedArtifactAsync_rejects_selected_wall_candidate()
```

```csharp
[Fact]
public async Task Selecting_room_label_populates_selected_item_summary()
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~ExcludeSelectedArtifactAsync|FullyQualifiedName~Selecting_room_label_populates_selected_item_summary" --artifacts-path .\.artifacts-test\desktop-viewmodel-red
```

Expected: FAIL because the unified selection/exclude API does not exist.

- [ ] **Step 3: Write the minimal ViewModel implementation**

Add:

- `SelectedRoomLabel`
- `ExcludeSelectedArtifactAsync(...)`
- compact section title properties such as `PlanElementsTitle`, `LinesSectionTitle`, `RoomsSectionTitle`
- selected-item inspector properties such as:

```csharp
public bool HasSelectedArtifact => ...;
public string SelectedArtifactTypeLabel => ...;
public string SelectedArtifactTitle => ...;
public string SelectedArtifactSubtitle => ...;
public string ExcludeSelectedArtifactLabel => "Exclude from Curation";
```

Route exclusion in one place:

```csharp
public async Task ExcludeSelectedArtifactAsync(CancellationToken cancellationToken)
{
    if (SelectedCandidate is not null)
    {
        await RejectSelectedCandidateAsync(cancellationToken);
        return;
    }

    if (SelectedRoomLabel is not null)
    {
        await RemoveSelectedRoomLabelAsync(cancellationToken);
        return;
    }

    if (SelectedOpeningCandidate is not null)
    {
        await RemoveSelectedOpeningCandidateAsync(cancellationToken);
        return;
    }

    // ... opening label, fixed component, protected detail
}
```

- [ ] **Step 4: Run ViewModel tests to verify green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-viewmodel-green
```

Expected: PASS.

---

### Task 3: Redesign the review window layout

**Files:**
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

- [ ] **Step 1: Write the failing layout test**

Update expectations to the new truth:

```csharp
Assert.Contains("Plan Elements", xaml, StringComparison.Ordinal);
Assert.Contains("Selected Item", xaml, StringComparison.Ordinal);
Assert.Contains("Exclude from Curation", xaml, StringComparison.Ordinal);
Assert.DoesNotContain("Reject Selected Line", xaml, StringComparison.Ordinal);
Assert.DoesNotContain("Remove Selected Opening", xaml, StringComparison.Ordinal);
Assert.DoesNotContain("Remove Selected Label", xaml, StringComparison.Ordinal);
Assert.DoesNotContain("Remove Selected Component", xaml, StringComparison.Ordinal);
Assert.DoesNotContain("Remove Selected Detail", xaml, StringComparison.Ordinal);
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~ReviewFloorPlanWindowLayoutTests" --artifacts-path .\.artifacts-test\desktop-layout-red
```

Expected: FAIL because the XAML still uses the old layout and button copy.

- [ ] **Step 3: Write the minimal XAML/code-behind implementation**

Implement:

- left panel = grouped `Plan Elements`
- center = existing preview
- right panel = selected item summary + one exclude button + pinch tools
- code-behind routes:

```csharp
private async void ExcludeSelectedArtifactButton_OnClick(object? sender, RoutedEventArgs e)
{
    if (DataContext is not FloorPlanReviewViewModel viewModel)
    {
        return;
    }

    await viewModel.ExcludeSelectedArtifactAsync(CancellationToken.None);
}
```

- [ ] **Step 4: Run layout test to verify green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~ReviewFloorPlanWindowLayoutTests" --artifacts-path .\.artifacts-test\desktop-layout-green
```

Expected: PASS.

---

### Task 4: Verify composition and focused regression suite

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/Composition/DesktopServiceRegistrationTests.cs`

- [ ] **Step 1: Add composition assertion for the new handler**

```csharp
Assert.NotNull(scope.ServiceProvider.GetRequiredService<RemoveRoomLabelHandler>());
```

- [ ] **Step 2: Run test to verify it fails if registration is missing**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AddDesktopSlice1_registers_review_session_services" --artifacts-path .\.artifacts-test\desktop-composition-red
```

- [ ] **Step 3: Register the handler and re-run green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AddDesktopSlice1_registers_review_session_services" --artifacts-path .\.artifacts-test\desktop-composition-green
dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .\.artifacts-test\application-final
dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .\.artifacts-test\infrastructure-final
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-final
```

Expected: PASS across all three test projects.

---

Because the user explicitly asked me to implement now, I will execute this plan inline rather than pausing for an execution-mode choice.
