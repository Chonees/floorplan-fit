using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class ExportAdjustedDxfHandlerTests
{
    [Fact]
    public async Task HandleAsync_exports_dirty_dimensions_persists_imported_document_and_marks_overrides_clean()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var curationId = Guid.NewGuid();
        var sourceDocumentId = Guid.NewGuid();
        var measurementContextId = Guid.NewGuid();
        var source = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SEMINOLE2000.dxf")
        {
            ImportedDocumentId = sourceDocumentId,
            MeasurementContextId = measurementContextId,
            OriginalFileName = "SEMINOLE2000.dxf",
            DxfVersion = "AC1027"
        };
        var reviewSession = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Curated Draft",
            1,
            null,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [])
        {
            Dimensions =
            [
                CreateDimension("AB12", isEdited: true, isDirty: true),
                CreateDimension("ZZ99", isEdited: true, isDirty: false)
            ]
        };
        var sourceReader = new FakeFloorPlanExtractionSourceReader(source);
        var reviewReader = new FakeFloorPlanReviewSessionReader(reviewSession);
        var repository = new InMemoryFloorPlanDimensionOverrideRepository();
        var importedDocumentRepository = new InMemoryImportedDocumentRepository();
        var managedFileStorage = new FakeManagedFileStorage(@"C:\workspace\library\adjusted-dxf\SEMINOLE2000-adjusted.dxf");
        var exporter = new FakeAdjustedDxfExporter();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 12, 1, 5, 0, DateTimeKind.Utc));
        var fileHashService = new FakeFileHashService("abc123");
        var handler = new ExportAdjustedDxfHandler(
            sourceReader,
            reviewReader,
            repository,
            importedDocumentRepository,
            managedFileStorage,
            exporter,
            fileHashService,
            unitOfWork,
            clock);

        var response = await handler.HandleAsync(templateId, versionId, curationId, CancellationToken.None);

        Assert.Equal(@"C:\workspace\library\adjusted-dxf\SEMINOLE2000-adjusted.dxf", response.ManagedFilePath);
        Assert.Single(exporter.ExportCalls);
        var exportCall = exporter.ExportCalls.Single();
        Assert.Equal(source.ManagedFilePath, exportCall.SourceFilePath);
        Assert.Equal(response.ManagedFilePath, exportCall.OutputFilePath);
        var exportedDimension = Assert.Single(exportCall.Dimensions);
        Assert.Equal("AB12", exportedDimension.SourceHandle);

        var importedDocument = Assert.Single(importedDocumentRepository.Items);
        Assert.Equal(ImportedDocumentType.ExportedAdjustedDxf, importedDocument.DocumentType);
        Assert.Equal(measurementContextId, importedDocument.MeasurementContextId);
        Assert.Equal("abc123", importedDocument.Sha256);

        Assert.Single(repository.MarkExportedCalls);
        var markExport = repository.MarkExportedCalls.Single();
        Assert.Equal(curationId, markExport.CurationId);
        Assert.Equal(["AB12"], markExport.SourceDimensionKeys);
        Assert.Equal(clock.UtcNow, markExport.ExportedAtUtc);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    private static DimensionDto CreateDimension(string sourceHandle, bool isEdited, bool isDirty)
    {
        return new DimensionDto(
            Guid.NewGuid(),
            $"DIMENSION:{sourceHandle}",
            "DIMS",
            "DIMENSION",
            "*D169",
            "10'-4\"",
            "GeometryBlock",
            string.Empty,
            124m,
            3149.6m,
            "Inch",
            0,
            0m,
            0m,
            100m,
            100m,
            0m,
            224m,
            100m,
            0m,
            100m,
            140m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = sourceHandle,
            IsEdited = isEdited,
            IsDirty = isDirty,
            RenderTextX = 162m,
            RenderTextY = 148m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleCenter",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto($"{sourceHandle}-LINE-1", 1, 100m, 140m, 100m, 100m),
                new DimensionLinePrimitiveDto($"{sourceHandle}-LINE-2", 2, 224m, 140m, 224m, 100m),
                new DimensionLinePrimitiveDto($"{sourceHandle}-LINE-3", 3, 100m, 140m, 224m, 140m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto($"{sourceHandle}-TEXT-1", 1, "10'-4\"", 162m, 148m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleCenter"
                }
            ]
        };
    }

    private sealed class FakeFloorPlanExtractionSourceReader : IFloorPlanExtractionSourceReader
    {
        private readonly FloorPlanExtractionSource source;

        public FakeFloorPlanExtractionSourceReader(FloorPlanExtractionSource source)
        {
            this.source = source;
        }

        public Task<FloorPlanExtractionSource?> GetCurrentSourceAsync(Guid templateId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanExtractionSource?>(source);

        public Task<FloorPlanExtractionSource?> GetByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanExtractionSource?>(source);
    }

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly FloorPlanReviewSessionDto session;

        public FakeFloorPlanReviewSessionReader(FloorPlanReviewSessionDto session)
        {
            this.session = session;
        }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(session);

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(Guid templateId, Guid floorPlanVersionId, CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanReviewSessionDto?>(session);
    }

    private sealed class InMemoryFloorPlanDimensionOverrideRepository : IFloorPlanDimensionOverrideRepository
    {
        public List<(Guid CurationId, IReadOnlyList<string> SourceDimensionKeys, DateTime ExportedAtUtc)> MarkExportedCalls { get; } = [];

        public Task<IReadOnlyList<FloorPlanDimensionOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FloorPlanDimensionOverride>>([]);

        public Task UpsertAsync(FloorPlanDimensionOverride dimensionOverride, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task MarkExportedAsync(Guid floorPlanCurationId, IReadOnlyList<string> sourceDimensionKeys, DateTime exportedAtUtc, CancellationToken cancellationToken)
        {
            MarkExportedCalls.Add((floorPlanCurationId, sourceDimensionKeys, exportedAtUtc));
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

    private sealed class FakeManagedFileStorage : IManagedFileStorage
    {
        private readonly string managedPath;

        public FakeManagedFileStorage(string managedPath)
        {
            this.managedPath = managedPath;
        }

        public Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken)
            => Task.FromResult(managedPath);

        public Task<string> ReserveAdjustedDxfPathAsync(string sourceFileName, CancellationToken cancellationToken)
            => Task.FromResult(managedPath);
    }

    private sealed class FakeAdjustedDxfExporter : IAdjustedDxfExporter
    {
        public List<(string SourceFilePath, string OutputFilePath, IReadOnlyList<DimensionDto> Dimensions)> ExportCalls { get; } = [];

        public Task ExportAsync(string sourceFilePath, string outputFilePath, IReadOnlyList<DimensionDto> dimensions, CancellationToken cancellationToken)
        {
            ExportCalls.Add((sourceFilePath, outputFilePath, dimensions));
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

        public Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
            => Task.FromResult(hash);
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

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
