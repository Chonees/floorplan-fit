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
