# Loop 1 Native Dimension Extraction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring native floor-plan dimensions into the next Loop 1 extraction and expose them as a read-only `Dimensions` section in Review with CAD-visible measurement text.

**Architecture:** Add a new extracted-dimension family parallel to rooms/openings/fixed/protected artifacts. Infrastructure resolves native `DIMENSION` entities and their visible text, Application persists them in the existing extraction run, Contracts extend the review session DTO, and Desktop shows a read-only review queue section plus inspector details.

**Tech Stack:** C#, .NET 10, Avalonia UI, MVVM, SQLite, IxMilia.Dxf, xUnit

---

## File structure map

**Create**
- `src/FloorplanFit.Application/Abstractions/DetectedDimension.cs`
- `src/FloorplanFit.Application/Abstractions/IDimensionExtractor.cs`
- `src/FloorplanFit.Application/Abstractions/IExtractedDimensionRepository.cs`
- `src/FloorplanFit.Domain/FloorPlans/ExtractedDimension.cs`
- `src/FloorplanFit.Contracts/FloorPlans/DimensionDto.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDimensionExtractor.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedDimensionRepository.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaDimensionExtractorTests.cs`

**Modify**
- `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs`
- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Extraction/ExtractWallCandidatesHandlerTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Review/FloorPlanReviewSessionReaderIntegrationTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

---

### Task 1: Add the dimension domain/contracts and prove the DXF extractor behavior

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/DetectedDimension.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IDimensionExtractor.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ExtractedDimension.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/DimensionDto.cs`
- Create: `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDimensionExtractor.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaDimensionExtractorTests.cs`

- [ ] **Step 1: Write the failing extractor tests**

Add tests that prove the real product rules:

```csharp
[Fact]
public async Task ExtractAsync_reads_native_dimensions_from_seminole_and_excludes_electrical_wiring()
{
    var solutionRoot = RepositoryPaths.FindSolutionRoot();
    var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
    var extractor = new IxMiliaDimensionExtractor();

    var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

    Assert.Equal(326, dimensions.Count);
    Assert.DoesNotContain(dimensions, item => item.SourceLayer == "ELECTRICAL WIRING");
}

[Fact]
public async Task ExtractAsync_prefers_rendered_geometry_block_text_when_dimension_text_field_is_empty()
{
    var solutionRoot = RepositoryPaths.FindSolutionRoot();
    var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
    var extractor = new IxMiliaDimensionExtractor();

    var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

    Assert.Contains(dimensions, item =>
        item.RawTextOverride == string.Empty &&
        item.DisplayText == "10'-4"" &&
        item.DisplayTextSource == "GeometryBlock");
}

[Fact]
public async Task ExtractAsync_reads_santa_barbara_native_dimensions()
{
    var solutionRoot = RepositoryPaths.FindSolutionRoot();
    var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
    var extractor = new IxMiliaDimensionExtractor();

    var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

    Assert.Equal(114, dimensions.Count);
}
```

- [ ] **Step 2: Run the targeted extractor tests to verify RED**

Run:

```powershell
dotnet test .	ests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaDimensionExtractorTests" --artifacts-path .\.artifacts-test\infra-dimensions-red
```

Expected: FAIL because the dimension extractor and models do not exist yet.

- [ ] **Step 3: Write the minimal extractor and data models**

Create `DetectedDimension` with these core fields:

```csharp
public sealed record DetectedDimension(
    string SourceEntityRef,
    string SourceLayer,
    string SourceEntityKind,
    string? GeometryBlockName,
    string DisplayText,
    string DisplayTextSource,
    string RawTextOverride,
    decimal MeasurementSourceUnits,
    decimal MeasurementMillimeters,
    string SourceUnit,
    int DimType,
    decimal Angle,
    decimal ObliqueAngle,
    decimal DefPointX,
    decimal DefPointY,
    decimal DefPointZ,
    decimal DefPoint2X,
    decimal DefPoint2Y,
    decimal DefPoint2Z,
    decimal DefPoint3X,
    decimal DefPoint3Y,
    decimal DefPoint3Z,
    decimal Confidence,
    string? DetectionNotes);
```

The extractor should:

```csharp
- iterate modelspace DIMENSION entities
- skip source layer ELECTRICAL WIRING
- call GetMeasurement()
- resolve display text from geometry-block MTEXT/TEXT first
- fall back to dxf.text when non-empty/non-<>
- generate fallback text only when both prior sources fail
- convert measurement to millimeters using the DXF source unit
```

- [ ] **Step 4: Run the targeted extractor tests again to verify GREEN**

Run:

```powershell
dotnet test .	ests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaDimensionExtractorTests" --artifacts-path .\.artifacts-test\infra-dimensions-green
```

Expected: PASS.

---

### Task 2: Persist extracted dimensions inside the existing extraction run

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/IExtractedDimensionRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedDimensionRepository.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Extraction/ExtractWallCandidatesHandlerTests.cs`

- [ ] **Step 1: Write the failing application test**

Extend `ExtractWallCandidatesHandlerTests` with a dimension assertion:

```csharp
var dimensions = new InMemoryExtractedDimensionRepository();

var handler = new ExtractWallCandidatesHandler(
    extractor,
    roomLabelExtractor,
    openingExtractor,
    fixedComponentExtractor,
    protectedDetailExtractor,
    new FakeDimensionExtractor(
    [
        new DetectedDimension(
            "DIMENSION:1",
            "DIMS",
            "DIMENSION",
            "*D169",
            "5'-8"",
            "GeometryBlock",
            string.Empty,
            68m,
            1727.2m,
            "Inch",
            160,
            0m,
            0m,
            372.5742m,
            516.9566m,
            0m,
            440.5742m,
            521.7785m,
            0m,
            372.5742m,
            526.9408m,
            0m,
            0.99m,
            null)
    ]),
    runs,
    candidates,
    roomLabels,
    openings,
    openingLabels,
    fixedComponents,
    protectedDetails,
    dimensions,
    unitOfWork,
    clock);

Assert.Single(dimensions.Items);
Assert.Equal("5'-8"", dimensions.Items.Single().DisplayText);
```

- [ ] **Step 2: Run the targeted application test to verify RED**

Run:

```powershell
dotnet test .	ests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~ExtractWallCandidatesHandlerTests" --artifacts-path .\.artifacts-testpp-dimensions-red
```

Expected: FAIL because the handler constructor and persistence path do not know dimensions yet.

- [ ] **Step 3: Add repository contract, domain persistence model, schema, and handler wiring**

Create `ExtractedDimension` with the persisted fields mirroring the detected model plus `Id`, `WallExtractionRunId`, and `SortOrder`.

Add the table in `SqliteSchemaInitializer`:

```sql
CREATE TABLE IF NOT EXISTS extracted_dimensions (
    id TEXT PRIMARY KEY,
    wall_extraction_run_id TEXT NOT NULL,
    source_entity_ref TEXT NOT NULL,
    source_layer TEXT NULL,
    source_entity_kind TEXT NOT NULL,
    geometry_block_name TEXT NULL,
    display_text TEXT NOT NULL,
    display_text_source TEXT NOT NULL,
    raw_text_override TEXT NOT NULL,
    measurement_source_units TEXT NOT NULL,
    measurement_millimeters TEXT NOT NULL,
    source_unit TEXT NOT NULL,
    dim_type INTEGER NOT NULL,
    angle TEXT NOT NULL,
    oblique_angle TEXT NOT NULL,
    defpoint_x TEXT NOT NULL,
    defpoint_y TEXT NOT NULL,
    defpoint_z TEXT NOT NULL,
    defpoint2_x TEXT NOT NULL,
    defpoint2_y TEXT NOT NULL,
    defpoint2_z TEXT NOT NULL,
    defpoint3_x TEXT NOT NULL,
    defpoint3_y TEXT NOT NULL,
    defpoint3_z TEXT NOT NULL,
    confidence TEXT NOT NULL,
    detection_notes TEXT NULL,
    sort_order INTEGER NOT NULL
);
```

Update `ExtractWallCandidatesHandler` to:

```csharp
- accept IDimensionExtractor and IExtractedDimensionRepository
- extract dimensions after protected details
- map them to ExtractedDimension with sortOrder = index + 1
- persist them before SaveChangesAsync
```

- [ ] **Step 4: Run the targeted application test again to verify GREEN**

Run:

```powershell
dotnet test .	ests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~ExtractWallCandidatesHandlerTests" --artifacts-path .\.artifacts-testpp-dimensions-green
```

Expected: PASS.

---

### Task 3: Surface dimensions in the review-session read model

**Files:**
- Modify: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Review/FloorPlanReviewSessionReaderIntegrationTests.cs`

- [ ] **Step 1: Write the failing reader integration test**

Extend the existing integration fixture seed with one `ExtractedDimension`, then assert:

```csharp
Assert.NotEmpty(reviewSession.Dimensions);
var dimension = Assert.Single(reviewSession.Dimensions);
Assert.Equal("10'-4"", dimension.DisplayText);
Assert.Equal("DIMS", dimension.SourceLayer);
Assert.Equal("GeometryBlock", dimension.DisplayTextSource);
Assert.Equal(123.81038730518773m, dimension.MeasurementSourceUnits);
```

- [ ] **Step 2: Run the targeted reader test to verify RED**

Run:

```powershell
dotnet test .	ests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests" --artifacts-path .\.artifacts-test\infra-dimension-reader-red
```

Expected: FAIL because the review DTO and reader do not expose dimensions yet.

- [ ] **Step 3: Extend contracts and reader minimally**

Create `DimensionDto` and extend the session record:

```csharp
public sealed record DimensionDto(
    Guid DimensionId,
    string SourceEntityRef,
    string SourceLayer,
    string DisplayText,
    string DisplayTextSource,
    decimal MeasurementSourceUnits,
    decimal MeasurementMillimeters,
    string SourceUnit,
    int DimType,
    decimal Angle,
    decimal ObliqueAngle,
    int SortOrder);
```

Then add `IReadOnlyList<DimensionDto> Dimensions` to `FloorPlanReviewSessionDto` and load them in `SqliteFloorPlanReviewSessionReader` with `ORDER BY sort_order ASC, id ASC`.

- [ ] **Step 4: Run the targeted reader test again to verify GREEN**

Run:

```powershell
dotnet test .	ests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests" --artifacts-path .\.artifacts-test\infra-dimension-reader-green
```

Expected: PASS.

---

### Task 4: Show a read-only Dimensions section in Review

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

- [ ] **Step 1: Write the failing Desktop tests**

Add ViewModel assertions:

```csharp
Assert.Single(viewModel.Dimensions);
Assert.Single(viewModel.VisibleDimensions);
Assert.Equal("Dimensions (1)", viewModel.DimensionsSectionTitle);
viewModel.SelectedDimension = viewModel.Dimensions.Single();
Assert.Equal("Dimension", viewModel.SelectedArtifactTypeLabel);
Assert.Equal("10'-4"", viewModel.SelectedArtifactTitle);
Assert.Contains("GeometryBlock", viewModel.SelectedArtifactDetails, StringComparison.OrdinalIgnoreCase);
```

Add layout assertions:

```csharp
Assert.Contains("VisibleDimensions", xaml, StringComparison.Ordinal);
Assert.Contains("DimensionsSectionTitle", xaml, StringComparison.Ordinal);
Assert.Contains("SelectedDimension", xaml, StringComparison.Ordinal);
Assert.DoesNotContain("Exclude from Curation" IsEnabled="{Binding HasSelectedDimension}", xaml, StringComparison.Ordinal);
```

- [ ] **Step 2: Run the targeted Desktop tests to verify RED**

Run:

```powershell
dotnet test .	ests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests" --artifacts-path .\.artifacts-test\desktop-dimensions-red
```

Expected: FAIL because the Desktop review queue does not know dimensions yet.

- [ ] **Step 3: Extend the ViewModel and XAML without faking editability**

Add to the ViewModel:

```csharp
public ObservableCollection<DimensionDto> Dimensions { get; } = [];
public ObservableCollection<DimensionDto> VisibleDimensions { get; } = [];
[ObservableProperty] private DimensionDto? selectedDimension;
public string DimensionsSectionTitle => $"Dimensions ({VisibleDimensions.Count})";
```

Selection rules:

```csharp
- SelectedDimension participates in inspector title/subtitle/details
- dimensions are included in queue counts/search
- dimensions do NOT make CanUseActionsTool true
- dimensions do NOT route through ExcludeSelectedArtifactAsync
```

Update XAML to add a read-only Dimensions folder/list under the review queue and bind a simple inspector summary.

- [ ] **Step 4: Run the targeted Desktop tests again to verify GREEN**

Run:

```powershell
dotnet test .	ests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests" --artifacts-path .\.artifacts-test\desktop-dimensions-green
```

Expected: PASS.

---

### Task 5: Fresh verification before claiming success

**Files:**
- Verify only

- [ ] **Step 1: Run focused Application verification**

```powershell
dotnet test .	ests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~ExtractWallCandidatesHandlerTests" --artifacts-path .\.artifacts-testpp-dimensions-final
```

Expected: PASS.

- [ ] **Step 2: Run focused Infrastructure verification**

```powershell
dotnet test .	ests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaDimensionExtractorTests|FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests" --artifacts-path .\.artifacts-test\infra-dimensions-final
```

Expected: PASS.

- [ ] **Step 3: Run focused Desktop verification**

```powershell
dotnet test .	ests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests" --artifacts-path .\.artifacts-test\desktop-dimensions-final
```

Expected: PASS.

- [ ] **Step 4: Inspect the final diff and stop without building**

```powershell
git status --short
git diff --stat
```

Expected: only the intended dimension-extraction slice files changed.

- [ ] **Step 5: Do not build**

This repo explicitly forbids builds after changes. Test evidence is the only valid completion proof.
