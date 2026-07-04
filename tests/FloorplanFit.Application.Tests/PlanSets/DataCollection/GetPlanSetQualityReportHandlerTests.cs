using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.DataCollection;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.DataCollection;

public sealed class GetPlanSetQualityReportHandlerTests
{
    [Fact]
    public async Task HandleAsync_summarizes_registration_and_projection_quality_events()
    {
        var planSetVersionId = Guid.NewGuid();
        var otherPlanSetVersionId = Guid.NewGuid();
        var reader = new FakePlanSetAuditEventReader(
            new PlanSetAuditEvent(
                Guid.NewGuid(),
                "SheetRegistration",
                Guid.NewGuid(),
                "SheetRegistrationQualityMeasured",
                $$"""
                {"planSetVersionId":"{{planSetVersionId}}","method":"WholeSheetSimilarity","confidence":0.82,"status":"PendingConfirmation","warning":"Needs visual review","ruleSummary":null}
                """,
                new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc)),
            new PlanSetAuditEvent(
                Guid.NewGuid(),
                "SheetAdjustmentProjection",
                Guid.NewGuid(),
                "SheetAdjustmentProjectionQualityMeasured",
                $$"""
                {"planSetVersionId":"{{planSetVersionId}}","method":"ElectricalWholeSheetSimilarity","confidence":0.92,"status":"ReadyForExport","warning":null,"ruleSummary":null}
                """,
                new DateTime(2026, 6, 30, 19, 0, 0, DateTimeKind.Utc)),
            new PlanSetAuditEvent(
                Guid.NewGuid(),
                "SheetRegistration",
                Guid.NewGuid(),
                "SheetRegistrationQualityMeasured",
                $$"""
                {"planSetVersionId":"{{otherPlanSetVersionId}}","method":"RoofFootprintWithOverhang","confidence":0.4,"status":"PendingConfirmation","warning":"Ignore me","ruleSummary":"PreserveOverhangInches=12"}
                """,
                new DateTime(2026, 6, 30, 20, 0, 0, DateTimeKind.Utc)));
        var handler = new GetPlanSetQualityReportHandler(reader);

        var report = await handler.HandleAsync(planSetVersionId, CancellationToken.None);

        Assert.Equal(planSetVersionId, report.PlanSetVersionId);
        Assert.Equal(1, report.RegistrationEventCount);
        Assert.Equal(1, report.ProjectionEventCount);
        Assert.Equal(0.82m, report.LowestRegistrationConfidence);
        Assert.Equal(0.92m, report.LowestProjectionConfidence);
        Assert.Equal(1, report.ManualRegistrationCount);
        Assert.Equal(0, report.ManualProjectionCount);
        Assert.Contains(report.Signals, signal =>
            signal.Kind == "Registration" &&
            signal.Method == "WholeSheetSimilarity" &&
            signal.Status == "PendingConfirmation");
        Assert.Contains(report.Signals, signal =>
            signal.Kind == "Projection" &&
            signal.Method == "ElectricalWholeSheetSimilarity" &&
            signal.Status == "ReadyForExport");
    }

    [Fact]
    public async Task HandleAsync_uses_latest_quality_signal_per_registration_and_projection()
    {
        var planSetVersionId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var projectionId = Guid.NewGuid();
        var reader = new FakePlanSetAuditEventReader(
            new PlanSetAuditEvent(
                Guid.NewGuid(),
                "SheetRegistration",
                registrationId,
                "SheetRegistrationQualityMeasured",
                $$"""
                {"planSetVersionId":"{{planSetVersionId}}","method":"WholeSheetSimilarity","confidence":0.72,"status":"PendingConfirmation","warning":"Needs review","ruleSummary":null}
                """,
                new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc)),
            new PlanSetAuditEvent(
                Guid.NewGuid(),
                "SheetRegistration",
                registrationId,
                "SheetRegistrationQualityMeasured",
                $$"""
                {"planSetVersionId":"{{planSetVersionId}}","method":"WholeSheetSimilarity","confidence":0.72,"status":"Confirmed","warning":"Needs review","ruleSummary":null}
                """,
                new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc)),
            new PlanSetAuditEvent(
                Guid.NewGuid(),
                "SheetAdjustmentProjection",
                projectionId,
                "SheetAdjustmentProjectionQualityMeasured",
                $$"""
                {"planSetVersionId":"{{planSetVersionId}}","method":"ElectricalWholeSheetSimilarity","confidence":0.76,"status":"RequiresManualConfirmation","warning":"Needs review","ruleSummary":null}
                """,
                new DateTime(2026, 7, 1, 8, 30, 0, DateTimeKind.Utc)),
            new PlanSetAuditEvent(
                Guid.NewGuid(),
                "SheetAdjustmentProjection",
                projectionId,
                "SheetAdjustmentProjectionQualityMeasured",
                $$"""
                {"planSetVersionId":"{{planSetVersionId}}","method":"ElectricalWholeSheetSimilarity","confidence":0.76,"status":"ReadyForExport","warning":"Needs review","ruleSummary":null}
                """,
                new DateTime(2026, 7, 1, 9, 30, 0, DateTimeKind.Utc)));
        var handler = new GetPlanSetQualityReportHandler(reader);

        var report = await handler.HandleAsync(planSetVersionId, CancellationToken.None);

        Assert.Equal(1, report.RegistrationEventCount);
        Assert.Equal(1, report.ProjectionEventCount);
        Assert.Equal(0, report.ManualRegistrationCount);
        Assert.Equal(0, report.ManualProjectionCount);
        Assert.Contains(report.Signals, signal => signal.AggregateId == registrationId && signal.Status == "Confirmed");
        Assert.Contains(report.Signals, signal => signal.AggregateId == projectionId && signal.Status == "ReadyForExport");
        Assert.DoesNotContain(report.Signals, signal => signal.Status == "PendingConfirmation");
        Assert.DoesNotContain(report.Signals, signal => signal.Status == "RequiresManualConfirmation");
    }

    [Fact]
    public async Task HandleAsync_includes_classification_quality_signal_without_changing_registration_projection_counts()
    {
        var planSetVersionId = Guid.NewGuid();
        var sheetId = Guid.NewGuid();
        var reader = new FakePlanSetAuditEventReader(
            new PlanSetAuditEvent(
                Guid.NewGuid(),
                "PlanSheet",
                sheetId,
                "SheetClassificationQualityMeasured",
                $$"""
                {"planSetVersionId":"{{planSetVersionId}}","sheetType":"ElectricalPlan","source":"Classifier","confidence":0.9,"status":"AutoClassified","warning":null,"ruleSummary":"Matched ElectricalPlan layer keywords."}
                """,
                new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)));
        var handler = new GetPlanSetQualityReportHandler(reader);

        var report = await handler.HandleAsync(planSetVersionId, CancellationToken.None);

        Assert.Equal(0, report.RegistrationEventCount);
        Assert.Equal(0, report.ProjectionEventCount);
        Assert.Contains(report.Signals, signal =>
            signal.Kind == "Classification" &&
            signal.AggregateId == sheetId &&
            signal.Method == "Classifier" &&
            signal.Status == "AutoClassified" &&
            signal.Confidence == 0.9m);
    }

    private sealed class FakePlanSetAuditEventReader : IPlanSetAuditEventReader
    {
        private readonly IReadOnlyList<PlanSetAuditEvent> events;

        public FakePlanSetAuditEventReader(params PlanSetAuditEvent[] events)
        {
            this.events = events;
        }

        public Task<IReadOnlyList<PlanSetAuditEvent>> ListQualityEventsByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<PlanSetAuditEvent> result = events
                .Where(auditEvent => auditEvent.PayloadJson.Contains(planSetVersionId.ToString(), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return Task.FromResult(result);
        }
    }
}
