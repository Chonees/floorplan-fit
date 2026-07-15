using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public sealed record ProjectedPlanSheetExportRecipe(
    SheetRegistrationTransform RegistrationTransform,
    AdjustmentRecipeSummaryDto CanonicalRecipe,
    decimal? CanonicalSourceWidthInches = null,
    decimal? CanonicalSourceHeightInches = null,
    ProjectedPlanSheetOutlineNormalization? OutlineNormalization = null,
    SheetRegistrationStatus? RegistrationStatus = null,
    WholePlanRegistrationProof? WholePlanRegistrationProof = null);

public sealed record ProjectedPlanSheetOutlineNormalization(
    decimal SourceMinX,
    decimal SourceMinY,
    decimal SourceMaxX,
    decimal SourceMaxY,
    decimal ScaleX,
    decimal ScaleY,
    string AnchorX,
    string AnchorY,
    string Status,
    string Reason);
