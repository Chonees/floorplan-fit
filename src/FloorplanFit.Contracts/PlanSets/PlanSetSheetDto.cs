using System.Collections.Generic;
using System.Globalization;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record PlanSetSheetDto(
    Guid SheetId,
    string SheetType,
    string Name,
    Guid ImportedDocumentId,
    Guid? SourceFloorPlanVersionId,
    bool IsCanonical,
    string RegistrationStatus,
    string ProjectionStatus,
    Guid? SheetRegistrationId = null,
    Guid? SheetProjectionId = null,
    string? RegistrationMethod = null,
    decimal? RegistrationConfidence = null,
    string? RegistrationWarning = null,
    string? RegistrationRuleSummary = null,
    string? ProjectionMethod = null,
    decimal? ProjectionConfidence = null,
    string? ProjectionWarning = null,
    string? ProjectionRuleSummary = null)
{
    private bool HasEditableRegistration =>
        RegistrationStatus is "Unregistered" or "Rejected";

    public bool CanUnlink => !IsCanonical;

    public bool CanRegisterDependent =>
        !IsCanonical &&
        SheetType is "ElectricalPlan" or "RoofPlan" or "FacadeElevation" &&
        HasEditableRegistration &&
        ProjectionStatus == "NotProjected";

    public bool CanRegisterElectrical => CanRegisterDependent && SheetType == "ElectricalPlan";

    public bool CanCorrectSheetType =>
        !IsCanonical &&
        HasEditableRegistration &&
        ProjectionStatus == "NotProjected";

    public bool CanCorrectToElectrical => CanCorrectSheetType && SheetType != "ElectricalPlan";

    public bool CanCorrectToRoof => CanCorrectSheetType && SheetType != "RoofPlan";

    public bool CanCorrectToFacade => CanCorrectSheetType && SheetType != "FacadeElevation";

    public bool CanConfirmRegistration =>
        !IsCanonical &&
        SheetRegistrationId.HasValue &&
        RegistrationStatus == "PendingConfirmation";

    public bool CanRejectRegistration =>
        !IsCanonical &&
        SheetRegistrationId.HasValue &&
        RegistrationStatus == "PendingConfirmation" &&
        ProjectionStatus == "NotProjected";

    public bool CanConfirmProjection =>
        !IsCanonical &&
        SheetProjectionId.HasValue &&
        ProjectionStatus == "RequiresManualConfirmation";

    public string RegistrationQualityLabel =>
        BuildQualityLabel(RegistrationMethod, RegistrationConfidence, RegistrationWarning, RegistrationRuleSummary);

    public string ProjectionQualityLabel =>
        BuildQualityLabel(ProjectionMethod, ProjectionConfidence, ProjectionWarning, ProjectionRuleSummary);

    private static string BuildQualityLabel(
        string? method,
        decimal? confidence,
        string? warning,
        string? ruleSummary)
    {
        var parts = new List<string>();
        AddIfPresent(parts, method);
        if (confidence.HasValue)
        {
            parts.Add($"confidence {confidence.Value.ToString("0.##", CultureInfo.InvariantCulture)}");
        }

        AddIfPresent(parts, warning);
        AddIfPresent(parts, ruleSummary);

        return string.Join(" | ", parts);
    }

    private static void AddIfPresent(List<string> parts, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(value.Trim());
        }
    }
}
