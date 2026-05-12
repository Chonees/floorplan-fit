using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Domain.Measurement;

namespace FloorplanFit.Application.Tests.FloorPlans.Import;

public sealed class ImportFloorPlanHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_imported_floor_plan_and_returns_library_item()
    {
        var now = new DateTime(2026, 4, 25, 22, 0, 0, DateTimeKind.Utc);
        var detectedDocument = new DetectedFloorPlanDocument(
            "SANTA-BARBARA.dxf",
            "SANTA-BARBARA",
            LengthUnit.Inch,
            25.4m,
            "AC1027",
            "bbox:0,0,1633.6555599104756,1079.9999999999998");

        var dxfGateway = new FakeDxfGateway(detectedDocument);
        var managedFileStorage = new FakeManagedFileStorage(@"C:\workspace\library\raw-dxf\SANTA-BARBARA.dxf");
        var templateRepository = new InMemoryFloorPlanTemplateRepository();
        var versionRepository = new InMemoryFloorPlanVersionRepository();
        var documentRepository = new InMemoryImportedDocumentRepository();
        var measurementRepository = new InMemoryMeasurementContextRepository();
        var unitOfWork = new FakeUnitOfWork();
        var fileHashService = new FakeFileHashService("abc123");
        var clock = new FakeClock(now);

        var handler = new ImportFloorPlanHandler(
            dxfGateway,
            managedFileStorage,
            templateRepository,
            versionRepository,
            documentRepository,
            measurementRepository,
            unitOfWork,
            fileHashService,
            clock,
            new ImportFloorPlanResultFactory());

        var response = await handler.HandleAsync(
            new ImportFloorPlanRequest(@"PLANS\originalFloorPlans\SANTA-BARBARA.dxf"),
            CancellationToken.None);

        Assert.Equal("santa-barbara", response.Item.Code);
        Assert.Equal("SANTA-BARBARA", response.Item.Name);
        Assert.Equal("Imported", response.Item.Status);
        Assert.Equal(1, response.Item.ActiveVersionNumber);
        Assert.Equal(now, response.Item.ImportedAtUtc);
        Assert.Equal("inch", response.Item.SourceUnit);

        var storedMeasurement = Assert.Single(measurementRepository.Items);
        Assert.Equal(LengthUnit.Inch, storedMeasurement.SourceUnit);
        Assert.Equal(25.4m, storedMeasurement.ToMillimetersFactor);

        var storedDocument = Assert.Single(documentRepository.Items);
        Assert.Equal(ImportedDocumentType.FloorPlanDxf, storedDocument.DocumentType);
        Assert.Equal("SANTA-BARBARA.dxf", storedDocument.OriginalFileName);
        Assert.Equal(@"C:\workspace\library\raw-dxf\SANTA-BARBARA.dxf", storedDocument.StoragePath);
        Assert.Equal("abc123", storedDocument.Sha256);

        var storedTemplate = Assert.Single(templateRepository.Items);
        Assert.Equal("santa-barbara", storedTemplate.Code);
        Assert.Equal("SANTA-BARBARA", storedTemplate.Name);

        var storedVersion = Assert.Single(versionRepository.Items);
        Assert.Equal(1, storedVersion.VersionNumber);
        Assert.Equal(storedDocument.Id, storedVersion.ImportedDocumentId);
        Assert.Equal(storedVersion.Id, storedTemplate.CurrentVersionId);

        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_reads_and_hashes_the_managed_copy_instead_of_the_external_source_path()
    {
        const string sourcePath = @"C:\imports\SANTA-BARBARA.dxf";
        const string managedPath = @"C:\workspace\library\raw-dxf\SANTA-BARBARA.dxf";
        var now = new DateTime(2026, 4, 29, 12, 0, 0, DateTimeKind.Utc);

        var dxfGateway = new FakeDxfGateway(
            new DetectedFloorPlanDocument(
                "SANTA-BARBARA.dxf",
                "SANTA-BARBARA",
                LengthUnit.Inch,
                25.4m,
                "AC1032",
                "bbox:0,0,1633.6555599104756,1079.9999999999998"));

        var managedFileStorage = new FakeManagedFileStorage(managedPath);
        var templateRepository = new InMemoryFloorPlanTemplateRepository();
        var versionRepository = new InMemoryFloorPlanVersionRepository();
        var documentRepository = new InMemoryImportedDocumentRepository();
        var measurementRepository = new InMemoryMeasurementContextRepository();
        var unitOfWork = new FakeUnitOfWork();
        var fileHashService = new FakeFileHashService("abc123");
        var clock = new FakeClock(now);

        var handler = new ImportFloorPlanHandler(
            dxfGateway,
            managedFileStorage,
            templateRepository,
            versionRepository,
            documentRepository,
            measurementRepository,
            unitOfWork,
            fileHashService,
            clock,
            new ImportFloorPlanResultFactory());

        await handler.HandleAsync(new ImportFloorPlanRequest(sourcePath), CancellationToken.None);

        Assert.Equal(sourcePath, managedFileStorage.SourcePathReceived);
        Assert.Equal(managedPath, dxfGateway.FilePathReceived);
        Assert.Equal(managedPath, fileHashService.FilePathReceived);
        Assert.Equal(managedPath, Assert.Single(documentRepository.Items).StoragePath);
    }

    [Fact]
    public async Task HandleAsync_reimporting_the_same_source_path_creates_version_2_of_the_same_template()
    {
        const string sourcePath = @"C:\imports\SANTA-BARBARA.dxf";
        var now = new DateTime(2026, 4, 29, 15, 0, 0, DateTimeKind.Utc);

        var dxfGateway = new FakeDxfGateway(filePath =>
        {
            var fileName = Path.GetFileName(filePath);
            var suggestedName = Path.GetFileNameWithoutExtension(filePath);

            return new DetectedFloorPlanDocument(
                fileName,
                suggestedName,
                LengthUnit.Inch,
                25.4m,
                "AC1032",
                "bbox:0,0,1633.6555599104756,1079.9999999999998");
        });

        var managedFileStorage = new SequenceManagedFileStorage(
            @"C:\workspace\library\raw-dxf\SANTA-BARBARA.dxf",
            @"C:\workspace\library\raw-dxf\SANTA-BARBARA-2.dxf");
        var templateRepository = new InMemoryFloorPlanTemplateRepository();
        var versionRepository = new InMemoryFloorPlanVersionRepository();
        var documentRepository = new InMemoryImportedDocumentRepository();
        var measurementRepository = new InMemoryMeasurementContextRepository();
        var unitOfWork = new FakeUnitOfWork();
        var fileHashService = new FakeFileHashService("abc123");
        var clock = new FakeClock(now);

        var handler = new ImportFloorPlanHandler(
            dxfGateway,
            managedFileStorage,
            templateRepository,
            versionRepository,
            documentRepository,
            measurementRepository,
            unitOfWork,
            fileHashService,
            clock,
            new ImportFloorPlanResultFactory());

        var firstResponse = await handler.HandleAsync(new ImportFloorPlanRequest(sourcePath), CancellationToken.None);
        var secondResponse = await handler.HandleAsync(new ImportFloorPlanRequest(sourcePath), CancellationToken.None);

        Assert.Equal("santa-barbara", firstResponse.Item.Code);
        Assert.Equal("santa-barbara", secondResponse.Item.Code);
        Assert.Equal("SANTA-BARBARA", secondResponse.Item.Name);
        Assert.Equal(2, secondResponse.Item.ActiveVersionNumber);

        var storedTemplate = Assert.Single(templateRepository.Items);
        Assert.Equal("santa-barbara", storedTemplate.Code);
        Assert.Equal("SANTA-BARBARA", storedTemplate.Name);

        Assert.Equal(2, versionRepository.Items.Count);
        Assert.Equal([1, 2], versionRepository.Items.Select(item => item.VersionNumber).OrderBy(number => number).ToArray());

        Assert.Equal(2, documentRepository.Items.Count);
        Assert.All(documentRepository.Items, item => Assert.Equal("SANTA-BARBARA.dxf", item.OriginalFileName));
        Assert.Contains(documentRepository.Items, item => item.StoragePath.EndsWith("SANTA-BARBARA.dxf", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(documentRepository.Items, item => item.StoragePath.EndsWith("SANTA-BARBARA-2.dxf", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeDxfGateway : IDxfGateway
    {
        private readonly Func<string, DetectedFloorPlanDocument> documentFactory;

        public FakeDxfGateway(DetectedFloorPlanDocument document)
        {
            documentFactory = _ => document;
        }

        public FakeDxfGateway(Func<string, DetectedFloorPlanDocument> documentFactory)
        {
            this.documentFactory = documentFactory;
        }

        public string? FilePathReceived { get; private set; }

        public Task<DetectedFloorPlanDocument> ReadFloorPlanAsync(string filePath, CancellationToken cancellationToken)
        {
            FilePathReceived = filePath;
            return Task.FromResult(documentFactory(filePath));
        }
    }

    private sealed class FakeManagedFileStorage : IManagedFileStorage
    {
        private readonly string managedPath;

        public FakeManagedFileStorage(string managedPath)
        {
            this.managedPath = managedPath;
        }

        public string? SourcePathReceived { get; private set; }

        public Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            SourcePathReceived = sourceFilePath;
            return Task.FromResult(managedPath);
        }

        public Task<string> ReserveAdjustedDxfPathAsync(string sourceFileName, CancellationToken cancellationToken)
            => Task.FromResult(managedPath);
    }

    private sealed class SequenceManagedFileStorage : IManagedFileStorage
    {
        private readonly Queue<string> managedPaths;

        public SequenceManagedFileStorage(params string[] managedPaths)
        {
            this.managedPaths = new Queue<string>(managedPaths);
        }

        public Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            if (managedPaths.Count == 0)
            {
                throw new InvalidOperationException("No managed path configured for this import.");
            }

            return Task.FromResult(managedPaths.Dequeue());
        }

        public Task<string> ReserveAdjustedDxfPathAsync(string sourceFileName, CancellationToken cancellationToken)
        {
            if (managedPaths.Count == 0)
            {
                throw new InvalidOperationException("No managed path configured for this adjusted DXF export.");
            }

            return Task.FromResult(managedPaths.Dequeue());
        }
    }

    private sealed class InMemoryFloorPlanTemplateRepository : IFloorPlanTemplateRepository
    {
        public List<FloorPlanTemplate> Items { get; } = [];

        public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.SingleOrDefault(item => item.Code == code));
        }

        public Task<FloorPlanTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == templateId));
        }

        public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            Items.Add(template);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            var index = Items.FindIndex(item => item.Id == template.Id);

            if (index >= 0)
            {
                Items[index] = template;
            }
            else
            {
                Items.Add(template);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFloorPlanVersionRepository : IFloorPlanVersionRepository
    {
        public List<FloorPlanVersion> Items { get; } = [];

        public Task<FloorPlanVersion?> GetByIdAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == floorPlanVersionId));
        }

        public Task<int> GetNextVersionNumberAsync(Guid floorPlanTemplateId, CancellationToken cancellationToken)
        {
            var nextVersion = Items
                .Where(item => item.FloorPlanTemplateId == floorPlanTemplateId)
                .Select(item => item.VersionNumber)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return Task.FromResult(nextVersion);
        }

        public Task AddAsync(FloorPlanVersion version, CancellationToken cancellationToken)
        {
            Items.Add(version);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == floorPlanVersionId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryImportedDocumentRepository : IImportedDocumentRepository
    {
        public List<ImportedDocument> Items { get; } = [];

        public Task AddAsync(ImportedDocument document, CancellationToken cancellationToken)
        {
            Items.Add(document);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMeasurementContextRepository : IMeasurementContextRepository
    {
        public List<MeasurementContext> Items { get; } = [];

        public Task AddAsync(MeasurementContext context, CancellationToken cancellationToken)
        {
            Items.Add(context);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool SaveChangesCalled { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileHashService : IFileHashService
    {
        private readonly string hash;

        public FakeFileHashService(string hash)
        {
            this.hash = hash;
        }

        public string? FilePathReceived { get; private set; }

        public Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
        {
            FilePathReceived = filePath;
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
}



