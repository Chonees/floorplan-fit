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
    WholePlanRegistrationProof? WholePlanRegistrationProof = null,
    string? CanonicalFloorPlanExportPath = null,
    ElectricalOverlayReconciliation? OverlayReconciliation = null);

/// <summary>
/// Commissioned discipline evidence for the Electrical overlay. When the canonical recipe deforms
/// locally, composition refuses raw overlay import: every device must carry exactly one confirmed
/// wall/room host binding and every geometric carrier must be a commissioned wire route or an
/// explicitly declared static graphic. Daily export never infers hosts or connectivity.
/// </summary>
public sealed record ElectricalOverlayReconciliation(
    IReadOnlyList<ElectricalDeviceHostBinding> DeviceBindings,
    IReadOnlyList<ElectricalWireRouteBinding> WireRoutes)
{
    public IReadOnlyList<string> StaticCarrierHandles { get; init; } = [];
}

public sealed record ElectricalDeviceHostBinding(
    string DeviceHandle,
    string BlockName,
    string HostKind,
    string HostSourceEntityRef,
    string Role,
    decimal HostDeltaXSourceUnits = 0m,
    decimal HostDeltaYSourceUnits = 0m);

public sealed record ElectricalWireRouteBinding(
    string CarrierHandle,
    string StartDeviceHandle,
    string EndDeviceHandle);

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
