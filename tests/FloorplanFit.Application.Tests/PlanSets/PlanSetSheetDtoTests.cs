using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets;

public sealed class PlanSetSheetDtoTests
{
    [Fact]
    public void Rejected_not_projected_dependent_sheet_can_be_recovered()
    {
        var sheet = new PlanSetSheetDto(
            Guid.NewGuid(),
            "ElectricalPlan",
            "Electrical",
            Guid.NewGuid(),
            SourceFloorPlanVersionId: null,
            IsCanonical: false,
            RegistrationStatus: "Rejected",
            ProjectionStatus: "NotProjected",
            SheetRegistrationId: Guid.NewGuid());

        Assert.True(sheet.CanUnlink);
        Assert.True(sheet.CanRegisterDependent);
        Assert.True(sheet.CanCorrectSheetType);
    }

    [Fact]
    public void Pending_not_projected_dependent_sheet_can_reject_registration()
    {
        var sheet = new PlanSetSheetDto(
            Guid.NewGuid(),
            "ElectricalPlan",
            "Electrical",
            Guid.NewGuid(),
            SourceFloorPlanVersionId: null,
            IsCanonical: false,
            RegistrationStatus: "PendingConfirmation",
            ProjectionStatus: "NotProjected",
            SheetRegistrationId: Guid.NewGuid());

        Assert.True(sheet.CanRejectRegistration);
        Assert.True(sheet.CanConfirmRegistration);
    }

    [Fact]
    public void Confirmed_ready_for_export_electrical_sheet_can_be_unlinked_for_safe_replacement()
    {
        var sheet = new PlanSetSheetDto(
            Guid.NewGuid(),
            "ElectricalPlan",
            "Electrical",
            Guid.NewGuid(),
            SourceFloorPlanVersionId: Guid.NewGuid(),
            IsCanonical: false,
            RegistrationStatus: "Confirmed",
            ProjectionStatus: "ReadyForExport",
            SheetRegistrationId: Guid.NewGuid(),
            SheetProjectionId: Guid.NewGuid());

        Assert.True(sheet.CanUnlink);
    }

    [Fact]
    public void Quality_labels_summarize_method_confidence_and_warning()
    {
        var sheet = new PlanSetSheetDto(
            Guid.NewGuid(),
            "ElectricalPlan",
            "Electrical",
            Guid.NewGuid(),
            SourceFloorPlanVersionId: null,
            IsCanonical: false,
            RegistrationStatus: "PendingConfirmation",
            ProjectionStatus: "RequiresManualConfirmation",
            SheetRegistrationId: Guid.NewGuid(),
            SheetProjectionId: Guid.NewGuid(),
            RegistrationMethod: "WholeSheetSimilarity",
            RegistrationConfidence: 0.25m,
            RegistrationWarning: "Needs manual review",
            ProjectionMethod: "ElectricalWholeSheetSimilarity",
            ProjectionConfidence: 0.5m,
            ProjectionWarning: "Compression review");

        // The separator is the ASCII pipe that PlanSetSheetDto joins parts with, because
        // these labels are written into audit artifacts that PowerShell verifiers read.
        Assert.Equal(
            "WholeSheetSimilarity | confidence 0.25 | Needs manual review",
            sheet.RegistrationQualityLabel);
        Assert.Equal(
            "ElectricalWholeSheetSimilarity | confidence 0.5 | Compression review",
            sheet.ProjectionQualityLabel);
    }
}
