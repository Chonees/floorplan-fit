using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.DataCollection;

public sealed class GetPlanSetQualityReportHandler
{
    private const string ClassificationEventType = "SheetClassificationQualityMeasured";
    private const string RegistrationEventType = "SheetRegistrationQualityMeasured";
    private const string ProjectionEventType = "SheetAdjustmentProjectionQualityMeasured";

    private readonly IPlanSetAuditEventReader auditEventReader;

    public GetPlanSetQualityReportHandler(IPlanSetAuditEventReader auditEventReader)
    {
        this.auditEventReader = auditEventReader;
    }

    public async Task<PlanSetQualityReportDto> HandleAsync(
        Guid planSetVersionId,
        CancellationToken cancellationToken)
    {
        if (planSetVersionId == Guid.Empty)
        {
            throw new ArgumentException("Plan-set version is required.", nameof(planSetVersionId));
        }

        var events = await auditEventReader.ListQualityEventsByPlanSetVersionAsync(
            planSetVersionId,
            cancellationToken);
        var signals = events
            .Select(TryMapSignal)
            .Where(signal => signal is not null)
            .Select(signal => signal!)
            .GroupBy(signal => new { signal.Kind, signal.AggregateId })
            .Select(group => group.OrderByDescending(signal => signal.OccurredAtUtc).First())
            .ToArray();
        var registrationSignals = signals.Where(signal => signal.Kind == "Registration").ToArray();
        var projectionSignals = signals.Where(signal => signal.Kind == "Projection").ToArray();

        return new PlanSetQualityReportDto(
            planSetVersionId,
            registrationSignals.Length,
            projectionSignals.Length,
            LowestConfidence(registrationSignals),
            LowestConfidence(projectionSignals),
            registrationSignals.Count(RequiresManualReview),
            projectionSignals.Count(RequiresManualReview),
            signals);
    }

    private static PlanSetQualitySignalDto? TryMapSignal(PlanSetAuditEvent auditEvent)
    {
        var kind = auditEvent.EventType switch
        {
            ClassificationEventType => "Classification",
            RegistrationEventType => "Registration",
            ProjectionEventType => "Projection",
            _ => null
        };
        if (kind is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(auditEvent.PayloadJson);
        var root = document.RootElement;
        return new PlanSetQualitySignalDto(
            kind,
            auditEvent.AggregateId,
            ReadString(root, "method") ?? ReadString(root, "source") ?? string.Empty,
            ReadDecimal(root, "confidence"),
            ReadString(root, "status") ?? string.Empty,
            ReadString(root, "warning"),
            ReadString(root, "ruleSummary"),
            auditEvent.OccurredAtUtc);
    }

    private static decimal? LowestConfidence(IReadOnlyList<PlanSetQualitySignalDto> signals)
    {
        var confidences = signals
            .Where(signal => signal.Confidence.HasValue)
            .Select(signal => signal.Confidence!.Value)
            .ToArray();
        return confidences.Length == 0 ? null : confidences.Min();
    }

    private static bool RequiresManualReview(PlanSetQualitySignalDto signal)
        => !string.Equals(signal.Status, "ReadyForExport", StringComparison.OrdinalIgnoreCase) &&
           !string.Equals(signal.Status, "Confirmed", StringComparison.OrdinalIgnoreCase);

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        return value.GetString();
    }

    private static decimal? ReadDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        return value.GetDecimal();
    }
}
