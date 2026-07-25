using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public sealed record CadStretchRecipeCompilationRequest(
    Guid PinchGroupId,
    string AxisTag,
    string Edge,
    decimal DeltaSourceUnits,
    decimal SourceToMillimetersFactor,
    decimal CoordinateTolerance,
    IReadOnlyList<PinchMarkerDto> Markers,
    IReadOnlyList<WallCandidateDto> WallCandidates,
    IReadOnlyList<GeometryPathDto> GeometryPaths);

public sealed record CadStretchRecipeCompilationResult(
    bool Succeeded,
    AdjustmentRecipeStretchActionDto? Action,
    string? RejectionReason);

public sealed record CadStretchRecipeGroupCompilationResult(
    bool Succeeded,
    IReadOnlyList<AdjustmentRecipeStretchActionDto> Actions,
    string? RejectionReason);

public static class CadStretchRecipeCompiler
{
    public static CadStretchRecipeGroupCompilationResult CompileGroup(CadStretchRecipeCompilationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SourceToMillimetersFactor <= 0m)
        {
            return RejectGroup("Source-to-millimeters factor must be positive.");
        }

        if (request.CoordinateTolerance < 0m)
        {
            return RejectGroup("Coordinate tolerance cannot be negative.");
        }

        if (!IsSupportedAxisEdge(request.AxisTag, request.Edge))
        {
            return RejectGroup($"Unsupported CAD stretch axis/edge '{request.AxisTag}/{request.Edge}'.");
        }

        if (request.DeltaSourceUnits <= 0m)
        {
            return RejectGroup("CAD stretch delta must be positive.");
        }

        var markers = SelectMarkers(request);
        if (markers.Length < 2 || markers.Length % 2 != 0)
        {
            return RejectGroup("A CAD stretch pinch group requires an even marker count of at least two.");
        }

        var pairCount = markers.Length / 2;
        var pairCapacities = new decimal[pairCount];
        for (var stationIndex = 0; stationIndex < pairCount; stationIndex++)
        {
            var firstMarker = markers[stationIndex * 2];
            var secondMarker = markers[(stationIndex * 2) + 1];
            if (firstMarker.GeometryPathId == secondMarker.GeometryPathId)
            {
                return RejectGroup($"CAD stretch station {stationIndex} requires two distinct wall paths.");
            }

            try
            {
                pairCapacities[stationIndex] = Math.Min(
                    firstMarker.MaxTrimMm / request.SourceToMillimetersFactor,
                    secondMarker.MaxTrimMm / request.SourceToMillimetersFactor);
            }
            catch (OverflowException)
            {
                return RejectGroup($"CAD stretch station {stationIndex} has an invalid paired-wall capacity.");
            }

            if (pairCapacities[stationIndex] <= 0m)
            {
                return RejectGroup($"CAD stretch station {stationIndex} must have positive paired-wall capacity.");
            }
        }

        decimal totalCapacity;
        try
        {
            totalCapacity = pairCapacities.Sum();
        }
        catch (OverflowException)
        {
            return RejectGroup("CAD stretch group capacity is invalid.");
        }

        if (request.DeltaSourceUnits > totalCapacity)
        {
            return RejectGroup($"Requested delta {request.DeltaSourceUnits} exceeds group capacity {totalCapacity}.");
        }

        var stationDeltas = AllocateGroupDelta(request.DeltaSourceUnits, pairCapacities);
        var compiledStations = new List<CompiledStation>(pairCount);
        for (var stationIndex = 0; stationIndex < pairCount; stationIndex++)
        {
            PinchMarkerDto[] pairMarkers =
            [
                markers[stationIndex * 2],
                markers[(stationIndex * 2) + 1]
            ];
            var stationRequest = request with
            {
                DeltaSourceUnits = stationDeltas[stationIndex],
                Markers = pairMarkers
            };
            var stationCompilation = Compile(stationRequest);
            if (!stationCompilation.Succeeded || stationCompilation.Action is null)
            {
                return RejectGroup(
                    $"CAD stretch station {stationIndex} failed: {stationCompilation.RejectionReason ?? "Unknown rejection."}");
            }

            compiledStations.Add(new CompiledStation(stationIndex, stationCompilation.Action));
        }

        var actions = OrderAndAugmentGroupActions(
            request.PinchGroupId,
            request.Edge,
            compiledStations);
        return new CadStretchRecipeGroupCompilationResult(true, actions, null);
    }

    public static CadStretchRecipeCompilationResult Compile(CadStretchRecipeCompilationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SourceToMillimetersFactor <= 0m)
        {
            return Reject("Source-to-millimeters factor must be positive.");
        }

        if (request.CoordinateTolerance < 0m)
        {
            return Reject("Coordinate tolerance cannot be negative.");
        }

        if (!IsSupportedAxisEdge(request.AxisTag, request.Edge))
        {
            return Reject($"Unsupported CAD stretch axis/edge '{request.AxisTag}/{request.Edge}'.");
        }

        var markers = SelectMarkers(request);
        if (markers.Length != 2 || markers.Select(marker => marker.GeometryPathId).Distinct().Count() != 2)
        {
            return Reject("A CAD stretch pinch group requires exactly two distinct wall faces.");
        }

        if (request.DeltaSourceUnits <= 0m)
        {
            return Reject("CAD stretch delta must be positive.");
        }

        var maxDelta = markers.Min(marker => marker.MaxTrimMm / request.SourceToMillimetersFactor);
        if (maxDelta <= 0m || request.DeltaSourceUnits > maxDelta + request.CoordinateTolerance)
        {
            return Reject($"Requested delta {request.DeltaSourceUnits} exceeds paired-wall capacity {maxDelta}.");
        }

        var geometryGroups = request.GeometryPaths
            .GroupBy(path => path.Id)
            .ToArray();
        if (geometryGroups.Any(group => group.Count() != 1))
        {
            return Reject("Canonical source geometry contains duplicate geometry path IDs.");
        }

        var geometryById = geometryGroups
            .ToDictionary(group => group.Key, group => group.Single());
        var acceptedCandidates = request.WallCandidates
            .Where(candidate => string.Equals(candidate.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var candidateGroups = acceptedCandidates
            .GroupBy(candidate => candidate.CandidateId)
            .ToArray();
        if (candidateGroups.Any(group => group.Count() != 1))
        {
            return Reject("Canonical source geometry contains duplicate wall candidate IDs.");
        }

        var candidatesById = candidateGroups
            .ToDictionary(group => group.Key, group => group.Single());
        var resolvedTargets = new List<ResolvedTarget>(2);

        foreach (var marker in markers)
        {
            if (!candidatesById.TryGetValue(marker.SourceCandidateId, out var candidate) ||
                candidate.GeometryPathId != marker.GeometryPathId)
            {
                return Reject($"Pinch marker '{marker.PinchMarkerId}' has no matching wall candidate geometry.");
            }

            if (!geometryById.TryGetValue(marker.GeometryPathId, out var path) || path.Segments.Count == 0)
            {
                return Reject($"Pinch marker '{marker.PinchMarkerId}' has no geometry path.");
            }

            if (!TryResolveTarget(
                    marker,
                    candidate,
                    path,
                    request.AxisTag,
                    request.Edge,
                    request.CoordinateTolerance,
                    out var target,
                    out var targetError))
            {
                return Reject(targetError);
            }

            resolvedTargets.Add(target);
        }

        if (string.Equals(resolvedTargets[0].Candidate.SourceEntityRef, resolvedTargets[1].Candidate.SourceEntityRef, StringComparison.OrdinalIgnoreCase))
        {
            return Reject("Paired wall faces must resolve to two distinct source spans.");
        }

        var cutCoordinate = (resolvedTargets[0].CutCoordinate + resolvedTargets[1].CutCoordinate) / 2m;
        var roles = new List<AdjustmentRecipeEntityRoleDto>(resolvedTargets.Count);
        foreach (var target in resolvedTargets)
        {
            roles.Add(new AdjustmentRecipeEntityRoleDto(
                target.Candidate.SourceEntityRef,
                target.Path.Id,
                target.Segment.SortOrder,
                "Stretch",
                [target.ClosingVertexIndex]));
        }

        var targetPathIds = resolvedTargets.Select(target => target.Path.Id).ToHashSet();
        var firstClosingEndpoint = ClosingEndpoint(resolvedTargets[0]);
        var secondClosingEndpoint = ClosingEndpoint(resolvedTargets[1]);
        var eligibleCandidates = new List<ResolvedRigidMoveCandidate>();
        foreach (var candidate in acceptedCandidates
                     .Where(candidate => candidate.GeometryPathId.HasValue)
                     .Where(candidate => !targetPathIds.Contains(candidate.GeometryPathId!.Value))
                     .OrderBy(candidate => candidate.SortOrder)
                     .ThenBy(candidate => candidate.CandidateId))
        {
            if (!geometryById.TryGetValue(candidate.GeometryPathId!.Value, out var path) || path.Segments.Count == 0)
            {
                continue;
            }

            if (IsWhollyOnClosingSide(
                    path,
                    request.AxisTag,
                    request.Edge,
                    resolvedTargets[0].CutCoordinate,
                    resolvedTargets[1].CutCoordinate,
                    request.CoordinateTolerance))
            {
                eligibleCandidates.Add(new ResolvedRigidMoveCandidate(candidate, path));
            }
        }

        var reachedCandidateIds = ResolveConnectedClosingComponent(
            eligibleCandidates,
            firstClosingEndpoint,
            secondClosingEndpoint,
            request.CoordinateTolerance);
        foreach (var candidate in eligibleCandidates
                     .Where(candidate => reachedCandidateIds.Contains(candidate.Candidate.CandidateId)))
        {
            roles.Add(new AdjustmentRecipeEntityRoleDto(
                candidate.Candidate.SourceEntityRef,
                candidate.Path.Id,
                null,
                "RigidMove",
                []));
        }

        if (!TryResolveBounds(request.GeometryPaths, out var bounds))
        {
            return Reject("Canonical source geometry has no finite stretch bounds.");
        }

        var action = new AdjustmentRecipeStretchActionDto(
            request.PinchGroupId.ToString("D"),
            NormalizeAxis(request.AxisTag),
            NormalizeEdge(request.Edge),
            cutCoordinate,
            request.DeltaSourceUnits,
            maxDelta,
            request.CoordinateTolerance,
            bounds,
            resolvedTargets.Select(target => new AdjustmentRecipeTargetSpanDto(
                target.Candidate.SourceEntityRef,
                target.Path.Id,
                target.Segment.SortOrder,
                target.Segment.StartX,
                target.Segment.StartY,
                target.Segment.EndX,
                target.Segment.EndY,
                target.ClosingVertexIndex)).ToArray(),
            roles);

        return new CadStretchRecipeCompilationResult(true, action, null);
    }

    private static PinchMarkerDto[] SelectMarkers(CadStretchRecipeCompilationRequest request)
        => request.Markers
            .Where(marker => marker.PinchGroupId == request.PinchGroupId)
            .Where(marker => string.Equals(marker.AxisTag, request.AxisTag, StringComparison.OrdinalIgnoreCase))
            .OrderBy(marker => marker.SortOrder)
            .ThenBy(marker => marker.PinchMarkerId)
            .ToArray();

    private static decimal[] AllocateGroupDelta(decimal totalDelta, IReadOnlyList<decimal> capacities)
    {
        var allocations = new decimal[capacities.Count];
        var remainingDelta = totalDelta;

        while (remainingDelta > 0m)
        {
            var openStations = Enumerable.Range(0, capacities.Count)
                .Where(index => allocations[index] < capacities[index])
                .ToArray();
            var equalShare = remainingDelta / openStations.Length;
            var cappedStations = openStations
                .Where(index => capacities[index] - allocations[index] < equalShare)
                .ToArray();

            if (cappedStations.Length == 0)
            {
                for (var index = 0; index < openStations.Length - 1; index++)
                {
                    var stationDelta = Math.Min(equalShare, remainingDelta);
                    allocations[openStations[index]] += stationDelta;
                    remainingDelta -= stationDelta;
                }

                var lastStation = openStations[^1];
                var lastStationDelta = Math.Min(
                    remainingDelta,
                    capacities[lastStation] - allocations[lastStation]);
                allocations[lastStation] += lastStationDelta;
                remainingDelta -= lastStationDelta;
                continue;
            }

            foreach (var stationIndex in cappedStations)
            {
                var stationDelta = capacities[stationIndex] - allocations[stationIndex];
                allocations[stationIndex] += stationDelta;
                remainingDelta -= stationDelta;
            }
        }

        return allocations;
    }

    private static AdjustmentRecipeStretchActionDto[] OrderAndAugmentGroupActions(
        Guid pinchGroupId,
        string edge,
        IReadOnlyList<CompiledStation> compiledStations)
    {
        var orderedStations = (IsPositiveClosingEdge(edge)
                ? compiledStations.OrderBy(station => station.Action.CutCoordinate)
                : compiledStations.OrderByDescending(station => station.Action.CutCoordinate))
            .ThenBy(station => station.OriginalPairIndex)
            .ToArray();
        var actions = new AdjustmentRecipeStretchActionDto[orderedStations.Length];

        for (var executionIndex = 0; executionIndex < orderedStations.Length; executionIndex++)
        {
            var action = orderedStations[executionIndex].Action;
            var roles = new List<AdjustmentRecipeEntityRoleDto>(action.CanonicalEntityRoles.Count);
            var representedPathIds = new HashSet<Guid>();
            var representedEntityRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var role in action.CanonicalEntityRoles)
            {
                AddRoleIfUnrepresented(roles, representedPathIds, representedEntityRefs, role);
            }

            for (var laterIndex = executionIndex + 1; laterIndex < orderedStations.Length; laterIndex++)
            {
                var laterAction = orderedStations[laterIndex].Action;
                foreach (var target in laterAction.TargetSpans)
                {
                    AddRoleIfUnrepresented(
                        roles,
                        representedPathIds,
                        representedEntityRefs,
                        new AdjustmentRecipeEntityRoleDto(
                            target.SourceEntityRef,
                            target.GeometryPathId,
                            null,
                            "RigidMove",
                            []));
                }

                foreach (var role in laterAction.CanonicalEntityRoles
                             .Where(role => string.Equals(role.Role, "RigidMove", StringComparison.OrdinalIgnoreCase)))
                {
                    AddRoleIfUnrepresented(
                        roles,
                        representedPathIds,
                        representedEntityRefs,
                        role);
                }
            }

            actions[executionIndex] = action with
            {
                ActionId = GroupActionId(pinchGroupId, executionIndex),
                CanonicalEntityRoles = roles.ToArray()
            };
        }

        return actions;
    }

    private static void AddRoleIfUnrepresented(
        ICollection<AdjustmentRecipeEntityRoleDto> roles,
        ISet<Guid> representedPathIds,
        ISet<string> representedEntityRefs,
        AdjustmentRecipeEntityRoleDto role)
    {
        if ((role.GeometryPathId.HasValue && representedPathIds.Contains(role.GeometryPathId.Value)) ||
            representedEntityRefs.Contains(role.EntityRef))
        {
            return;
        }

        roles.Add(role);
        if (role.GeometryPathId.HasValue)
        {
            representedPathIds.Add(role.GeometryPathId.Value);
        }

        representedEntityRefs.Add(role.EntityRef);
    }

    private static bool TryResolveTarget(
        PinchMarkerDto marker,
        WallCandidateDto candidate,
        GeometryPathDto path,
        string axisTag,
        string edge,
        decimal tolerance,
        out ResolvedTarget target,
        out string error)
    {
        target = default!;
        if (path.Segments.Count != 1)
        {
            error = $"Target span '{candidate.SourceEntityRef}' must resolve to one single segment in CAD stretch v2.";
            return false;
        }

        if (!TryResolveSegmentAtRatio(path, marker.PositionRatio, out var segment, out var point))
        {
            error = $"Pinch marker '{marker.PinchMarkerId}' could not resolve one target segment.";
            return false;
        }

        var isWidth = IsWidth(axisTag);
        if (isWidth
                ? Math.Abs(segment.StartY - segment.EndY) > tolerance
                : Math.Abs(segment.StartX - segment.EndX) > tolerance)
        {
            error = $"Target span '{candidate.SourceEntityRef}' is not parallel to axis '{axisTag}'.";
            return false;
        }

        var cutCoordinate = isWidth ? point.X : point.Y;
        var startCoordinate = isWidth ? segment.StartX : segment.StartY;
        var endCoordinate = isWidth ? segment.EndX : segment.EndY;
        var startSide = SignedClosingDistance(startCoordinate, cutCoordinate, edge);
        var endSide = SignedClosingDistance(endCoordinate, cutCoordinate, edge);
        if (startSide > tolerance && endSide < -tolerance)
        {
            target = new ResolvedTarget(candidate, path, segment, cutCoordinate, 0);
            error = string.Empty;
            return true;
        }

        if (endSide > tolerance && startSide < -tolerance)
        {
            target = new ResolvedTarget(candidate, path, segment, cutCoordinate, 1);
            error = string.Empty;
            return true;
        }

        error = $"Target span '{candidate.SourceEntityRef}' does not cross its pinch cut with one closing-side endpoint.";
        return false;
    }

    private static bool TryResolveSegmentAtRatio(
        GeometryPathDto path,
        decimal positionRatio,
        out GeometrySegmentDto segment,
        out CadStretchPoint point)
    {
        segment = default!;
        point = default!;
        var lengths = path.Segments.Select(SegmentLength).ToArray();
        var totalLength = lengths.Sum();
        if (!double.IsFinite(totalLength) || totalLength <= double.Epsilon)
        {
            return false;
        }

        var targetLength = totalLength * (double)decimal.Clamp(positionRatio, 0m, 1m);
        var traversed = 0d;
        for (var index = 0; index < path.Segments.Count; index++)
        {
            var length = lengths[index];
            if (index < path.Segments.Count - 1 && traversed + length < targetLength)
            {
                traversed += length;
                continue;
            }

            segment = path.Segments[index];
            var ratio = length <= double.Epsilon ? 0m : decimal.CreateChecked((targetLength - traversed) / length);
            ratio = decimal.Clamp(ratio, 0m, 1m);
            point = new CadStretchPoint(
                segment.StartX + ((segment.EndX - segment.StartX) * ratio),
                segment.StartY + ((segment.EndY - segment.StartY) * ratio));
            return true;
        }

        return false;
    }

    private static CadStretchPoint ClosingEndpoint(ResolvedTarget target)
        => target.ClosingVertexIndex == 0
            ? new CadStretchPoint(target.Segment.StartX, target.Segment.StartY)
            : new CadStretchPoint(target.Segment.EndX, target.Segment.EndY);

    private static bool IsWhollyOnClosingSide(
        GeometryPathDto path,
        string axisTag,
        string edge,
        decimal firstCutCoordinate,
        decimal secondCutCoordinate,
        decimal tolerance)
    {
        var closingBoundary = IsPositiveClosingEdge(edge)
            ? Math.Max(firstCutCoordinate, secondCutCoordinate)
            : Math.Min(firstCutCoordinate, secondCutCoordinate);
        return path.Segments.Any(segment => SegmentLength(segment) > double.Epsilon) &&
               path.Segments
                   .SelectMany(segment => new[]
                   {
                       IsWidth(axisTag) ? segment.StartX : segment.StartY,
                       IsWidth(axisTag) ? segment.EndX : segment.EndY
                   })
                   .All(coordinate => SignedClosingDistance(coordinate, closingBoundary, edge) > tolerance);
    }

    private static HashSet<Guid> ResolveConnectedClosingComponent(
        IReadOnlyList<ResolvedRigidMoveCandidate> candidates,
        CadStretchPoint firstClosingEndpoint,
        CadStretchPoint secondClosingEndpoint,
        decimal tolerance)
    {
        var reachedCandidateIds = new HashSet<Guid>();
        var pendingCandidateIndexes = new Queue<int>();
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            if (!PathTouchesPoint(candidate.Path, firstClosingEndpoint, tolerance) &&
                !PathTouchesPoint(candidate.Path, secondClosingEndpoint, tolerance))
            {
                continue;
            }

            reachedCandidateIds.Add(candidate.Candidate.CandidateId);
            pendingCandidateIndexes.Enqueue(index);
        }

        while (pendingCandidateIndexes.Count > 0)
        {
            var reachedCandidate = candidates[pendingCandidateIndexes.Dequeue()];
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (reachedCandidateIds.Contains(candidate.Candidate.CandidateId) ||
                    !PathsConnect(reachedCandidate.Path, candidate.Path, tolerance))
                {
                    continue;
                }

                reachedCandidateIds.Add(candidate.Candidate.CandidateId);
                pendingCandidateIndexes.Enqueue(index);
            }
        }

        return reachedCandidateIds;
    }

    private static bool PathTouchesPoint(GeometryPathDto path, CadStretchPoint point, decimal tolerance)
        => path.Segments.Any(segment => IsPointOnSegment(point, segment, tolerance));

    private static bool PathsConnect(GeometryPathDto first, GeometryPathDto second, decimal tolerance)
        => first.Segments.Any(firstSegment =>
            second.Segments.Any(secondSegment => SegmentsConnect(firstSegment, secondSegment, tolerance)));

    private static bool SegmentsConnect(
        GeometrySegmentDto first,
        GeometrySegmentDto second,
        decimal tolerance)
    {
        var firstStart = new CadStretchPoint(first.StartX, first.StartY);
        var firstEnd = new CadStretchPoint(first.EndX, first.EndY);
        var secondStart = new CadStretchPoint(second.StartX, second.StartY);
        var secondEnd = new CadStretchPoint(second.EndX, second.EndY);
        return SegmentsProperlyIntersect(firstStart, firstEnd, secondStart, secondEnd) ||
               IsPointOnSegment(firstStart, second, tolerance) ||
               IsPointOnSegment(firstEnd, second, tolerance) ||
               IsPointOnSegment(secondStart, first, tolerance) ||
               IsPointOnSegment(secondEnd, first, tolerance);
    }

    private static bool SegmentsProperlyIntersect(
        CadStretchPoint firstStart,
        CadStretchPoint firstEnd,
        CadStretchPoint secondStart,
        CadStretchPoint secondEnd)
    {
        var firstStartSide = Cross(firstStart, firstEnd, secondStart);
        var firstEndSide = Cross(firstStart, firstEnd, secondEnd);
        var secondStartSide = Cross(secondStart, secondEnd, firstStart);
        var secondEndSide = Cross(secondStart, secondEnd, firstEnd);
        return double.IsFinite(firstStartSide) &&
               double.IsFinite(firstEndSide) &&
               double.IsFinite(secondStartSide) &&
               double.IsFinite(secondEndSide) &&
               HaveOppositeSigns(firstStartSide, firstEndSide) &&
               HaveOppositeSigns(secondStartSide, secondEndSide);
    }

    private static bool IsPointOnSegment(
        CadStretchPoint point,
        GeometrySegmentDto segment,
        decimal tolerance)
    {
        var startX = (double)segment.StartX;
        var startY = (double)segment.StartY;
        var deltaX = (double)segment.EndX - startX;
        var deltaY = (double)segment.EndY - startY;
        var lengthSquared = (deltaX * deltaX) + (deltaY * deltaY);
        if (!double.IsFinite(lengthSquared))
        {
            return false;
        }

        var offsetX = (double)point.X - startX;
        var offsetY = (double)point.Y - startY;
        var projection = lengthSquared <= double.Epsilon
            ? 0d
            : Math.Clamp(((offsetX * deltaX) + (offsetY * deltaY)) / lengthSquared, 0d, 1d);
        var distanceX = offsetX - (projection * deltaX);
        var distanceY = offsetY - (projection * deltaY);
        var distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
        var allowedDistance = (double)tolerance;
        return double.IsFinite(distanceSquared) &&
               distanceSquared <= allowedDistance * allowedDistance;
    }

    private static double Cross(CadStretchPoint start, CadStretchPoint end, CadStretchPoint point)
        => (((double)end.X - (double)start.X) * ((double)point.Y - (double)start.Y)) -
           (((double)end.Y - (double)start.Y) * ((double)point.X - (double)start.X));

    private static bool HaveOppositeSigns(double first, double second)
        => (first > 0d && second < 0d) || (first < 0d && second > 0d);

    private static decimal SignedClosingDistance(decimal coordinate, decimal cutCoordinate, string edge)
        => IsPositiveClosingEdge(edge)
            ? coordinate - cutCoordinate
            : cutCoordinate - coordinate;

    private static bool IsPositiveClosingEdge(string edge)
        => string.Equals(edge, "Right", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(edge, "Top", StringComparison.OrdinalIgnoreCase);

    private static double SegmentLength(GeometrySegmentDto segment)
    {
        var deltaX = (double)(segment.EndX - segment.StartX);
        var deltaY = (double)(segment.EndY - segment.StartY);
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    private static bool TryResolveBounds(
        IReadOnlyList<GeometryPathDto> paths,
        out AdjustmentRecipeBoundsDto bounds)
    {
        var points = paths
            .SelectMany(path => path.Segments)
            .SelectMany(segment => new[]
            {
                new CadStretchPoint(segment.StartX, segment.StartY),
                new CadStretchPoint(segment.EndX, segment.EndY)
            })
            .ToArray();
        if (points.Length == 0)
        {
            bounds = default!;
            return false;
        }

        bounds = new AdjustmentRecipeBoundsDto(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
        return true;
    }

    private static string NormalizeAxis(string axisTag)
        => IsWidth(axisTag) ? "Width" : "Height";

    private static string NormalizeEdge(string edge)
        => edge.Trim() switch
        {
            var value when value.Equals("Left", StringComparison.OrdinalIgnoreCase) => "Left",
            var value when value.Equals("Right", StringComparison.OrdinalIgnoreCase) => "Right",
            var value when value.Equals("Top", StringComparison.OrdinalIgnoreCase) => "Top",
            var value when value.Equals("Bottom", StringComparison.OrdinalIgnoreCase) => "Bottom",
            _ => edge.Trim()
        };

    private static bool IsWidth(string axisTag)
        => string.Equals(axisTag, "Width", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedAxisEdge(string axisTag, string edge)
        => IsWidth(axisTag)
            ? string.Equals(edge, "Left", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(edge, "Right", StringComparison.OrdinalIgnoreCase)
            : string.Equals(axisTag, "Height", StringComparison.OrdinalIgnoreCase) &&
              (string.Equals(edge, "Top", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(edge, "Bottom", StringComparison.OrdinalIgnoreCase));

    private static CadStretchRecipeCompilationResult Reject(string reason)
        => new(false, null, reason);

    private static CadStretchRecipeGroupCompilationResult RejectGroup(string reason)
        => new(false, [], reason);

    private static string GroupActionId(Guid groupId, int stationIndex)
        => string.Concat(
            groupId.ToString("D"),
            ":station:",
            stationIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private sealed record ResolvedTarget(
        WallCandidateDto Candidate,
        GeometryPathDto Path,
        GeometrySegmentDto Segment,
        decimal CutCoordinate,
        int ClosingVertexIndex);

    private sealed record ResolvedRigidMoveCandidate(
        WallCandidateDto Candidate,
        GeometryPathDto Path);

    private sealed record CompiledStation(
        int OriginalPairIndex,
        AdjustmentRecipeStretchActionDto Action);

}
