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

public sealed class CuratedPlanArtifactReviewSessionIntegrationTests
{
    [Fact]
    public async Task GetByTemplateAsync_returns_resolved_curated_artifacts_with_reclassified_and_excluded_overlays()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-curated-review-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 5, 11, 16, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var import = await ExecuteImportAsync(workspace, sourcePath, now);
            var seeded = await SeedExtractionDraftAndOverlayAsync(workspace, now.AddMinutes(5));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var reader = new SqliteFloorPlanReviewSessionReader(session);

            var reviewSession = await reader.GetByTemplateAsync(import.Item.TemplateId, CancellationToken.None);

            Assert.NotNull(reviewSession);
            Assert.Equal(3, reviewSession.CuratedPlanArtifacts.Count);

            var reclassifiedOpening = Assert.Single(reviewSession.CuratedPlanArtifacts, item =>
                item.SourceArtifactKind == FloorPlanArtifactSourceKinds.OpeningCandidate);
            Assert.Equal(FloorPlanArtifactTaxonomy.OpeningFamily, reclassifiedOpening.DetectedFamily);
            Assert.Equal(FloorPlanArtifactTaxonomy.FixedFamily, reclassifiedOpening.ResolvedFamily);
            Assert.Equal(FloorPlanArtifactTaxonomy.WetFixtureCategory, reclassifiedOpening.ResolvedCategory);
            Assert.Equal(FloorPlanArtifactTaxonomy.TubType, reclassifiedOpening.ResolvedType);
            Assert.Equal(FloorPlanArtifactDecisionState.Reclassified.ToString(), reclassifiedOpening.DecisionState);
            Assert.Equal("#FFDC2626", reclassifiedOpening.ResolvedColorArgb);
            Assert.Contains(seeded.OpeningGeometryPathId, reclassifiedOpening.GeometryPathIds);

            var fixedComponent = Assert.Single(reviewSession.CuratedPlanArtifacts, item =>
                item.SourceArtifactKind == FloorPlanArtifactSourceKinds.FixedPlanComponent);
            Assert.Equal(FloorPlanArtifactTaxonomy.FixedFamily, fixedComponent.DetectedFamily);
            Assert.Equal(FloorPlanArtifactTaxonomy.FixedFamily, fixedComponent.ResolvedFamily);
            Assert.Equal(FloorPlanArtifactDecisionState.DetectedDefault.ToString(), fixedComponent.DecisionState);

            var excludedProtected = Assert.Single(reviewSession.CuratedPlanArtifacts, item =>
                item.SourceArtifactKind == FloorPlanArtifactSourceKinds.ProtectedDetailAssembly);
            Assert.Equal(FloorPlanArtifactDecisionState.Excluded.ToString(), excludedProtected.DecisionState);
            Assert.Equal(FloorPlanArtifactTaxonomy.ProtectedFamily, excludedProtected.ResolvedFamily);
            Assert.Equal(FloorPlanArtifactTaxonomy.WetAssemblyCategory, excludedProtected.ResolvedCategory);
            Assert.Equal(FloorPlanArtifactTaxonomy.UnknownWetAssemblyType, excludedProtected.ResolvedType);
            Assert.Equal(FloorPlanArtifactTaxonomy.UnknownColorArgb, excludedProtected.ResolvedColorArgb);
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

    private static async Task<SeededArtifacts> SeedExtractionDraftAndOverlayAsync(AppWorkspace workspace, DateTime now)
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

        var wallCandidate = new ExtractedWallCandidate(
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
        await new SqliteExtractedWallCandidateRepository(session).AddRangeAsync(
            [wallCandidate],
            [new DetectedWallCandidate("LINE:1", "WALLS", [new GeometryPoint(0m, 0m), new GeometryPoint(120m, 0m)], null, 0.95m, null)],
            CancellationToken.None);

        var openingId = Guid.NewGuid();
        await new SqliteExtractedOpeningCandidateRepository(session).AddRangeAsync(
            [new ExtractedOpeningCandidate(openingId, extractionRun.Id, "LINE:DOOR:1", "DOORS", "Door", "LINE", Guid.Empty, 0.95m, null, 1)],
            [new DetectedOpeningCandidate("LINE:DOOR:1", "DOORS", "Door", "LINE", [new GeometryPoint(40m, 0m), new GeometryPoint(76m, 0m)], 0.95m, null)],
            CancellationToken.None);
        var opening = (await new SqliteExtractedOpeningCandidateRepository(session).ListByExtractionRunAsync(extractionRun.Id, CancellationToken.None)).Single();

        var fixedId = Guid.NewGuid();
        await new SqliteExtractedFixedPlanComponentRepository(session).AddRangeAsync(
            [new ExtractedFixedPlanComponent(fixedId, extractionRun.Id, "INSERT:1", "FIXTURES", "Toilet", "INSERT", "TOILET1", 0.95m, null, 1, "#FF7F7F7F")],
            [new DetectedFixedPlanComponent("INSERT:1", "FIXTURES", "Toilet", "INSERT", "TOILET1", [[new GeometryPoint(1400m, 668m), new GeometryPoint(1412m, 668m)]], 0.95m, null, "#FF7F7F7F")],
            CancellationToken.None);
        var fixedComponent = (await new SqliteExtractedFixedPlanComponentRepository(session).ListByExtractionRunAsync(extractionRun.Id, CancellationToken.None)).Single();

        var protectedId = Guid.NewGuid();
        await new SqliteExtractedProtectedDetailAssemblyRepository(session).AddRangeAsync(
            [new ExtractedProtectedDetailAssembly(protectedId, extractionRun.Id, "DETAIL:MISC:1", "MISC", "WetAreaDetail", "DETAIL-GROUP", 0.90m, null, 1, "#FF00FF00")],
            [new DetectedProtectedDetailAssembly("DETAIL:MISC:1", "MISC", "WetAreaDetail", "DETAIL-GROUP", [[new GeometryPoint(416m, 585m), new GeometryPoint(468m, 585m)]], 0.90m, null, "#FF00FF00")],
            CancellationToken.None);
        var protectedAssembly = (await new SqliteExtractedProtectedDetailAssemblyRepository(session).ListByExtractionRunAsync(extractionRun.Id, CancellationToken.None)).Single();

        var draft = new FloorPlanCuration(Guid.NewGuid(), versionId, 1, FloorPlanCurationStatus.Draft, null, null, now.AddMinutes(1), null);
        await new SqliteFloorPlanCurationRepository(session).AddAsync(draft, CancellationToken.None);

        var overlayRepository = new SqliteFloorPlanArtifactClassificationRepository(session);
        await overlayRepository.UpsertAsync(
            new FloorPlanArtifactClassification(
                draft.Id,
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                opening.Id,
                FloorPlanArtifactTaxonomy.FixedFamily,
                FloorPlanArtifactTaxonomy.WetFixtureCategory,
                FloorPlanArtifactTaxonomy.TubType,
                FloorPlanArtifactDecisionState.Reclassified,
                now.AddMinutes(2)),
            CancellationToken.None);
        await overlayRepository.UpsertAsync(
            new FloorPlanArtifactClassification(
                draft.Id,
                FloorPlanArtifactSourceKinds.ProtectedDetailAssembly,
                protectedAssembly.Id,
                FloorPlanArtifactTaxonomy.ProtectedFamily,
                FloorPlanArtifactTaxonomy.WetAssemblyCategory,
                FloorPlanArtifactTaxonomy.UnknownWetAssemblyType,
                FloorPlanArtifactDecisionState.Excluded,
                now.AddMinutes(3)),
            CancellationToken.None);

        await session.CommitAsync(CancellationToken.None);

        return new SeededArtifacts(opening.GeometryPathId ?? Guid.Empty);
    }

    private sealed record SeededArtifacts(Guid OpeningGeometryPathId);

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
