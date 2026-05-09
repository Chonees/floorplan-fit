namespace FloorplanFit.Application.Abstractions;

public sealed record DetectedOpeningLabel(
    string SourceEntityRef,
    string SourceLayer,
    string Kind,
    string Text,
    decimal X,
    decimal Y,
    decimal Confidence,
    string? DetectionNotes,
    string? SourceEntityKind = null,
    decimal? TextHeight = null,
    decimal RotationDegrees = 0m,
    string? TextStyleName = null,
    string? HorizontalAlignment = null,
    string? VerticalAlignment = null,
    string? AttachmentPoint = null,
    string? ColorArgb = null);
