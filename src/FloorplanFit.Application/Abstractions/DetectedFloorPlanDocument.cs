using FloorplanFit.Domain.Measurement;

namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedFloorPlanDocument(
    string OriginalFileName,
    string SuggestedName,
    LengthUnit SourceUnit,
    decimal ToMillimetersFactor,
    string? DxfVersion,
    string GeometryFingerprint);
