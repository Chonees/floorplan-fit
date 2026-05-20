using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

internal sealed class FloorPlanReviewQueueCoordinator
{
    public ReviewQueueProjection BuildProjection(
        string selectedReviewQueueFilter,
        string reviewQueueSearchText,
        IReadOnlyList<WallCandidateDto> wallCandidates,
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<CuratedPlanArtifactDto> visibleCuratedPlanArtifacts)
    {
        var visibleWalls = wallCandidates
            .Where(candidate => ShouldIncludeWallCandidateInQueue(candidate, selectedReviewQueueFilter, reviewQueueSearchText))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.SourceEntityRef, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var visibleRoomLabels = roomLabels
            .Where(label => ShouldIncludeRoomLabelInQueue(label, selectedReviewQueueFilter, reviewQueueSearchText))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Text, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var visibleOpeningLabels = openingLabels
            .Where(label => ShouldIncludeOpeningLabelInQueue(label, selectedReviewQueueFilter, reviewQueueSearchText))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Text, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var visibleDimensions = dimensions
            .Where(dimension => ShouldIncludeDimensionInQueue(dimension, selectedReviewQueueFilter, reviewQueueSearchText))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.SourceEntityRef, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var curatedArtifactGroups = visibleCuratedPlanArtifacts
            .Where(artifact => ShouldIncludeCuratedArtifactInQueue(artifact, selectedReviewQueueFilter, reviewQueueSearchText))
            .GroupBy(item => (item.ResolvedFamily, item.ResolvedCategory))
            .OrderBy(group => FloorPlanArtifactTaxonomy.ResolveFamilySortOrder(group.Key.ResolvedFamily))
            .ThenBy(group => FloorPlanArtifactTaxonomy.ResolveCategorySortOrder(group.Key.ResolvedFamily, group.Key.ResolvedCategory))
            .Select(group => new CuratedArtifactGroupViewModel(
                group.Key.ResolvedFamily,
                group.Key.ResolvedCategory,
                FloorPlanReviewDisplayText.GetCuratedGroupTitle(group.Key.ResolvedFamily, group.Key.ResolvedCategory),
                FloorPlanReviewDisplayText.GetCuratedGroupSubtitle(group.Key.ResolvedFamily, group.Key.ResolvedCategory),
                group.First().ResolvedColorArgb,
                group.OrderBy(item => item.SortOrder).ThenBy(item => item.SourceEntityRef, StringComparer.OrdinalIgnoreCase).ToArray()))
            .ToArray();

        return new ReviewQueueProjection(
            visibleWalls,
            visibleRoomLabels,
            visibleOpeningLabels,
            visibleDimensions,
            curatedArtifactGroups);
    }

    public ReviewQueueExpansionState NormalizeExpansion(ReviewQueueExpansionState current, ReviewQueueProjection projection)
    {
        var normalized = current;

        if (normalized.IsStructureQueueExpanded && !projection.HasVisibleWallCandidates)
        {
            normalized = normalized with { IsStructureQueueExpanded = false };
        }

        if (normalized.IsRoomNamesQueueExpanded && !projection.HasVisibleRoomLabels)
        {
            normalized = normalized with { IsRoomNamesQueueExpanded = false };
        }

        if (normalized.IsOpeningCodesQueueExpanded && !projection.HasVisibleOpeningLabels)
        {
            normalized = normalized with { IsOpeningCodesQueueExpanded = false };
        }

        if (normalized.IsDimensionsQueueExpanded && !projection.HasVisibleDimensions)
        {
            normalized = normalized with { IsDimensionsQueueExpanded = false };
        }

        if (normalized.IsCuratedObjectsQueueExpanded && !projection.HasVisibleCuratedArtifactGroups)
        {
            normalized = normalized with { IsCuratedObjectsQueueExpanded = false };
        }

        if (normalized.HasAnyExpandedSection)
        {
            return normalized;
        }

        if (projection.HasVisibleCuratedArtifactGroups)
        {
            return normalized with { IsCuratedObjectsQueueExpanded = true };
        }

        if (projection.HasVisibleRoomLabels)
        {
            return normalized with { IsRoomNamesQueueExpanded = true };
        }

        if (projection.HasVisibleOpeningLabels)
        {
            return normalized with { IsOpeningCodesQueueExpanded = true };
        }

        if (projection.HasVisibleDimensions)
        {
            return normalized with { IsDimensionsQueueExpanded = true };
        }

        return projection.HasVisibleWallCandidates
            ? normalized with { IsStructureQueueExpanded = true }
            : normalized;
    }

    public ReviewQueueExpansionState CollapseSectionsExcept(ReviewQueueExpansionState current, ReviewQueueSection expandedSection)
    {
        return expandedSection switch
        {
            ReviewQueueSection.Structure => current with
            {
                IsRoomNamesQueueExpanded = false,
                IsOpeningCodesQueueExpanded = false,
                IsDimensionsQueueExpanded = false,
                IsCuratedObjectsQueueExpanded = false
            },
            ReviewQueueSection.RoomNames => current with
            {
                IsStructureQueueExpanded = false,
                IsOpeningCodesQueueExpanded = false,
                IsDimensionsQueueExpanded = false,
                IsCuratedObjectsQueueExpanded = false
            },
            ReviewQueueSection.OpeningCodes => current with
            {
                IsStructureQueueExpanded = false,
                IsRoomNamesQueueExpanded = false,
                IsDimensionsQueueExpanded = false,
                IsCuratedObjectsQueueExpanded = false
            },
            ReviewQueueSection.Dimensions => current with
            {
                IsStructureQueueExpanded = false,
                IsRoomNamesQueueExpanded = false,
                IsOpeningCodesQueueExpanded = false,
                IsCuratedObjectsQueueExpanded = false
            },
            ReviewQueueSection.CuratedObjects => current with
            {
                IsStructureQueueExpanded = false,
                IsRoomNamesQueueExpanded = false,
                IsOpeningCodesQueueExpanded = false,
                IsDimensionsQueueExpanded = false
            },
            _ => current
        };
    }

    private static bool ShouldIncludeWallCandidateInQueue(
        WallCandidateDto candidate,
        string selectedReviewQueueFilter,
        string reviewQueueSearchText)
    {
        if (!MatchesReviewQueueFilter(selectedReviewQueueFilter, "Structure", changed: false))
        {
            return false;
        }

        return MatchesReviewQueueSearch(reviewQueueSearchText, candidate.SourceEntityRef, candidate.SourceLayer, candidate.AssemblyHint);
    }

    private static bool ShouldIncludeRoomLabelInQueue(
        RoomLabelDto label,
        string selectedReviewQueueFilter,
        string reviewQueueSearchText)
    {
        var changed = label.HasManualPosition || label.HasManualTextHeight;
        if (!MatchesReviewQueueFilter(selectedReviewQueueFilter, "Text & Notes", changed))
        {
            return false;
        }

        return MatchesReviewQueueSearch(reviewQueueSearchText, label.Text, label.SourceEntityRef, label.SourceLayer);
    }

    private static bool ShouldIncludeOpeningLabelInQueue(
        OpeningLabelDto label,
        string selectedReviewQueueFilter,
        string reviewQueueSearchText)
    {
        var changed = label.HasManualPosition || label.HasManualTextHeight;
        if (!MatchesReviewQueueFilter(selectedReviewQueueFilter, "Text & Notes", changed))
        {
            return false;
        }

        return MatchesReviewQueueSearch(reviewQueueSearchText, label.Text, label.Kind, label.SourceEntityRef, label.SourceLayer);
    }

    private static bool ShouldIncludeDimensionInQueue(
        DimensionDto dimension,
        string selectedReviewQueueFilter,
        string reviewQueueSearchText)
    {
        if (!MatchesReviewQueueFilter(selectedReviewQueueFilter, "Dimensions", changed: false))
        {
            return false;
        }

        return MatchesReviewQueueSearch(
            reviewQueueSearchText,
            dimension.DisplayText,
            dimension.SourceEntityRef,
            dimension.SourceLayer,
            dimension.GeometryBlockName,
            dimension.RawTextOverride,
            dimension.SourceEntityKind);
    }

    private static bool ShouldIncludeCuratedArtifactInQueue(
        CuratedPlanArtifactDto artifact,
        string selectedReviewQueueFilter,
        string reviewQueueSearchText)
    {
        var changed =
            artifact.HasManualPosition ||
            !string.Equals(artifact.DecisionState, FloorPlanArtifactDecisionState.DetectedDefault.ToString(), StringComparison.Ordinal);

        if (!MatchesReviewQueueFilter(selectedReviewQueueFilter, "Curated Objects", changed))
        {
            return false;
        }

        return MatchesReviewQueueSearch(
            reviewQueueSearchText,
            artifact.SourceEntityRef,
            artifact.SourceLayer,
            artifact.ResolvedFamily,
            artifact.ResolvedCategory,
            artifact.ResolvedType,
            artifact.SourceBlockName,
            artifact.SourceEntityKind);
    }

    private static bool MatchesReviewQueueFilter(string selectedReviewQueueFilter, string bucket, bool changed)
    {
        return selectedReviewQueueFilter switch
        {
            "Everything" => true,
            "Changed only" => changed,
            "Text & Notes" => string.Equals(bucket, "Text & Notes", StringComparison.Ordinal),
            "Dimensions" => string.Equals(bucket, "Dimensions", StringComparison.Ordinal),
            "Curated Objects" => string.Equals(bucket, "Curated Objects", StringComparison.Ordinal),
            "Structure" => string.Equals(bucket, "Structure", StringComparison.Ordinal),
            _ => true
        };
    }

    private static bool MatchesReviewQueueSearch(string reviewQueueSearchText, params string?[] values)
    {
        var search = reviewQueueSearchText.Trim();
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }
}

internal enum ReviewQueueSection
{
    Structure,
    RoomNames,
    OpeningCodes,
    Dimensions,
    CuratedObjects
}

internal readonly record struct ReviewQueueProjection(
    IReadOnlyList<WallCandidateDto> VisibleWallCandidates,
    IReadOnlyList<RoomLabelDto> VisibleRoomLabels,
    IReadOnlyList<OpeningLabelDto> VisibleOpeningLabels,
    IReadOnlyList<DimensionDto> VisibleDimensions,
    IReadOnlyList<CuratedArtifactGroupViewModel> CuratedArtifactGroups)
{
    public bool HasVisibleWallCandidates => VisibleWallCandidates.Count > 0;

    public bool HasVisibleRoomLabels => VisibleRoomLabels.Count > 0;

    public bool HasVisibleOpeningLabels => VisibleOpeningLabels.Count > 0;

    public bool HasVisibleDimensions => VisibleDimensions.Count > 0;

    public bool HasVisibleCuratedArtifactGroups => CuratedArtifactGroups.Count > 0;
}

internal readonly record struct ReviewQueueExpansionState(
    bool IsStructureQueueExpanded,
    bool IsRoomNamesQueueExpanded,
    bool IsOpeningCodesQueueExpanded,
    bool IsDimensionsQueueExpanded,
    bool IsCuratedObjectsQueueExpanded)
{
    public bool HasAnyExpandedSection =>
        IsStructureQueueExpanded ||
        IsRoomNamesQueueExpanded ||
        IsOpeningCodesQueueExpanded ||
        IsDimensionsQueueExpanded ||
        IsCuratedObjectsQueueExpanded;
}
