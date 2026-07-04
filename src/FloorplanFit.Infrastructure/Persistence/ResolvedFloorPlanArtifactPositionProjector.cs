using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Infrastructure.Persistence;

internal static class ResolvedFloorPlanArtifactPositionProjector
{
    public static RoomLabelDto Resolve(RoomLabelDto label, FloorPlanArtifactPosition? position, FloorPlanLabelOverride? labelOverride = null)
    {
        var detectedX = label.X;
        var detectedY = label.Y;
        var detectedTextHeight = label.TextHeight;
        var resolvedTextHeight = labelOverride?.ResolvedTextHeight ?? detectedTextHeight;
        var hasManualTextHeight = labelOverride?.ResolvedTextHeight is decimal manualHeight &&
                                  manualHeight != detectedTextHeight;

        if (position is null || position.PositionMode != FloorPlanArtifactPositionMode.AbsolutePoint)
        {
            return label with
            {
                HasManualPosition = false,
                DetectedX = detectedX,
                DetectedY = detectedY,
                TextHeight = resolvedTextHeight,
                HasManualTextHeight = hasManualTextHeight,
                DetectedTextHeight = detectedTextHeight
            };
        }

        var resolvedX = position.ResolvedX ?? detectedX;
        var resolvedY = position.ResolvedY ?? detectedY;
        return label with
        {
            X = resolvedX,
            Y = resolvedY,
            HasManualPosition = resolvedX != detectedX || resolvedY != detectedY,
            DetectedX = detectedX,
            DetectedY = detectedY,
            TextHeight = resolvedTextHeight,
            HasManualTextHeight = hasManualTextHeight,
            DetectedTextHeight = detectedTextHeight
        };
    }

    public static OpeningLabelDto Resolve(OpeningLabelDto label, FloorPlanArtifactPosition? position, FloorPlanLabelOverride? labelOverride = null)
    {
        var detectedX = label.X;
        var detectedY = label.Y;
        var detectedTextHeight = label.TextHeight;
        var resolvedTextHeight = labelOverride?.ResolvedTextHeight ?? detectedTextHeight;
        var hasManualTextHeight = labelOverride?.ResolvedTextHeight is decimal manualHeight &&
                                  manualHeight != detectedTextHeight;

        if (position is null || position.PositionMode != FloorPlanArtifactPositionMode.AbsolutePoint)
        {
            return label with
            {
                HasManualPosition = false,
                DetectedX = detectedX,
                DetectedY = detectedY,
                TextHeight = resolvedTextHeight,
                HasManualTextHeight = hasManualTextHeight,
                DetectedTextHeight = detectedTextHeight
            };
        }

        var resolvedX = position.ResolvedX ?? detectedX;
        var resolvedY = position.ResolvedY ?? detectedY;
        return label with
        {
            X = resolvedX,
            Y = resolvedY,
            HasManualPosition = resolvedX != detectedX || resolvedY != detectedY,
            DetectedX = detectedX,
            DetectedY = detectedY,
            TextHeight = resolvedTextHeight,
            HasManualTextHeight = hasManualTextHeight,
            DetectedTextHeight = detectedTextHeight
        };
    }

    public static CuratedPlanArtifactDto Resolve(CuratedPlanArtifactDto artifact, FloorPlanArtifactPosition? position)
    {
        if (position is null || position.PositionMode != FloorPlanArtifactPositionMode.Translation)
        {
            return artifact with
            {
                HasManualPosition = false,
                TranslationDx = 0m,
                TranslationDy = 0m
            };
        }

        var dx = position.TranslationDx ?? 0m;
        var dy = position.TranslationDy ?? 0m;
        return artifact with
        {
            HasManualPosition = dx != 0m || dy != 0m,
            TranslationDx = dx,
            TranslationDy = dy
        };
    }

    public static IReadOnlyList<GeometryPathDto> ApplyGeometryTranslations(
        IReadOnlyList<GeometryPathDto> geometryPaths,
        IReadOnlyList<CuratedPlanArtifactDto> artifacts)
    {
        var pathTranslations = artifacts
            .Where(item => item.HasManualPosition)
            .SelectMany(item => item.GeometryPathIds.Select(pathId => new KeyValuePair<Guid, (decimal Dx, decimal Dy)>(pathId, (item.TranslationDx, item.TranslationDy))))
            .ToDictionary(item => item.Key, item => item.Value);

        return geometryPaths
            .Select(path =>
            {
                if (!pathTranslations.TryGetValue(path.Id, out var translation))
                {
                    return path;
                }

                return new GeometryPathDto(
                    path.Id,
                    path.IsClosed,
                    path.Segments
                        .Select(segment => new GeometrySegmentDto(
                            segment.GeometryPathId,
                            segment.SortOrder,
                            segment.StartX + translation.Dx,
                            segment.StartY + translation.Dy,
                            segment.EndX + translation.Dx,
                            segment.EndY + translation.Dy))
                        .ToArray());
            })
            .ToArray();
    }
}
