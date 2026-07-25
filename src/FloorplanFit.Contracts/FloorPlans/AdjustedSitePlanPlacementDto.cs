namespace FloorplanFit.Contracts.FloorPlans;

/// <summary>
/// Full placement state of an adjusted floor plan against a site plan, expressed so the
/// raw source DXFs can be combined without re-deriving anything from the preview.
/// <para>
/// The exported drawing keeps the FLOOR PLAN in its own source coordinate system (so its
/// native blocks, dimensions and layers stay byte-faithful) and brings the SITE PLAN into
/// that system with the inverse affine map: q = (p - SiteOffset) / FloorToSiteScale.
/// </para>
/// <para>
/// <see cref="FloorToSiteScale"/> and <see cref="SiteOffsetX"/>/<see cref="SiteOffsetY"/>
/// describe the affine part of the preview (projection centering plus any manual move):
/// previewPoint = floorSourcePoint * scale + offset. Compression steps are NOT affine, so
/// they are carried separately, already converted to FLOOR-PLAN SOURCE coordinates, making
/// them invariant to manual moves performed after a plan was applied.
/// </para>
/// <para>
/// <see cref="AdjustedDimensions"/> carries the affected native dimension block patches
/// that the preview already recalculated and highlighted. These patches are also expressed
/// in floor-plan source coordinates so export does not recompute cota text independently.
/// </para>
/// </summary>
public sealed record AdjustedSitePlanPlacementDto(
    decimal FloorToSiteScale,
    decimal SiteOffsetX,
    decimal SiteOffsetY,
    IReadOnlyList<AdjustedCompressionStepDto> CompressionSteps,
    IReadOnlyList<DimensionDto> AdjustedDimensions)
{
    public AdjustmentInputAuditDto? InputAudit { get; init; }

    public IReadOnlyList<FloorPlanAdjustmentOperationImpactDto> FloorPlanImpactAudit { get; init; } = [];

    public IReadOnlyList<AdjustmentRecipeStretchActionDto> StretchActions { get; init; } = [];

    public AdjustedSitePlanPlacementDto(
        decimal FloorToSiteScale,
        decimal SiteOffsetX,
        decimal SiteOffsetY,
        IReadOnlyList<AdjustedCompressionStepDto> CompressionSteps)
        : this(FloorToSiteScale, SiteOffsetX, SiteOffsetY, CompressionSteps, [])
    {
    }
}

public sealed record AdjustmentInputAuditDto(
    decimal OriginalWidthInches,
    decimal OriginalHeightInches,
    decimal RequestedWidthInches,
    decimal RequestedHeightInches,
    decimal RequiredWidthDeltaInches,
    decimal RequiredHeightDeltaInches,
    string Source);

public sealed record FloorPlanAdjustmentOperationImpactDto(
    string OperationId,
    int OperationIndex,
    string Kind,
    string AxisTag,
    string Edge,
    decimal Coordinate,
    decimal ExpectedDeltaSourceUnits,
    int AffectedEntities,
    int AffectedVertices,
    decimal MeasuredMinDeltaSourceUnits,
    decimal MeasuredMaxDeltaSourceUnits,
    string Status,
    string? Warning = null);

/// <summary>
/// One applied auto-fit compression, in floor-plan source coordinates. Semantics match the
/// preview exactly: for each marker, points at or beyond the marker coordinate (towards
/// <see cref="Edge"/>) shift by the marker trim, accumulating across markers.
/// </summary>
public sealed record AdjustedCompressionStepDto(
    string AxisTag,
    string Edge,
    IReadOnlyList<AdjustedCompressionMarkerDto> Markers);

public sealed record AdjustedCompressionMarkerDto(
    decimal Coordinate,
    decimal TrimSourceUnits);

public sealed record AdjustmentRecipeSummaryDto(
    string Version,
    decimal FloorToSiteScale,
    decimal SiteOffsetX,
    decimal SiteOffsetY,
    IReadOnlyList<AdjustmentRecipeOperationDto> Operations)
{
    public IReadOnlyList<AdjustmentRecipeStretchActionDto> StretchActions { get; init; } = [];

    public static AdjustmentRecipeSummaryDto FromPlacement(AdjustedSitePlanPlacementDto placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        if (placement.StretchActions.Count > 0)
        {
            return new AdjustmentRecipeSummaryDto(
                "v2",
                placement.FloorToSiteScale,
                placement.SiteOffsetX,
                placement.SiteOffsetY,
                [])
            {
                StretchActions = placement.StretchActions.ToArray()
            };
        }

        return new AdjustmentRecipeSummaryDto(
            "v1",
            placement.FloorToSiteScale,
            placement.SiteOffsetX,
            placement.SiteOffsetY,
            placement.CompressionSteps
                .SelectMany(step => step.Markers.Select(marker =>
                    new AdjustmentRecipeOperationDto(
                        ResolveOperationKind(step),
                        step.AxisTag,
                        step.Edge,
                        marker.Coordinate,
                        marker.TrimSourceUnits)))
                .ToArray());
    }

    public string ToSheetReviewSummary(string sheetKind)
    {
        var normalizedSheetKind = string.IsNullOrWhiteSpace(sheetKind) ? "DependentSheet" : sheetKind.Trim();
        if (StretchActions.Count > 0)
        {
            var actions = string.Join(
                ", ",
                StretchActions.Select(action =>
                    $"{action.ActionId} {action.AxisTag}/{action.Edge} delta {action.DeltaSourceUnits}"));

            return $"{normalizedSheetKind}: affine placement applied; CAD stretch recipe requires entity-aware projection: {actions}.";
        }

        if (Operations.Count == 0)
        {
            return $"{normalizedSheetKind}: affine placement applied; no local compression operations.";
        }

        var operations = string.Join(
            ", ",
            Operations.Select(operation =>
                $"{operation.Kind} {operation.Edge} @{operation.Coordinate} delta {operation.DeltaSourceUnits}"));

        return $"{normalizedSheetKind}: affine placement applied; local recipe requires review before DXF deformation: {operations}.";
    }

    private static string ResolveOperationKind(AdjustedCompressionStepDto step)
        => string.Equals(step.AxisTag, "Height", StringComparison.OrdinalIgnoreCase)
            ? "VerticalCompression"
            : "HorizontalCompression";
}

public sealed record AdjustmentRecipeOperationDto(
    string Kind,
    string AxisTag,
    string Edge,
    decimal Coordinate,
    decimal DeltaSourceUnits);

public sealed record AdjustmentRecipeStretchActionDto(
    string ActionId,
    string AxisTag,
    string Edge,
    decimal CutCoordinate,
    decimal DeltaSourceUnits,
    decimal MaxDeltaSourceUnits,
    decimal CoordinateTolerance,
    AdjustmentRecipeBoundsDto CanonicalSourceBounds,
    IReadOnlyList<AdjustmentRecipeTargetSpanDto> TargetSpans,
    IReadOnlyList<AdjustmentRecipeEntityRoleDto> CanonicalEntityRoles);

public sealed record AdjustmentRecipeTargetSpanDto(
    string SourceEntityRef,
    Guid GeometryPathId,
    int SegmentSortOrder,
    decimal StartX,
    decimal StartY,
    decimal EndX,
    decimal EndY,
    int ClosingVertexIndex);

public sealed record AdjustmentRecipeEntityRoleDto(
    string EntityRef,
    Guid? GeometryPathId,
    int? SegmentSortOrder,
    string Role,
    IReadOnlyList<int> VertexIndices,
    string? Reason = null)
{
    public Guid? HostGeometryPathId { get; init; }

    public int? HostSegmentSortOrder { get; init; }
}

public sealed record AdjustmentRecipeBoundsDto(
    decimal MinX,
    decimal MinY,
    decimal MaxX,
    decimal MaxY);
