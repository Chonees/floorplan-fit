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

public sealed class FloorPlanReviewSessionReaderIntegrationTests
{
    [Fact]
    public async Task GetByTemplateAsync_returns_candidates_pinch_markers_and_geometry_for_the_current_floor_plan()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-review-reader-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 4, 30, 16, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var import = await ExecuteImportAsync(workspace, sourcePath, now);
            var rejectedGeometryPathId = await SeedExtractionAndDraftAsync(workspace, now.AddMinutes(5));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var reader = new SqliteFloorPlanReviewSessionReader(session);

            var reviewSession = await reader.GetByTemplateAsync(import.Item.TemplateId, CancellationToken.None);

            Assert.NotNull(reviewSession);
            Assert.Equal(import.Item.TemplateId, reviewSession.TemplateId);
            Assert.Equal("Curated Draft", reviewSession.Status);
            Assert.NotEmpty(reviewSession.WallCandidates);
            Assert.NotEmpty(reviewSession.RoomLabels);
            Assert.NotEmpty(reviewSession.OpeningCandidates);
            Assert.NotEmpty(reviewSession.OpeningLabels);
            Assert.NotEmpty(reviewSession.FixedPlanComponents);
            Assert.NotEmpty(reviewSession.ProtectedDetailAssemblies);
            Assert.NotEmpty(reviewSession.Dimensions);
            Assert.NotNull(reviewSession.MeasurementContext);
            Assert.Single(reviewSession.PinchGroups);
            Assert.Single(reviewSession.PinchMarkers);
            Assert.NotEmpty(reviewSession.GeometryPaths);

            var candidate = reviewSession.WallCandidates.Single();
            Assert.Equal("WALLS", candidate.SourceLayer);
            Assert.NotNull(candidate.GeometryPathId);
            Assert.Equal("Accepted", candidate.Status);
            Assert.DoesNotContain(reviewSession.WallCandidates, item => item.SourceEntityRef == "LINE:REJECTED");
            Assert.DoesNotContain(reviewSession.GeometryPaths, item => item.Id == rejectedGeometryPathId);

            var pinchMarker = reviewSession.PinchMarkers.Single();
            Assert.Equal("Patio", pinchMarker.PinchGroupName);
            Assert.Equal(reviewSession.PinchGroups.Single().PinchGroupId, pinchMarker.PinchGroupId);
            Assert.Equal(candidate.CandidateId, pinchMarker.SourceCandidateId);
            Assert.Equal(nameof(PinchAxisTag.Width), pinchMarker.AxisTag);
            Assert.Equal(candidate.GeometryPathId, pinchMarker.GeometryPathId);

            var path = Assert.Single(reviewSession.GeometryPaths, item => item.Id == candidate.GeometryPathId);
            Assert.Single(path.Segments);
            Assert.Equal("KITCHEN", reviewSession.RoomLabels.Single().Text);
            Assert.Equal("Door", reviewSession.OpeningCandidates.Single().Kind);
            Assert.Equal("2668", reviewSession.OpeningLabels.Single().Text);
            Assert.Equal("Toilet", reviewSession.FixedPlanComponents.Single().Kind);
            Assert.Equal("#FF7F7F7F", reviewSession.FixedPlanComponents.Single().ColorArgb);
            Assert.Contains(reviewSession.FixedPlanComponents.Single().GeometryPathIds, pathId =>
                reviewSession.GeometryPaths.Any(path => path.Id == pathId));
            Assert.Equal("WetAreaDetail", reviewSession.ProtectedDetailAssemblies.Single().Kind);
            Assert.Equal("MISC", reviewSession.ProtectedDetailAssemblies.Single().SourceLayer);
            Assert.Contains(reviewSession.ProtectedDetailAssemblies.Single().GeometryPathIds, pathId =>
                reviewSession.GeometryPaths.Any(path => path.Id == pathId));
            Assert.Contains(reviewSession.GeometryPaths, item => item.Id == reviewSession.OpeningCandidates.Single().GeometryPathId);
            var dimension = reviewSession.Dimensions.Single();
            Assert.Equal("Inch", reviewSession.MeasurementContext!.SourceUnit);
            Assert.Equal(25.4m, reviewSession.MeasurementContext.ToMillimetersFactor);
            Assert.Equal(1m, reviewSession.MeasurementContext.LinearToleranceMm);
            Assert.Equal(0.5m, reviewSession.MeasurementContext.AngularToleranceDeg);
            Assert.Equal("10'-4\"", dimension.DisplayText);
            Assert.Equal("GeometryBlock", dimension.DisplayTextSource);
            Assert.Equal("DIMS", dimension.SourceLayer);
            Assert.Equal(123.810387305188m, dimension.MeasurementSourceUnits);
            Assert.Equal("AB12", dimension.SourceHandle);
            Assert.Single(reviewSession.DimensionBindings);
            Assert.Equal(dimension.DimensionId, reviewSession.DimensionBindings[0].DimensionId);
            Assert.Equal("LinearSpan", reviewSession.DimensionBindings[0].BindingKind);
            Assert.Equal(408.8391899621098m, dimension.RenderTextX);
            Assert.Equal(518.9677806582538m, dimension.RenderTextY);
            Assert.Equal(3.5m, dimension.RenderTextHeight);
            Assert.Equal("MiddleCenter", dimension.RenderTextAttachmentPoint);
            Assert.Equal(3, dimension.LineSegments.Count);
            Assert.Equal(3, dimension.LinePrimitives.Count);
            Assert.Single(dimension.TextPrimitives);
            Assert.Equal(2, dimension.InsertPrimitives.Count);
            Assert.Equal("_Dot", dimension.InsertPrimitives[0].Name);
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

    private static async Task<ImportFloorPlanResponse> ExecuteImportAsync(
        AppWorkspace workspace,
        string sourcePath,
        DateTime now)
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

    private static async Task<Guid> SeedExtractionAndDraftAsync(AppWorkspace workspace, DateTime now)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var templateRepository = new SqliteFloorPlanTemplateRepository(session);
        var template = await templateRepository.GetByCodeAsync("santa-barbara", CancellationToken.None)
            ?? throw new InvalidOperationException("Expected imported template.");
        var versionId = template.CurrentVersionId ?? throw new InvalidOperationException("Expected current version id.");

        var extractionRun = new WallExtractionRun(
            Guid.NewGuid(),
            versionId,
            "Completed",
            now,
            now,
            "ixmilia-wall-layer-v1",
            null);

        await new SqliteWallExtractionRunRepository(session).AddAsync(extractionRun, CancellationToken.None);

        var candidate = new ExtractedWallCandidate(
            Guid.NewGuid(),
            extractionRun.Id,
            "LINE:1",
            "WALLS",
            Guid.Empty,
            null,
            0.95m,
            null,
            ExtractedWallCandidateStatus.Accepted,
            1);
        var rejectedCandidate = new ExtractedWallCandidate(
            Guid.NewGuid(),
            extractionRun.Id,
            "LINE:REJECTED",
            "WALLS",
            Guid.Empty,
            null,
            0.70m,
            "False positive rejected by curation.",
            ExtractedWallCandidateStatus.Rejected,
            2);

        await new SqliteExtractedWallCandidateRepository(session).AddRangeAsync(
            [candidate, rejectedCandidate],
            [
                new DetectedWallCandidate(
                    "LINE:1",
                    "WALLS",
                    [new GeometryPoint(0m, 0m), new GeometryPoint(120m, 0m)],
                    null,
                    0.95m,
                    null),
                new DetectedWallCandidate(
                    "LINE:REJECTED",
                    "WALLS",
                    [new GeometryPoint(0m, 20m), new GeometryPoint(120m, 20m)],
                    null,
                    0.70m,
                    null)
            ],
            CancellationToken.None);

        var persistedCandidate = await new SqliteExtractedWallCandidateRepository(session).GetByIdAsync(candidate.Id, CancellationToken.None)
            ?? throw new InvalidOperationException("Expected persisted candidate.");
        var persistedRejectedCandidate = await new SqliteExtractedWallCandidateRepository(session).GetByIdAsync(rejectedCandidate.Id, CancellationToken.None)
            ?? throw new InvalidOperationException("Expected persisted rejected candidate.");
        var rejectedGeometryPathId = persistedRejectedCandidate.GeometryPathId
            ?? throw new InvalidOperationException("Expected rejected candidate geometry path.");

        await new SqliteExtractedRoomLabelRepository(session).AddRangeAsync(
            [
                new ExtractedRoomLabel(
                    Guid.NewGuid(),
                    extractionRun.Id,
                    "TEXT:1",
                    "ROOM LBLS",
                    "KITCHEN",
                    125m,
                    784m,
                    0.95m,
                    "Detected from ROOM LBLS text entity.",
                    1)
            ],
            CancellationToken.None);

        await new SqliteExtractedOpeningCandidateRepository(session).AddRangeAsync(
            [
                new ExtractedOpeningCandidate(
                    Guid.NewGuid(),
                    extractionRun.Id,
                    "LINE:1",
                    "DOORS",
                    "Door",
                    "LINE",
                    Guid.Empty,
                    0.95m,
                    "Detected from DOORS line entity.",
                    1)
            ],
            [
                new DetectedOpeningCandidate(
                    "LINE:1",
                    "DOORS",
                    "Door",
                    "LINE",
                    [new GeometryPoint(40m, 0m), new GeometryPoint(76m, 0m)],
                    0.95m,
                    "Detected from DOORS line entity.")
            ],
            CancellationToken.None);

        await new SqliteExtractedOpeningLabelRepository(session).AddRangeAsync(
            [
                new ExtractedOpeningLabel(
                    Guid.NewGuid(),
                    extractionRun.Id,
                    "TEXT:1",
                    "DOORTEXT",
                    "Door",
                    "2668",
                    50m,
                    20m,
                    0.95m,
                    "Detected from DOORTEXT text entity.",
                    1)
            ],
            CancellationToken.None);

        await new SqliteExtractedFixedPlanComponentRepository(session).AddRangeAsync(
            [
                new ExtractedFixedPlanComponent(
                    Guid.NewGuid(),
                    extractionRun.Id,
                    "INSERT:1",
                    "FIXTURES",
                    "Toilet",
                    "INSERT",
                    "TOILET1",
                    0.95m,
                    "Detected from TOILET1 block insert.",
                    1,
                    "#FF7F7F7F")
            ],
            [
                new DetectedFixedPlanComponent(
                    "INSERT:1",
                    "FIXTURES",
                    "Toilet",
                    "INSERT",
                    "TOILET1",
                    [
                        [new GeometryPoint(1400m, 668m), new GeometryPoint(1412m, 668m)],
                        [new GeometryPoint(1400m, 672m), new GeometryPoint(1412m, 672m)]
                    ],
                    0.95m,
                    "Detected from TOILET1 block insert.",
                    "#FF7F7F7F")
            ],
            CancellationToken.None);

        await new SqliteExtractedProtectedDetailAssemblyRepository(session).AddRangeAsync(
            [
                new ExtractedProtectedDetailAssembly(
                    Guid.NewGuid(),
                    extractionRun.Id,
                    "DETAIL:MISC:1",
                    "MISC",
                    "WetAreaDetail",
                    "DETAIL-GROUP",
                    0.90m,
                    "Detected from MISC protected detail geometry.",
                    1,
                    "#FF00FF00")
            ],
            [
                new DetectedProtectedDetailAssembly(
                    "DETAIL:MISC:1",
                    "MISC",
                    "WetAreaDetail",
                    "DETAIL-GROUP",
                    [
                        [new GeometryPoint(416m, 585m), new GeometryPoint(468m, 585m)],
                        [new GeometryPoint(468m, 606m), new GeometryPoint(472m, 606m)]
                    ],
                    0.90m,
                    "Detected from MISC protected detail geometry.",
                    "#FF00FF00")
            ],
            CancellationToken.None);

        await new SqliteExtractedDimensionRepository(session).AddRangeAsync(
            [
                new ExtractedDimension(
                    Guid.NewGuid(),
                    extractionRun.Id,
                    "DIMENSION:1",
                    "DIMS",
                    "DIMENSION",
                    "*D169",
                    "10'-4\"",
                    "GeometryBlock",
                    string.Empty,
                    123.810387305188m,
                    3144.7838375517752m,
                    "Inch",
                    0,
                    0m,
                    0m,
                    94.5741888255622m,
                    516.95664946623m,
                    0m,
                    218.38457613075m,
                    524.795084103958m,
                    0m,
                    94.5741888255622m,
                    537.195356591169m,
                    0.0000000000000074m,
                    0.99m,
                    "Detected native DIMENSION on layer DIMS.",
                    1,
                    renderTextX: 408.8391899621098m,
                    renderTextY: 518.9677806582538m,
                    renderTextHeight: 3.5m,
                    renderTextRotationDegrees: 0m,
                    renderTextStyleName: "ARCH",
                    renderTextAttachmentPoint: "MiddleCenter",
                    lineSegments:
                    [
                        new ExtractedDimensionLineSegment(440.5741888255912m, 519.7784891962231m, 440.5741888255912m, 512.9566494662152m),
                        new ExtractedDimensionLineSegment(372.5741888256203m, 524.9408070879156m, 372.5741888256203m, 512.9566494662152m),
                        new ExtractedDimensionLineSegment(437.0741888255913m, 516.9566494662152m, 376.0741888256204m, 516.9566494662152m)
                    ],
                    sourceHandle: "AB12",
                    linePrimitives:
                    [
                        new ExtractedDimensionLinePrimitive("AB12-LINE-1", 1, 440.5741888255912m, 519.7784891962231m, 440.5741888255912m, 512.9566494662152m),
                        new ExtractedDimensionLinePrimitive("AB12-LINE-2", 2, 372.5741888256203m, 524.9408070879156m, 372.5741888256203m, 512.9566494662152m),
                        new ExtractedDimensionLinePrimitive("AB12-LINE-3", 3, 437.0741888255913m, 516.9566494662152m, 376.0741888256204m, 516.9566494662152m)
                    ],
                    textPrimitives:
                    [
                        new ExtractedDimensionTextPrimitive("AB12-TEXT-1", 1, "10'-4\"", 408.8391899621098m, 518.9677806582538m, 3.5m, 0m)
                        {
                            StyleName = "ARCH",
                            AttachmentPoint = "MiddleCenter"
                        }
                    ],
                    insertPrimitives:
                    [
                        new ExtractedDimensionInsertPrimitive("AB12-INSERT-1", 1, "_Dot", 440.5741888255912m, 516.9566494662152m, 0m),
                        new ExtractedDimensionInsertPrimitive("AB12-INSERT-2", 2, "_Dot", 372.5741888256203m, 516.9566494662152m, 0m)
                    ])
            ],
            CancellationToken.None);

        var draft = new FloorPlanCuration(
            Guid.NewGuid(),
            versionId,
            curationVersion: 1,
            FloorPlanCurationStatus.Draft,
            basedOnCurationId: null,
            notes: "draft",
            createdAtUtc: now.AddMinutes(1),
            publishedAtUtc: null);

        await new SqliteFloorPlanCurationRepository(session).AddAsync(draft, CancellationToken.None);

        var pinchGroupId = Guid.NewGuid();
        await new SqlitePinchGroupRepository(session).AddAsync(
            new PinchGroup(pinchGroupId, draft.Id, "Patio", PinchAxisTag.Width, 1),
            CancellationToken.None);

        await new SqlitePinchMarkerRepository(session).AddAsync(
            new PinchMarker(
                Guid.NewGuid(),
                draft.Id,
                pinchGroupId,
                persistedCandidate.Id,
                persistedCandidate.GeometryPathId ?? Guid.Empty,
                0.5m,
                120m,
                1),
            CancellationToken.None);

        await session.CommitAsync(CancellationToken.None);
        return rejectedGeometryPathId;
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
