using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Export;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Export;

public sealed class ExportProjectedPlanSheetHandlerTests
{
    [Fact]
    public async Task HandleAsync_exports_ready_projection_with_approved_transform()
    {
        var projection = CreateProjection(SheetAdjustmentProjectionStatus.ReadyForExport);
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = new ExportProjectedPlanSheetHandler(
            new FakeSheetAdjustmentProjectionRepository(projection),
            exporter);

        var response = await handler.HandleAsync(
            new ExportProjectedPlanSheetRequest(
                projection.Id,
                @"C:\library\raw-dxf\electrical.dxf",
                @"C:\exports\plan-set\electrical-adjusted.dxf"),
            CancellationToken.None);

        Assert.Equal(projection.Id, response.ProjectionId);
        Assert.Equal(@"C:\exports\plan-set\electrical-adjusted.dxf", response.OutputFilePath);
        var call = Assert.Single(exporter.Calls);
        Assert.Equal(@"C:\library\raw-dxf\electrical.dxf", call.SourceFilePath);
        Assert.Equal(@"C:\exports\plan-set\electrical-adjusted.dxf", call.OutputFilePath);
        Assert.Same(projection.Transform, call.Transform);
    }

    [Fact]
    public async Task HandleAsync_rejects_manual_projection_before_writing_artifact()
    {
        var projection = CreateProjection(SheetAdjustmentProjectionStatus.RequiresManualConfirmation);
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = new ExportProjectedPlanSheetHandler(
            new FakeSheetAdjustmentProjectionRepository(projection),
            exporter);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new ExportProjectedPlanSheetRequest(
                    projection.Id,
                    @"C:\library\raw-dxf\roof.dxf",
                    @"C:\exports\plan-set\roof-adjusted.dxf"),
                CancellationToken.None));

        Assert.Empty(exporter.Calls);
    }

    private static SheetAdjustmentProjection CreateProjection(SheetAdjustmentProjectionStatus status)
    {
        return new SheetAdjustmentProjection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(
                scale: 1.15m,
                rotationDegrees: 0m,
                translateX: 12m,
                translateY: 24m),
            confidence: status is SheetAdjustmentProjectionStatus.ReadyForExport ? 0.92m : 0.65m,
            status,
            warning: null,
            canonicalCompressionStepCount: 0,
            createdAtUtc: new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc));
    }

    private sealed class FakeSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        private readonly SheetAdjustmentProjection projection;

        public FakeSheetAdjustmentProjectionRepository(SheetAdjustmentProjection projection)
        {
            this.projection = projection;
        }

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<SheetAdjustmentProjection?>(projection.Id == projectionId ? projection : null);
        }

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingProjectedPlanSheetExporter : IProjectedPlanSheetExporter
    {
        public List<Call> Calls { get; } = [];

        public Task ExportAsync(
            string sourceFilePath,
            string outputFilePath,
            SheetAdjustmentProjectionTransform transform,
            CancellationToken cancellationToken)
        {
            Calls.Add(new Call(sourceFilePath, outputFilePath, transform));
            return Task.CompletedTask;
        }

        public sealed record Call(
            string SourceFilePath,
            string OutputFilePath,
            SheetAdjustmentProjectionTransform Transform);
    }
}
