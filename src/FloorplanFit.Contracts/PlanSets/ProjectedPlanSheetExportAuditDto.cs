namespace FloorplanFit.Contracts.PlanSets;

public sealed record ProjectedPlanSheetExportAuditDto(
    IReadOnlyList<ProjectedPlanSheetOperationAuditDto> Operations,
    ProjectedPlanSheetDxfSafetyAuditDto DxfSafety,
    ProjectedPlanSheetOutlineCongruenceAuditDto? OutlineCongruence = null);

public sealed record ProjectedPlanSheetOperationAuditDto(
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
    string? Reason = null);

public sealed record ProjectedPlanSheetDxfSafetyAuditDto(
    bool OutputFileExists,
    long OutputFileBytes,
    int EntityCountBefore,
    int EntityCountAfter,
    int InsertCountAfter,
    int DimensionCountAfter,
    int EllipseCountAfter,
    int WireOrCurveCountAfter,
    int MissingHandleCountAfter,
    int MissingOwnerCountAfter,
    int UnsupportedCrossingEntityCount);

public sealed record ProjectedPlanSheetOutlineCongruenceAuditDto(
    string Status,
    string Reason,
    decimal ToleranceInches,
    bool NormalizationApplied,
    ProjectedPlanSheetOutlineDto? CanonicalSourceOutline,
    ProjectedPlanSheetOutlineDto? ElectricalSourceOutline,
    ProjectedPlanSheetOutlineDto? ElectricalNormalizedSourceOutline,
    ProjectedPlanSheetOutlineDto? ElectricalExportOutline,
    decimal? SourceWidthMismatchInches,
    decimal? SourceHeightMismatchInches,
    decimal? ExportWidthMismatchInches,
    decimal? ExportHeightMismatchInches,
    string? AnchorX,
    string? AnchorY,
    decimal? ScaleX,
    decimal? ScaleY);

public sealed record ProjectedPlanSheetOutlineDto(
    decimal MinX,
    decimal MinY,
    decimal MaxX,
    decimal MaxY,
    decimal Width,
    decimal Height);
