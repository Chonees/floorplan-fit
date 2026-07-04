namespace FloorplanFit.Contracts.FloorPlans;

public sealed record SitePlanPreviewDto
{
    public SitePlanPreviewDto(
        string fileName,
        string sourceUnit,
        decimal toMillimetersFactor,
        IReadOnlyList<GeometryPathDto> geometryPaths,
        SitePlanBuildableAreaDto buildableArea,
        IReadOnlyList<SitePlanRenderPathDto>? renderPaths = null,
        IReadOnlyList<SitePlanTextDto>? texts = null)
    {
        FileName = fileName;
        SourceUnit = sourceUnit;
        ToMillimetersFactor = toMillimetersFactor;
        GeometryPaths = geometryPaths;
        BuildableArea = buildableArea;
        RenderPaths = renderPaths ?? geometryPaths
            .Select(path => new SitePlanRenderPathDto(
                path.Id,
                string.Empty,
                "GEOMETRY",
                path.IsClosed,
                path.Segments,
                ColorArgb: null,
                IsSetback: false))
            .ToArray();
        Texts = texts ?? [];
    }

    public string FileName { get; init; }

    public string SourceUnit { get; init; }

    public decimal ToMillimetersFactor { get; init; }

    public IReadOnlyList<GeometryPathDto> GeometryPaths { get; init; }

    public SitePlanBuildableAreaDto BuildableArea { get; init; }

    public IReadOnlyList<SitePlanRenderPathDto> RenderPaths { get; init; }

    public IReadOnlyList<SitePlanTextDto> Texts { get; init; }
}

public sealed record SitePlanRenderPathDto(
    Guid Id,
    string SourceLayer,
    string SourceEntityKind,
    bool IsClosed,
    IReadOnlyList<GeometrySegmentDto> Segments,
    string? ColorArgb,
    bool IsSetback);

public sealed record SitePlanTextDto(
    Guid TextId,
    string SourceLayer,
    string SourceEntityKind,
    string Text,
    decimal X,
    decimal Y,
    decimal Height,
    decimal RotationDegrees,
    string? ColorArgb,
    bool IsSetback);

public sealed record SitePlanBuildableAreaDto(
    decimal MinX,
    decimal MinY,
    decimal MaxX,
    decimal MaxY)
{
    public decimal CenterX => (MinX + MaxX) / 2m;

    public decimal CenterY => (MinY + MaxY) / 2m;
}
