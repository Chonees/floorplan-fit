namespace FloorplanFit.Contracts.FloorPlans;

public sealed record RoomLabelDto(
    Guid RoomLabelId,
    string SourceEntityRef,
    string SourceLayer,
    string Text,
    decimal X,
    decimal Y,
    decimal Confidence,
    string? DetectionNotes,
    int SortOrder,
    string? SourceEntityKind = null,
    decimal? TextHeight = null,
    decimal RotationDegrees = 0m,
    string? TextStyleName = null,
    string? HorizontalAlignment = null,
    string? VerticalAlignment = null,
    string? AttachmentPoint = null,
    string? ColorArgb = null);
