# House Plan Set Multiple Sheets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Phase 2 by allowing a HousePlanSet version to store dependent sheets such as electrical, roof, and facade/elevation DXFs beside the canonical floor-plan sheet.

**Architecture:** Keep the Phase 1 bridge: the existing current `FloorPlanVersion.Id` acts as `PlanSetVersionId` until real `plan_set_versions` exists. Add a minimal `plan_sheets` table and PlanSets import/read surface. Do not create registration, projection, export, or sheet-specific fit logic in this phase.

**Tech Stack:** C#/.NET 10, SQLite via `Microsoft.Data.Sqlite`, existing storage/hash/DXF metadata ports, xUnit. No `dotnet build`; use `git diff --check` and only use `dotnet test --no-build` if the compiled test assembly already contains the new tests.

---

## Scope Boundary

This plan implements **Phase 2 - Multiple Sheets per House** from `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`.

In scope:

- store dependent sheet metadata for a plan-set version
- import managed DXF files as dependent plan sheets
- user-selected sheet type is the Phase 2 classification source
- show dependent sheets in the PlanSet library read model
- keep canonical floor-plan sheet behavior from Phase 1

Out of scope:

- sheet registration transforms
- electrical projection
- roof overhang rules
- facade/elevation projection
- multi-sheet export
- Desktop UI wiring
- independent fit engines

## Data Model

Use one minimal table:

```text
plan_sheets
- id TEXT PRIMARY KEY
- plan_set_version_id TEXT NOT NULL
- sheet_type INTEGER NOT NULL
- imported_document_id TEXT NOT NULL
- measurement_context_id TEXT NOT NULL
- name TEXT NOT NULL
- status TEXT NOT NULL
- created_at_utc TEXT NOT NULL
```

No `house_plan_sets` / `plan_set_versions` table yet. Phase 1 already maps `FloorPlanTemplate.Id` to `HousePlanSetId` and current `FloorPlanVersion.Id` to `PlanSetVersionId`. Adding real plan-set tables before dependent sheets prove the model is ceremony.

## File Structure

Create:

- `src/FloorplanFit.Domain/PlanSets/PlanSheetStatus.cs` - persisted sheet lifecycle vocabulary.
- `src/FloorplanFit.Domain/PlanSets/PlanSheet.cs` - domain entity for dependent sheets.
- `src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetRequest.cs` - import request.
- `src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetResponse.cs` - import response.
- `src/FloorplanFit.Application/Abstractions/IPlanSheetRepository.cs` - write/read-by-id port.
- `src/FloorplanFit.Application/Abstractions/IPlanSheetReader.cs` - read-model port for PlanSet library.
- `src/FloorplanFit.Application/PlanSets/Import/ImportPlanSheetHandler.cs` - imports a managed dependent sheet.
- `src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSheetRepository.cs` - SQLite repository + reader.
- `tests/FloorplanFit.Application.Tests/PlanSets/Import/ImportPlanSheetHandlerTests.cs` - import behavior tests.

Modify:

- `src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs` - add `PlanSheetDxf = 5`.
- `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs` - include persisted dependent sheets.
- `tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs` - provide fake sheet reader and assert dependent sheets.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs` - create `plan_sheets`.
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs` - register PlanSheet repository/reader and handler.
- `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md` - record Phase 2 bridge.

---

### Task 1: Add PlanSheet domain model

**Files:**
- Create: `src/FloorplanFit.Domain/PlanSets/PlanSheetStatus.cs`
- Create: `src/FloorplanFit.Domain/PlanSets/PlanSheet.cs`

- [ ] **Step 1: Add sheet status**

Create `src/FloorplanFit.Domain/PlanSets/PlanSheetStatus.cs`:

```csharp
namespace FloorplanFit.Domain.PlanSets;

public enum PlanSheetStatus
{
    Imported = 1,
    Archived = 2
}
```

- [ ] **Step 2: Add sheet entity**

Create `src/FloorplanFit.Domain/PlanSets/PlanSheet.cs`:

```csharp
namespace FloorplanFit.Domain.PlanSets;

public sealed class PlanSheet
{
    public PlanSheet(
        Guid id,
        Guid planSetVersionId,
        PlanSheetType sheetType,
        Guid importedDocumentId,
        Guid measurementContextId,
        string name,
        PlanSheetStatus status,
        DateTime createdAtUtc)
    {
        if (sheetType is PlanSheetType.Unknown)
        {
            throw new ArgumentException("A dependent plan sheet must have an explicit sheet type.", nameof(sheetType));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Sheet name is required.", nameof(name));
        }

        Id = id;
        PlanSetVersionId = planSetVersionId;
        SheetType = sheetType;
        ImportedDocumentId = importedDocumentId;
        MeasurementContextId = measurementContextId;
        Name = name;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid PlanSetVersionId { get; }

    public PlanSheetType SheetType { get; }

    public Guid ImportedDocumentId { get; }

    public Guid MeasurementContextId { get; }

    public string Name { get; }

    public PlanSheetStatus Status { get; }

    public DateTime CreatedAtUtc { get; }
}
```

- [ ] **Step 3: Commit**

```powershell
git add -- src/FloorplanFit.Domain/PlanSets/PlanSheetStatus.cs src/FloorplanFit.Domain/PlanSets/PlanSheet.cs
git commit -m "feat: add plan sheet domain model"
```

---

### Task 2: Add import contracts and document type

**Files:**
- Create: `src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetRequest.cs`
- Create: `src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetResponse.cs`
- Modify: `src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs`

- [ ] **Step 1: Add document type**

Modify `src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs`:

```csharp
namespace FloorplanFit.Domain.Documents;

public enum ImportedDocumentType
{
    FloorPlanDxf = 1,
    SitePlanDxf = 2,
    ExportedAdjustedDxf = 3,
    ExportedReport = 4,
    PlanSheetDxf = 5
}
```

- [ ] **Step 2: Add request contract**

Create `src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetRequest.cs`:

```csharp
namespace FloorplanFit.Contracts.PlanSets;

public sealed record ImportPlanSheetRequest(
    Guid PlanSetVersionId,
    string SheetType,
    string FilePath,
    string? Name = null);
```

- [ ] **Step 3: Add response contract**

Create `src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetResponse.cs`:

```csharp
namespace FloorplanFit.Contracts.PlanSets;

public sealed record ImportPlanSheetResponse(
    Guid SheetId,
    Guid PlanSetVersionId,
    string SheetType,
    string Name,
    Guid ImportedDocumentId,
    Guid MeasurementContextId,
    string Status,
    DateTime ImportedAtUtc);
```

- [ ] **Step 4: Commit**

```powershell
git add -- src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetRequest.cs src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetResponse.cs
git commit -m "feat: add plan sheet import contracts"
```

---

### Task 3: Add PlanSheet ports

**Files:**
- Create: `src/FloorplanFit.Application/Abstractions/IPlanSheetRepository.cs`
- Create: `src/FloorplanFit.Application/Abstractions/IPlanSheetReader.cs`

- [ ] **Step 1: Add repository port**

Create `src/FloorplanFit.Application/Abstractions/IPlanSheetRepository.cs`:

```csharp
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSheetRepository
{
    Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken);

    Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Add read-model port**

Create `src/FloorplanFit.Application/Abstractions/IPlanSheetReader.cs`:

```csharp
using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSheetReader
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
        IReadOnlyCollection<Guid> planSetVersionIds,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Commit**

```powershell
git add -- src/FloorplanFit.Application/Abstractions/IPlanSheetRepository.cs src/FloorplanFit.Application/Abstractions/IPlanSheetReader.cs
git commit -m "feat: add plan sheet ports"
```

---

### Task 4: Write RED tests for importing a dependent sheet

**Files:**
- Create: `tests/FloorplanFit.Application.Tests/PlanSets/Import/ImportPlanSheetHandlerTests.cs`

- [ ] **Step 1: Create test folder**

```powershell
New-Item -ItemType Directory -Force tests\FloorplanFit.Application.Tests\PlanSets\Import
```

- [ ] **Step 2: Add tests**

Create `tests/FloorplanFit.Application.Tests/PlanSets/Import/ImportPlanSheetHandlerTests.cs` with:

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Import;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.Measurement;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Import;

public sealed class ImportPlanSheetHandlerTests
{
    [Fact]
    public async Task HandleAsync_imports_user_classified_electrical_sheet_as_dependent_plan_sheet()
    {
        var planSetVersionId = Guid.NewGuid();
        var clock = new FakeClock(new DateTime(2026, 6, 30, 16, 0, 0, DateTimeKind.Utc));
        var storage = new FakeManagedFileStorage("C:\\managed\\electrical.dxf");
        var dxf = new FakeDxfGateway();
        var hash = new FakeHashService("hash-123");
        var measurementRepository = new CapturingMeasurementContextRepository();
        var documentRepository = new CapturingImportedDocumentRepository();
        var sheetRepository = new CapturingPlanSheetRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new ImportPlanSheetHandler(
            dxf,
            storage,
            documentRepository,
            measurementRepository,
            sheetRepository,
            unitOfWork,
            hash,
            clock);

        var response = await handler.HandleAsync(
            new ImportPlanSheetRequest(
                planSetVersionId,
                "ElectricalPlan",
                "C:\\source\\electrical.dxf",
                "Electrical"),
            CancellationToken.None);

        Assert.Equal(planSetVersionId, response.PlanSetVersionId);
        Assert.Equal("ElectricalPlan", response.SheetType);
        Assert.Equal("Electrical", response.Name);
        Assert.Equal("Imported", response.Status);
        Assert.Equal(clock.UtcNow, response.ImportedAtUtc);
        Assert.True(unitOfWork.Saved);

        var measurement = Assert.Single(measurementRepository.Items);
        Assert.Equal(LengthUnit.Inch, measurement.SourceUnit);

        var document = Assert.Single(documentRepository.Items);
        Assert.Equal(ImportedDocumentType.PlanSheetDxf, document.DocumentType);
        Assert.Equal("electrical.dxf", document.OriginalFileName);
        Assert.Equal("C:\\managed\\electrical.dxf", document.StoragePath);
        Assert.Equal("hash-123", document.Sha256);
        Assert.Equal(measurement.Id, document.MeasurementContextId);

        var sheet = Assert.Single(sheetRepository.Items);
        Assert.Equal(response.SheetId, sheet.Id);
        Assert.Equal(planSetVersionId, sheet.PlanSetVersionId);
        Assert.Equal(PlanSheetType.ElectricalPlan, sheet.SheetType);
        Assert.Equal(document.Id, sheet.ImportedDocumentId);
        Assert.Equal(measurement.Id, sheet.MeasurementContextId);
        Assert.Equal("Electrical", sheet.Name);
        Assert.Equal(PlanSheetStatus.Imported, sheet.Status);
    }

    [Fact]
    public async Task HandleAsync_rejects_floor_plan_sheet_import_because_floor_plan_stays_canonical_flow()
    {
        var handler = new ImportPlanSheetHandler(
            new FakeDxfGateway(),
            new FakeManagedFileStorage("C:\\managed\\floor.dxf"),
            new CapturingImportedDocumentRepository(),
            new CapturingMeasurementContextRepository(),
            new CapturingPlanSheetRepository(),
            new CapturingUnitOfWork(),
            new FakeHashService("hash"),
            new FakeClock(new DateTime(2026, 6, 30, 16, 0, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new ImportPlanSheetRequest(Guid.NewGuid(), "FloorPlan", "C:\\source\\floor.dxf"),
            CancellationToken.None));
    }

    private sealed class FakeDxfGateway : IDxfGateway
    {
        public Task<DetectedFloorPlanDocument> ReadFloorPlanAsync(string filePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DetectedFloorPlanDocument(
                Path.GetFileName(filePath),
                Path.GetFileNameWithoutExtension(filePath),
                LengthUnit.Inch,
                25.4m,
                "AC1032",
                "bbox:0,0,10,10"));
        }
    }

    private sealed class FakeManagedFileStorage : IManagedFileStorage
    {
        private readonly string managedPath;

        public FakeManagedFileStorage(string managedPath)
        {
            this.managedPath = managedPath;
        }

        public Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(managedPath);
        }

        public Task<string> ReserveAdjustedDxfPathAsync(string sourceFileName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeHashService : IFileHashService
    {
        private readonly string hash;

        public FakeHashService(string hash)
        {
            this.hash = hash;
        }

        public Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(hash);
        }
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class CapturingMeasurementContextRepository : IMeasurementContextRepository
    {
        public List<MeasurementContext> Items { get; } = [];

        public Task AddAsync(MeasurementContext context, CancellationToken cancellationToken)
        {
            Items.Add(context);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingImportedDocumentRepository : IImportedDocumentRepository
    {
        public List<ImportedDocument> Items { get; } = [];

        public Task AddAsync(ImportedDocument document, CancellationToken cancellationToken)
        {
            Items.Add(document);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingPlanSheetRepository : IPlanSheetRepository
    {
        public List<PlanSheet> Items { get; } = [];

        public Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken)
        {
            Items.Add(sheet);
            return Task.CompletedTask;
        }

        public Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.Id == sheetId));
        }
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public bool Saved { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 3: Record RED without forcing build**

Run:

```powershell
Test-Path src\FloorplanFit.Application\PlanSets\Import\ImportPlanSheetHandler.cs
Test-Path src\FloorplanFit.Application\Abstractions\IPlanSheetRepository.cs
```

Expected:

```text
False
False
```

Do not run a command that compiles.

---

### Task 5: Implement minimal ImportPlanSheetHandler

**Files:**
- Create: `src/FloorplanFit.Application/PlanSets/Import/ImportPlanSheetHandler.cs`

- [ ] **Step 1: Create application folder**

```powershell
New-Item -ItemType Directory -Force src\FloorplanFit.Application\PlanSets\Import
```

- [ ] **Step 2: Add handler**

Create `src/FloorplanFit.Application/PlanSets/Import/ImportPlanSheetHandler.cs`:

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.Measurement;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Import;

public sealed class ImportPlanSheetHandler
{
    private readonly IDxfGateway dxfGateway;
    private readonly IManagedFileStorage managedFileStorage;
    private readonly IImportedDocumentRepository importedDocumentRepository;
    private readonly IMeasurementContextRepository measurementContextRepository;
    private readonly IPlanSheetRepository planSheetRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IFileHashService fileHashService;
    private readonly IClock clock;

    public ImportPlanSheetHandler(
        IDxfGateway dxfGateway,
        IManagedFileStorage managedFileStorage,
        IImportedDocumentRepository importedDocumentRepository,
        IMeasurementContextRepository measurementContextRepository,
        IPlanSheetRepository planSheetRepository,
        IUnitOfWork unitOfWork,
        IFileHashService fileHashService,
        IClock clock)
    {
        this.dxfGateway = dxfGateway;
        this.managedFileStorage = managedFileStorage;
        this.importedDocumentRepository = importedDocumentRepository;
        this.measurementContextRepository = measurementContextRepository;
        this.planSheetRepository = planSheetRepository;
        this.unitOfWork = unitOfWork;
        this.fileHashService = fileHashService;
        this.clock = clock;
    }

    public async Task<ImportPlanSheetResponse> HandleAsync(
        ImportPlanSheetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PlanSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan set version is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            throw new ArgumentException("A sheet file path is required.", nameof(request));
        }

        if (!Enum.TryParse<PlanSheetType>(request.SheetType, ignoreCase: true, out var sheetType) ||
            sheetType is PlanSheetType.Unknown)
        {
            throw new ArgumentException("A known dependent sheet type is required.", nameof(request));
        }

        if (sheetType is PlanSheetType.FloorPlan)
        {
            throw new ArgumentException("Floor plans must use the canonical floor-plan import flow.", nameof(request));
        }

        var sourceFileName = Path.GetFileName(request.FilePath);
        var sheetName = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.FilePath)
            : request.Name.Trim();
        var managedFilePath = await managedFileStorage.CopyIntoLibraryAsync(request.FilePath, cancellationToken);
        var detectedDocument = await dxfGateway.ReadFloorPlanAsync(managedFilePath, cancellationToken);
        var importedAtUtc = clock.UtcNow;

        var measurementContext = new MeasurementContext(
            Guid.NewGuid(),
            detectedDocument.SourceUnit,
            detectedDocument.ToMillimetersFactor,
            1m,
            0.5m,
            importedAtUtc);

        var sha256 = await fileHashService.ComputeSha256Async(managedFilePath, cancellationToken);
        var importedDocument = new ImportedDocument(
            Guid.NewGuid(),
            ImportedDocumentType.PlanSheetDxf,
            sourceFileName,
            managedFilePath,
            sha256,
            detectedDocument.DxfVersion,
            measurementContext.Id,
            importedAtUtc);

        var sheet = new PlanSheet(
            Guid.NewGuid(),
            request.PlanSetVersionId,
            sheetType,
            importedDocument.Id,
            measurementContext.Id,
            sheetName,
            PlanSheetStatus.Imported,
            importedAtUtc);

        await measurementContextRepository.AddAsync(measurementContext, cancellationToken);
        await importedDocumentRepository.AddAsync(importedDocument, cancellationToken);
        await planSheetRepository.AddAsync(sheet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportPlanSheetResponse(
            sheet.Id,
            sheet.PlanSetVersionId,
            sheet.SheetType.ToString(),
            sheet.Name,
            sheet.ImportedDocumentId,
            sheet.MeasurementContextId,
            sheet.Status.ToString(),
            sheet.CreatedAtUtc);
    }
}
```

- [ ] **Step 3: Verify without compile**

```powershell
git diff --check -- src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Application/Abstractions src/FloorplanFit.Application/PlanSets/Import tests/FloorplanFit.Application.Tests/PlanSets/Import
```

- [ ] **Step 4: Commit**

```powershell
git add -- src/FloorplanFit.Domain/PlanSets/PlanSheetStatus.cs src/FloorplanFit.Domain/PlanSets/PlanSheet.cs src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetRequest.cs src/FloorplanFit.Contracts/PlanSets/ImportPlanSheetResponse.cs src/FloorplanFit.Application/Abstractions/IPlanSheetRepository.cs src/FloorplanFit.Application/Abstractions/IPlanSheetReader.cs src/FloorplanFit.Application/PlanSets/Import/ImportPlanSheetHandler.cs tests/FloorplanFit.Application.Tests/PlanSets/Import/ImportPlanSheetHandlerTests.cs
git commit -m "feat: import dependent plan sheets"
```

---

### Task 6: Include dependent sheets in PlanSet library

**Files:**
- Modify: `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs`
- Modify: `tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs`

- [ ] **Step 1: Update tests**

Add `using FloorplanFit.Contracts.PlanSets;` to `GetPlanSetLibraryHandlerTests.cs`.

Change handler creation to:

```csharp
var handler = new GetPlanSetLibraryHandler(
    new FakeFloorPlanLibraryReader([item]),
    new FakePlanSheetReader(new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>()));
```

Add this fake:

```csharp
private sealed class FakePlanSheetReader : IPlanSheetReader
{
    private readonly IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> sheets;

    public FakePlanSheetReader(IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> sheets)
    {
        this.sheets = sheets;
    }

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
        IReadOnlyCollection<Guid> planSetVersionIds,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(sheets);
    }
}
```

Add this test:

```csharp
[Fact]
public async Task HandleAsync_includes_dependent_sheets_for_current_plan_set_version()
{
    var templateId = Guid.NewGuid();
    var currentVersionId = Guid.NewGuid();
    var importedAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    var electricalSheet = new PlanSetSheetDto(
        Guid.NewGuid(),
        "ElectricalPlan",
        "Electrical",
        Guid.NewGuid(),
        null,
        IsCanonical: false,
        "Unregistered",
        "NotProjected");
    var item = new FloorPlanLibraryItemDto(
        templateId,
        "seminole2000",
        "SEMINOLE2000",
        1,
        currentVersionId,
        1,
        [
            new FloorPlanLibraryVersionDto(
                currentVersionId,
                1,
                "Published",
                importedAtUtc,
                "inch",
                IsCurrent: true)
        ]);

    var handler = new GetPlanSetLibraryHandler(
        new FakeFloorPlanLibraryReader([item]),
        new FakePlanSheetReader(new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>
        {
            [currentVersionId] = [electricalSheet]
        }));

    var result = await handler.HandleAsync(CancellationToken.None);

    var planSet = Assert.Single(result);
    Assert.Equal(2, planSet.Sheets.Count);
    Assert.Contains(planSet.Sheets, sheet => sheet.IsCanonical && sheet.SheetType == "FloorPlan");
    Assert.Contains(planSet.Sheets, sheet => !sheet.IsCanonical && sheet.SheetType == "ElectricalPlan");
}
```

- [ ] **Step 2: Update handler**

Replace `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs` with:

```csharp
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.PlanSets.Library;

public sealed class GetPlanSetLibraryHandler
{
    private const string FloorPlanSheetType = "FloorPlan";
    private const string CanonicalRegistrationStatus = "Canonical";
    private const string CanonicalProjectionStatus = "CanonicalSource";

    private readonly IFloorPlanLibraryReader floorPlanLibraryReader;
    private readonly IPlanSheetReader planSheetReader;

    public GetPlanSetLibraryHandler(
        IFloorPlanLibraryReader floorPlanLibraryReader,
        IPlanSheetReader planSheetReader)
    {
        this.floorPlanLibraryReader = floorPlanLibraryReader;
        this.planSheetReader = planSheetReader;
    }

    public async Task<IReadOnlyList<PlanSetLibraryItemDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var floorPlans = await floorPlanLibraryReader.ListAsync(cancellationToken);
        var versionIds = floorPlans
            .Select(item => item.CurrentVersionId ?? item.CurrentVersion?.VersionId)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .ToArray();
        var dependentSheets = await planSheetReader.ListByPlanSetVersionIdsAsync(versionIds, cancellationToken);
        return floorPlans.Select(item => Project(item, dependentSheets)).ToArray();
    }

    private static PlanSetLibraryItemDto Project(
        FloorPlanLibraryItemDto floorPlan,
        IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> dependentSheetsByVersion)
    {
        var currentVersion = floorPlan.CurrentVersion;
        var canonicalVersionId = floorPlan.CurrentVersionId ?? currentVersion?.VersionId;
        var sheets = new List<PlanSetSheetDto>();

        if (canonicalVersionId.HasValue)
        {
            sheets.Add(new PlanSetSheetDto(
                canonicalVersionId.Value,
                FloorPlanSheetType,
                $"{floorPlan.Name} Floor Plan",
                Guid.Empty,
                canonicalVersionId.Value,
                IsCanonical: true,
                CanonicalRegistrationStatus,
                CanonicalProjectionStatus));

            if (dependentSheetsByVersion.TryGetValue(canonicalVersionId.Value, out var dependentSheets))
            {
                sheets.AddRange(dependentSheets);
            }
        }

        return new PlanSetLibraryItemDto(
            floorPlan.TemplateId,
            floorPlan.Code,
            floorPlan.Name,
            canonicalVersionId,
            canonicalVersionId,
            floorPlan.ActivePublishedCurationId,
            sheets);
    }
}
```

- [ ] **Step 3: Verify diff**

```powershell
git diff --check -- src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs
```

- [ ] **Step 4: Commit**

```powershell
git add -- src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs
git commit -m "feat: list dependent plan sheets"
```

---

### Task 7: Add SQLite persistence and DI wiring

**Files:**
- Create: `src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSheetRepository.cs`
- Modify: `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- Modify: `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

- [ ] **Step 1: Add repository**

Create `src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSheetRepository.cs` with:

```csharp
using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqlitePlanSheetRepository : IPlanSheetRepository, IPlanSheetReader
{
    private readonly SqliteSession session;

    public SqlitePlanSheetRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO plan_sheets (
                id,
                plan_set_version_id,
                sheet_type,
                imported_document_id,
                measurement_context_id,
                name,
                status,
                created_at_utc)
            VALUES (
                $id,
                $plan_set_version_id,
                $sheet_type,
                $imported_document_id,
                $measurement_context_id,
                $name,
                $status,
                $created_at_utc)
            """);
        command.Parameters.AddWithValue("$id", sheet.Id.ToString());
        command.Parameters.AddWithValue("$plan_set_version_id", sheet.PlanSetVersionId.ToString());
        command.Parameters.AddWithValue("$sheet_type", (int)sheet.SheetType);
        command.Parameters.AddWithValue("$imported_document_id", sheet.ImportedDocumentId.ToString());
        command.Parameters.AddWithValue("$measurement_context_id", sheet.MeasurementContextId.ToString());
        command.Parameters.AddWithValue("$name", sheet.Name);
        command.Parameters.AddWithValue("$status", sheet.Status.ToString());
        command.Parameters.AddWithValue("$created_at_utc", sheet.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT id, plan_set_version_id, sheet_type, imported_document_id, measurement_context_id, name, status, created_at_utc
            FROM plan_sheets
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", sheetId.ToString());

        using var reader = command.ExecuteReader();
        return Task.FromResult(reader.Read() ? MapSheet(reader) : null);
    }

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
        IReadOnlyCollection<Guid> planSetVersionIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (planSetVersionIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>>(
                new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>());
        }

        var ids = planSetVersionIds.ToArray();
        var parameterNames = ids.Select((_, index) => $"$id{index}").ToArray();
        using var command = CreateCommand(
            $"""
            SELECT id, plan_set_version_id, sheet_type, imported_document_id, measurement_context_id, name, status, created_at_utc
            FROM plan_sheets
            WHERE plan_set_version_id IN ({string.Join(", ", parameterNames)})
            ORDER BY created_at_utc ASC, name ASC
            """);

        for (var index = 0; index < ids.Length; index++)
        {
            command.Parameters.AddWithValue(parameterNames[index], ids[index].ToString());
        }

        using var reader = command.ExecuteReader();
        var mutable = new Dictionary<Guid, List<PlanSetSheetDto>>();
        while (reader.Read())
        {
            var sheet = MapSheet(reader);
            if (!mutable.TryGetValue(sheet.PlanSetVersionId, out var items))
            {
                items = [];
                mutable[sheet.PlanSetVersionId] = items;
            }

            items.Add(new PlanSetSheetDto(
                sheet.Id,
                sheet.SheetType.ToString(),
                sheet.Name,
                sheet.ImportedDocumentId,
                null,
                IsCanonical: false,
                "Unregistered",
                "NotProjected"));
        }

        return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>>(
            mutable.ToDictionary(item => item.Key, item => (IReadOnlyList<PlanSetSheetDto>)item.Value));
    }

    private static PlanSheet MapSheet(SqliteDataReader reader)
    {
        return new PlanSheet(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            (PlanSheetType)reader.GetInt32(2),
            Guid.Parse(reader.GetString(3)),
            Guid.Parse(reader.GetString(4)),
            reader.GetString(5),
            Enum.Parse<PlanSheetStatus>(reader.GetString(6)),
            DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
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

- [ ] **Step 2: Add schema**

In `SqliteSchemaInitializer`, add:

```sql
CREATE TABLE IF NOT EXISTS plan_sheets (
    id TEXT PRIMARY KEY,
    plan_set_version_id TEXT NOT NULL,
    sheet_type INTEGER NOT NULL,
    imported_document_id TEXT NOT NULL,
    measurement_context_id TEXT NOT NULL,
    name TEXT NOT NULL,
    status TEXT NOT NULL,
    created_at_utc TEXT NOT NULL
);
```

Then call `EnsureColumnExists` for each non-id column so older local DBs can migrate idempotently.

- [ ] **Step 3: Wire DI**

In `DesktopServiceRegistration`, register:

```csharp
services.AddScoped<SqlitePlanSheetRepository>();
services.AddScoped<IPlanSheetRepository>(provider => provider.GetRequiredService<SqlitePlanSheetRepository>());
services.AddScoped<IPlanSheetReader>(provider => provider.GetRequiredService<SqlitePlanSheetRepository>());
services.AddScoped<ImportPlanSheetHandler>();
services.AddScoped<FloorplanFit.Application.PlanSets.Library.GetPlanSetLibraryHandler>();
```

- [ ] **Step 4: Verify diff**

```powershell
git diff --check -- src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSheetRepository.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs
```

- [ ] **Step 5: Commit**

```powershell
git add -- src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSheetRepository.cs src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs
git commit -m "feat: persist dependent plan sheets"
```

---

### Task 8: Document Phase 2 bridge and final checks

**Files:**
- Modify: `docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md`

- [ ] **Step 1: Add Phase 2 implementation bridge**

Under `## Phase 2 - Multiple Sheets per House`, add:

```markdown
### Phase 2 implementation bridge

The Phase 2 implementation stores dependent sheets in `plan_sheets` using the current `FloorPlanVersion.Id` as the temporary `PlanSetVersionId`. User-selected sheet type is the first classification source.

This enables a current house plan set to hold a canonical floor-plan sheet plus dependent electrical, roof, or facade/elevation sheet records without introducing registration/projection yet.
```

- [ ] **Step 2: Run scoped no-build checks**

```powershell
git diff --check -- src/FloorplanFit.Domain/PlanSets src/FloorplanFit.Contracts/PlanSets src/FloorplanFit.Application/PlanSets src/FloorplanFit.Application/Abstractions src/FloorplanFit.Infrastructure/Persistence src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs tests/FloorplanFit.Application.Tests/PlanSets docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md
```

Expected: no output.

Do not run `dotnet build`.

- [ ] **Step 3: Commit docs**

```powershell
git add -- docs/superpowers/specs/2026-06-30-house-plan-set-modularization-design.md
git commit -m "docs: record plan set phase two bridge"
```

---

## Completion Checklist

- [ ] Dependent sheets have a domain model.
- [ ] Dependent sheets have import contracts.
- [ ] Non-floor sheet import writes imported document, measurement context, and plan sheet.
- [ ] Floor-plan sheets are rejected by dependent-sheet import to preserve the canonical floor-plan flow.
- [ ] PlanSet library read model includes canonical floor-plan sheet plus dependent sheets.
- [ ] SQLite has a `plan_sheets` table.
- [ ] DI exposes PlanSheet repository/reader/import handler.
- [ ] No registration/projection/export engine is added.
- [ ] No Desktop UI behavior is changed.
- [ ] No `dotnet build` command is run.

## Self-Review Notes

Spec coverage:

- Implements Phase 2 only.
- Advances SheetImport and user-driven SheetClassification.
- Leaves SheetRegistration, SheetAdjustmentProjection, MultiSheetExportAudit, and DataCollection for later phases.

Red-flag scan:

- The plan contains no empty implementation markers.
- Created files have concrete code or exact implementation constraints.
- Commands use `--no-build` only where test execution is optional and safe.

Type consistency:

- `PlanSetVersionId` means current floor-plan version id until real `plan_set_versions` exists.
- `PlanSheetType` values match strings used by contracts and tests.
- `PlanSheetStatus.Imported` maps to response status `"Imported"`.
