# Loop 1 Canonical Non-Wall Positioning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the user drag any non-wall artifact in the Loop 1 review preview and persist that new location as canonical curated truth.

**Architecture:** Reuse the same overlay pattern already introduced for curated classification. Labels get absolute curated `X/Y` overrides, geometry artifacts get canonical `dx/dy` translation overlays, and the Infrastructure review reader resolves those overlays into the DTOs and preview geometry that Desktop consumes directly.

**Tech Stack:** C#, .NET 10, Avalonia UI, MVVM, SQLite, xUnit

---

### Task 1: Add the canonical artifact-position domain model

**Files:**
- Create: `src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPositionMode.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPositionSourceKinds.cs`
- Create: `src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPosition.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanArtifactPositionTests.cs`

- [ ] **Step 1: Write the failing domain tests**

Add these tests:

```csharp
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class FloorPlanArtifactPositionTests
{
    [Fact]
    public void CreateAbsolutePoint_accepts_room_labels_and_stores_resolved_coordinates()
    {
        var updatedAt = new DateTime(2026, 5, 11, 19, 0, 0, DateTimeKind.Utc);

        var position = FloorPlanArtifactPosition.CreateAbsolutePoint(
            Guid.NewGuid(),
            FloorPlanArtifactPositionSourceKinds.RoomLabel,
            Guid.NewGuid(),
            220m,
            410m,
            updatedAt);

        Assert.Equal(FloorPlanArtifactPositionMode.AbsolutePoint, position.PositionMode);
        Assert.Equal(220m, position.ResolvedX);
        Assert.Equal(410m, position.ResolvedY);
        Assert.Null(position.TranslationDx);
        Assert.Null(position.TranslationDy);
        Assert.Equal(updatedAt, position.UpdatedAtUtc);
    }

    [Fact]
    public void CreateTranslation_accepts_curated_geometry_kinds_and_stores_offsets()
    {
        var position = FloorPlanArtifactPosition.CreateTranslation(
            Guid.NewGuid(),
            FloorPlanArtifactPositionSourceKinds.OpeningCandidate,
            Guid.NewGuid(),
            24m,
            -12m,
            new DateTime(2026, 5, 11, 19, 1, 0, DateTimeKind.Utc));

        Assert.Equal(FloorPlanArtifactPositionMode.Translation, position.PositionMode);
        Assert.Equal(24m, position.TranslationDx);
        Assert.Equal(-12m, position.TranslationDy);
        Assert.Null(position.ResolvedX);
        Assert.Null(position.ResolvedY);
    }

    [Fact]
    public void CreateAbsolutePoint_rejects_geometry_only_source_kinds()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            FloorPlanArtifactPosition.CreateAbsolutePoint(
                Guid.NewGuid(),
                FloorPlanArtifactPositionSourceKinds.FixedPlanComponent,
                Guid.NewGuid(),
                10m,
                20m,
                DateTime.UtcNow));

        Assert.Contains("AbsolutePoint", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateTranslation_rejects_unsupported_source_kinds_like_walls_and_pinches()
    {
        Assert.Throws<ArgumentException>(() =>
            FloorPlanArtifactPosition.CreateTranslation(
                Guid.NewGuid(),
                "WallCandidate",
                Guid.NewGuid(),
                4m,
                0m,
                DateTime.UtcNow));

        Assert.Throws<ArgumentException>(() =>
            FloorPlanArtifactPosition.CreateTranslation(
                Guid.NewGuid(),
                "PinchMarker",
                Guid.NewGuid(),
                4m,
                0m,
                DateTime.UtcNow));
    }
}
```

- [ ] **Step 2: Run the targeted test to verify it fails**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~FloorPlanArtifactPositionTests" --artifacts-path .\.artifacts-test\app-position-domain-red
```

Expected: FAIL because the position mode, source-kind, and entity types do not exist yet.

- [ ] **Step 3: Write the minimal domain implementation**

Create the enum:

```csharp
namespace FloorplanFit.Domain.FloorPlans;

public enum FloorPlanArtifactPositionMode
{
    AbsolutePoint = 1,
    Translation = 2
}
```

Create the position source kinds:

```csharp
namespace FloorplanFit.Domain.FloorPlans;

public static class FloorPlanArtifactPositionSourceKinds
{
    public const string RoomLabel = "RoomLabel";
    public const string OpeningLabel = "OpeningLabel";
    public const string OpeningCandidate = FloorPlanArtifactSourceKinds.OpeningCandidate;
    public const string FixedPlanComponent = FloorPlanArtifactSourceKinds.FixedPlanComponent;
    public const string ProtectedDetailAssembly = FloorPlanArtifactSourceKinds.ProtectedDetailAssembly;

    public static bool IsSupported(string sourceArtifactKind)
    {
        return string.Equals(sourceArtifactKind, RoomLabel, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, OpeningLabel, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, OpeningCandidate, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, FixedPlanComponent, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, ProtectedDetailAssembly, StringComparison.Ordinal);
    }

    public static bool SupportsAbsolutePoint(string sourceArtifactKind)
    {
        return string.Equals(sourceArtifactKind, RoomLabel, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, OpeningLabel, StringComparison.Ordinal);
    }

    public static bool SupportsTranslation(string sourceArtifactKind)
    {
        return string.Equals(sourceArtifactKind, OpeningCandidate, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, FixedPlanComponent, StringComparison.Ordinal) ||
               string.Equals(sourceArtifactKind, ProtectedDetailAssembly, StringComparison.Ordinal);
    }
}
```

Create the entity:

```csharp
namespace FloorplanFit.Domain.FloorPlans;

public sealed class FloorPlanArtifactPosition
{
    private FloorPlanArtifactPosition(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        FloorPlanArtifactPositionMode positionMode,
        decimal? resolvedX,
        decimal? resolvedY,
        decimal? translationDx,
        decimal? translationDy,
        DateTime updatedAtUtc)
    {
        if (!FloorPlanArtifactPositionSourceKinds.IsSupported(sourceArtifactKind))
        {
            throw new ArgumentException("Unsupported source artifact kind.", nameof(sourceArtifactKind));
        }

        if (positionMode == FloorPlanArtifactPositionMode.AbsolutePoint &&
            !FloorPlanArtifactPositionSourceKinds.SupportsAbsolutePoint(sourceArtifactKind))
        {
            throw new ArgumentException("AbsolutePoint mode is only valid for label artifacts.", nameof(sourceArtifactKind));
        }

        if (positionMode == FloorPlanArtifactPositionMode.Translation &&
            !FloorPlanArtifactPositionSourceKinds.SupportsTranslation(sourceArtifactKind))
        {
            throw new ArgumentException("Translation mode is only valid for movable geometry artifacts.", nameof(sourceArtifactKind));
        }

        if (positionMode == FloorPlanArtifactPositionMode.AbsolutePoint && (!resolvedX.HasValue || !resolvedY.HasValue))
        {
            throw new ArgumentException("AbsolutePoint mode requires resolved X/Y coordinates.");
        }

        if (positionMode == FloorPlanArtifactPositionMode.Translation && (!translationDx.HasValue || !translationDy.HasValue))
        {
            throw new ArgumentException("Translation mode requires translation dx/dy.");
        }

        FloorPlanCurationId = floorPlanCurationId;
        SourceArtifactKind = sourceArtifactKind;
        SourceArtifactId = sourceArtifactId;
        PositionMode = positionMode;
        ResolvedX = resolvedX;
        ResolvedY = resolvedY;
        TranslationDx = translationDx;
        TranslationDy = translationDy;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid FloorPlanCurationId { get; }
    public string SourceArtifactKind { get; }
    public Guid SourceArtifactId { get; }
    public FloorPlanArtifactPositionMode PositionMode { get; }
    public decimal? ResolvedX { get; }
    public decimal? ResolvedY { get; }
    public decimal? TranslationDx { get; }
    public decimal? TranslationDy { get; }
    public DateTime UpdatedAtUtc { get; }

    public static FloorPlanArtifactPosition CreateAbsolutePoint(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        decimal resolvedX,
        decimal resolvedY,
        DateTime updatedAtUtc)
        => new(
            floorPlanCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            FloorPlanArtifactPositionMode.AbsolutePoint,
            resolvedX,
            resolvedY,
            null,
            null,
            updatedAtUtc);

    public static FloorPlanArtifactPosition CreateTranslation(
        Guid floorPlanCurationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        decimal translationDx,
        decimal translationDy,
        DateTime updatedAtUtc)
        => new(
            floorPlanCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            FloorPlanArtifactPositionMode.Translation,
            null,
            null,
            translationDx,
            translationDy,
            updatedAtUtc);
}
```

- [ ] **Step 4: Run the targeted test again to verify green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~FloorPlanArtifactPositionTests" --artifacts-path .\.artifacts-test\app-position-domain-green
```

Expected: PASS.

- [ ] **Step 5: Commit the domain slice**

```powershell
git add src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPositionMode.cs src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPositionSourceKinds.cs src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPosition.cs tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanArtifactPositionTests.cs
git commit -m "feat: add floor plan artifact position domain model"
```

---

### Task 2: Add Application save/restore handlers for canonical artifact positions

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/IFloorPlanArtifactPositionRepository.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/SaveFloorPlanArtifactPositionHandler.cs`
- Create: `src/FloorplanFit.Application/FloorPlans/Curation/RestoreFloorPlanArtifactPositionHandler.cs`
- Test: `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanArtifactPositionHandlersTests.cs`

- [ ] **Step 1: Write the failing handler tests**

Add these tests:

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class FloorPlanArtifactPositionHandlersTests
{
    [Fact]
    public async Task SaveFloorPlanArtifactPositionHandler_upserts_absolute_point_overlay_and_saves_changes()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 11, 20, 0, 0, DateTimeKind.Utc),
            null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 20, 5, 0, DateTimeKind.Utc));
        var handler = new SaveFloorPlanArtifactPositionHandler(curationRepository, positionRepository, unitOfWork, clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanArtifactPositionSourceKinds.RoomLabel,
            Guid.NewGuid(),
            FloorPlanArtifactPositionMode.AbsolutePoint,
            512m,
            144m,
            null,
            null,
            CancellationToken.None);

        var saved = Assert.Single(positionRepository.Items);
        Assert.Equal(FloorPlanArtifactPositionMode.AbsolutePoint, saved.PositionMode);
        Assert.Equal(512m, saved.ResolvedX);
        Assert.Equal(144m, saved.ResolvedY);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RestoreFloorPlanArtifactPositionHandler_writes_neutral_translation_overlay_for_geometry_artifacts()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 11, 20, 0, 0, DateTimeKind.Utc),
            null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 20, 7, 0, DateTimeKind.Utc));
        var handler = new RestoreFloorPlanArtifactPositionHandler(curationRepository, positionRepository, unitOfWork, clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanArtifactPositionSourceKinds.FixedPlanComponent,
            Guid.NewGuid(),
            FloorPlanArtifactPositionMode.Translation,
            null,
            null,
            0m,
            0m,
            CancellationToken.None);

        var saved = Assert.Single(positionRepository.Items);
        Assert.Equal(FloorPlanArtifactPositionMode.Translation, saved.PositionMode);
        Assert.Equal(0m, saved.TranslationDx);
        Assert.Equal(0m, saved.TranslationDy);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task RestoreFloorPlanArtifactPositionHandler_writes_detected_label_coordinates_for_absolute_point_restore()
    {
        var curation = new FloorPlanCuration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            FloorPlanCurationStatus.Draft,
            null,
            null,
            new DateTime(2026, 5, 11, 20, 0, 0, DateTimeKind.Utc),
            null);
        var curationRepository = new InMemoryFloorPlanCurationRepository(curation);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 11, 20, 8, 0, DateTimeKind.Utc));
        var handler = new RestoreFloorPlanArtifactPositionHandler(curationRepository, positionRepository, unitOfWork, clock);

        await handler.HandleAsync(
            curation.Id,
            FloorPlanArtifactPositionSourceKinds.OpeningLabel,
            Guid.NewGuid(),
            FloorPlanArtifactPositionMode.AbsolutePoint,
            120m,
            84m,
            null,
            null,
            CancellationToken.None);

        var saved = Assert.Single(positionRepository.Items);
        Assert.Equal(120m, saved.ResolvedX);
        Assert.Equal(84m, saved.ResolvedY);
        Assert.True(unitOfWork.SaveChangesCalled);
    }
}
```

- [ ] **Step 2: Run the targeted handler tests to verify red**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~FloorPlanArtifactPositionHandlersTests" --artifacts-path .\.artifacts-test\app-position-handlers-red
```

Expected: FAIL because the repository contract and handlers do not exist yet.

- [ ] **Step 3: Write the Application abstraction and handlers**

Create the repository contract:

```csharp
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanArtifactPositionRepository
{
    Task<IReadOnlyList<FloorPlanArtifactPosition>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken);
    Task UpsertAsync(FloorPlanArtifactPosition position, CancellationToken cancellationToken);
}
```

Create the save handler:

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class SaveFloorPlanArtifactPositionHandler
{
    private readonly IFloorPlanCurationRepository curationRepository;
    private readonly IFloorPlanArtifactPositionRepository positionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public SaveFloorPlanArtifactPositionHandler(
        IFloorPlanCurationRepository curationRepository,
        IFloorPlanArtifactPositionRepository positionRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.curationRepository = curationRepository;
        this.positionRepository = positionRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task HandleAsync(
        Guid curationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        FloorPlanArtifactPositionMode positionMode,
        decimal? resolvedX,
        decimal? resolvedY,
        decimal? translationDx,
        decimal? translationDy,
        CancellationToken cancellationToken)
    {
        var curation = await curationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }

        var position = positionMode switch
        {
            FloorPlanArtifactPositionMode.AbsolutePoint => FloorPlanArtifactPosition.CreateAbsolutePoint(
                curationId,
                sourceArtifactKind,
                sourceArtifactId,
                resolvedX ?? throw new InvalidOperationException("Resolved X is required for AbsolutePoint mode."),
                resolvedY ?? throw new InvalidOperationException("Resolved Y is required for AbsolutePoint mode."),
                clock.UtcNow),
            FloorPlanArtifactPositionMode.Translation => FloorPlanArtifactPosition.CreateTranslation(
                curationId,
                sourceArtifactKind,
                sourceArtifactId,
                translationDx ?? throw new InvalidOperationException("Translation dx is required for Translation mode."),
                translationDy ?? throw new InvalidOperationException("Translation dy is required for Translation mode."),
                clock.UtcNow),
            _ => throw new InvalidOperationException("Unsupported artifact position mode.")
        };

        await positionRepository.UpsertAsync(position, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

Create the restore handler. IMPORTANT: write a neutral overlay row instead of deleting, so draft lineage can safely override inherited published positions.

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RestoreFloorPlanArtifactPositionHandler
{
    private readonly IFloorPlanCurationRepository curationRepository;
    private readonly IFloorPlanArtifactPositionRepository positionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public RestoreFloorPlanArtifactPositionHandler(
        IFloorPlanCurationRepository curationRepository,
        IFloorPlanArtifactPositionRepository positionRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.curationRepository = curationRepository;
        this.positionRepository = positionRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task HandleAsync(
        Guid curationId,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        FloorPlanArtifactPositionMode positionMode,
        decimal? detectedX,
        decimal? detectedY,
        decimal? translationDx,
        decimal? translationDy,
        CancellationToken cancellationToken)
    {
        var curation = await curationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }

        var position = positionMode switch
        {
            FloorPlanArtifactPositionMode.AbsolutePoint => FloorPlanArtifactPosition.CreateAbsolutePoint(
                curationId,
                sourceArtifactKind,
                sourceArtifactId,
                detectedX ?? throw new InvalidOperationException("Detected X is required for AbsolutePoint restore."),
                detectedY ?? throw new InvalidOperationException("Detected Y is required for AbsolutePoint restore."),
                clock.UtcNow),
            FloorPlanArtifactPositionMode.Translation => FloorPlanArtifactPosition.CreateTranslation(
                curationId,
                sourceArtifactKind,
                sourceArtifactId,
                translationDx ?? 0m,
                translationDy ?? 0m,
                clock.UtcNow),
            _ => throw new InvalidOperationException("Unsupported artifact position mode.")
        };

        await positionRepository.UpsertAsync(position, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Run the targeted handler tests again to verify green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~FloorPlanArtifactPositionHandlersTests" --artifacts-path .\.artifacts-test\app-position-handlers-green
```

Expected: PASS.

- [ ] **Step 5: Commit the Application slice**

```powershell
git add src/FloorplanFit.Application/Abstractions/IFloorPlanArtifactPositionRepository.cs src/FloorplanFit.Application/FloorPlans/Curation/SaveFloorPlanArtifactPositionHandler.cs src/FloorplanFit.Application/FloorPlans/Curation/RestoreFloorPlanArtifactPositionHandler.cs tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanArtifactPositionHandlersTests.cs
git commit -m "feat: add floor plan artifact position handlers"
```

---

### Task 3: Persist the canonical position overlay in SQLite

**Files:**
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanArtifactPositionRepository.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanArtifactPositionPersistenceIntegrationTests.cs`

- [ ] **Step 1: Write the failing Infrastructure tests**

Add these tests:

```csharp
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class FloorPlanArtifactPositionPersistenceIntegrationTests
{
    [Fact]
    public async Task Schema_initializer_creates_floorplan_artifact_positions_table()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-position-schema-{Guid.NewGuid():N}.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(dbPath, CancellationToken.None);
            await using var connection = new SqliteConnection($"Data Source={dbPath}");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'floorplan_artifact_positions'";
            var result = command.ExecuteScalar();

            Assert.Equal("floorplan_artifact_positions", result);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public async Task Artifact_position_repository_upserts_and_reads_absolute_and_translation_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"floorplan-fit-position-repo-{Guid.NewGuid():N}.db");
        var curationId = Guid.NewGuid();

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(dbPath, CancellationToken.None);
            await using var session = await SqliteSession.OpenAsync(dbPath, CancellationToken.None);
            var repository = new SqliteFloorPlanArtifactPositionRepository(session);

            await repository.UpsertAsync(
                FloorPlanArtifactPosition.CreateAbsolutePoint(
                    curationId,
                    FloorPlanArtifactPositionSourceKinds.RoomLabel,
                    Guid.NewGuid(),
                    320m,
                    640m,
                    new DateTime(2026, 5, 11, 21, 0, 0, DateTimeKind.Utc)),
                CancellationToken.None);
            await repository.UpsertAsync(
                FloorPlanArtifactPosition.CreateTranslation(
                    curationId,
                    FloorPlanArtifactPositionSourceKinds.OpeningCandidate,
                    Guid.NewGuid(),
                    18m,
                    -6m,
                    new DateTime(2026, 5, 11, 21, 1, 0, DateTimeKind.Utc)),
                CancellationToken.None);

            var items = await repository.ListByCurationAsync(curationId, CancellationToken.None);

            Assert.Equal(2, items.Count);
            Assert.Contains(items, item => item.PositionMode == FloorPlanArtifactPositionMode.AbsolutePoint && item.ResolvedX == 320m && item.ResolvedY == 640m);
            Assert.Contains(items, item => item.PositionMode == FloorPlanArtifactPositionMode.Translation && item.TranslationDx == 18m && item.TranslationDy == -6m);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}
```

- [ ] **Step 2: Run the targeted Infrastructure tests to verify red**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FloorPlanArtifactPositionPersistenceIntegrationTests" --artifacts-path .\.artifacts-test\infra-position-repo-red
```

Expected: FAIL because the SQLite table and repository do not exist yet.

- [ ] **Step 3: Add the SQLite schema and repository**

Add the table to `SqliteSchemaInitializer` and call a dedicated schema-guard method:

```csharp
CREATE TABLE IF NOT EXISTS floorplan_artifact_positions (
    floorplan_curation_id TEXT NOT NULL,
    source_artifact_kind TEXT NOT NULL,
    source_artifact_id TEXT NOT NULL,
    position_mode INTEGER NOT NULL,
    resolved_x TEXT NULL,
    resolved_y TEXT NULL,
    translation_dx TEXT NULL,
    translation_dy TEXT NULL,
    updated_at_utc TEXT NOT NULL,
    PRIMARY KEY (floorplan_curation_id, source_artifact_kind, source_artifact_id)
);
```

```csharp
private static void EnsureArtifactPositionSchema(SqliteConnection connection)
{
    EnsureColumnExists(connection, "floorplan_artifact_positions", "position_mode", "INTEGER NOT NULL DEFAULT 1");
    EnsureColumnExists(connection, "floorplan_artifact_positions", "resolved_x", "TEXT NULL");
    EnsureColumnExists(connection, "floorplan_artifact_positions", "resolved_y", "TEXT NULL");
    EnsureColumnExists(connection, "floorplan_artifact_positions", "translation_dx", "TEXT NULL");
    EnsureColumnExists(connection, "floorplan_artifact_positions", "translation_dy", "TEXT NULL");
    EnsureColumnExists(connection, "floorplan_artifact_positions", "updated_at_utc", "TEXT NOT NULL DEFAULT ''");
}
```

Create the repository:

```csharp
using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanArtifactPositionRepository : IFloorPlanArtifactPositionRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanArtifactPositionRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<IReadOnlyList<FloorPlanArtifactPosition>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                floorplan_curation_id,
                source_artifact_kind,
                source_artifact_id,
                position_mode,
                resolved_x,
                resolved_y,
                translation_dx,
                translation_dy,
                updated_at_utc
            FROM floorplan_artifact_positions
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY updated_at_utc ASC, source_artifact_kind ASC, source_artifact_id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

        var items = new List<FloorPlanArtifactPosition>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var mode = (FloorPlanArtifactPositionMode)reader.GetInt32(3);
            items.Add(mode == FloorPlanArtifactPositionMode.AbsolutePoint
                ? FloorPlanArtifactPosition.CreateAbsolutePoint(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    Guid.Parse(reader.GetString(2)),
                    decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                    DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
                : FloorPlanArtifactPosition.CreateTranslation(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    Guid.Parse(reader.GetString(2)),
                    decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                    DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
        }

        return Task.FromResult<IReadOnlyList<FloorPlanArtifactPosition>>(items);
    }

    public Task UpsertAsync(FloorPlanArtifactPosition position, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO floorplan_artifact_positions (
                floorplan_curation_id,
                source_artifact_kind,
                source_artifact_id,
                position_mode,
                resolved_x,
                resolved_y,
                translation_dx,
                translation_dy,
                updated_at_utc
            )
            VALUES (
                $floorplan_curation_id,
                $source_artifact_kind,
                $source_artifact_id,
                $position_mode,
                $resolved_x,
                $resolved_y,
                $translation_dx,
                $translation_dy,
                $updated_at_utc
            )
            ON CONFLICT(floorplan_curation_id, source_artifact_kind, source_artifact_id)
            DO UPDATE SET
                position_mode = excluded.position_mode,
                resolved_x = excluded.resolved_x,
                resolved_y = excluded.resolved_y,
                translation_dx = excluded.translation_dx,
                translation_dy = excluded.translation_dy,
                updated_at_utc = excluded.updated_at_utc
            """);

        command.Parameters.AddWithValue("$floorplan_curation_id", position.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_artifact_kind", position.SourceArtifactKind);
        command.Parameters.AddWithValue("$source_artifact_id", position.SourceArtifactId.ToString());
        command.Parameters.AddWithValue("$position_mode", (int)position.PositionMode);
        command.Parameters.AddWithValue("$resolved_x", position.ResolvedX is null ? DBNull.Value : position.ResolvedX.Value.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$resolved_y", position.ResolvedY is null ? DBNull.Value : position.ResolvedY.Value.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$translation_dx", position.TranslationDx is null ? DBNull.Value : position.TranslationDx.Value.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$translation_dy", position.TranslationDy is null ? DBNull.Value : position.TranslationDy.Value.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updated_at_utc", position.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
```

- [ ] **Step 4: Run the targeted Infrastructure tests again to verify green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FloorPlanArtifactPositionPersistenceIntegrationTests" --artifacts-path .\.artifacts-test\infra-position-repo-green
```

Expected: PASS.

- [ ] **Step 5: Commit the Infrastructure persistence slice**

```powershell
git add src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanArtifactPositionRepository.cs tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanArtifactPositionPersistenceIntegrationTests.cs
git commit -m "feat: persist floor plan artifact positions"
```

---

### Task 4: Resolve canonical positions in the review read model and DTO contracts

**Files:**
- Modify: `src/FloorplanFit.Contracts/FloorPlans/RoomLabelDto.cs`
- Modify: `src/FloorplanFit.Contracts/FloorPlans/OpeningLabelDto.cs`
- Modify: `src/FloorplanFit.Contracts/FloorPlans/CuratedPlanArtifactDto.cs`
- Create: `src/FloorplanFit.Infrastructure/Persistence/ResolvedFloorPlanArtifactPositionProjector.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- Test: `tests/FloorplanFit.Infrastructure.Tests/Review/CanonicalArtifactPositionReviewSessionIntegrationTests.cs`

- [ ] **Step 1: Write the failing review-reader integration test**

Add a concrete integration test that seeds one room label, one opening label, one opening candidate geometry path, and one draft curation with three position overlay rows:

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Review;

public sealed class CanonicalArtifactPositionReviewSessionIntegrationTests
{
    [Fact]
    public async Task GetByTemplateAsync_returns_resolved_label_coordinates_and_translated_curated_geometry()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-position-review-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 5, 11, 22, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var import = await ExecuteImportAsync(workspace, sourcePath, now);
            var seeded = await SeedExtractionDraftAndPositionOverlayAsync(workspace, now.AddMinutes(5));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var reader = new SqliteFloorPlanReviewSessionReader(session);
            var reviewSession = await reader.GetByTemplateAsync(import.Item.TemplateId, CancellationToken.None);

            Assert.NotNull(reviewSession);

            var roomLabel = Assert.Single(reviewSession.RoomLabels);
            Assert.True(roomLabel.HasManualPosition);
            Assert.Equal(280m, roomLabel.X);
            Assert.Equal(156m, roomLabel.Y);
            Assert.Equal(240m, roomLabel.DetectedX);
            Assert.Equal(180m, roomLabel.DetectedY);

            var openingLabel = Assert.Single(reviewSession.OpeningLabels);
            Assert.True(openingLabel.HasManualPosition);
            Assert.Equal(640m, openingLabel.X);
            Assert.Equal(96m, openingLabel.Y);
            Assert.Equal(600m, openingLabel.DetectedX);
            Assert.Equal(120m, openingLabel.DetectedY);

            var curatedOpening = Assert.Single(reviewSession.CuratedPlanArtifacts, item =>
                item.SourceArtifactKind == FloorPlanArtifactSourceKinds.OpeningCandidate);
            Assert.True(curatedOpening.HasManualPosition);
            Assert.Equal(24m, curatedOpening.TranslationDx);
            Assert.Equal(-12m, curatedOpening.TranslationDy);

            var movedOpeningPath = Assert.Single(reviewSession.GeometryPaths, path => path.Id == seeded.OpeningGeometryPathId);
            Assert.Equal(64m, movedOpeningPath.Segments.Single().StartX);
            Assert.Equal(-12m, movedOpeningPath.Segments.Single().StartY);

            using var geometryCommand = session.Connection.CreateCommand();
            geometryCommand.Transaction = session.Transaction;
            geometryCommand.CommandText = "SELECT start_x, start_y FROM geometry_segments WHERE geometry_path_id = $id ORDER BY sort_order LIMIT 1";
            geometryCommand.Parameters.AddWithValue("$id", seeded.OpeningGeometryPathId.ToString());
            using var geometryReader = geometryCommand.ExecuteReader();
            Assert.True(geometryReader.Read());
            Assert.Equal("40", geometryReader.GetString(0));
            Assert.Equal("0", geometryReader.GetString(1));
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
}

Copy `ExecuteImportAsync(...)` unchanged from
`tests/FloorplanFit.Infrastructure.Tests/Review/CuratedPlanArtifactReviewSessionIntegrationTests.cs`.

Add a new `SeedExtractionDraftAndPositionOverlayAsync(...)` helper that:
1. seeds one wall extraction run
2. inserts one room label at detected `(240, 180)`
3. inserts one opening label at detected `(600, 120)`
4. inserts one opening candidate with a geometry path starting at `(40, 0)`
5. creates one draft curation
6. writes three position overlay rows:
   - `RoomLabel -> AbsolutePoint(280, 156)`
   - `OpeningLabel -> AbsolutePoint(640, 96)`
   - `OpeningCandidate -> Translation(24, -12)`
7. commits and returns `new SeededArtifacts(opening.GeometryPathId ?? Guid.Empty)`
```

- [ ] **Step 2: Run the targeted review-reader test to verify red**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~CanonicalArtifactPositionReviewSessionIntegrationTests" --artifacts-path .\.artifacts-test\infra-position-review-red
```

Expected: FAIL because DTOs do not expose manual-position metadata and the reader still returns extractor-first positions.

- [ ] **Step 3: Extend the contracts with resolved-position metadata**

Append defaults to the DTOs to preserve existing call sites.

```csharp
namespace FloorplanFit.Contracts.FloorPlans;

public sealed record RoomLabelDto(
    Guid RoomLabelId,
    string SourceEntityRef,
    string SourceLayer,
    string Text,
    decimal X,
    decimal Y,
    decimal Confidence,
    string? DetectionNotes,
    int SortOrder,
    string? SourceEntityKind = null,
    decimal? TextHeight = null,
    decimal RotationDegrees = 0m,
    string? TextStyleName = null,
    string? HorizontalAlignment = null,
    string? VerticalAlignment = null,
    string? AttachmentPoint = null,
    string? ColorArgb = null,
    bool HasManualPosition = false,
    decimal? DetectedX = null,
    decimal? DetectedY = null);
```

```csharp
namespace FloorplanFit.Contracts.FloorPlans;

public sealed record OpeningLabelDto(
    Guid OpeningLabelId,
    string SourceEntityRef,
    string SourceLayer,
    string Kind,
    string Text,
    decimal X,
    decimal Y,
    decimal Confidence,
    string? DetectionNotes,
    int SortOrder,
    string? SourceEntityKind = null,
    decimal? TextHeight = null,
    decimal RotationDegrees = 0m,
    string? TextStyleName = null,
    string? HorizontalAlignment = null,
    string? VerticalAlignment = null,
    string? AttachmentPoint = null,
    string? ColorArgb = null,
    bool HasManualPosition = false,
    decimal? DetectedX = null,
    decimal? DetectedY = null);
```

```csharp
namespace FloorplanFit.Contracts.FloorPlans;

public sealed record CuratedPlanArtifactDto(
    Guid SourceArtifactId,
    string SourceArtifactKind,
    string SourceEntityRef,
    string SourceLayer,
    string SourceEntityKind,
    string? SourceBlockName,
    IReadOnlyList<Guid> GeometryPathIds,
    decimal Confidence,
    string? DetectionNotes,
    int SortOrder,
    string DetectedFamily,
    string DetectedCategory,
    string DetectedType,
    string ResolvedFamily,
    string ResolvedCategory,
    string ResolvedType,
    string DecisionState,
    string ResolvedColorArgb,
    bool HasManualPosition = false,
    decimal TranslationDx = 0m,
    decimal TranslationDy = 0m);
```

- [ ] **Step 4: Add a dedicated Infrastructure projector and wire it into the review reader**

Create the projector helper:

```csharp
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Infrastructure.Persistence;

internal static class ResolvedFloorPlanArtifactPositionProjector
{
    public static RoomLabelDto Resolve(RoomLabelDto label, FloorPlanArtifactPosition? position)
    {
        if (position is null || position.PositionMode != FloorPlanArtifactPositionMode.AbsolutePoint)
        {
            return label with { HasManualPosition = false, DetectedX = label.X, DetectedY = label.Y };
        }

        var resolvedX = position.ResolvedX!.Value;
        var resolvedY = position.ResolvedY!.Value;
        return label with
        {
            X = resolvedX,
            Y = resolvedY,
            HasManualPosition = resolvedX != label.X || resolvedY != label.Y,
            DetectedX = label.X,
            DetectedY = label.Y
        };
    }

    public static OpeningLabelDto Resolve(OpeningLabelDto label, FloorPlanArtifactPosition? position)
    {
        if (position is null || position.PositionMode != FloorPlanArtifactPositionMode.AbsolutePoint)
        {
            return label with { HasManualPosition = false, DetectedX = label.X, DetectedY = label.Y };
        }

        var resolvedX = position.ResolvedX!.Value;
        var resolvedY = position.ResolvedY!.Value;
        return label with
        {
            X = resolvedX,
            Y = resolvedY,
            HasManualPosition = resolvedX != label.X || resolvedY != label.Y,
            DetectedX = label.X,
            DetectedY = label.Y
        };
    }

    public static CuratedPlanArtifactDto Resolve(CuratedPlanArtifactDto artifact, FloorPlanArtifactPosition? position)
    {
        if (position is null || position.PositionMode != FloorPlanArtifactPositionMode.Translation)
        {
            return artifact with { HasManualPosition = false, TranslationDx = 0m, TranslationDy = 0m };
        }

        var dx = position.TranslationDx ?? 0m;
        var dy = position.TranslationDy ?? 0m;
        return artifact with
        {
            HasManualPosition = dx != 0m || dy != 0m,
            TranslationDx = dx,
            TranslationDy = dy
        };
    }

    public static IReadOnlyList<GeometryPathDto> ApplyGeometryTranslations(
        IReadOnlyList<GeometryPathDto> geometryPaths,
        IReadOnlyList<CuratedPlanArtifactDto> artifacts)
    {
        var pathTranslations = artifacts
            .Where(item => item.HasManualPosition)
            .SelectMany(item => item.GeometryPathIds.Select(pathId => new { pathId, item.TranslationDx, item.TranslationDy }))
            .GroupBy(item => item.pathId)
            .ToDictionary(group => group.Key, group => (group.Last().TranslationDx, group.Last().TranslationDy));

        return geometryPaths.Select(path =>
            pathTranslations.TryGetValue(path.Id, out var translation)
                ? new GeometryPathDto(
                    path.Id,
                    path.IsClosed,
                    path.Segments.Select(segment => new GeometrySegmentDto(
                        segment.GeometryPathId,
                        segment.SortOrder,
                        segment.StartX + translation.TranslationDx,
                        segment.StartY + translation.TranslationDy,
                        segment.EndX + translation.TranslationDx,
                        segment.EndY + translation.TranslationDy)).ToArray())
                : path).ToArray();
    }
}
```

Update the reader to load and apply lineage-aware position overrides:

```csharp
var positionOverrides = GetArtifactPositionOverrides(curationContext.LineageCurationIds);
var roomLabels = extractionRunId is null
    ? []
    : GetRoomLabels(extractionRunId.Value)
        .Select(label => ResolvedFloorPlanArtifactPositionProjector.Resolve(
            label,
            positionOverrides.GetValueOrDefault((FloorPlanArtifactPositionSourceKinds.RoomLabel, label.RoomLabelId))))
        .ToArray();
var openingLabels = extractionRunId is null
    ? []
    : GetOpeningLabels(extractionRunId.Value)
        .Select(label => ResolvedFloorPlanArtifactPositionProjector.Resolve(
            label,
            positionOverrides.GetValueOrDefault((FloorPlanArtifactPositionSourceKinds.OpeningLabel, label.OpeningLabelId))))
        .ToArray();
```

Update curated artifacts and resolved geometry:

```csharp
var curatedPlanArtifacts = BuildCuratedPlanArtifacts(
    openingCandidates,
    fixedPlanComponents,
    protectedDetailAssemblies,
    GetArtifactClassificationOverrides(curationContext.LineageCurationIds),
    positionOverrides);

var geometryPaths = ResolvedFloorPlanArtifactPositionProjector.ApplyGeometryTranslations(
    GetGeometryPaths(geometryPathIds),
    curatedPlanArtifacts);
```

Add a position override query next to the existing classification override query:

```csharp
private IReadOnlyDictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactPosition> GetArtifactPositionOverrides(
    IReadOnlyList<Guid> lineageCurationIds)
{
    var items = new Dictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactPosition>();
    foreach (var curationId in lineageCurationIds)
    {
        using var command = CreateCommand(
            """
            SELECT
                floorplan_curation_id,
                source_artifact_kind,
                source_artifact_id,
                position_mode,
                resolved_x,
                resolved_y,
                translation_dx,
                translation_dy,
                updated_at_utc
            FROM floorplan_artifact_positions
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY updated_at_utc ASC, source_artifact_kind ASC, source_artifact_id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var mode = (FloorPlanArtifactPositionMode)reader.GetInt32(3);
            var position = mode == FloorPlanArtifactPositionMode.AbsolutePoint
                ? FloorPlanArtifactPosition.CreateAbsolutePoint(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    Guid.Parse(reader.GetString(2)),
                    decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                    DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
                : FloorPlanArtifactPosition.CreateTranslation(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    Guid.Parse(reader.GetString(2)),
                    decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                    DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

            items[(position.SourceArtifactKind, position.SourceArtifactId)] = position;
        }
    }

    return items;
}
```

- [ ] **Step 5: Run the targeted review-reader test again to verify green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~CanonicalArtifactPositionReviewSessionIntegrationTests" --artifacts-path .\.artifacts-test\infra-position-review-green
```

Expected: PASS.

- [ ] **Step 6: Commit the read-model slice**

```powershell
git add src/FloorplanFit.Contracts/FloorPlans/RoomLabelDto.cs src/FloorplanFit.Contracts/FloorPlans/OpeningLabelDto.cs src/FloorplanFit.Contracts/FloorPlans/CuratedPlanArtifactDto.cs src/FloorplanFit.Infrastructure/Persistence/ResolvedFloorPlanArtifactPositionProjector.cs src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs tests/FloorplanFit.Infrastructure.Tests/Review/CanonicalArtifactPositionReviewSessionIntegrationTests.cs
git commit -m "feat: resolve canonical artifact positions in review"
```

---

### Task 5: Add preview drag-authoring and label hit-testing

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs`
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Controls/MovableArtifactPreviewControlTests.cs`

- [ ] **Step 1: Write the failing Desktop control tests**

Add these tests:

```csharp
using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class MovableArtifactPreviewControlTests
{
    [Fact]
    public void GetRoomLabelBounds_returns_a_non_empty_hit_target_around_the_rendered_text()
    {
        var pathId = Guid.NewGuid();
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(
            [new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 100m)])],
            new Rect(0, 0, 500, 500),
            48d)!.Value;
        var roomLabel = new RoomLabelDto(Guid.NewGuid(), "TEXT:1", "ROOM LBLS", "KITCHEN", 25m, 75m, 0.95m, null, 1, TextHeight: 8m);

        var bounds = CadTextPreviewLayerRenderer.GetRoomLabelBounds(roomLabel, viewport);

        Assert.True(bounds.Width > 0d);
        Assert.True(bounds.Height > 0d);
        Assert.True(bounds.Contains(viewport.Project(roomLabel.X, roomLabel.Y)));
    }

    [Fact]
    public void ApplyAbsolutePointDelta_uses_model_space_delta_not_pixels()
    {
        var moved = FloorPlanPreviewControl.ApplyAbsolutePointDelta(240m, 180m, 40m, -24m);

        Assert.Equal(280m, moved.X);
        Assert.Equal(156m, moved.Y);
    }

    [Fact]
    public void ApplyTranslationDelta_preserves_existing_manual_offset_and_adds_new_world_delta()
    {
        var moved = FloorPlanPreviewControl.ApplyTranslationDelta(12m, -6m, 8m, 4m);

        Assert.Equal(20m, moved.Dx);
        Assert.Equal(-2m, moved.Dy);
    }

    [Fact]
    public void BuildHitTestGeometry_still_prioritizes_curated_geometry_before_walls_while_labels_use_their_own_hit_targets()
    {
        var wallPathId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var geometry = FloorPlanPreviewControl.BuildHitTestGeometry(
            [
                new GeometryPathDto(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 100m, 0m)]),
                new GeometryPathDto(curatedPathId, false, [new GeometrySegmentDto(curatedPathId, 1, 20m, 0m, 40m, 0m)])
            ],
            curatedArtifacts:
            [
                new CuratedPlanArtifactDto(
                    Guid.NewGuid(),
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    "LINE:DOOR:1",
                    "DOORS",
                    "LINE",
                    null,
                    [curatedPathId],
                    0.95m,
                    null,
                    1,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactTaxonomy.FixedFamily,
                    FloorPlanArtifactTaxonomy.WetFixtureCategory,
                    FloorPlanArtifactTaxonomy.TubType,
                    FloorPlanArtifactDecisionState.Reclassified.ToString(),
                    "#FFDC2626")
            ]);

        Assert.Equal([curatedPathId, wallPathId], geometry.Select(item => item.Id).ToArray());
    }
}
```

- [ ] **Step 2: Run the targeted Desktop control tests to verify red**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~MovableArtifactPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-position-preview-red
```

Expected: FAIL because label bounds and drag math helpers do not exist yet.

- [ ] **Step 3: Add label hit-target helpers and preview drag events**

Expose measurable bounds from the text renderer:

```csharp
internal static Rect GetRoomLabelBounds(RoomLabelDto roomLabel, FloorPlanPreviewGeometry.PreviewViewport viewport)
{
    var plan = CreateRoomLabelRenderPlan(roomLabel, viewport);
    return GetTextBounds(plan, roomLabel.TextStyleName);
}

internal static Rect GetOpeningLabelBounds(OpeningLabelDto openingLabel, FloorPlanPreviewGeometry.PreviewViewport viewport)
{
    var plan = CreateOpeningLabelRenderPlan(openingLabel, viewport);
    return GetTextBounds(plan, openingLabel.TextStyleName);
}

internal static Rect GetTextBounds(TextRenderPlan plan, string? textStyleName)
{
    var text = new FormattedText(
        plan.Text,
        CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        CreateTypeface(textStyleName),
        plan.FontSize,
        Brushes.Black);
    var origin = ResolveTextOriginForMetrics(plan, text.Width, text.Height, text.Baseline);
    return new Rect(origin, new Size(text.Width, text.Height));
}
```

Add movement helpers and events to the preview control:

```csharp
public event EventHandler<RoomLabelClickedEventArgs>? RoomLabelClicked;
public event EventHandler<OpeningLabelClickedEventArgs>? OpeningLabelClicked;
public event EventHandler<MovableArtifactMovedEventArgs>? MovableArtifactMoved;

internal static (decimal X, decimal Y) ApplyAbsolutePointDelta(decimal baseX, decimal baseY, decimal deltaX, decimal deltaY)
    => (baseX + deltaX, baseY + deltaY);

internal static (decimal Dx, decimal Dy) ApplyTranslationDelta(decimal baseDx, decimal baseDy, decimal deltaX, decimal deltaY)
    => (baseDx + deltaX, baseDy + deltaY);
```

Introduce a control-local drag state and compute world-space deltas from the viewport so movement is zoom/pan safe:

```csharp
private PreviewArtifactMoveState? activeArtifactMove;

private readonly record struct PreviewArtifactMoveState(
    string SourceArtifactKind,
    Guid SourceArtifactId,
    FloorPlanArtifactPositionMode PositionMode,
    Point PointerStart,
    decimal BaseX,
    decimal BaseY,
    decimal BaseDx,
    decimal BaseDy);
```

On pointer press:
- check room-label bounds first
- then opening-label bounds
- then existing curated geometry hit-testing
- never start drag for walls or pinch markers

On pointer move:
- unproject start and current pointers through the active viewport
- compute `deltaX` and `deltaY` in model units
- keep the drag state updated and call `InvalidateVisual()`

On pointer release:
- emit `MovableArtifactMoved` with final absolute point or translation payload
- clear capture and drag state

During render:
- if dragging a label, render a transient `RoomLabels` / `OpeningLabels` projection with the moved coordinates
- if dragging curated geometry, render a transient translated `GeometryPaths` projection for the selected curated artifact only

- [ ] **Step 4: Run the targeted Desktop control tests again to verify green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~MovableArtifactPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-position-preview-green
```

Expected: PASS.

- [ ] **Step 5: Commit the preview drag-authoring slice**

```powershell
git add src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs tests/FloorplanFit.Desktop.Tests/Controls/MovableArtifactPreviewControlTests.cs
git commit -m "feat: add preview drag authoring for movable artifacts"
```

---

### Task 6: Wire canonical position save/restore into the review ViewModel and window

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/ViewModels/MovableArtifactFloorPlanReviewViewModelTests.cs`
- Test: `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

- [ ] **Step 1: Write the failing ViewModel and layout tests**

Add ViewModel tests like these:

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class MovableArtifactFloorPlanReviewViewModelTests
{
    [Fact]
    public async Task SaveMovedRoomLabelPositionAsync_persists_absolute_point_and_reselects_the_room_label()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var labelId = Guid.NewGuid();
        var session = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [],
            [new RoomLabelDto(labelId, "TEXT:1", "ROOM LBLS", "KITCHEN", 240m, 180m, 0.95m, null, 1, HasManualPosition: false, DetectedX: 240m, DetectedY: 180m)],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var services = BuildServices(template, session, positionRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectRoomLabel(labelId);

        await viewModel.SaveMovedArtifactPositionAsync(
            new FloorPlanPreviewControl.MovableArtifactMovedEventArgs(
                FloorPlanArtifactPositionSourceKinds.RoomLabel,
                labelId,
                FloorPlanArtifactPositionMode.AbsolutePoint,
                280m,
                156m,
                null,
                null),
            CancellationToken.None);

        var saved = Assert.Single(positionRepository.Items);
        Assert.Equal(FloorPlanArtifactPositionSourceKinds.RoomLabel, saved.SourceArtifactKind);
        Assert.Equal(labelId, saved.SourceArtifactId);
        Assert.Equal(FloorPlanArtifactPositionMode.AbsolutePoint, saved.PositionMode);
        Assert.Equal(280m, saved.ResolvedX);
        Assert.Equal(156m, saved.ResolvedY);
        Assert.Equal("KITCHEN", viewModel.SelectedRoomLabel?.Text);
    }

    [Fact]
    public async Task SaveMovedCuratedArtifactPositionAsync_persists_translation_and_reselects_the_curated_artifact()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var artifactId = Guid.NewGuid();
        var pathId = Guid.NewGuid();
        var session = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, 40m, 0m, 76m, 0m)])],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [
                new CuratedPlanArtifactDto(
                    artifactId,
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    "LINE:DOOR:1",
                    "DOORS",
                    "LINE",
                    null,
                    [pathId],
                    0.95m,
                    null,
                    1,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
                    "#FF455668")
            ]);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var services = BuildServices(template, session, positionRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectPreviewPath(pathId);

        await viewModel.SaveMovedArtifactPositionAsync(
            new FloorPlanPreviewControl.MovableArtifactMovedEventArgs(
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                artifactId,
                FloorPlanArtifactPositionMode.Translation,
                null,
                null,
                24m,
                -12m),
            CancellationToken.None);

        var saved = Assert.Single(positionRepository.Items);
        Assert.Equal(FloorPlanArtifactSourceKinds.OpeningCandidate, saved.SourceArtifactKind);
        Assert.Equal(artifactId, saved.SourceArtifactId);
        Assert.Equal(FloorPlanArtifactPositionMode.Translation, saved.PositionMode);
        Assert.Equal(24m, saved.TranslationDx);
        Assert.Equal(-12m, saved.TranslationDy);
        Assert.Equal("LINE:DOOR:1", viewModel.SelectedCuratedArtifact?.SourceEntityRef);
    }

    [Fact]
    public async Task RestoreSelectedArtifactPositionAsync_uses_detected_room_label_coordinates_or_zero_translation()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var labelId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var pathId = Guid.NewGuid();
        var session = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, 40m, 0m, 76m, 0m)])],
            [new RoomLabelDto(labelId, "TEXT:1", "ROOM LBLS", "KITCHEN", 280m, 156m, 0.95m, null, 1, HasManualPosition: true, DetectedX: 240m, DetectedY: 180m)],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [
                new CuratedPlanArtifactDto(
                    artifactId,
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    "LINE:DOOR:1",
                    "DOORS",
                    "LINE",
                    null,
                    [pathId],
                    0.95m,
                    null,
                    1,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
                    "#FF455668",
                    HasManualPosition: true,
                    TranslationDx: 24m,
                    TranslationDy: -12m)
            ]);
        var positionRepository = new InMemoryFloorPlanArtifactPositionRepository();
        var services = BuildServices(template, session, positionRepository);

        using var provider = services.BuildServiceProvider();
        var viewModel = new FloorPlanReviewViewModel(provider.GetRequiredService<IServiceScopeFactory>(), templateId);
        await viewModel.LoadAsync(CancellationToken.None);

        viewModel.SelectRoomLabel(labelId);
        await viewModel.RestoreSelectedArtifactPositionAsync(CancellationToken.None);
        var restoredLabel = Assert.Single(positionRepository.Items);
        Assert.Equal(FloorPlanArtifactPositionSourceKinds.RoomLabel, restoredLabel.SourceArtifactKind);
        Assert.Equal(FloorPlanArtifactPositionMode.AbsolutePoint, restoredLabel.PositionMode);
        Assert.Equal(240m, restoredLabel.ResolvedX);
        Assert.Equal(180m, restoredLabel.ResolvedY);

        viewModel.SelectPreviewPath(pathId);
        await viewModel.RestoreSelectedArtifactPositionAsync(CancellationToken.None);
        Assert.Equal(2, positionRepository.Items.Count);
        var restoredGeometry = positionRepository.Items.Last();
        Assert.Equal(FloorPlanArtifactSourceKinds.OpeningCandidate, restoredGeometry.SourceArtifactKind);
        Assert.Equal(FloorPlanArtifactPositionMode.Translation, restoredGeometry.PositionMode);
        Assert.Equal(0m, restoredGeometry.TranslationDx);
        Assert.Equal(0m, restoredGeometry.TranslationDy);
    }
}

Reuse the existing `InMemoryFloorPlanTemplateRepository`,
`InMemoryFloorPlanCurationRepository`, `FakeFloorPlanReviewSessionReader`,
`FakeUnitOfWork`, and `FakeClock` helpers from
`tests/FloorplanFit.Desktop.Tests/ViewModels/CuratedArtifactFloorPlanReviewViewModelTests.cs`.

private static ServiceCollection BuildServices(
    FloorPlanTemplate template,
    FloorPlanReviewSessionDto session,
    InMemoryFloorPlanArtifactPositionRepository positionRepository)
{
    var services = new ServiceCollection();
    services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
    services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
    services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
    services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 11, 23, 0, 0, DateTimeKind.Utc)));
    services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(session));
    services.AddSingleton<IFloorPlanArtifactPositionRepository>(positionRepository);
    services.AddTransient<SaveFloorPlanArtifactPositionHandler>();
    services.AddTransient<RestoreFloorPlanArtifactPositionHandler>();
    services.AddTransient<StartOrResumeCurationHandler>();
    services.AddTransient<OpenFloorPlanReviewSessionHandler>();
    services.AddTransient<GetFloorPlanReviewSessionHandler>();
    return services;
}

private sealed class InMemoryFloorPlanArtifactPositionRepository : IFloorPlanArtifactPositionRepository
{
    public List<FloorPlanArtifactPosition> Items { get; } = [];

    public Task<IReadOnlyList<FloorPlanArtifactPosition>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<FloorPlanArtifactPosition>>(Items.Where(item => item.FloorPlanCurationId == floorPlanCurationId).ToArray());
    }

    public Task UpsertAsync(FloorPlanArtifactPosition position, CancellationToken cancellationToken)
    {
        Items.RemoveAll(item =>
            item.FloorPlanCurationId == position.FloorPlanCurationId &&
            item.SourceArtifactKind == position.SourceArtifactKind &&
            item.SourceArtifactId == position.SourceArtifactId);
        Items.Add(position);
        return Task.CompletedTask;
    }
}
```

Update the layout expectations:

```csharp
Assert.Contains("Restore Detected Position", xaml, StringComparison.Ordinal);
Assert.Contains("SelectedArtifactPositionSummary", xaml, StringComparison.Ordinal);
Assert.Contains("MovableArtifactMoved", xaml, StringComparison.Ordinal);
Assert.Contains("RoomLabelClicked", xaml, StringComparison.Ordinal);
Assert.Contains("OpeningLabelClicked", xaml, StringComparison.Ordinal);
```

- [ ] **Step 2: Run the targeted Desktop tests to verify red**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~MovableArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests" --artifacts-path .\.artifacts-test\desktop-position-vm-red
```

Expected: FAIL because the ViewModel does not know how to save moved artifacts, restore positions, or preserve label selection across refreshes.

- [ ] **Step 3: Extend the ViewModel and service registration**

Register the new repository and handlers:

```csharp
services.AddScoped<IFloorPlanArtifactPositionRepository, SqliteFloorPlanArtifactPositionRepository>();
services.AddScoped<SaveFloorPlanArtifactPositionHandler>();
services.AddScoped<RestoreFloorPlanArtifactPositionHandler>();
```

Generalize refresh selection so room labels and opening labels survive refreshes too:

```csharp
private readonly record struct ReviewSelectionSnapshot(
    Guid? CandidateId,
    Guid? PinchMarkerId,
    Guid? PinchGroupId,
    Guid? RoomLabelId,
    Guid? OpeningLabelId,
    string? ArtifactSourceKind,
    Guid? ArtifactSourceId);

private ReviewSelectionSnapshot CaptureSelection()
{
    return new ReviewSelectionSnapshot(
        SelectedCandidate?.CandidateId,
        SelectedPinchMarker?.PinchMarkerId,
        SelectedPinchGroup?.PinchGroupId,
        SelectedRoomLabel?.RoomLabelId,
        SelectedOpeningLabel?.OpeningLabelId,
        SelectedCuratedArtifact?.SourceArtifactKind,
        SelectedCuratedArtifact?.SourceArtifactId);
}
```

Add ViewModel properties and methods:

```csharp
public bool CanRestoreSelectedArtifactPosition =>
    SelectedRoomLabel?.HasManualPosition == true ||
    SelectedOpeningLabel?.HasManualPosition == true ||
    SelectedCuratedArtifact?.HasManualPosition == true;

public string SelectedArtifactPositionSummary =>
    SelectedRoomLabel is not null
        ? $"Room label at ({SelectedRoomLabel.X}, {SelectedRoomLabel.Y}) mm. Detected: ({SelectedRoomLabel.DetectedX}, {SelectedRoomLabel.DetectedY}) mm."
        : SelectedOpeningLabel is not null
            ? $"Opening label at ({SelectedOpeningLabel.X}, {SelectedOpeningLabel.Y}) mm. Detected: ({SelectedOpeningLabel.DetectedX}, {SelectedOpeningLabel.DetectedY}) mm."
            : SelectedCuratedArtifact is not null
                ? $"Curated translation dx/dy: ({SelectedCuratedArtifact.TranslationDx}, {SelectedCuratedArtifact.TranslationDy}) mm."
                : string.Empty;

public void SelectRoomLabel(Guid roomLabelId)
{
    SelectedRoomLabel = RoomLabels.FirstOrDefault(item => item.RoomLabelId == roomLabelId);
}

public void SelectOpeningLabel(Guid openingLabelId)
{
    SelectedOpeningLabel = OpeningLabels.FirstOrDefault(item => item.OpeningLabelId == openingLabelId);
}
```

Add the save-moved-artifact flow:

```csharp
public async Task SaveMovedArtifactPositionAsync(FloorPlanPreviewControl.MovableArtifactMovedEventArgs e, CancellationToken cancellationToken)
{
    if (DraftCurationId == Guid.Empty)
    {
        return;
    }

    StatusMessage = $"Saving moved artifact {e.SourceArtifactKind}:{e.SourceArtifactId}...";

    using (var scope = scopeFactory.CreateScope())
    {
        var handler = scope.ServiceProvider.GetRequiredService<SaveFloorPlanArtifactPositionHandler>();
        await handler.HandleAsync(
            DraftCurationId,
            e.SourceArtifactKind,
            e.SourceArtifactId,
            e.PositionMode,
            e.ResolvedX,
            e.ResolvedY,
            e.TranslationDx,
            e.TranslationDy,
            cancellationToken);
    }

    await RefreshSessionAsync(CaptureSelection(), cancellationToken);
    StatusMessage = "Saved canonical artifact position";
}
```

Add restore-position flow:

```csharp
public async Task RestoreSelectedArtifactPositionAsync(CancellationToken cancellationToken)
{
    if (!CanRestoreSelectedArtifactPosition || DraftCurationId == Guid.Empty)
    {
        return;
    }

    using var scope = scopeFactory.CreateScope();
    var handler = scope.ServiceProvider.GetRequiredService<RestoreFloorPlanArtifactPositionHandler>();

    if (SelectedRoomLabel is not null)
    {
        await handler.HandleAsync(
            DraftCurationId,
            FloorPlanArtifactPositionSourceKinds.RoomLabel,
            SelectedRoomLabel.RoomLabelId,
            FloorPlanArtifactPositionMode.AbsolutePoint,
            SelectedRoomLabel.DetectedX,
            SelectedRoomLabel.DetectedY,
            null,
            null,
            cancellationToken);
    }
    else if (SelectedOpeningLabel is not null)
    {
        await handler.HandleAsync(
            DraftCurationId,
            FloorPlanArtifactPositionSourceKinds.OpeningLabel,
            SelectedOpeningLabel.OpeningLabelId,
            FloorPlanArtifactPositionMode.AbsolutePoint,
            SelectedOpeningLabel.DetectedX,
            SelectedOpeningLabel.DetectedY,
            null,
            null,
            cancellationToken);
    }
    else if (SelectedCuratedArtifact is not null)
    {
        await handler.HandleAsync(
            DraftCurationId,
            SelectedCuratedArtifact.SourceArtifactKind,
            SelectedCuratedArtifact.SourceArtifactId,
            FloorPlanArtifactPositionMode.Translation,
            null,
            null,
            0m,
            0m,
            cancellationToken);
    }

    await RefreshSessionAsync(CaptureSelection(), cancellationToken);
    StatusMessage = "Restored detected artifact position";
}
```

- [ ] **Step 4: Update the window bindings and event handlers**

Wire the preview control events in XAML:

```xml
<controls:FloorPlanPreviewControl Grid.Row="3"
                                  GeometryPathClicked="PreviewControl_OnGeometryPathClicked"
                                  RoomLabelClicked="PreviewControl_OnRoomLabelClicked"
                                  OpeningLabelClicked="PreviewControl_OnOpeningLabelClicked"
                                  MovableArtifactMoved="PreviewControl_OnMovableArtifactMoved"
                                  GeometryPaths="{Binding GeometryPaths}"
                                  RoomLabels="{Binding RoomLabels}"
                                  OpeningLabels="{Binding OpeningLabels}"
                                  CuratedPlanArtifacts="{Binding VisibleCuratedPlanArtifacts}" />
```

Add the inspector button and summary:

```xml
<Button Content="Restore Detected Position"
        IsEnabled="{Binding CanRestoreSelectedArtifactPosition}"
        Click="RestorePositionButton_OnClick" />
<TextBlock x:Name="SelectedArtifactPositionSummary"
           Text="{Binding SelectedArtifactPositionSummary}"
           TextWrapping="Wrap" />
```

Handle the events in code-behind:

```csharp
private void PreviewControl_OnRoomLabelClicked(object? sender, FloorPlanPreviewControl.RoomLabelClickedEventArgs e)
{
    if (DataContext is FloorPlanReviewViewModel viewModel)
    {
        viewModel.SelectRoomLabel(e.RoomLabelId);
    }
}

private void PreviewControl_OnOpeningLabelClicked(object? sender, FloorPlanPreviewControl.OpeningLabelClickedEventArgs e)
{
    if (DataContext is FloorPlanReviewViewModel viewModel)
    {
        viewModel.SelectOpeningLabel(e.OpeningLabelId);
    }
}

private async void PreviewControl_OnMovableArtifactMoved(object? sender, FloorPlanPreviewControl.MovableArtifactMovedEventArgs e)
{
    if (DataContext is FloorPlanReviewViewModel viewModel)
    {
        await viewModel.SaveMovedArtifactPositionAsync(e, CancellationToken.None);
    }
}

private async void RestorePositionButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    if (DataContext is FloorPlanReviewViewModel viewModel)
    {
        await viewModel.RestoreSelectedArtifactPositionAsync(CancellationToken.None);
    }
}
```

- [ ] **Step 5: Run the targeted Desktop tests again to verify green**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~MovableArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests" --artifacts-path .\.artifacts-test\desktop-position-vm-green
```

Expected: PASS.

- [ ] **Step 6: Commit the Desktop integration slice**

```powershell
git add src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Desktop.Tests/ViewModels/MovableArtifactFloorPlanReviewViewModelTests.cs tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs
git commit -m "feat: wire canonical artifact positioning into review ui"
```

---

### Task 7: Run the full regression suite and finish the feature branch cleanly

**Files:**
- Verify only: no new files

- [ ] **Step 1: Run the full Application regression suite**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .\.artifacts-test\app-position-full
```

Expected: PASS.

- [ ] **Step 2: Run the full Infrastructure regression suite**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --artifacts-path .\.artifacts-test\infra-position-full
```

Expected: PASS.

- [ ] **Step 3: Run the full Desktop regression suite**

Run:

```powershell
dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-position-full
```

Expected: PASS.

- [ ] **Step 4: Inspect git diff before the final commit**

Run:

```powershell
git status --short
git diff --stat
```

Expected: only the intended Loop 1 canonical non-wall positioning files remain staged or modified.

- [ ] **Step 5: Create the final feature commit**

```powershell
git add src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPositionMode.cs src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPositionSourceKinds.cs src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPosition.cs src/FloorplanFit.Application/Abstractions/IFloorPlanArtifactPositionRepository.cs src/FloorplanFit.Application/FloorPlans/Curation/SaveFloorPlanArtifactPositionHandler.cs src/FloorplanFit.Application/FloorPlans/Curation/RestoreFloorPlanArtifactPositionHandler.cs src/FloorplanFit.Contracts/FloorPlans/RoomLabelDto.cs src/FloorplanFit.Contracts/FloorPlans/OpeningLabelDto.cs src/FloorplanFit.Contracts/FloorPlans/CuratedPlanArtifactDto.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanArtifactPositionRepository.cs src/FloorplanFit.Infrastructure/Persistence/ResolvedFloorPlanArtifactPositionProjector.cs src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanArtifactPositionTests.cs tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanArtifactPositionHandlersTests.cs tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanArtifactPositionPersistenceIntegrationTests.cs tests/FloorplanFit.Infrastructure.Tests/Review/CanonicalArtifactPositionReviewSessionIntegrationTests.cs tests/FloorplanFit.Desktop.Tests/Controls/MovableArtifactPreviewControlTests.cs tests/FloorplanFit.Desktop.Tests/ViewModels/MovableArtifactFloorPlanReviewViewModelTests.cs tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs
git commit -m "feat: support canonical non-wall positioning in review"
```

- [ ] **Step 6: Stop. Do not build.**

This repo explicitly forbids builds after changes. Verification for this feature is test-only.
