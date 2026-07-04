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
    public AdjustedSitePlanPlacementDto(
        decimal FloorToSiteScale,
        decimal SiteOffsetX,
        decimal SiteOffsetY,
        IReadOnlyList<AdjustedCompressionStepDto> CompressionSteps)
        : this(FloorToSiteScale, SiteOffsetX, SiteOffsetY, CompressionSteps, [])
    {
    }
}

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
    public static AdjustmentRecipeSummaryDto FromPlacement(AdjustedSitePlanPlacementDto placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

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
