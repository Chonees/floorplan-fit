using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Review;

public sealed class DimensionOverrideReviewSessionIntegrationTests
{
    [Fact]
    public async Task GetByTemplateAsync_overlays_manual_dimension_snapshot_and_export_state()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-dimension-override-review-{Guid.NewGuid():N}");
        var sourcePath = Path.Combine(tempRoot, "source.dxf");
        var now = new DateTime(2026, 5, 12, 1, 0, 0, DateTimeKind.Utc);

        try
        {
            Directory.CreateDirectory(tempRoot);
            await File.WriteAllTextAsync(sourcePath, "0\nSECTION\n2\nHEADER\n0\nENDSEC\n0\nEOF\n");

            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var import = await ExecuteImportAsync(workspace, sourcePath, now);
            await SeedExtractionDraftAndDimensionOverrideAsync(workspace, now.AddMinutes(5));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var reader = new SqliteFloorPlanReviewSessionReader(session);

            var reviewSession = await reader.GetByTemplateAsync(import.Item.TemplateId, CancellationToken.None);

            var dimension = Assert.Single(reviewSession!.Dimensions);
            Assert.True(dimension.IsEdited);
            Assert.False(dimension.IsDirty);
            Assert.Equal("11'-0\"", dimension.DisplayText);
            Assert.Equal(248m, dimension.DefPoint2X);
            Assert.Equal(248m, dimension.LinePrimitives[1].StartX);
            Assert.Equal("_Dot", dimension.InsertPrimitives[1].Name);
            Assert.NotNull(dimension.LastExportedAtUtc);
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

    [Fact]
    public async Task GetByTemplateAsync_overlays_manual_dimension_binding_override_into_bindings_and_associations()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-dimension-binding-review-{Guid.NewGuid():N}");
        var sourcePath = Path.Combine(tempRoot, "source.dxf");
        var now = new DateTime(2026, 5, 12, 3, 0, 0, DateTimeKind.Utc);

        try
        {
            Directory.CreateDirectory(tempRoot);
            await File.WriteAllTextAsync(sourcePath, "0\nSECTION\n2\nHEADER\n0\nENDSEC\n0\nEOF\n");

            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var import = await ExecuteImportAsync(workspace, sourcePath, now);
            await SeedExtractionDraftAndDimensionOverrideAsync(workspace, now.AddMinutes(5), includeBindingOverride: true);

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var reader = new SqliteFloorPlanReviewSessionReader(session);

            var reviewSession = await reader.GetByTemplateAsync(import.Item.TemplateId, CancellationToken.None);

            var binding = Assert.Single(reviewSession!.DimensionBindings);
            Assert.True(binding.HasManualBindingOverride);
            Assert.Equal("LinearSpan", binding.BindingKind);
            Assert.True(binding.IsResolved);
            Assert.Equal(2, binding.Anchors.Count);
            Assert.Equal("edge-a", binding.Anchors[0].EdgeKey);
            Assert.Equal("edge-b", binding.Anchors[1].EdgeKey);
            Assert.Equal("Projected", binding.Anchors[1].EdgeAnchorKind);
            Assert.Equal(0.5m, binding.Anchors[1].SegmentRatio);

            var association = Assert.Single(reviewSession.DimensionAssociations);
            Assert.Equal(binding.DimensionId, association.DimensionId);
            Assert.True(association.IsFullyResolved);
            Assert.Equal("edge-a", association.StartAnchor?.EdgeKey);
            Assert.Equal("edge-b", association.EndAnchor?.EdgeKey);
            Assert.Equal("Projected", association.EndAnchor?.EdgeAnchorKind);
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

    private static async Task<ImportFloorPlanResponse> ExecuteImportAsync(AppWorkspace workspace, string sourcePath, DateTime now)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);

        var handler = new ImportFloorPlanHandler(
            new IxMiliaDxfGateway(),
            new ManagedFileStorage(workspace),
            new SqliteFloorPlanTemplateRepository(session),
            new SqliteFloorPlanVersionRepository(session),
            new SqliteImportedDocumentRepository(session),
            new SqliteMeasurementContextRepository(session),
            new SqliteUnitOfWork(session),
            new Sha256FileHashService(),
            new FixedClock(now),
            new ImportFloorPlanResultFactory());

        return await handler.HandleAsync(new ImportFloorPlanRequest(sourcePath), CancellationToken.None);
    }

    private static async Task SeedExtractionDraftAndDimensionOverrideAsync(AppWorkspace workspace, DateTime now, bool includeBindingOverride = false)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var templateRepository = new SqliteFloorPlanTemplateRepository(session);
        var template = await templateRepository.GetByCodeAsync("source", CancellationToken.None)
            ?? throw new InvalidOperationException("Expected imported template.");
        var versionId = template.CurrentVersionId ?? throw new InvalidOperationException("Expected current version id.");

        var extractionRun = new WallExtractionRun(Guid.NewGuid(), versionId, "Completed", now, now, "ixmilia-wall-layer-v1", null);
        await new SqliteWallExtractionRunRepository(session).AddAsync(extractionRun, CancellationToken.None);

        await new SqliteExtractedDimensionRepository(session).AddRangeAsync(
            [
                new ExtractedDimension(
                    Guid.NewGuid(),
                    extractionRun.Id,
                    "DIMENSION:AB12",
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
                    "Detected native DIMENSION on layer DIMS.",
                    1,
                    renderTextX: 162m,
                    renderTextY: 148m,
                    renderTextHeight: 3.5m,
                    renderTextRotationDegrees: 0m,
                    renderTextStyleName: "ARCH",
                    renderTextAttachmentPoint: "MiddleCenter",
                    lineSegments:
                    [
                        new ExtractedDimensionLineSegment(100m, 140m, 100m, 100m),
                        new ExtractedDimensionLineSegment(224m, 140m, 224m, 100m),
                        new ExtractedDimensionLineSegment(100m, 140m, 224m, 140m)
                    ],
                    sourceHandle: "AB12",
                    linePrimitives:
                    [
                        new ExtractedDimensionLinePrimitive("LINE-1", 1, 100m, 140m, 100m, 100m),
                        new ExtractedDimensionLinePrimitive("LINE-2", 2, 224m, 140m, 224m, 100m),
                        new ExtractedDimensionLinePrimitive("LINE-3", 3, 100m, 140m, 224m, 140m)
                    ],
                    textPrimitives:
                    [
                        new ExtractedDimensionTextPrimitive("TEXT-1", 1, "10'-4\"", 162m, 148m, 3.5m, 0m)
                        {
                            StyleName = "ARCH",
                            AttachmentPoint = "MiddleCenter"
                        }
                    ],
                    insertPrimitives:
                    [
                        new ExtractedDimensionInsertPrimitive("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                        new ExtractedDimensionInsertPrimitive("INSERT-2", 2, "_Dot", 224m, 140m, 0m)
                    ])
            ],
            CancellationToken.None);

        var draft = new FloorPlanCuration(Guid.NewGuid(), versionId, 1, FloorPlanCurationStatus.Draft, null, "draft", now.AddMinutes(1), null);
        await new SqliteFloorPlanCurationRepository(session).AddAsync(draft, CancellationToken.None);

        await new SqliteFloorPlanDimensionOverrideRepository(session).UpsertAsync(
            FloorPlanDimensionOverride.CreateManualSnapshot(
                draft.Id,
                "AB12",
                "DIMENSION:AB12",
                "AB12",
                "11'-0\"",
                100m,
                100m,
                0m,
                248m,
                100m,
                0m,
                100m,
                140m,
                0m,
                180m,
                148m,
                3.5m,
                0m,
                "ARCH",
                null,
                null,
                "MiddleCenter",
                [
                    new ExtractedDimensionLinePrimitive("LINE-1", 1, 100m, 140m, 100m, 100m),
                    new ExtractedDimensionLinePrimitive("LINE-2", 2, 248m, 140m, 248m, 100m),
                    new ExtractedDimensionLinePrimitive("LINE-3", 3, 100m, 140m, 248m, 140m)
                ],
                [
                    new ExtractedDimensionTextPrimitive("TEXT-1", 1, "11'-0\"", 180m, 148m, 3.5m, 0m)
                    {
                        StyleName = "ARCH",
                        AttachmentPoint = "MiddleCenter"
                    }
                ],
                [
                    new ExtractedDimensionInsertPrimitive("INSERT-1", 1, "_Dot", 100m, 140m, 0m),
                    new ExtractedDimensionInsertPrimitive("INSERT-2", 2, "_Dot", 248m, 140m, 0m)
                ],
                [],
                [],
                [],
                now.AddMinutes(2),
                now.AddMinutes(3)),
            CancellationToken.None);

        if (includeBindingOverride)
        {
            await new SqliteFloorPlanDimensionBindingOverrideRepository(session).UpsertAsync(
                FloorPlanDimensionBindingOverride.CreateManualOverride(
                    draft.Id,
                    "AB12",
                    "LinearSpan",
                    true,
                    0.96m,
                    "Manual endpoint rebind.",
                    new FloorPlanDimensionMeasuredSpanOverride("Width", 100m, 248m, 0m),
                    [
                        new FloorPlanDimensionBindingAnchorOverride(1, "edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m, null),
                        new FloorPlanDimensionBindingAnchorOverride(2, "edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Projected", 248m, 120m, 0m, 0.5m)
                    ],
                    now.AddMinutes(4)),
                CancellationToken.None);
        }

        await session.CommitAsync(CancellationToken.None);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
