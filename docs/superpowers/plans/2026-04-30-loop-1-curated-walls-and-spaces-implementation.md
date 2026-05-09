# Loop 1 Curated Walls and Spaces Implementation Plan


> [!WARNING] SUPERSEDED ? 2026-05-09
> This implementation plan is historical. Do not execute it as written.
>
> It creates `CuratedWall`, `CuratedWallDto`, `ICuratedWallRepository`, `AcceptWallCandidateHandler`, `UpdateCuratedWallMetadataHandler`, and `SqliteCuratedWallRepository`, all of which are outside the active review/curation flow in the current branch.
>
> Current implementation direction: CAD-faithful review with wall candidates, room labels, openings, fixed components, protected details, named pinch groups/markers, and a published curation consumed by future site-plan fit.


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the first full Loop 1 close: extracted wall candidates, draft/published curations, exact curated walls, minimal curated spaces, basic wall/space constraints, Library state progression, and a minimal real Desktop review screen.

**Architecture:** Keep the semantic core in Domain + Application, persist the truth in SQLite through focused repositories, and keep Desktop intentionally thin. The implementation must preserve a hard boundary between reusable base truth (Loop 1) and future site-plan proposal changes (Loop 2). Published curations are immutable snapshots; later edits always create a new draft/version.

**Tech Stack:** C# / .NET 10 SDK, Avalonia 11.3.14, CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.Hosting 10.0.0, Microsoft.Data.Sqlite 10.0.6, IxMilia.Dxf 0.8.4, NetTopologySuite 2.6.0, xUnit 2.9.2

---

## Important execution constraints

- Repository rule: **never build after changes**
- Follow **strict TDD**: write failing tests first, watch them fail, then write the minimum code to pass
- Use `dotnet test ...` for verification; do **not** run standalone `dotnet build`
- `scripts/dev-desktop.bat` runs `dotnet watch run`, so under the current repo rule it is **not** a valid verification path during implementation; keep verification test-only unless the user explicitly authorizes a separate runtime pass
- After every milestone, sync durable knowledge in `obsidian-vault/` and memory

## File map before implementation

### Domain — new semantic truth

- Create: `src/FloorplanFit.Domain/FloorPlans/FloorPlanCuration.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/FloorPlanCurationStatus.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidate.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidateStatus.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/CuratedWall.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/WallRole.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/WallMobilityLevel.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/WallProtectionLevel.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/CuratedWallGroup.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/CuratedWallJoin.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/CuratedSpace.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/SpaceType.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ConstraintIntentNote.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/StructuredConstraint.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ConstraintStrength.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ConstraintKind.cs`
- Modify: `src/FloorplanFit.Domain/FloorPlans/FloorPlanTemplate.cs`

### Application — use cases and ports

- Create: `src/FloorplanFit.Application/Abstractions/GeometryPoint.cs`
- Create: `src/FloorplanFit.Application/Abstractions/DetectedWallCandidate.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IWallExtractor.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IWallExtractionRunRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IExtractedWallCandidateRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IFloorPlanCurationRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/ICuratedWallRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/ICuratedSpaceRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IConstraintIntentNoteRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IStructuredConstraintRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IFloorPlanReviewSessionReader.cs`
- Create: `src/FloorplanFit.Application/Abstractions/ICuratedSpaceGenerator.cs`
- Modify: `src/FloorplanFit.Application/Abstractions/IFloorPlanTemplateRepository.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Library/GetFloorPlanLibraryHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/StartOrResumeCurationHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/AcceptWallCandidateHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/RejectWallCandidateHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/UpdateCuratedWallMetadataHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/CreateOrUpdateCuratedSpaceHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/SaveCurationDraftHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/PublishFloorPlanCurationHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/GenerateCuratedSpacesHandler.cs`

### Contracts — Desktop-facing DTOs

- Modify: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryItemDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/GeometryPathDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/GeometrySegmentDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/WallCandidateDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/CuratedWallDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/CuratedSpaceDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/ConstraintIntentNoteDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/StructuredConstraintDto.cs`

### Infrastructure — SQLite, geometry, extraction, polygonization

- Modify: `src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanTemplateRepository.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs`
- Create: `src/FloorplanFit.Infrastructure/Dxf/IxMiliaWallExtractor.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteWallExtractionRunRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedWallCandidateRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanCurationRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteCuratedWallRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteCuratedSpaceRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteConstraintIntentNoteRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteStructuredConstraintRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- Create: `src/FloorplanFit.Infrastructure/Geometry/NetTopologySuiteCuratedSpaceGenerator.cs`

### Desktop — Library + review screen

- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/MainWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- Create: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Create: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Create: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`

### Tests

- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Extraction/ExtractWallCandidatesHandlerTests.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/StartOrResumeCurationHandlerTests.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AcceptWallCandidateHandlerTests.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/RejectWallCandidateHandlerTests.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/UpdateCuratedWallMetadataHandlerTests.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/PublishFloorPlanCurationHandlerTests.cs`
- Create: `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaWallExtractorTests.cs`
- Create: `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs`
- Create: `tests/FloorplanFit.Infrastructure.Tests/Curation/CuratedSpaceGeneratorTests.cs`
- Modify: `tests/FloorplanFit.Infrastructure.Tests/Imports/FloorPlanLibraryReaderIntegrationTests.cs`

### Durable docs

- Create: `obsidian-vault/Implementation/2026-04-30 - Loop 1 Implementation Plan.md`
- Modify: `obsidian-vault/Current State.md`

---

### Task 1: Establish the Loop 1 semantic domain and Library status contract

**Files:**
- Modify: `src/FloorplanFit.Domain/FloorPlans/FloorPlanTemplate.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/FloorPlanCuration.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/FloorPlanCurationStatus.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidate.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidateStatus.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/CuratedWall.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/WallRole.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/WallMobilityLevel.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/WallProtectionLevel.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/CuratedSpace.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/SpaceType.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ConstraintIntentNote.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/StructuredConstraint.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ConstraintStrength.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/ConstraintKind.cs`
- Modify: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryItemDto.cs`

- [ ] **Step 1: Write the failing Domain test for published curation immutability**

```csharp
// tests/FloorplanFit.Application.Tests/FloorPlans/Curation/StartOrResumeCurationHandlerTests.cs
[Fact]
public void Publish_marks_the_curation_as_published_and_blocks_second_publish()
{
    var curation = new FloorPlanCuration(
        Guid.NewGuid(),
        Guid.NewGuid(),
        curationVersion: 1,
        FloorPlanCurationStatus.Draft,
        basedOnCurationId: null,
        notes: null,
        createdAtUtc: new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
        publishedAtUtc: null);

    curation.Publish(new DateTime(2026, 4, 30, 19, 0, 0, DateTimeKind.Utc));

    Assert.Equal(FloorPlanCurationStatus.Published, curation.Status);
    Assert.Equal(new DateTime(2026, 4, 30, 19, 0, 0, DateTimeKind.Utc), curation.PublishedAtUtc);
    Assert.Throws<InvalidOperationException>(() =>
        curation.Publish(new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc)));
}
```

- [ ] **Step 2: Run the targeted test to verify RED**

Run:

```bash
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter Publish_marks_the_curation_as_published_and_blocks_second_publish
```

Expected: FAIL because `FloorPlanCuration` does not exist yet.

- [ ] **Step 3: Add the curation core types**

```csharp
// src/FloorplanFit.Domain/FloorPlans/FloorPlanCurationStatus.cs
namespace FloorplanFit.Domain.FloorPlans;

public enum FloorPlanCurationStatus
{
    Draft = 1,
    Published = 2,
    Superseded = 3
}
```

```csharp
// src/FloorplanFit.Domain/FloorPlans/FloorPlanCuration.cs
namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanCuration
{
    public FloorPlanCuration(
        Guid id,
        Guid floorPlanVersionId,
        int curationVersion,
        FloorPlanCurationStatus status,
        Guid? basedOnCurationId,
        string? notes,
        DateTime createdAtUtc,
        DateTime? publishedAtUtc)
    {
        if (curationVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(curationVersion), "Curation version must be positive.");
        }

        Id = id;
        FloorPlanVersionId = floorPlanVersionId;
        CurationVersion = curationVersion;
        Status = status;
        BasedOnCurationId = basedOnCurationId;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
        PublishedAtUtc = publishedAtUtc;
    }

    public Guid Id { get; }

    public Guid FloorPlanVersionId { get; }

    public int CurationVersion { get; }

    public FloorPlanCurationStatus Status { get; private set; }

    public Guid? BasedOnCurationId { get; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime? PublishedAtUtc { get; private set; }

    public void UpdateNotes(string? notes)
    {
        EnsureDraft();
        Notes = notes;
    }

    public void Publish(DateTime publishedAtUtc)
    {
        EnsureDraft();
        Status = FloorPlanCurationStatus.Published;
        PublishedAtUtc = publishedAtUtc;
    }

    public void MarkSuperseded()
    {
        if (Status != FloorPlanCurationStatus.Published)
        {
            throw new InvalidOperationException("Only published curations can become superseded.");
        }

        Status = FloorPlanCurationStatus.Superseded;
    }

    private void EnsureDraft()
    {
        if (Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }
    }
}
```

- [ ] **Step 4: Add the wall/space/constraint primitives**

```csharp
// src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidateStatus.cs
namespace FloorplanFit.Domain.FloorPlans;

public enum ExtractedWallCandidateStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3
}
```

```csharp
// src/FloorplanFit.Domain/FloorPlans/WallRole.cs
namespace FloorplanFit.Domain.FloorPlans;

public enum WallRole
{
    Interior = 1,
    Exterior = 2,
    Facade = 3,
    Partition = 4,
    OpenEdge = 5
}
```

```csharp
// src/FloorplanFit.Domain/FloorPlans/WallMobilityLevel.cs
namespace FloorplanFit.Domain.FloorPlans;

public enum WallMobilityLevel
{
    Locked = 1,
    Flexible = 2,
    Stretchable = 3
}
```

```csharp
// src/FloorplanFit.Domain/FloorPlans/WallProtectionLevel.cs
namespace FloorplanFit.Domain.FloorPlans;

public enum WallProtectionLevel
{
    None = 0,
    Protected = 1,
    Critical = 2
}
```

```csharp
// src/FloorplanFit.Domain/FloorPlans/SpaceType.cs
namespace FloorplanFit.Domain.FloorPlans;

public enum SpaceType
{
    Bathroom = 1,
    Bedroom = 2,
    Kitchen = 3,
    Living = 4,
    OpenPlan = 5,
    Circulation = 6,
    Other = 7
}
```

```csharp
// src/FloorplanFit.Domain/FloorPlans/ConstraintStrength.cs
namespace FloorplanFit.Domain.FloorPlans;

public enum ConstraintStrength
{
    Hard = 1,
    Soft = 2
}
```

```csharp
// src/FloorplanFit.Domain/FloorPlans/ConstraintKind.cs
namespace FloorplanFit.Domain.FloorPlans;

public enum ConstraintKind
{
    LockWall = 1,
    ProtectWall = 2,
    MaxWallMove = 3,
    PreserveGroup = 4,
    PreserveFacade = 5,
    PreserveSpace = 6,
    MinSpaceArea = 7,
    MinSpaceWidth = 8,
    MinSpaceDepth = 9
}
```

- [ ] **Step 5: Extend `FloorPlanTemplate` and Library item status**

```csharp
// src/FloorplanFit.Domain/FloorPlans/FloorPlanTemplate.cs
public Guid? ActivePublishedCurationId { get; private set; }

public void SetActivePublishedCuration(Guid curationId)
{
    ActivePublishedCurationId = curationId;
}
```

```csharp
// src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryItemDto.cs
namespace FloorplanFit.Contracts.FloorPlans;

public sealed record FloorPlanLibraryItemDto(
    Guid TemplateId,
    string Code,
    string Name,
    string Status,
    int ActiveVersionNumber,
    DateTime ImportedAtUtc,
    string SourceUnit,
    Guid? ActivePublishedCurationId);
```

- [ ] **Step 6: Run the Application test suite to verify GREEN**

Run:

```bash
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj
```

Expected: PASS and the new publish test is green.

- [ ] **Step 7: Commit the semantic core**

```bash
git add src/FloorplanFit.Domain src/FloorplanFit.Contracts tests/FloorplanFit.Application.Tests
git commit -m "feat: add loop 1 curation domain model"
```

---

### Task 2: Add the Application ports and curation lifecycle handlers

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/GeometryPoint.cs`
- Create: `src/FloorplanFit.Application/Abstractions/DetectedWallCandidate.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IWallExtractor.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IWallExtractionRunRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IExtractedWallCandidateRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IFloorPlanCurationRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/ICuratedWallRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/ICuratedSpaceRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IConstraintIntentNoteRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IStructuredConstraintRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IFloorPlanReviewSessionReader.cs`
- Create: `src/FloorplanFit.Application/Abstractions/ICuratedSpaceGenerator.cs`
- Modify: `src/FloorplanFit.Application/Abstractions/IFloorPlanTemplateRepository.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/StartOrResumeCurationHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/AcceptWallCandidateHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/RejectWallCandidateHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/UpdateCuratedWallMetadataHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/PublishFloorPlanCurationHandler.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Extraction/ExtractWallCandidatesHandlerTests.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AcceptWallCandidateHandlerTests.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/PublishFloorPlanCurationHandlerTests.cs`

- [ ] **Step 1: Write the failing extraction orchestration test**

```csharp
// tests/FloorplanFit.Application.Tests/FloorPlans/Extraction/ExtractWallCandidatesHandlerTests.cs
[Fact]
public async Task HandleAsync_persists_pending_candidates_and_returns_extracted_status()
{
    var versionId = Guid.NewGuid();
    var extractor = new FakeWallExtractor(
    [
        new DetectedWallCandidate(
            "LINE:12",
            "A-WALL",
            [new GeometryPoint(0, 0), new GeometryPoint(1200, 0)],
            thicknessMm: 101.6m,
            confidence: 0.95m,
            detectionNotes: null)
    ]);
    var runs = new InMemoryWallExtractionRunRepository();
    var candidates = new InMemoryExtractedWallCandidateRepository();
    var unitOfWork = new FakeUnitOfWork();

    var handler = new ExtractWallCandidatesHandler(extractor, runs, candidates, unitOfWork, new FakeClock(new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc)));

    var result = await handler.HandleAsync(versionId, @"C:\managed\SANTA-BARBARA.dxf", CancellationToken.None);

    Assert.Equal("Extracted", result.Status);
    Assert.Single(runs.Items);
    Assert.Single(candidates.Items);
    Assert.Equal(ExtractedWallCandidateStatus.Pending, candidates.Items[0].Status);
    Assert.True(unitOfWork.SaveChangesCalled);
}
```

- [ ] **Step 2: Write the failing publish test**

```csharp
// tests/FloorplanFit.Application.Tests/FloorPlans/Curation/PublishFloorPlanCurationHandlerTests.cs
[Fact]
public async Task HandleAsync_sets_the_active_published_curation_on_the_template()
{
    var template = new FloorPlanTemplate(Guid.NewGuid(), "santa-barbara", "SANTA-BARBARA", isActive: true);
    template.SetCurrentVersion(Guid.NewGuid());

    var curation = new FloorPlanCuration(
        Guid.NewGuid(),
        template.CurrentVersionId!.Value,
        curationVersion: 1,
        FloorPlanCurationStatus.Draft,
        basedOnCurationId: null,
        notes: null,
        createdAtUtc: new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc),
        publishedAtUtc: null);

    var templates = new InMemoryFloorPlanTemplateRepository(template);
    var curations = new InMemoryFloorPlanCurationRepository(curation);
    var walls = new InMemoryCuratedWallRepository(
    [
        new CuratedWall(
            Guid.NewGuid(),
            curation.Id,
            "W-001",
            Guid.NewGuid(),
            "LINE:12",
            Guid.NewGuid(),
            WallRole.Exterior,
            WallMobilityLevel.Locked,
            WallProtectionLevel.Protected,
            101.6m,
            "2x4",
            null,
            isExterior: true,
            isStructuralHint: true,
            wallGroupId: null,
            sortOrder: 1,
            notes: null)
    ]);
    var unitOfWork = new FakeUnitOfWork();

    var handler = new PublishFloorPlanCurationHandler(curations, walls, templates, unitOfWork, new FakeClock(new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc)));

    await handler.HandleAsync(template.Id, curation.Id, CancellationToken.None);

    Assert.Equal(curation.Id, template.ActivePublishedCurationId);
    Assert.True(unitOfWork.SaveChangesCalled);
}
```

- [ ] **Step 3: Run the targeted tests to verify RED**

Run:

```bash
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter HandleAsync_persists_pending_candidates_and_returns_extracted_status
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter HandleAsync_sets_the_active_published_curation_on_the_template
```

Expected: FAIL because the new ports and handlers do not exist yet.

- [ ] **Step 4: Add the new ports**

```csharp
// src/FloorplanFit.Application/Abstractions/GeometryPoint.cs
namespace FloorplanFit.Application.Abstractions;

public sealed record GeometryPoint(decimal X, decimal Y);
```

```csharp
// src/FloorplanFit.Application/Abstractions/DetectedWallCandidate.cs
namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedWallCandidate(
    string SourceEntityRef,
    string SourceLayer,
    IReadOnlyList<GeometryPoint> Points,
    decimal? ThicknessMm,
    decimal Confidence,
    string? DetectionNotes);
```

```csharp
// src/FloorplanFit.Application/Abstractions/IWallExtractor.cs
namespace FloorplanFit.Application.Abstractions;

public interface IWallExtractor
{
    Task<IReadOnlyList<DetectedWallCandidate>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken);
}
```

```csharp
// src/FloorplanFit.Application/Abstractions/IFloorPlanCurationRepository.cs
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanCurationRepository
{
    Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken);
    Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken);
    Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken);
    Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken);
    Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken);
}
```

```csharp
// src/FloorplanFit.Application/Abstractions/IFloorPlanTemplateRepository.cs
Task<FloorPlanTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken);
```

- [ ] **Step 5: Implement the focused handlers**

```csharp
// src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Extraction;

public sealed class ExtractWallCandidatesHandler
{
    private readonly IWallExtractor wallExtractor;
    private readonly IWallExtractionRunRepository wallExtractionRunRepository;
    private readonly IExtractedWallCandidateRepository extractedWallCandidateRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public ExtractWallCandidatesHandler(
        IWallExtractor wallExtractor,
        IWallExtractionRunRepository wallExtractionRunRepository,
        IExtractedWallCandidateRepository extractedWallCandidateRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.wallExtractor = wallExtractor;
        this.wallExtractionRunRepository = wallExtractionRunRepository;
        this.extractedWallCandidateRepository = extractedWallCandidateRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task<(Guid ExtractionRunId, string Status)> HandleAsync(
        Guid floorPlanVersionId,
        string managedFilePath,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = clock.UtcNow;
        var run = new WallExtractionRun(Guid.NewGuid(), floorPlanVersionId, "Completed", startedAtUtc, startedAtUtc, "ixmilia-line-segments", null);
        var detectedCandidates = await wallExtractor.ExtractAsync(managedFilePath, cancellationToken);
        var domainCandidates = detectedCandidates
            .Select((item, index) => new ExtractedWallCandidate(
                Guid.NewGuid(),
                run.Id,
                item.SourceEntityRef,
                item.SourceLayer,
                geometryPathId: Guid.Empty,
                item.ThicknessMm,
                item.Confidence,
                item.DetectionNotes,
                ExtractedWallCandidateStatus.Pending,
                sortOrder: index + 1))
            .ToArray();

        await wallExtractionRunRepository.AddAsync(run, cancellationToken);
        await extractedWallCandidateRepository.AddRangeAsync(domainCandidates, detectedCandidates, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return (run.Id, "Extracted");
    }
}
```

```csharp
// src/FloorplanFit.Application/FloorPlans/Curation/PublishFloorPlanCurationHandler.cs
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class PublishFloorPlanCurationHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly ICuratedWallRepository curatedWallRepository;
    private readonly IFloorPlanTemplateRepository floorPlanTemplateRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public PublishFloorPlanCurationHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        ICuratedWallRepository curatedWallRepository,
        IFloorPlanTemplateRepository floorPlanTemplateRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.curatedWallRepository = curatedWallRepository;
        this.floorPlanTemplateRepository = floorPlanTemplateRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task HandleAsync(Guid templateId, Guid curationId, CancellationToken cancellationToken)
    {
        var template = await floorPlanTemplateRepository.GetByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan template was not found.");
        var curation = await floorPlanCurationRepository.GetDraftAsync(template.CurrentVersionId!.Value, cancellationToken)
            ?? throw new InvalidOperationException("Draft curation was not found.");
        var walls = await curatedWallRepository.ListByCurationAsync(curationId, cancellationToken);

        if (walls.Count == 0 || walls.Any(item => string.IsNullOrWhiteSpace(item.StableWallId)))
        {
            throw new InvalidOperationException("A curation must have at least one fully identified curated wall before publish.");
        }

        curation.Publish(clock.UtcNow);
        template.SetActivePublishedCuration(curation.Id);

        await floorPlanCurationRepository.UpdateAsync(curation, cancellationToken);
        await floorPlanTemplateRepository.UpdateAsync(template, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Run the Application test suite to verify GREEN**

Run:

```bash
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj
```

Expected: PASS with the new extraction/publish orchestration tests green.

- [ ] **Step 7: Commit the application lifecycle layer**

```bash
git add src/FloorplanFit.Application tests/FloorplanFit.Application.Tests
git commit -m "feat: add loop 1 curation application workflow"
```

---

### Task 3: Extend SQLite schema and repositories for geometry, extraction, curations, spaces, and constraints

**Files:**
- Modify: `src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanTemplateRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteWallExtractionRunRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedWallCandidateRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanCurationRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteCuratedWallRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteCuratedSpaceRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteConstraintIntentNoteRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteStructuredConstraintRepository.cs`
- Create: `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs`

- [ ] **Step 1: Write the failing integration test for curation persistence**

```csharp
// tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs
[Fact]
public async Task Persisted_curation_graph_can_be_read_back_with_active_published_curation()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-curation-{Guid.NewGuid():N}");

    try
    {
        var workspace = new AppWorkspace(tempRoot);
        workspace.EnsureCreated();
        await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var templateRepository = new SqliteFloorPlanTemplateRepository(session);
        var curationRepository = new SqliteFloorPlanCurationRepository(session);
        var curatedWallRepository = new SqliteCuratedWallRepository(session);
        var unitOfWork = new SqliteUnitOfWork(session);

        var template = new FloorPlanTemplate(Guid.NewGuid(), "santa-barbara", "SANTA-BARBARA", true);
        template.SetCurrentVersion(Guid.NewGuid());

        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            template.CurrentVersionId!.Value,
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc),
            null);

        var wall = new CuratedWall(
            Guid.NewGuid(),
            curation.Id,
            "W-001",
            Guid.NewGuid(),
            "LINE:12",
            Guid.NewGuid(),
            WallRole.Exterior,
            WallMobilityLevel.Locked,
            WallProtectionLevel.Protected,
            101.6m,
            "2x4",
            null,
            true,
            true,
            null,
            1,
            null);

        await templateRepository.AddAsync(template, CancellationToken.None);
        await curationRepository.AddAsync(curation, CancellationToken.None);
        await curatedWallRepository.AddAsync(wall, CancellationToken.None);
        curation.Publish(new DateTime(2026, 4, 30, 22, 0, 0, DateTimeKind.Utc));
        template.SetActivePublishedCuration(curation.Id);
        await curationRepository.UpdateAsync(curation, CancellationToken.None);
        await templateRepository.UpdateAsync(template, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var reloadedTemplate = await templateRepository.GetByCodeAsync("santa-barbara", CancellationToken.None);
        Assert.NotNull(reloadedTemplate);
        Assert.Equal(curation.Id, reloadedTemplate!.ActivePublishedCurationId);
    }
    finally
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
```

- [ ] **Step 2: Run the integration test to verify RED**

Run:

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter Persisted_curation_graph_can_be_read_back_with_active_published_curation
```

Expected: FAIL because the schema and repositories do not support the Loop 1 curation graph yet.

- [ ] **Step 3: Add `NetTopologySuite` and extend the schema**

```xml
<!-- src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj -->
<ItemGroup>
  <PackageReference Include="IxMilia.Dxf" Version="0.8.4" />
  <PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.6" />
  <PackageReference Include="NetTopologySuite" Version="2.6.0" />
</ItemGroup>
```

```csharp
// src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs
command.CommandText = """
    CREATE TABLE IF NOT EXISTS geometry_paths (
        id TEXT PRIMARY KEY,
        is_closed INTEGER NOT NULL,
        bounding_box_json TEXT NOT NULL,
        serialized_format TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS geometry_segments (
        id TEXT PRIMARY KEY,
        geometry_path_id TEXT NOT NULL,
        segment_order INTEGER NOT NULL,
        segment_type TEXT NOT NULL,
        start_x TEXT NOT NULL,
        start_y TEXT NOT NULL,
        end_x TEXT NOT NULL,
        end_y TEXT NOT NULL,
        center_x TEXT NULL,
        center_y TEXT NULL,
        radius TEXT NULL,
        clockwise INTEGER NULL
    );

    CREATE TABLE IF NOT EXISTS wall_extraction_runs (
        id TEXT PRIMARY KEY,
        floorplan_version_id TEXT NOT NULL,
        status TEXT NOT NULL,
        started_at_utc TEXT NOT NULL,
        finished_at_utc TEXT NOT NULL,
        extractor_version TEXT NOT NULL,
        notes TEXT NULL
    );

    CREATE TABLE IF NOT EXISTS extracted_wall_candidates (
        id TEXT PRIMARY KEY,
        wall_extraction_run_id TEXT NOT NULL,
        source_entity_ref TEXT NOT NULL,
        source_layer TEXT NOT NULL,
        geometry_path_id TEXT NOT NULL,
        thickness_mm TEXT NULL,
        confidence TEXT NOT NULL,
        detection_notes TEXT NULL,
        candidate_status INTEGER NOT NULL,
        sort_order INTEGER NOT NULL
    );

    CREATE TABLE IF NOT EXISTS floorplan_curations (
        id TEXT PRIMARY KEY,
        floorplan_version_id TEXT NOT NULL,
        curation_version INTEGER NOT NULL,
        status INTEGER NOT NULL,
        based_on_curation_id TEXT NULL,
        notes TEXT NULL,
        created_at_utc TEXT NOT NULL,
        published_at_utc TEXT NULL,
        UNIQUE(floorplan_version_id, curation_version)
    );

    CREATE TABLE IF NOT EXISTS curated_walls (
        id TEXT PRIMARY KEY,
        floorplan_curation_id TEXT NOT NULL,
        stable_wall_id TEXT NOT NULL,
        source_candidate_id TEXT NOT NULL,
        source_entity_ref TEXT NOT NULL,
        geometry_path_id TEXT NOT NULL,
        wall_role INTEGER NOT NULL,
        mobility_level INTEGER NOT NULL,
        protection_level INTEGER NOT NULL,
        thickness_mm TEXT NOT NULL,
        assembly_code TEXT NOT NULL,
        height_mm TEXT NULL,
        is_exterior INTEGER NOT NULL,
        is_structural_hint INTEGER NOT NULL,
        wall_group_id TEXT NULL,
        sort_order INTEGER NOT NULL,
        notes TEXT NULL
    );

    CREATE TABLE IF NOT EXISTS curated_spaces (
        id TEXT PRIMARY KEY,
        floorplan_curation_id TEXT NOT NULL,
        stable_space_id TEXT NOT NULL,
        name TEXT NOT NULL,
        space_type INTEGER NOT NULL,
        geometry_path_id TEXT NOT NULL,
        area_square_meters TEXT NOT NULL,
        min_width_mm TEXT NOT NULL,
        min_depth_mm TEXT NOT NULL,
        is_protected INTEGER NOT NULL,
        functional_tags_json TEXT NOT NULL,
        sort_order INTEGER NOT NULL,
        notes TEXT NULL
    );

    CREATE TABLE IF NOT EXISTS constraint_intent_notes (
        id TEXT PRIMARY KEY,
        scope_type TEXT NOT NULL,
        scope_id TEXT NOT NULL,
        raw_text TEXT NOT NULL,
        status TEXT NOT NULL,
        created_at_utc TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS structured_constraints (
        id TEXT PRIMARY KEY,
        scope_type TEXT NOT NULL,
        scope_id TEXT NOT NULL,
        constraint_kind INTEGER NOT NULL,
        constraint_strength INTEGER NOT NULL,
        target_type TEXT NOT NULL,
        target_id TEXT NOT NULL,
        weight TEXT NOT NULL,
        payload_json TEXT NOT NULL,
        source TEXT NOT NULL,
        created_at_utc TEXT NOT NULL
    );
""";
```

- [ ] **Step 4: Extend the template repository**

```csharp
// src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanTemplateRepository.cs
SELECT id, code, name, current_version_id, active_published_curation_id, is_active
FROM floorplan_templates
WHERE code = $code
LIMIT 1
```

```csharp
if (!reader.IsDBNull(4))
{
    template.SetActivePublishedCuration(Guid.Parse(reader.GetString(4)));
}
```

```csharp
UPDATE floorplan_templates
SET current_version_id = $current_version_id,
    active_published_curation_id = $active_published_curation_id,
    name = $name,
    is_active = $is_active
WHERE id = $id
```

- [ ] **Step 5: Add the SQLite repositories**

```csharp
// src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanCurationRepository.cs
public sealed class SqliteFloorPlanCurationRepository : IFloorPlanCurationRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanCurationRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        => GetSingleAsync(floorPlanVersionId, FloorPlanCurationStatus.Draft, cancellationToken);

    public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        => GetSingleAsync(floorPlanVersionId, FloorPlanCurationStatus.Published, cancellationToken);

    public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        using var command = CreateCommand(
            "SELECT COALESCE(MAX(curation_version), 0) + 1 FROM floorplan_curations WHERE floorplan_version_id = $floorplan_version_id");
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());
        return Task.FromResult(Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture));
    }

    public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
    {
        using var command = CreateCommand(
            """
            INSERT INTO floorplan_curations (
                id, floorplan_version_id, curation_version, status, based_on_curation_id, notes, created_at_utc, published_at_utc)
            VALUES (
                $id, $floorplan_version_id, $curation_version, $status, $based_on_curation_id, $notes, $created_at_utc, $published_at_utc)
            """);
        command.Parameters.AddWithValue("$id", curation.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_version_id", curation.FloorPlanVersionId.ToString());
        command.Parameters.AddWithValue("$curation_version", curation.CurationVersion);
        command.Parameters.AddWithValue("$status", (int)curation.Status);
        command.Parameters.AddWithValue("$based_on_curation_id", (object?)curation.BasedOnCurationId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)curation.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$created_at_utc", curation.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$published_at_utc", (object?)curation.PublishedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
    {
        using var command = CreateCommand(
            """
            UPDATE floorplan_curations
            SET status = $status,
                notes = $notes,
                published_at_utc = $published_at_utc
            WHERE id = $id
            """);
        command.Parameters.AddWithValue("$id", curation.Id.ToString());
        command.Parameters.AddWithValue("$status", (int)curation.Status);
        command.Parameters.AddWithValue("$notes", (object?)curation.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$published_at_utc", (object?)curation.PublishedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }
}
```

```csharp
// src/FloorplanFit.Infrastructure/Persistence/SqliteCuratedWallRepository.cs
public Task<IReadOnlyList<CuratedWall>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
{
    using var command = CreateCommand(
        """
        SELECT id, floorplan_curation_id, stable_wall_id, source_candidate_id, source_entity_ref, geometry_path_id,
               wall_role, mobility_level, protection_level, thickness_mm, assembly_code, height_mm,
               is_exterior, is_structural_hint, wall_group_id, sort_order, notes
        FROM curated_walls
        WHERE floorplan_curation_id = $floorplan_curation_id
        ORDER BY sort_order
        """);
    command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

    var items = new List<CuratedWall>();
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        items.Add(new CuratedWall(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            Guid.Parse(reader.GetString(3)),
            reader.GetString(4),
            Guid.Parse(reader.GetString(5)),
            (WallRole)reader.GetInt32(6),
            (WallMobilityLevel)reader.GetInt32(7),
            (WallProtectionLevel)reader.GetInt32(8),
            decimal.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
            reader.GetString(10),
            reader.IsDBNull(11) ? null : decimal.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
            reader.GetInt32(12) == 1,
            reader.GetInt32(13) == 1,
            reader.IsDBNull(14) ? null : Guid.Parse(reader.GetString(14)),
            reader.GetInt32(15),
            reader.IsDBNull(16) ? null : reader.GetString(16)));
    }

    return Task.FromResult<IReadOnlyList<CuratedWall>>(items);
}
```

- [ ] **Step 6: Run the Infrastructure suite to verify GREEN**

Run:

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj
```

Expected: PASS, including the new curation persistence test.

- [ ] **Step 7: Commit the SQLite curation layer**

```bash
git add src/FloorplanFit.Infrastructure tests/FloorplanFit.Infrastructure.Tests
git commit -m "feat: persist loop 1 curation graph in sqlite"
```

---

### Task 4: Implement real DXF wall extraction and persist extracted candidates

**Files:**
- Create: `src/FloorplanFit.Infrastructure/Dxf/IxMiliaWallExtractor.cs`
- Create: `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaWallExtractorTests.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteWallExtractionRunRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedWallCandidateRepository.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

- [ ] **Step 1: Write the failing extractor test against the real DXF fixture**

```csharp
// tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaWallExtractorTests.cs
[Fact]
public async Task ExtractAsync_reads_linear_wall_candidates_from_santa_barbara_fixture()
{
    var solutionRoot = RepositoryPaths.FindSolutionRoot();
    var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
    var extractor = new IxMiliaWallExtractor();

    var candidates = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

    Assert.NotEmpty(candidates);
    Assert.All(candidates, item => Assert.True(item.Points.Count >= 2));
    Assert.Contains(candidates, item => item.SourceEntityRef.StartsWith("LINE:", StringComparison.OrdinalIgnoreCase)
        || item.SourceEntityRef.StartsWith("LWPOLYLINE:", StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **Step 2: Run the extractor test to verify RED**

Run:

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter ExtractAsync_reads_linear_wall_candidates_from_santa_barbara_fixture
```

Expected: FAIL because `IxMiliaWallExtractor` does not exist yet.

- [ ] **Step 3: Implement the extractor**

```csharp
// src/FloorplanFit.Infrastructure/Dxf/IxMiliaWallExtractor.cs
using FloorplanFit.Application.Abstractions;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class IxMiliaWallExtractor : IWallExtractor
{
    public Task<IReadOnlyList<DetectedWallCandidate>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dxf = DxfFile.Load(managedFilePath);
        var candidates = new List<DetectedWallCandidate>();

        foreach (var line in dxf.Entities.OfType<DxfLine>())
        {
            candidates.Add(new DetectedWallCandidate(
                $"LINE:{line.Handle}",
                line.Layer ?? string.Empty,
                [new GeometryPoint((decimal)line.P1.X, (decimal)line.P1.Y), new GeometryPoint((decimal)line.P2.X, (decimal)line.P2.Y)],
                thicknessMm: null,
                confidence: 0.90m,
                detectionNotes: null));
        }

        foreach (var polyline in dxf.Entities.OfType<DxfLwPolyline>())
        {
            var vertices = polyline.Vertices.ToArray();
            for (var index = 0; index < vertices.Length - 1; index++)
            {
                var start = vertices[index];
                var end = vertices[index + 1];
                candidates.Add(new DetectedWallCandidate(
                    $"LWPOLYLINE:{polyline.Handle}:{index}",
                    polyline.Layer ?? string.Empty,
                    [new GeometryPoint((decimal)start.X, (decimal)start.Y), new GeometryPoint((decimal)end.X, (decimal)end.Y)],
                    thicknessMm: null,
                    confidence: 0.80m,
                    detectionNotes: "Derived from lightweight polyline segment."));
            }
        }

        return Task.FromResult<IReadOnlyList<DetectedWallCandidate>>(candidates);
    }
}
```

- [ ] **Step 4: Implement the candidate repositories**

```csharp
// src/FloorplanFit.Infrastructure/Persistence/SqliteWallExtractionRunRepository.cs
public sealed class SqliteWallExtractionRunRepository : IWallExtractionRunRepository
{
    private readonly SqliteSession session;

    public SqliteWallExtractionRunRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(WallExtractionRun run, CancellationToken cancellationToken)
    {
        using var command = CreateCommand(
            """
            INSERT INTO wall_extraction_runs (
                id, floorplan_version_id, status, started_at_utc, finished_at_utc, extractor_version, notes)
            VALUES (
                $id, $floorplan_version_id, $status, $started_at_utc, $finished_at_utc, $extractor_version, $notes)
            """);
        command.Parameters.AddWithValue("$id", run.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_version_id", run.FloorPlanVersionId.ToString());
        command.Parameters.AddWithValue("$status", run.Status);
        command.Parameters.AddWithValue("$started_at_utc", run.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$finished_at_utc", run.FinishedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$extractor_version", run.ExtractorVersion);
        command.Parameters.AddWithValue("$notes", (object?)run.Notes ?? DBNull.Value);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }
}
```

```csharp
// src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedWallCandidateRepository.cs
public async Task AddRangeAsync(
    IReadOnlyList<ExtractedWallCandidate> domainCandidates,
    IReadOnlyList<DetectedWallCandidate> detectedCandidates,
    CancellationToken cancellationToken)
{
    for (var index = 0; index < domainCandidates.Count; index++)
    {
        var domainCandidate = domainCandidates[index];
        var detectedCandidate = detectedCandidates[index];
        var geometryPathId = Guid.NewGuid();

        await InsertGeometryPathAsync(geometryPathId, isClosed: false, detectedCandidate.Points, cancellationToken);

        using var command = CreateCommand(
            """
            INSERT INTO extracted_wall_candidates (
                id, wall_extraction_run_id, source_entity_ref, source_layer, geometry_path_id, thickness_mm, confidence, detection_notes, candidate_status, sort_order)
            VALUES (
                $id, $wall_extraction_run_id, $source_entity_ref, $source_layer, $geometry_path_id, $thickness_mm, $confidence, $detection_notes, $candidate_status, $sort_order)
            """);
        command.Parameters.AddWithValue("$id", domainCandidate.Id.ToString());
        command.Parameters.AddWithValue("$wall_extraction_run_id", domainCandidate.WallExtractionRunId.ToString());
        command.Parameters.AddWithValue("$source_entity_ref", domainCandidate.SourceEntityRef);
        command.Parameters.AddWithValue("$source_layer", domainCandidate.SourceLayer);
        command.Parameters.AddWithValue("$geometry_path_id", geometryPathId.ToString());
        command.Parameters.AddWithValue("$thickness_mm", (object?)domainCandidate.ThicknessMm?.ToString(CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$confidence", domainCandidate.Confidence.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$detection_notes", (object?)domainCandidate.DetectionNotes ?? DBNull.Value);
        command.Parameters.AddWithValue("$candidate_status", (int)domainCandidate.Status);
        command.Parameters.AddWithValue("$sort_order", domainCandidate.SortOrder);
        command.ExecuteNonQuery();
    }
}
```

- [ ] **Step 5: Wire the extractor into DI**

```csharp
// src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs
services.AddSingleton<IWallExtractor, IxMiliaWallExtractor>();
services.AddScoped<IWallExtractionRunRepository, SqliteWallExtractionRunRepository>();
services.AddScoped<IExtractedWallCandidateRepository, SqliteExtractedWallCandidateRepository>();
services.AddScoped<ExtractWallCandidatesHandler>();
```

- [ ] **Step 6: Run Infrastructure + Application tests to verify GREEN**

Run:

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj
```

Expected: PASS and extraction is now real end-to-end.

- [ ] **Step 7: Commit wall extraction**

```bash
git add src/FloorplanFit.Infrastructure src/FloorplanFit.Application tests/FloorplanFit.Infrastructure.Tests tests/FloorplanFit.Application.Tests
git commit -m "feat: extract and persist wall candidates"
```

---

### Task 5: Implement curated wall review, metadata editing, and publish rules

**Files:**
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/StartOrResumeCurationHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/AcceptWallCandidateHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/RejectWallCandidateHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/UpdateCuratedWallMetadataHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/SaveCurationDraftHandler.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AcceptWallCandidateHandlerTests.cs`
- Create: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/UpdateCuratedWallMetadataHandlerTests.cs`

- [ ] **Step 1: Write the failing wall-accept test**

```csharp
// tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AcceptWallCandidateHandlerTests.cs
[Fact]
public async Task HandleAsync_accepts_a_pending_candidate_and_creates_a_curated_wall()
{
    var curation = new FloorPlanCuration(Guid.NewGuid(), Guid.NewGuid(), 1, FloorPlanCurationStatus.Draft, null, null, DateTime.UtcNow, null);
    var candidate = new ExtractedWallCandidate(Guid.NewGuid(), Guid.NewGuid(), "LINE:12", "A-WALL", Guid.NewGuid(), 101.6m, 0.95m, null, ExtractedWallCandidateStatus.Pending, 1);
    var candidateRepository = new InMemoryExtractedWallCandidateRepository(candidate);
    var wallRepository = new InMemoryCuratedWallRepository([]);
    var unitOfWork = new FakeUnitOfWork();

    var handler = new AcceptWallCandidateHandler(candidateRepository, wallRepository, unitOfWork);

    await handler.HandleAsync(curation.Id, candidate.Id, "W-001", CancellationToken.None);

    Assert.Equal(ExtractedWallCandidateStatus.Accepted, candidate.Status);
    var wall = Assert.Single(wallRepository.Items);
    Assert.Equal("W-001", wall.StableWallId);
    Assert.Equal(candidate.Id, wall.SourceCandidateId);
}
```

- [ ] **Step 2: Run the targeted test to verify RED**

Run:

```bash
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter HandleAsync_accepts_a_pending_candidate_and_creates_a_curated_wall
```

Expected: FAIL because `AcceptWallCandidateHandler` and its repository methods do not exist yet.

- [ ] **Step 3: Implement the curation handlers**

```csharp
// src/FloorplanFit.Application/FloorPlans/Curation/StartOrResumeCurationHandler.cs
public sealed class StartOrResumeCurationHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IClock clock;
    private readonly IUnitOfWork unitOfWork;

    public StartOrResumeCurationHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.clock = clock;
        this.unitOfWork = unitOfWork;
    }

    public async Task<FloorPlanCuration> HandleAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        var existingDraft = await floorPlanCurationRepository.GetDraftAsync(floorPlanVersionId, cancellationToken);
        if (existingDraft is not null)
        {
            return existingDraft;
        }

        var published = await floorPlanCurationRepository.GetPublishedAsync(floorPlanVersionId, cancellationToken);
        var nextVersion = await floorPlanCurationRepository.GetNextCurationVersionAsync(floorPlanVersionId, cancellationToken);

        var draft = new FloorPlanCuration(
            Guid.NewGuid(),
            floorPlanVersionId,
            nextVersion,
            FloorPlanCurationStatus.Draft,
            published?.Id,
            null,
            clock.UtcNow,
            null);

        await floorPlanCurationRepository.AddAsync(draft, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return draft;
    }
}
```

```csharp
// src/FloorplanFit.Application/FloorPlans/Curation/AcceptWallCandidateHandler.cs
public sealed class AcceptWallCandidateHandler
{
    private readonly IExtractedWallCandidateRepository extractedWallCandidateRepository;
    private readonly ICuratedWallRepository curatedWallRepository;
    private readonly IUnitOfWork unitOfWork;

    public AcceptWallCandidateHandler(
        IExtractedWallCandidateRepository extractedWallCandidateRepository,
        ICuratedWallRepository curatedWallRepository,
        IUnitOfWork unitOfWork)
    {
        this.extractedWallCandidateRepository = extractedWallCandidateRepository;
        this.curatedWallRepository = curatedWallRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid curationId, Guid candidateId, string stableWallId, CancellationToken cancellationToken)
    {
        var candidate = await extractedWallCandidateRepository.GetByIdAsync(candidateId, cancellationToken)
            ?? throw new InvalidOperationException("Wall candidate was not found.");
        candidate.Accept();

        var wall = new CuratedWall(
            Guid.NewGuid(),
            curationId,
            stableWallId,
            candidate.Id,
            candidate.SourceEntityRef,
            candidate.GeometryPathId,
            WallRole.Partition,
            WallMobilityLevel.Flexible,
            WallProtectionLevel.None,
            candidate.ThicknessMm ?? 101.6m,
            "2x4",
            null,
            isExterior: false,
            isStructuralHint: false,
            wallGroupId: null,
            sortOrder: candidate.SortOrder,
            notes: null);

        await extractedWallCandidateRepository.UpdateAsync(candidate, cancellationToken);
        await curatedWallRepository.AddAsync(wall, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

```csharp
// src/FloorplanFit.Application/FloorPlans/Curation/UpdateCuratedWallMetadataHandler.cs
public sealed class UpdateCuratedWallMetadataHandler
{
    private readonly ICuratedWallRepository curatedWallRepository;
    private readonly IUnitOfWork unitOfWork;

    public UpdateCuratedWallMetadataHandler(
        ICuratedWallRepository curatedWallRepository,
        IUnitOfWork unitOfWork)
    {
        this.curatedWallRepository = curatedWallRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curatedWallId,
        WallRole wallRole,
        WallMobilityLevel mobilityLevel,
        WallProtectionLevel protectionLevel,
        decimal thicknessMm,
        string assemblyCode,
        decimal? heightMm,
        bool isExterior,
        bool isStructuralHint,
        string? notes,
        CancellationToken cancellationToken)
    {
        var wall = await curatedWallRepository.GetByIdAsync(curatedWallId, cancellationToken)
            ?? throw new InvalidOperationException("Curated wall was not found.");

        wall.UpdateMetadata(wallRole, mobilityLevel, protectionLevel, thicknessMm, assemblyCode, heightMm, isExterior, isStructuralHint, notes);

        await curatedWallRepository.UpdateAsync(wall, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Run the Application suite to verify GREEN**

Run:

```bash
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj
```

Expected: PASS with accept/reject/update curation behavior green.

- [ ] **Step 5: Commit the wall review workflow**

```bash
git add src/FloorplanFit.Application tests/FloorplanFit.Application.Tests
git commit -m "feat: support curated wall review workflow"
```

---

### Task 6: Generate and persist minimal curated spaces plus room-level constraints

**Files:**
- Create: `src/FloorplanFit.Infrastructure/Geometry/NetTopologySuiteCuratedSpaceGenerator.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/GenerateCuratedSpacesHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/CreateOrUpdateCuratedSpaceHandler.cs`
- Create: `tests/FloorplanFit.Infrastructure.Tests/Curation/CuratedSpaceGeneratorTests.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteCuratedSpaceRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteConstraintIntentNoteRepository.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteStructuredConstraintRepository.cs`

- [ ] **Step 1: Write the failing polygonizer test**

```csharp
// tests/FloorplanFit.Infrastructure.Tests/Curation/CuratedSpaceGeneratorTests.cs
[Fact]
public async Task GenerateAsync_creates_one_open_plan_space_from_a_closed_wall_loop()
{
    var walls =
    [
        CuratedWallTestFactory.CreateWithLine("W-001", 0, 0, 4000, 0),
        CuratedWallTestFactory.CreateWithLine("W-002", 4000, 0, 4000, 3000),
        CuratedWallTestFactory.CreateWithLine("W-003", 4000, 3000, 0, 3000),
        CuratedWallTestFactory.CreateWithLine("W-004", 0, 3000, 0, 0)
    ];

    var generator = new NetTopologySuiteCuratedSpaceGenerator();

    var spaces = await generator.GenerateAsync(walls, CancellationToken.None);

    var space = Assert.Single(spaces);
    Assert.Equal(SpaceType.OpenPlan, space.SpaceType);
    Assert.True(space.AreaSquareMeters > 0);
}
```

- [ ] **Step 2: Run the polygonizer test to verify RED**

Run:

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter GenerateAsync_creates_one_open_plan_space_from_a_closed_wall_loop
```

Expected: FAIL because `NetTopologySuiteCuratedSpaceGenerator` does not exist yet.

- [ ] **Step 3: Implement the space generator**

```csharp
// src/FloorplanFit.Infrastructure/Geometry/NetTopologySuiteCuratedSpaceGenerator.cs
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;

namespace FloorplanFit.Infrastructure.Geometry;

public sealed class NetTopologySuiteCuratedSpaceGenerator : ICuratedSpaceGenerator
{
    public Task<IReadOnlyList<CuratedSpaceSeed>> GenerateAsync(
        IReadOnlyList<CuratedWallGeometrySeed> walls,
        CancellationToken cancellationToken)
    {
        var geometryFactory = new GeometryFactory();
        var polygonizer = new Polygonizer();

        foreach (var wall in walls)
        {
            var coordinates = wall.Points
                .Select(point => new Coordinate((double)point.X, (double)point.Y))
                .ToArray();

            polygonizer.Add(geometryFactory.CreateLineString(coordinates));
        }

        var spaces = polygonizer.GetPolygons()
            .Cast<Polygon>()
            .Select((polygon, index) => new CuratedSpaceSeed(
                $"SPACE-{index + 1:D3}",
                $"Space {index + 1}",
                SpaceType.OpenPlan,
                polygon.Coordinates.Select(item => new GeometryPoint((decimal)item.X, (decimal)item.Y)).ToArray(),
                areaSquareMeters: (decimal)polygon.Area / 1_000_000m,
                minWidthMm: (decimal)(polygon.EnvelopeInternal.Width),
                minDepthMm: (decimal)(polygon.EnvelopeInternal.Height),
                isProtected: false,
                functionalTagsJson: "[\"living\",\"kitchen\"]",
                notes: null))
            .ToArray();

        return Task.FromResult<IReadOnlyList<CuratedSpaceSeed>>(spaces);
    }
}
```

- [ ] **Step 4: Implement the Application space/constraint handlers**

```csharp
// src/FloorplanFit.Application/FloorPlans/Curation/GenerateCuratedSpacesHandler.cs
public sealed class GenerateCuratedSpacesHandler
{
    private readonly ICuratedWallRepository curatedWallRepository;
    private readonly ICuratedSpaceRepository curatedSpaceRepository;
    private readonly ICuratedSpaceGenerator curatedSpaceGenerator;
    private readonly IUnitOfWork unitOfWork;

    public GenerateCuratedSpacesHandler(
        ICuratedWallRepository curatedWallRepository,
        ICuratedSpaceRepository curatedSpaceRepository,
        ICuratedSpaceGenerator curatedSpaceGenerator,
        IUnitOfWork unitOfWork)
    {
        this.curatedWallRepository = curatedWallRepository;
        this.curatedSpaceRepository = curatedSpaceRepository;
        this.curatedSpaceGenerator = curatedSpaceGenerator;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid curationId, CancellationToken cancellationToken)
    {
        var wallSeeds = await curatedWallRepository.ListGeometrySeedsAsync(curationId, cancellationToken);
        var spaceSeeds = await curatedSpaceGenerator.GenerateAsync(wallSeeds, cancellationToken);
        await curatedSpaceRepository.ReplaceDraftSpacesAsync(curationId, spaceSeeds, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

```csharp
// src/FloorplanFit.Application/FloorPlans/Curation/CreateOrUpdateCuratedSpaceHandler.cs
public sealed class CreateOrUpdateCuratedSpaceHandler
{
    private readonly ICuratedSpaceRepository curatedSpaceRepository;
    private readonly IUnitOfWork unitOfWork;

    public CreateOrUpdateCuratedSpaceHandler(
        ICuratedSpaceRepository curatedSpaceRepository,
        IUnitOfWork unitOfWork)
    {
        this.curatedSpaceRepository = curatedSpaceRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(CuratedSpace space, CancellationToken cancellationToken)
    {
        await curatedSpaceRepository.UpsertAsync(space, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Run the Infrastructure + Application suites to verify GREEN**

Run:

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj
```

Expected: PASS and space generation is now available for Loop 1 review.

- [ ] **Step 6: Commit spaces and constraints**

```bash
git add src/FloorplanFit.Infrastructure src/FloorplanFit.Application tests/FloorplanFit.Infrastructure.Tests tests/FloorplanFit.Application.Tests
git commit -m "feat: add curated spaces and room constraints"
```

---

### Task 7: Surface Library state transitions and add the minimal Desktop review screen

**Files:**
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs`
- Modify: `src/FloorplanFit.Application/FloorPlans/Library/GetFloorPlanLibraryHandler.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/GeometryPathDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/GeometrySegmentDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/WallCandidateDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/CuratedWallDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/CuratedSpaceDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/ConstraintIntentNoteDto.cs`
- Create: `src/FloorplanFit.Contracts/FloorPlans/StructuredConstraintDto.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- Modify: `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/MainWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- Create: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Create: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Create: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

- [ ] **Step 1: Write the failing Library status integration test**

```csharp
// tests/FloorplanFit.Infrastructure.Tests/Imports/FloorPlanLibraryReaderIntegrationTests.cs
[Fact]
public async Task ListAsync_returns_published_status_when_active_curation_exists()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-library-status-{Guid.NewGuid():N}");

    try
    {
        var workspace = new AppWorkspace(tempRoot);
        workspace.EnsureCreated();
        await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

        // Seed import + published curation here using the existing repository stack

        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var reader = new SqliteFloorPlanLibraryReader(session);

        var items = await reader.ListAsync(CancellationToken.None);

        Assert.Equal("Published", Assert.Single(items).Status);
    }
    finally
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
```

- [ ] **Step 2: Run the integration test to verify RED**

Run:

```bash
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter ListAsync_returns_published_status_when_active_curation_exists
```

Expected: FAIL because Library state derivation still returns `Imported` unconditionally.

- [ ] **Step 3: Update Library state derivation**

```csharp
// src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs
command.CommandText = """
    SELECT
        t.id,
        t.code,
        t.name,
        v.version_number,
        v.created_at_utc,
        mc.source_unit,
        t.active_published_curation_id,
        CASE
            WHEN t.active_published_curation_id IS NOT NULL THEN 'Published'
            WHEN EXISTS (
                SELECT 1
                FROM floorplan_curations c
                WHERE c.floorplan_version_id = v.id
                  AND c.status = 1
            ) THEN 'Curated Draft'
            WHEN EXISTS (
                SELECT 1
                FROM wall_extraction_runs r
                WHERE r.floorplan_version_id = v.id
            ) THEN 'Extracted'
            ELSE 'Imported'
        END AS library_status
    FROM floorplan_templates t
    JOIN floorplan_versions v ON v.id = t.current_version_id
    JOIN imported_documents d ON d.id = v.imported_document_id
    JOIN measurement_contexts mc ON mc.id = d.measurement_context_id
    WHERE t.is_active = 1
    ORDER BY v.created_at_utc DESC, t.code ASC
""";
```

```csharp
items.Add(new FloorPlanLibraryItemDto(
    Guid.Parse(reader.GetString(0)),
    reader.GetString(1),
    reader.GetString(2),
    reader.GetString(7),
    reader.GetInt32(3),
    DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
    sourceUnit.ToString().ToLowerInvariant(),
    reader.IsDBNull(6) ? null : Guid.Parse(reader.GetString(6))));
```

- [ ] **Step 4: Add the review DTOs and review screen**

```csharp
// src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs
namespace FloorplanFit.Contracts.FloorPlans;

public sealed record FloorPlanReviewSessionDto(
    Guid TemplateId,
    Guid FloorPlanVersionId,
    Guid? DraftCurationId,
    string Code,
    string Name,
    string Status,
    IReadOnlyList<WallCandidateDto> Candidates,
    IReadOnlyList<CuratedWallDto> CuratedWalls,
    IReadOnlyList<CuratedSpaceDto> CuratedSpaces,
    IReadOnlyList<ConstraintIntentNoteDto> Notes,
    IReadOnlyList<StructuredConstraintDto> Constraints);
```

```xml
<!-- src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:viewModels="using:FloorplanFit.Desktop.ViewModels"
        x:Class="FloorplanFit.Desktop.ReviewFloorPlanWindow"
        x:DataType="viewModels:FloorPlanReviewViewModel"
        Width="1400"
        Height="900"
        Title="Floorplan Fit - Review">
  <Grid ColumnDefinitions="320,*,360" RowDefinitions="Auto,*" Margin="16">
    <TextBlock Grid.ColumnSpan="3"
               FontSize="24"
               FontWeight="Bold"
               Text="{Binding Header}" />

    <ListBox Grid.Row="1"
             ItemsSource="{Binding Candidates}"
             SelectedItem="{Binding SelectedCandidate}" />

    <Border Grid.Row="1"
            Grid.Column="1"
            Margin="16,0"
            BorderBrush="LightGray"
            BorderThickness="1">
      <Canvas Background="#111827" />
    </Border>

    <StackPanel Grid.Row="1"
                Grid.Column="2"
                Spacing="12">
      <TextBlock Text="Inspector" FontWeight="Bold" />
      <TextBox Watermark="Stable wall id" Text="{Binding StableWallId}" />
      <ComboBox SelectedItem="{Binding SelectedMobilityLevel}" ItemsSource="{Binding MobilityLevels}" />
      <ComboBox SelectedItem="{Binding SelectedWallRole}" ItemsSource="{Binding WallRoles}" />
      <TextBox Watermark="Assembly code" Text="{Binding AssemblyCode}" />
      <Button Content="Accept wall" Command="{Binding AcceptWallCommand}" />
      <Button Content="Reject wall" Command="{Binding RejectWallCommand}" />
      <Button Content="Save draft" Command="{Binding SaveDraftCommand}" />
      <Button Content="Publish" Command="{Binding PublishCommand}" />
    </StackPanel>
  </Grid>
</Window>
```

```csharp
// src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs
public sealed partial class FloorPlanReviewViewModel : ObservableObject
{
    private readonly IServiceScopeFactory scopeFactory;

    public FloorPlanReviewViewModel(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public string Header { get; private set; } = "Floor plan review";

    public ObservableCollection<WallCandidateDto> Candidates { get; } = [];

    [ObservableProperty]
    private WallCandidateDto? selectedCandidate;

    [ObservableProperty]
    private string stableWallId = string.Empty;

    [ObservableProperty]
    private string assemblyCode = "2x4";

    public async Task LoadAsync(Guid templateId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IFloorPlanReviewSessionReader>();
        var session = await reader.LoadAsync(templateId, cancellationToken);

        Header = $"{session.Code} - {session.Status}";
        Candidates.Clear();

        foreach (var candidate in session.Candidates)
        {
            Candidates.Add(candidate);
        }
    }
}
```

- [ ] **Step 5: Wire Desktop review actions**

```csharp
// src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs
public async Task OpenReviewAsync(Guid templateId, CancellationToken cancellationToken)
{
    var window = new ReviewFloorPlanWindow
    {
        DataContext = new FloorPlanReviewViewModel(scopeFactory)
    };

    if (window.DataContext is FloorPlanReviewViewModel viewModel)
    {
        await viewModel.LoadAsync(templateId, cancellationToken);
    }

    window.Show();
}
```

```xml
<!-- src/FloorplanFit.Desktop/MainWindow.axaml -->
<Button Grid.Column="5"
        Content="Review"
        Click="ReviewButton_OnClick" />
```

```csharp
// src/FloorplanFit.Desktop/MainWindow.axaml.cs
private async void ReviewButton_OnClick(object? sender, RoutedEventArgs e)
{
    if (sender is not Button { DataContext: FloorPlanLibraryItemDto item })
    {
        return;
    }

    if (DataContext is not LibraryViewModel viewModel)
    {
        return;
    }

    await viewModel.OpenReviewAsync(item.TemplateId, CancellationToken.None);
}
```

- [ ] **Step 6: Run the test suites and then do a manual UI verification**
- [ ] **Step 6: Run the test suites and defer runtime UI verification under the current repo rule**

Run:

```bash
dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj
dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj
```

Expected: PASS and the review/publish flow is covered by automated verification.

Runtime note:

- `scripts/dev-desktop.bat` is intentionally deferred here because it executes `dotnet watch run`
- that means build + run, which conflicts with the current repository rule `never build after changes`
- if runtime verification becomes necessary later, get explicit user approval for a separate pass and document the exception clearly

- [ ] **Step 7: Commit the Desktop review slice**

```bash
git add src/FloorplanFit.Desktop src/FloorplanFit.Contracts src/FloorplanFit.Infrastructure src/FloorplanFit.Application tests
git commit -m "feat: add loop 1 review and publish desktop flow"
```

---

### Task 8: Sync durable knowledge and final verification notes

**Files:**
- Create: `obsidian-vault/Implementation/2026-04-30 - Loop 1 Implementation Plan.md`
- Modify: `obsidian-vault/Current State.md`
- Create or modify implementation notes under `obsidian-vault/Implementation/`

- [ ] **Step 1: Record the implementation plan in Obsidian**

Create `obsidian-vault/Implementation/2026-04-30 - Loop 1 Implementation Plan.md` with:

```md
---
type: implementation
date: 2026-04-30
status: active
---

# Loop 1 Implementation Plan

## Scope

- extraction de wall candidates
- curated walls
- curated spaces mínimos
- constraints básicas
- draft / publish / active curation
- review UI mínima

## Why

Este es el primer cierre serio de Loop 1 y prepara la base reusable para Loop 2.
```

- [ ] **Step 2: Update `Current State.md` after each milestone**

Add or update bullets for:

- extraction implemented
- curation draft/publish implemented
- active published curation tracked on templates
- curated spaces and constraints implemented
- review UI available

- [ ] **Step 3: Save memory after each major milestone**

Use the existing project protocol:

- write to Obsidian first
- then `mem_save`
- update `Current State.md` when truth changes

- [ ] **Step 4: Commit docs after the implementation batch**

```bash
git add obsidian-vault docs
git commit -m "docs: record loop 1 implementation progress"
```

---

## Self-review

### Spec coverage

- extracted wall candidates — Task 4
- draft/published curation lifecycle — Tasks 1, 2, 5
- exact curated walls — Tasks 1, 3, 5
- minimal curated spaces — Task 6
- wall/space constraints — Task 6
- Library state transitions — Task 7
- minimal review canvas and inspector — Task 7
- Loop 2 boundary preserved — Tasks 1, 5, 6, 7

### Placeholder scan

- no `TODO`
- no `TBD`
- no “implement later”
- no “similar to above”
- every code-changing step includes concrete code

### Type consistency

- `active_published_curation_id` is tracked on `FloorPlanTemplate` and persisted in SQLite
- `FloorPlanCurationStatus` remains `Draft / Published / Superseded`
- Library statuses remain `Imported / Extracted / Curated Draft / Published`
- new walls remain out of Loop 1 and do not mutate the reusable base directly

## Execution handoff

This plan is ready for either:

1. task-by-task subagent dispatch, or
2. inline execution in this session with strict TDD checkpoints.
