using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class ExportAdjustedDxfHandlerTests
{
    [Fact]
    public async Task HandleAsync_exports_edited_dimensions_persists_imported_document_and_marks_overrides_clean()
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
        Assert.Equal(["AB12", "ZZ99"], exportCall.Dimensions.Select(item => item.SourceHandle!).ToArray());

        var importedDocument = Assert.Single(importedDocumentRepository.Items);
        Assert.Equal(ImportedDocumentType.ExportedAdjustedDxf, importedDocument.DocumentType);
        Assert.Equal(measurementContextId, importedDocument.MeasurementContextId);
        Assert.Equal("abc123", importedDocument.Sha256);

        Assert.Single(repository.MarkExportedCalls);
        var markExport = repository.MarkExportedCalls.Single();
        Assert.Equal(curationId, markExport.CurationId);
        Assert.Equal(["AB12", "ZZ99"], markExport.SourceDimensionKeys);
        Assert.Equal(clock.UtcNow, markExport.ExportedAtUtc);
        Assert.True(unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task HandleAsync_exports_edited_dimensions_even_when_overrides_are_not_dirty()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var curationId = Guid.NewGuid();
        var measurementContextId = Guid.NewGuid();
        var source = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SEMINOLE2000.dxf")
        {
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
                CreateDimension("AB12", isEdited: true, isDirty: false),
                CreateDimension("ZZ99", isEdited: false, isDirty: false)
            ]
        };
        var exporter = new FakeAdjustedDxfExporter();
        var repository = new InMemoryFloorPlanDimensionOverrideRepository();
        var handler = new ExportAdjustedDxfHandler(
            new FakeFloorPlanExtractionSourceReader(source),
            new FakeFloorPlanReviewSessionReader(reviewSession),
            repository,
            new InMemoryImportedDocumentRepository(),
            new FakeManagedFileStorage(@"C:\workspace\library\adjusted-dxf\SEMINOLE2000-adjusted.dxf"),
            exporter,
            new FakeFileHashService("abc123"),
            new FakeUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 5, 21, 0, 0, DateTimeKind.Utc)));

        await handler.HandleAsync(templateId, versionId, curationId, CancellationToken.None);

        var exportedDimension = Assert.Single(exporter.ExportCalls.Single().Dimensions);
        Assert.Equal("AB12", exportedDimension.SourceHandle);
        var markExport = Assert.Single(repository.MarkExportedCalls);
        Assert.Equal(["AB12"], markExport.SourceDimensionKeys);
    }

    [Fact]
    public async Task HandleAsync_exports_dirty_dimensions_without_reprojecting_inferred_associations()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var curationId = Guid.NewGuid();
        var measurementContextId = Guid.NewGuid();
        var source = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SEMINOLE2000.dxf")
        {
            MeasurementContextId = measurementContextId,
            OriginalFileName = "SEMINOLE2000.dxf",
            DxfVersion = "AC1027"
        };
        var dimension = CreateDimension("AB12", isEdited: true, isDirty: true);
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
            Dimensions = [dimension],
            MeasurableEdges =
            [
                new MeasurableEdgeDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 100m, 100m, 100m, 140m),
                new MeasurableEdgeDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 260m, 100m, 260m, 140m)
            ],
            DimensionAssociations =
            [
                new DimensionAssociationDto(dimension.DimensionId, "MeasuredEndpointAnchors", true, 0.95m, "Resolved both endpoints.")
                {
                    StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m),
                    EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 260m, 100m, 0m)
                }
            ]
        };
        var sourceReader = new FakeFloorPlanExtractionSourceReader(source);
        var reviewReader = new FakeFloorPlanReviewSessionReader(reviewSession);
        var repository = new InMemoryFloorPlanDimensionOverrideRepository();
        var importedDocumentRepository = new InMemoryImportedDocumentRepository();
        var managedFileStorage = new FakeManagedFileStorage(@"C:\workspace\library\adjusted-dxf\SEMINOLE2000-adjusted.dxf");
        var exporter = new FakeAdjustedDxfExporter();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 5, 15, 18, 0, 0, DateTimeKind.Utc));
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

        await handler.HandleAsync(templateId, versionId, curationId, CancellationToken.None);

        var exportedDimension = Assert.Single(exporter.ExportCalls.Single().Dimensions);
        Assert.Equal(224m, exportedDimension.DefPoint2X);
        Assert.Equal(224m, exportedDimension.LinePrimitives[1].EndX);
        Assert.Equal(224m, exportedDimension.LinePrimitives[2].EndX);
    }

    [Fact]
    public async Task HandleAsync_exports_dirty_ordinate_x_dimensions_without_reprojecting_typed_inferred_bindings()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var curationId = Guid.NewGuid();
        var measurementContextId = Guid.NewGuid();
        var source = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SEMINOLE2000.dxf")
        {
            MeasurementContextId = measurementContextId,
            OriginalFileName = "SEMINOLE2000.dxf",
            DxfVersion = "AC1027"
        };
        var dimension = CreateOrdinateXDimension("AB12", isEdited: true, isDirty: true);
        var startAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m);
        var endAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 320m, 140m, 0m);
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
            Dimensions = [dimension],
            MeasurableEdges =
            [
                new MeasurableEdgeDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 100m, 100m, 100m, 140m),
                new MeasurableEdgeDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 40m, 1016m, 90m, 320m, 140m, 320m, 180m)
            ],
            DimensionAssociations =
            [
                new DimensionAssociationDto(dimension.DimensionId, "MeasuredEndpointAnchors", true, 0.95m, "Resolved ordinate anchors.")
                {
                    StartAnchor = startAnchor,
                    EndAnchor = endAnchor
                }
            ],
            DimensionBindings =
            [
                new DimensionBindingDto(dimension.DimensionId, "OrdinateX", true, 0.95m, "Resolved ordinate X binding.", false)
                {
                    Anchors = [startAnchor, endAnchor],
                    MeasuredSpan = new DimensionMeasuredSpanDto("OrdinateX", 100m, 320m, 0m)
                }
            ]
        };
        var exporter = new FakeAdjustedDxfExporter();
        var handler = new ExportAdjustedDxfHandler(
            new FakeFloorPlanExtractionSourceReader(source),
            new FakeFloorPlanReviewSessionReader(reviewSession),
            new InMemoryFloorPlanDimensionOverrideRepository(),
            new InMemoryImportedDocumentRepository(),
            new FakeManagedFileStorage(@"C:\workspace\library\adjusted-dxf\SEMINOLE2000-adjusted.dxf"),
            exporter,
            new FakeFileHashService("abc123"),
            new FakeUnitOfWork(),
            new FakeClock(new DateTime(2026, 5, 15, 19, 0, 0, DateTimeKind.Utc)));

        await handler.HandleAsync(templateId, versionId, curationId, CancellationToken.None);

        var exportedDimension = Assert.Single(exporter.ExportCalls.Single().Dimensions);
        Assert.Equal(260m, exportedDimension.DefPoint2X);
        Assert.Equal(140m, exportedDimension.DefPoint2Y);
        Assert.Equal(290m, exportedDimension.DefPoint3X);
        Assert.Equal(170m, exportedDimension.DefPoint3Y);
        Assert.Equal(160m, exportedDimension.MeasurementSourceUnits);
        Assert.Equal("13'-4\"", exportedDimension.DisplayText);
        Assert.Equal(260m, exportedDimension.LinePrimitives[0].StartX);
        Assert.Equal(290m, exportedDimension.LinePrimitives[0].EndX);
        Assert.Equal(304m, exportedDimension.TextPrimitives[0].X);
    }

    [Fact]
    public async Task HandleAsync_exports_dirty_radius_dimensions_without_reprojecting_typed_inferred_bindings()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var curationId = Guid.NewGuid();
        var measurementContextId = Guid.NewGuid();
        var source = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SEMINOLE2000.dxf")
        {
            MeasurementContextId = measurementContextId,
            OriginalFileName = "SEMINOLE2000.dxf",
            DxfVersion = "AC1027"
        };
        var dimension = CreateRadiusDimension("AB12", isEdited: true, isDirty: true);
        var startAnchor = new DimensionAnchorReferenceDto("edge-center", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m);
        var endAnchor = new DimensionAnchorReferenceDto("edge-feature", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 200m, 0m);
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
            Dimensions = [dimension],
            MeasurableEdges =
            [
                new MeasurableEdgeDto("edge-center", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), 0m, 0m, 0m, 100m, 100m, 100m, 100m),
                new MeasurableEdgeDto("edge-feature", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), 0m, 0m, 0m, 100m, 200m, 100m, 200m)
            ],
            DimensionAssociations =
            [
                new DimensionAssociationDto(dimension.DimensionId, "MeasuredEndpointAnchors", true, 0.95m, "Resolved radius anchors.")
                {
                    StartAnchor = startAnchor,
                    EndAnchor = endAnchor
                }
            ],
            DimensionBindings =
            [
                new DimensionBindingDto(dimension.DimensionId, "Radius", true, 0.95m, "Resolved radius binding.", false)
                {
                    Anchors = [startAnchor, endAnchor],
                    MeasuredSpan = new DimensionMeasuredSpanDto("Radial", 0m, 100m, 90m)
                }
            ]
        };
        var exporter = new FakeAdjustedDxfExporter();
        var handler = new ExportAdjustedDxfHandler(
            new FakeFloorPlanExtractionSourceReader(source),
            new FakeFloorPlanReviewSessionReader(reviewSession),
            new InMemoryFloorPlanDimensionOverrideRepository(),
            new InMemoryImportedDocumentRepository(),
            new FakeManagedFileStorage(@"C:\workspace\library\adjusted-dxf\SEMINOLE2000-adjusted.dxf"),
            exporter,
            new FakeFileHashService("abc123"),
            new FakeUnitOfWork(),
            new FakeClock(new DateTime(2026, 5, 15, 20, 0, 0, DateTimeKind.Utc)));

        await handler.HandleAsync(templateId, versionId, curationId, CancellationToken.None);

        var exportedDimension = Assert.Single(exporter.ExportCalls.Single().Dimensions);
        Assert.Equal(100m, exportedDimension.DefPointX);
        Assert.Equal(100m, exportedDimension.DefPointY);
        Assert.Equal(150m, exportedDimension.DefPoint2X);
        Assert.Equal(100m, exportedDimension.DefPoint2Y);
        Assert.Equal(170m, exportedDimension.DefPoint3X);
        Assert.Equal(120m, exportedDimension.DefPoint3Y);
        Assert.Equal(50m, exportedDimension.MeasurementSourceUnits);
        Assert.Equal("4'-2\"", exportedDimension.DisplayText);
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

    private static DimensionDto CreateOrdinateXDimension(string sourceHandle, bool isEdited, bool isDirty)
    {
        return new DimensionDto(
            Guid.NewGuid(),
            $"DIMENSION:{sourceHandle}",
            "DIMS",
            "DIMENSION",
            "*D250",
            "13'-4\"",
            "GeometryBlock",
            string.Empty,
            160m,
            4064m,
            "Inch",
            6,
            0m,
            0m,
            100m,
            100m,
            0m,
            260m,
            140m,
            0m,
            290m,
            170m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = sourceHandle,
            IsEdited = isEdited,
            IsDirty = isDirty,
            RenderTextX = 304m,
            RenderTextY = 174m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleLeft",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto($"{sourceHandle}-LINE-1", 1, 260m, 140m, 290m, 170m),
                new DimensionLinePrimitiveDto($"{sourceHandle}-LINE-2", 2, 290m, 170m, 320m, 170m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto($"{sourceHandle}-TEXT-1", 1, "13'-4\"", 304m, 174m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleLeft"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto($"{sourceHandle}-INSERT-1", 1, "_Dot", 260m, 140m, 0m),
                new DimensionInsertPrimitiveDto($"{sourceHandle}-INSERT-2", 2, "_Dot", 290m, 170m, 0m)
            ]
        };
    }

    private static DimensionDto CreateRadiusDimension(string sourceHandle, bool isEdited, bool isDirty)
    {
        return new DimensionDto(
            Guid.NewGuid(),
            $"DIMENSION:{sourceHandle}",
            "DIMS",
            "DIMENSION",
            "*D260",
            "4'-2\"",
            "GeometryBlock",
            string.Empty,
            50m,
            1270m,
            "Inch",
            4,
            0m,
            0m,
            100m,
            100m,
            0m,
            150m,
            100m,
            0m,
            170m,
            120m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = sourceHandle,
            IsEdited = isEdited,
            IsDirty = isDirty,
            RenderTextX = 180m,
            RenderTextY = 125m,
            RenderTextHeight = 3.5m,
            RenderTextStyleName = "ARCH",
            RenderTextAttachmentPoint = "MiddleLeft",
            LinePrimitives =
            [
                new DimensionLinePrimitiveDto($"{sourceHandle}-LINE-1", 1, 150m, 100m, 170m, 120m),
                new DimensionLinePrimitiveDto($"{sourceHandle}-LINE-2", 2, 170m, 120m, 200m, 120m)
            ],
            TextPrimitives =
            [
                new DimensionTextPrimitiveDto($"{sourceHandle}-TEXT-1", 1, "4'-2\"", 180m, 125m, 3.5m, 0m)
                {
                    StyleName = "ARCH",
                    AttachmentPoint = "MiddleLeft"
                }
            ],
            InsertPrimitives =
            [
                new DimensionInsertPrimitiveDto($"{sourceHandle}-INSERT-1", 1, "_Dot", 150m, 100m, 0m),
                new DimensionInsertPrimitiveDto($"{sourceHandle}-INSERT-2", 2, "_Dot", 170m, 120m, 0m)
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
