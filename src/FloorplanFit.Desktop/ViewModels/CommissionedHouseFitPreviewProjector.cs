using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;

namespace FloorplanFit.Desktop.ViewModels;

internal sealed record CommissionedHouseFitPreviewResult(
    bool Succeeded,
    IReadOnlyList<GeometryPathDto> GeometryPaths,
    IReadOnlyList<RoomLabelDto> RoomLabels,
    IReadOnlyList<OpeningLabelDto> OpeningLabels,
    IReadOnlyList<DimensionDto> Dimensions,
    string RejectionReason);

internal static class CommissionedHouseFitPreviewProjector
{
    public static CommissionedHouseFitPreviewResult Apply(
        IReadOnlyList<GeometryPathDto> projectedGeometryPaths,
        IReadOnlyList<AdjustmentRecipeStretchActionDto> sourceActions,
        decimal projectionScale,
        decimal projectionOffsetX,
        decimal projectionOffsetY)
        => Apply(
            projectedGeometryPaths,
            [],
            [],
            [],
            sourceActions,
            projectionScale,
            projectionOffsetX,
            projectionOffsetY);

    public static CommissionedHouseFitPreviewResult Apply(
        IReadOnlyList<GeometryPathDto> projectedGeometryPaths,
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<AdjustmentRecipeStretchActionDto> sourceActions,
        decimal projectionScale,
        decimal projectionOffsetX,
        decimal projectionOffsetY)
    {
        ArgumentNullException.ThrowIfNull(projectedGeometryPaths);
        ArgumentNullException.ThrowIfNull(roomLabels);
        ArgumentNullException.ThrowIfNull(openingLabels);
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(sourceActions);

        if (projectedGeometryPaths.Count == 0)
        {
            return Reject(projectedGeometryPaths, roomLabels, openingLabels, dimensions, "No projected FloorPlan geometry is available.");
        }

        if (projectionScale <= 0m)
        {
            return Reject(projectedGeometryPaths, roomLabels, openingLabels, dimensions, "FloorPlan projection scale must be positive.");
        }

        if (!TryBuildAnnotationOffsets(
                roomLabels,
                openingLabels,
                dimensions,
                sourceActions,
                projectionScale,
                out var offsets,
                out var annotationReason))
        {
            return Reject(projectedGeometryPaths, roomLabels, openingLabels, dimensions, annotationReason);
        }

        if (sourceActions.Count == 0)
        {
            return new(true, projectedGeometryPaths, roomLabels, openingLabels, dimensions, string.Empty);
        }

        try
        {
            var projectedActions = sourceActions
                .Select(action => ProjectAction(
                    action,
                    projectionScale,
                    projectionOffsetX,
                    projectionOffsetY))
                .ToArray();
            var geometry = FloorPlanPreviewGeometry.CreatePreviewGeometry(
                projectedGeometryPaths,
                projectedActions);
            if (!TryTranslateAnnotations(
                    roomLabels,
                    openingLabels,
                    dimensions,
                    offsets,
                    out var translatedRoomLabels,
                    out var translatedOpeningLabels,
                    out var translatedDimensions,
                    out var translationReason))
            {
                return Reject(projectedGeometryPaths, roomLabels, openingLabels, dimensions, translationReason);
            }

            return new(
                true,
                geometry,
                translatedRoomLabels,
                translatedOpeningLabels,
                translatedDimensions,
                string.Empty);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or ArgumentException or OverflowException)
        {
            return Reject(projectedGeometryPaths, roomLabels, openingLabels, dimensions, exception.Message);
        }
    }

    private static bool TryBuildAnnotationOffsets(
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<AdjustmentRecipeStretchActionDto> actions,
        decimal scale,
        out Dictionary<string, (decimal X, decimal Y, string? LastActionId)> offsets,
        out string reason)
    {
        offsets = new(StringComparer.OrdinalIgnoreCase);
        foreach (var label in roomLabels)
        {
            if (label is null)
            {
                reason = "Commissioned preview contains an incomplete room label annotation.";
                return false;
            }

            if (!TryAddAnnotation(offsets, label.SourceEntityRef, "room label", out reason))
            {
                return false;
            }
        }

        foreach (var label in openingLabels)
        {
            if (label is null)
            {
                reason = "Commissioned preview contains an incomplete opening label annotation.";
                return false;
            }

            if (!TryAddAnnotation(offsets, label.SourceEntityRef, "opening label", out reason))
            {
                return false;
            }
        }

        foreach (var dimension in dimensions)
        {
            if (dimension is null)
            {
                reason = "Commissioned preview contains an incomplete dimension annotation.";
                return false;
            }

            if (dimension.LineSegments is null ||
                dimension.LinePrimitives is null ||
                dimension.TextPrimitives is null ||
                dimension.InsertPrimitives is null ||
                dimension.CirclePrimitives is null ||
                dimension.ArcPrimitives is null ||
                dimension.SolidPrimitives is null ||
                dimension.RenderTextX.HasValue != dimension.RenderTextY.HasValue ||
                dimension.RenderTextHeight is < 0m ||
                dimension.TextPrimitives.Any(text => text.Height < 0m) ||
                dimension.CirclePrimitives.Any(circle => circle.Radius < 0m) ||
                dimension.ArcPrimitives.Any(arc => arc.Radius < 0m))
            {
                reason = $"Source-only annotation '{dimension.SourceEntityRef}' has an incomplete dimension payload.";
                return false;
            }

            if (!string.Equals(dimension.SourceEntityKind, "DIMENSION", StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Source-only annotation '{dimension.SourceEntityRef}' has unsupported dimension kind '{dimension.SourceEntityKind}'.";
                return false;
            }

            if (!TryAddAnnotation(offsets, dimension.SourceEntityRef, "dimension", out reason))
            {
                return false;
            }
        }

        foreach (var action in actions)
        {
            if (action is null)
            {
                reason = "Commissioned preview contains an incomplete action.";
                return false;
            }

            var actionId = string.IsNullOrWhiteSpace(action.ActionId) ? "<missing>" : action.ActionId;
            if (string.IsNullOrWhiteSpace(action.ActionId) ||
                action.CanonicalEntityRoles is null ||
                action.TargetSpans is null ||
                action.CanonicalSourceBounds is null)
            {
                reason = $"Action '{actionId}' has incomplete commissioned evidence.";
                return false;
            }

            if (!TryResolveVector(action, scale, out var deltaX, out var deltaY, out var vectorReason))
            {
                reason = $"Action '{actionId}' {vectorReason}";
                return false;
            }

            if (action.CanonicalEntityRoles.Any(role =>
                    role is null ||
                    string.IsNullOrWhiteSpace(role.EntityRef) ||
                    role.VertexIndices is null))
            {
                reason = $"Action '{actionId}' contains an incomplete annotation role.";
                return false;
            }

            foreach (var entityRef in offsets.Keys.ToArray())
            {
                var roles = action.CanonicalEntityRoles
                    .Where(role => string.Equals(role.EntityRef, entityRef, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (roles.Length == 0)
                {
                    reason = $"Source-only annotation '{entityRef}' is missing its role in action '{actionId}'.";
                    return false;
                }

                if (roles.Length != 1)
                {
                    reason = $"Source-only annotation '{entityRef}' has duplicate roles in action '{actionId}'.";
                    return false;
                }

                var role = roles[0];
                if (role.GeometryPathId.HasValue ||
                    role.SegmentSortOrder.HasValue ||
                    role.VertexIndices is null ||
                    role.VertexIndices.Count != 0)
                {
                    reason = $"Source-only annotation '{entityRef}' has an incomplete or contradictory role binding in action '{actionId}'.";
                    return false;
                }

                if (string.Equals(role.Role, "Fixed", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(role.Role, "RigidMove", StringComparison.OrdinalIgnoreCase))
                {
                    reason = $"Source-only annotation '{entityRef}' has unsupported role '{role.Role}' in action '{actionId}'; expected Fixed or RigidMove.";
                    return false;
                }

                var current = offsets[entityRef];
                try
                {
                    offsets[entityRef] = (
                        checked(current.X + deltaX),
                        checked(current.Y + deltaY),
                        actionId);
                }
                catch (OverflowException)
                {
                    reason = $"Source-only annotation '{entityRef}' cannot accumulate RigidMove for action '{actionId}' safely.";
                    return false;
                }
            }

            var trackedOffsets = offsets;
            var uncommissioned = action.CanonicalEntityRoles.FirstOrDefault(role =>
                !role.GeometryPathId.HasValue &&
                !trackedOffsets.ContainsKey(role.EntityRef));
            if (uncommissioned is not null)
            {
                reason = $"Source-only annotation role '{uncommissioned.EntityRef}' in action '{actionId}' has no commissioned preview entity.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryAddAnnotation(
        Dictionary<string, (decimal X, decimal Y, string? LastActionId)> offsets,
        string? entityRef,
        string annotationKind,
        out string reason)
    {
        if (string.IsNullOrWhiteSpace(entityRef))
        {
            reason = $"Commissioned preview contains a {annotationKind} without an exact source reference.";
            return false;
        }

        if (!offsets.TryAdd(entityRef, (0m, 0m, null)))
        {
            reason = $"Source-only annotation '{entityRef}' has a duplicate preview identity.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryResolveVector(
        AdjustmentRecipeStretchActionDto action,
        decimal scale,
        out decimal deltaX,
        out decimal deltaY,
        out string reason)
    {
        deltaX = 0m;
        deltaY = 0m;
        if (action.DeltaSourceUnits <= 0m)
        {
            reason = "requires a positive translation delta.";
            return false;
        }

        decimal delta;
        try
        {
            delta = checked(action.DeltaSourceUnits * scale);
        }
        catch (OverflowException)
        {
            reason = "has a translation delta that overflows preview coordinates.";
            return false;
        }

        if (string.Equals(action.AxisTag, "Width", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(action.Edge, "Right", StringComparison.OrdinalIgnoreCase))
            {
                deltaX = -delta;
                reason = string.Empty;
                return true;
            }

            if (string.Equals(action.Edge, "Left", StringComparison.OrdinalIgnoreCase))
            {
                deltaX = delta;
                reason = string.Empty;
                return true;
            }
        }
        else if (string.Equals(action.AxisTag, "Height", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(action.Edge, "Top", StringComparison.OrdinalIgnoreCase))
            {
                deltaY = -delta;
                reason = string.Empty;
                return true;
            }

            if (string.Equals(action.Edge, "Bottom", StringComparison.OrdinalIgnoreCase))
            {
                deltaY = delta;
                reason = string.Empty;
                return true;
            }
        }

        reason = $"has unsupported axis/edge '{action.AxisTag}/{action.Edge}'.";
        return false;
    }

    private static bool TryTranslateAnnotations(
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyDictionary<string, (decimal X, decimal Y, string? LastActionId)> offsets,
        out IReadOnlyList<RoomLabelDto> translatedRoomLabels,
        out IReadOnlyList<OpeningLabelDto> translatedOpeningLabels,
        out IReadOnlyList<DimensionDto> translatedDimensions,
        out string reason)
    {
        var roomResult = new List<RoomLabelDto>(roomLabels.Count);
        var openingResult = new List<OpeningLabelDto>(openingLabels.Count);
        var dimensionResult = new List<DimensionDto>(dimensions.Count);
        foreach (var label in roomLabels)
        {
            var offset = offsets[label.SourceEntityRef];
            try
            {
                roomResult.Add(offset.X == 0m && offset.Y == 0m
                    ? label
                    : SitePlanAdjustmentPreviewProjector.Translate(label, offset.X, offset.Y));
            }
            catch (OverflowException)
            {
                translatedRoomLabels = roomLabels;
                translatedOpeningLabels = openingLabels;
                translatedDimensions = dimensions;
                reason = $"Source-only annotation '{label.SourceEntityRef}' cannot apply cumulative translation through action '{offset.LastActionId ?? "<missing>"}' safely.";
                return false;
            }
        }

        foreach (var label in openingLabels)
        {
            var offset = offsets[label.SourceEntityRef];
            try
            {
                openingResult.Add(offset.X == 0m && offset.Y == 0m
                    ? label
                    : SitePlanAdjustmentPreviewProjector.Translate(label, offset.X, offset.Y));
            }
            catch (OverflowException)
            {
                translatedRoomLabels = roomLabels;
                translatedOpeningLabels = openingLabels;
                translatedDimensions = dimensions;
                reason = $"Source-only annotation '{label.SourceEntityRef}' cannot apply cumulative translation through action '{offset.LastActionId ?? "<missing>"}' safely.";
                return false;
            }
        }

        foreach (var dimension in dimensions)
        {
            var offset = offsets[dimension.SourceEntityRef];
            try
            {
                dimensionResult.Add(offset.X == 0m && offset.Y == 0m
                    ? dimension
                    : SitePlanAdjustmentPreviewProjector.Translate(dimension, offset.X, offset.Y));
            }
            catch (OverflowException)
            {
                translatedRoomLabels = roomLabels;
                translatedOpeningLabels = openingLabels;
                translatedDimensions = dimensions;
                reason = $"Source-only annotation '{dimension.SourceEntityRef}' cannot apply cumulative translation through action '{offset.LastActionId ?? "<missing>"}' safely.";
                return false;
            }
        }

        translatedRoomLabels = roomResult;
        translatedOpeningLabels = openingResult;
        translatedDimensions = dimensionResult;
        reason = string.Empty;
        return true;
    }

    private static AdjustmentRecipeStretchActionDto ProjectAction(
        AdjustmentRecipeStretchActionDto action,
        decimal scale,
        decimal offsetX,
        decimal offsetY)
    {
        ArgumentNullException.ThrowIfNull(action);
        var bounds = action.CanonicalSourceBounds;
        var geometricRoles = action.CanonicalEntityRoles
            .Where(role => role.GeometryPathId.HasValue)
            .ToArray();
        return action with
        {
            CutCoordinate = IsHeight(action.AxisTag)
                ? Y(action.CutCoordinate, scale, offsetY)
                : X(action.CutCoordinate, scale, offsetX),
            DeltaSourceUnits = Length(action.DeltaSourceUnits, scale),
            MaxDeltaSourceUnits = Length(action.MaxDeltaSourceUnits, scale),
            CoordinateTolerance = Length(action.CoordinateTolerance, scale),
            CanonicalSourceBounds = new AdjustmentRecipeBoundsDto(
                X(bounds.MinX, scale, offsetX),
                Y(bounds.MinY, scale, offsetY),
                X(bounds.MaxX, scale, offsetX),
                Y(bounds.MaxY, scale, offsetY)),
            TargetSpans = action.TargetSpans
                .Select(span => span with
                {
                    StartX = X(span.StartX, scale, offsetX),
                    StartY = Y(span.StartY, scale, offsetY),
                    EndX = X(span.EndX, scale, offsetX),
                    EndY = Y(span.EndY, scale, offsetY)
                })
                .ToArray(),
            CanonicalEntityRoles = geometricRoles
        };
    }

    private static bool IsHeight(string axisTag)
        => string.Equals(axisTag, "Height", StringComparison.OrdinalIgnoreCase);

    private static decimal X(decimal value, decimal scale, decimal offset)
        => checked((value * scale) + offset);

    private static decimal Y(decimal value, decimal scale, decimal offset)
        => checked((value * scale) + offset);

    private static decimal Length(decimal value, decimal scale)
        => checked(value * scale);

    private static CommissionedHouseFitPreviewResult Reject(
        IReadOnlyList<GeometryPathDto> original,
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<DimensionDto> dimensions,
        string reason)
        => new(false, original, roomLabels, openingLabels, dimensions, reason);
}
