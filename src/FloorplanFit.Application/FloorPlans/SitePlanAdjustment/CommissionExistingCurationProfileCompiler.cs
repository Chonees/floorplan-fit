using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public enum CommissionExistingCurationAuxiliaryEntityKind
{
    Opening,
    Label,
    ProtectedEntity
}

public sealed record CommissionExistingCurationPinchGroupSelection(
    Guid PinchGroupId,
    string ClosingEdge);

public sealed record CommissionExistingCurationVariableSelection(
    string Id,
    string Name,
    HouseAdaptationAxis Axis,
    int Priority,
    IReadOnlyList<CommissionExistingCurationPinchGroupSelection>? PinchGroups);

public sealed record CommissionExistingCurationAuxiliaryEntityBinding(
    string SourceEntityRef,
    CommissionExistingCurationAuxiliaryEntityKind Kind,
    AdjustmentRecipeBoundsDto? SourceBounds,
    Guid? GeometryPathId,
    int? SegmentSortOrder,
    bool IsImmutableSize,
    bool IsProtected)
{
    public Guid? HostGeometryPathId { get; init; }

    public int? HostSegmentSortOrder { get; init; }
}

public sealed record CommissionExistingCurationProfileCompilationRequest(
    Guid FloorPlanVersionId,
    Guid PublishedCurationId,
    decimal SourceToMillimetersFactor,
    decimal CoordinateTolerance,
    IReadOnlyList<CommissionExistingCurationVariableSelection>? VariableSelections,
    IReadOnlyList<PinchGroupDto>? PinchGroups,
    IReadOnlyList<PinchMarkerDto>? Markers,
    IReadOnlyList<WallCandidateDto>? WallCandidates,
    IReadOnlyList<GeometryPathDto>? GeometryPaths,
    IReadOnlyList<CommissionExistingCurationAuxiliaryEntityBinding>? AuxiliaryEntityBindings);

public sealed record CommissionExistingCurationProfileCompilationResult(
    bool Succeeded,
    CommissionedHouseAdaptationProfile? Profile,
    string? RejectionReason);

public static class CommissionExistingCurationProfileCompiler
{
    public static CommissionExistingCurationProfileCompilationResult Compile(
        CommissionExistingCurationProfileCompilationRequest? request)
    {
        if (request is null)
        {
            return Reject("A commissioning compilation request is required.");
        }

        if (request.FloorPlanVersionId == Guid.Empty || request.PublishedCurationId == Guid.Empty)
        {
            return Reject("Commissioning requires non-empty floor-plan and published-curation identities.");
        }

        if (request.SourceToMillimetersFactor <= 0m)
        {
            return Reject("Commissioning source-to-millimeters factor must be positive.");
        }

        if (request.CoordinateTolerance < 0m)
        {
            return Reject("Commissioning coordinate tolerance cannot be negative.");
        }

        if (request.VariableSelections is null ||
            request.PinchGroups is null ||
            request.Markers is null ||
            request.WallCandidates is null ||
            request.GeometryPaths is null ||
            request.AuxiliaryEntityBindings is null)
        {
            return Reject("Commissioning requires explicit variables, pinch data, geometry, and auxiliary bindings.");
        }

        if (!TryValidateVariables(request.VariableSelections, out var variables, out var variableReason))
        {
            return Reject(variableReason);
        }

        if (!TryCanonicalizeCuration(
                request.PinchGroups,
                request.Markers,
                request.WallCandidates,
                request.GeometryPaths,
                out var curation,
                out var curationReason))
        {
            return Reject(curationReason);
        }

        if (!TryValidateSelections(variables, curation.PinchGroupsById, out var selectedVariables, out var selectionReason))
        {
            return Reject(selectionReason);
        }

        if (!TryValidateAuxiliaryBindings(
                request.AuxiliaryEntityBindings,
                curation.AcceptedWallCandidates,
                curation.GeometryPathsById,
                out var auxiliaries,
                out var immutableRefs,
                out var protectedRefs,
                out var auxiliaryReason))
        {
            return Reject(auxiliaryReason);
        }

        var commissionedVariables = new List<CommissionedAdaptationVariable>(selectedVariables.Count);
        var actionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variable in selectedVariables)
        {
            if (!TryCompileVariable(
                    variable,
                    request.SourceToMillimetersFactor,
                    request.CoordinateTolerance,
                    curation,
                    auxiliaries,
                    actionIds,
                    out var commissionedVariable,
                    out var compilationReason))
            {
                return Reject(compilationReason);
            }

            commissionedVariables.Add(commissionedVariable);
        }

        var profile = new CommissionedHouseAdaptationProfile(
            request.FloorPlanVersionId,
            request.PublishedCurationId,
            request.SourceToMillimetersFactor,
            commissionedVariables.ToArray(),
            immutableRefs,
            protectedRefs)
        {
            AuxiliaryEntityBindings = auxiliaries
        };
        var readiness = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);
        if (!readiness.IsReady)
        {
            var reasons = readiness.Reasons.Count == 0
                ? "The compiled profile did not satisfy commissioned readiness."
                : string.Join(" ", readiness.Reasons);
            return Reject($"Compiled commissioning profile is not ready: {reasons}");
        }

        return new CommissionExistingCurationProfileCompilationResult(true, profile, null);
    }

    private static bool TryValidateVariables(
        IReadOnlyList<CommissionExistingCurationVariableSelection> candidates,
        out IReadOnlyList<CommissionExistingCurationVariableSelection> variables,
        out string reason)
    {
        variables = [];
        if (candidates.Count != 2 || candidates.Any(candidate => candidate is null))
        {
            reason = "Commissioning requires exactly one Width variable and exactly one Depth variable.";
            return false;
        }

        if (candidates.Any(candidate =>
                string.IsNullOrWhiteSpace(candidate.Id) ||
                string.IsNullOrWhiteSpace(candidate.Name) ||
                candidate.Priority <= 0 ||
                !Enum.IsDefined(candidate.Axis) ||
                candidate.PinchGroups is null ||
                candidate.PinchGroups.Count == 0))
        {
            reason = "Every commissioned variable requires an id, name, valid axis, positive priority, and explicit pinch groups.";
            return false;
        }

        if (candidates.Select(candidate => candidate.Id).Distinct(StringComparer.Ordinal).Count() != candidates.Count)
        {
            reason = "Commissioned variable ids must be unique.";
            return false;
        }

        var width = candidates.Where(candidate => candidate.Axis == HouseAdaptationAxis.Width).ToArray();
        var depth = candidates.Where(candidate => candidate.Axis == HouseAdaptationAxis.Depth).ToArray();
        if (width.Length != 1 || depth.Length != 1)
        {
            reason = "Commissioning requires exactly one Width variable and exactly one Depth variable.";
            return false;
        }

        variables = [width[0], depth[0]];
        reason = string.Empty;
        return true;
    }

    private static bool TryCanonicalizeCuration(
        IReadOnlyList<PinchGroupDto> pinchGroups,
        IReadOnlyList<PinchMarkerDto> markers,
        IReadOnlyList<WallCandidateDto> wallCandidates,
        IReadOnlyList<GeometryPathDto> geometryPaths,
        out CanonicalCuration curation,
        out string reason)
    {
        curation = default!;
        if (pinchGroups.Any(group => group is null || group.PinchGroupId == Guid.Empty))
        {
            reason = "Current curation contains an invalid pinch group identity.";
            return false;
        }

        var duplicateGroup = pinchGroups
            .GroupBy(group => group.PinchGroupId)
            .FirstOrDefault(group => group.Count() != 1);
        if (duplicateGroup is not null)
        {
            reason = $"Current curation contains duplicate pinch group '{duplicateGroup.Key:D}'.";
            return false;
        }

        if (markers.Any(marker =>
                marker is null ||
                marker.PinchMarkerId == Guid.Empty ||
                marker.PinchGroupId == Guid.Empty ||
                marker.SourceCandidateId == Guid.Empty ||
                marker.GeometryPathId == Guid.Empty))
        {
            reason = "Current curation contains an invalid pinch marker binding.";
            return false;
        }

        var duplicateMarker = markers
            .GroupBy(marker => marker.PinchMarkerId)
            .FirstOrDefault(group => group.Count() != 1);
        if (duplicateMarker is not null)
        {
            reason = $"Current curation contains duplicate pinch marker '{duplicateMarker.Key:D}'.";
            return false;
        }

        if (wallCandidates.Any(candidate => candidate is null))
        {
            reason = "Current curation contains a null wall candidate.";
            return false;
        }

        var acceptedCandidates = wallCandidates
            .Where(candidate => string.Equals(candidate.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.SortOrder)
            .ThenBy(candidate => candidate.CandidateId)
            .ToArray();
        if (acceptedCandidates.Any(candidate =>
                candidate.CandidateId == Guid.Empty ||
                string.IsNullOrWhiteSpace(candidate.SourceEntityRef)))
        {
            reason = "Current accepted wall candidates contain an invalid identity or source reference.";
            return false;
        }

        var duplicateCandidateId = acceptedCandidates
            .GroupBy(candidate => candidate.CandidateId)
            .FirstOrDefault(group => group.Count() != 1);
        if (duplicateCandidateId is not null)
        {
            reason = $"Current curation contains duplicate accepted wall candidate '{duplicateCandidateId.Key:D}'.";
            return false;
        }

        var duplicateCandidateRef = acceptedCandidates
            .GroupBy(candidate => candidate.SourceEntityRef, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() != 1);
        if (duplicateCandidateRef is not null)
        {
            reason = $"Current curation contains duplicate accepted source reference '{duplicateCandidateRef.Key}'.";
            return false;
        }

        if (geometryPaths.Any(path =>
                path is null ||
                path.Id == Guid.Empty ||
                path.Segments is null ||
                path.Segments.Any(segment => segment is null || segment.GeometryPathId != path.Id)))
        {
            reason = "Current curation contains an invalid geometry path or segment binding.";
            return false;
        }

        var duplicatePath = geometryPaths
            .GroupBy(path => path.Id)
            .FirstOrDefault(group => group.Count() != 1);
        if (duplicatePath is not null)
        {
            reason = $"Current curation contains duplicate geometry path '{duplicatePath.Key:D}'.";
            return false;
        }

        if (geometryPaths.Any(path => path.Segments
                .GroupBy(segment => segment.SortOrder)
                .Any(group => group.Count() != 1)))
        {
            reason = "Current curation contains duplicate geometry segment sort orders.";
            return false;
        }

        var canonicalPaths = geometryPaths
            .OrderBy(path => path.Id)
            .Select(path => path with
            {
                Segments = path.Segments
                    .OrderBy(segment => segment.SortOrder)
                    .ToArray()
            })
            .ToArray();
        var canonicalMarkers = markers
            .OrderBy(marker => marker.PinchGroupId)
            .ThenBy(marker => marker.SortOrder)
            .ThenBy(marker => marker.PinchMarkerId)
            .ToArray();
        curation = new CanonicalCuration(
            pinchGroups.ToDictionary(group => group.PinchGroupId),
            canonicalMarkers,
            acceptedCandidates,
            canonicalPaths,
            canonicalPaths.ToDictionary(path => path.Id));
        reason = string.Empty;
        return true;
    }

    private static bool TryValidateSelections(
        IReadOnlyList<CommissionExistingCurationVariableSelection> variables,
        IReadOnlyDictionary<Guid, PinchGroupDto> pinchGroupsById,
        out IReadOnlyList<ValidatedVariable> selectedVariables,
        out string reason)
    {
        var selectedGroupIds = new HashSet<Guid>();
        var validatedVariables = new List<ValidatedVariable>(variables.Count);
        foreach (var variable in variables)
        {
            var axisTag = AxisTag(variable.Axis);
            var groups = new List<ValidatedGroupSelection>(variable.PinchGroups!.Count);
            foreach (var selection in variable.PinchGroups)
            {
                if (selection is null || selection.PinchGroupId == Guid.Empty)
                {
                    selectedVariables = [];
                    reason = $"Commissioned {variable.Axis} variable contains an invalid pinch group selection.";
                    return false;
                }

                if (!selectedGroupIds.Add(selection.PinchGroupId))
                {
                    selectedVariables = [];
                    reason = $"Commissioning contains duplicate pinch group selection '{selection.PinchGroupId:D}'.";
                    return false;
                }

                if (!pinchGroupsById.TryGetValue(selection.PinchGroupId, out var group))
                {
                    selectedVariables = [];
                    reason = $"Selected pinch group '{selection.PinchGroupId:D}' does not exist in the current curation.";
                    return false;
                }

                if (!string.Equals(group.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase))
                {
                    selectedVariables = [];
                    reason = $"Selected pinch group '{selection.PinchGroupId:D}' has wrong axis '{group.AxisTag}' for {variable.Axis}.";
                    return false;
                }

                if (!TryNormalizeEdge(variable.Axis, selection.ClosingEdge, out var edge))
                {
                    selectedVariables = [];
                    reason = $"Selected pinch group '{selection.PinchGroupId:D}' has unsupported {variable.Axis} closing edge '{selection.ClosingEdge}'.";
                    return false;
                }

                groups.Add(new ValidatedGroupSelection(group, edge));
            }

            validatedVariables.Add(new ValidatedVariable(
                variable,
                groups
                    .OrderBy(selection => selection.Group.SortOrder)
                    .ThenBy(selection => selection.Group.PinchGroupId)
                    .ThenBy(selection => selection.Edge, StringComparer.Ordinal)
                    .ToArray()));
        }

        selectedVariables = validatedVariables;
        reason = string.Empty;
        return true;
    }

    private static bool TryValidateAuxiliaryBindings(
        IReadOnlyList<CommissionExistingCurationAuxiliaryEntityBinding> candidates,
        IReadOnlyList<WallCandidateDto> acceptedWallCandidates,
        IReadOnlyDictionary<Guid, GeometryPathDto> geometryPathsById,
        out IReadOnlyList<CommissionExistingCurationAuxiliaryEntityBinding> auxiliaries,
        out IReadOnlyList<string> immutableRefs,
        out IReadOnlyList<string> protectedRefs,
        out string reason)
    {
        auxiliaries = [];
        immutableRefs = [];
        protectedRefs = [];
        if (candidates.Count == 0)
        {
            reason = "Commissioned auxiliary coverage is empty; explicitly bind every supported opening, protected, and rigid auxiliary entity.";
            return false;
        }

        var acceptedRefs = acceptedWallCandidates
            .Select(candidate => candidate.SourceEntityRef)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var acceptedPathIds = acceptedWallCandidates
            .Where(candidate => candidate.GeometryPathId.HasValue)
            .Select(candidate => candidate.GeometryPathId!.Value)
            .ToHashSet();
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pathIds = new HashSet<Guid>();

        foreach (var binding in candidates)
        {
            if (binding is null || string.IsNullOrWhiteSpace(binding.SourceEntityRef))
            {
                reason = "Every auxiliary entity requires a nonblank source reference.";
                return false;
            }

            if (!Enum.IsDefined(binding.Kind))
            {
                reason = $"Auxiliary entity '{binding.SourceEntityRef}' has an unsupported kind.";
                return false;
            }

            if (!refs.Add(binding.SourceEntityRef) || acceptedRefs.Contains(binding.SourceEntityRef))
            {
                reason = $"Auxiliary entity '{binding.SourceEntityRef}' duplicates an existing target/ref.";
                return false;
            }

            if (!IsValidBounds(binding.SourceBounds))
            {
                reason = $"Auxiliary entity '{binding.SourceEntityRef}' lacks valid source bounds.";
                return false;
            }

            if (binding.GeometryPathId == Guid.Empty ||
                (binding.SegmentSortOrder.HasValue && !binding.GeometryPathId.HasValue))
            {
                reason = $"Auxiliary entity '{binding.SourceEntityRef}' has an invalid optional geometry binding.";
                return false;
            }

            if (binding.HostGeometryPathId == Guid.Empty ||
                binding.HostGeometryPathId.HasValue != binding.HostSegmentSortOrder.HasValue)
            {
                reason = $"Auxiliary entity '{binding.SourceEntityRef}' has incomplete structural wall host evidence.";
                return false;
            }

            if (binding.Kind != CommissionExistingCurationAuxiliaryEntityKind.Opening &&
                binding.HostGeometryPathId.HasValue)
            {
                reason = $"Non-opening auxiliary entity '{binding.SourceEntityRef}' cannot carry a structural wall host.";
                return false;
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.Opening &&
                !binding.IsImmutableSize)
            {
                reason = $"Opening '{binding.SourceEntityRef}' must use immutable-size semantics.";
                return false;
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.Opening &&
                (!binding.GeometryPathId.HasValue || !binding.SegmentSortOrder.HasValue))
            {
                reason = $"Opening '{binding.SourceEntityRef}' requires its own complete preview geometry path and segment binding.";
                return false;
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.Opening &&
                (!binding.HostGeometryPathId.HasValue || !binding.HostSegmentSortOrder.HasValue))
            {
                reason = $"Opening '{binding.SourceEntityRef}' requires exactly one accepted structural wall segment host.";
                return false;
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.ProtectedEntity &&
                !binding.IsProtected)
            {
                reason = $"Protected auxiliary entity '{binding.SourceEntityRef}' must retain protected semantics.";
                return false;
            }

            if (binding.GeometryPathId.HasValue)
            {
                var pathId = binding.GeometryPathId.Value;
                if (!geometryPathsById.TryGetValue(pathId, out var path))
                {
                    reason = $"Auxiliary entity '{binding.SourceEntityRef}' references missing geometry path '{pathId:D}'.";
                    return false;
                }

                if (!pathIds.Add(pathId))
                {
                    reason = $"Auxiliary entity '{binding.SourceEntityRef}' duplicates another auxiliary preview geometry binding.";
                    return false;
                }

                if (acceptedPathIds.Contains(pathId))
                {
                    reason = $"Auxiliary entity '{binding.SourceEntityRef}' uses accepted structural wall path '{pathId:D}' as its own preview geometry identity.";
                    return false;
                }

                if (binding.SegmentSortOrder.HasValue &&
                    path.Segments.Count(segment => segment.SortOrder == binding.SegmentSortOrder.Value) != 1)
                {
                    reason = $"Auxiliary entity '{binding.SourceEntityRef}' references a missing or duplicate geometry segment.";
                    return false;
                }
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.Opening)
            {
                var hostPathId = binding.HostGeometryPathId!.Value;
                var hostSegmentSortOrder = binding.HostSegmentSortOrder!.Value;
                if (binding.GeometryPathId == hostPathId)
                {
                    reason = $"Opening '{binding.SourceEntityRef}' conflates its own preview geometry path with structural wall host '{hostPathId:D}:{hostSegmentSortOrder}'.";
                    return false;
                }

                var acceptedHosts = acceptedWallCandidates
                    .Where(candidate => candidate.GeometryPathId == hostPathId)
                    .ToArray();
                if (acceptedHosts.Length == 0)
                {
                    reason = $"Opening '{binding.SourceEntityRef}' host '{hostPathId:D}:{hostSegmentSortOrder}' is not an accepted structural wall segment.";
                    return false;
                }

                if (acceptedHosts.Length != 1)
                {
                    reason = $"Opening '{binding.SourceEntityRef}' host path '{hostPathId:D}' has duplicate or ambiguous accepted structural wall candidates.";
                    return false;
                }

                if (!geometryPathsById.TryGetValue(hostPathId, out var hostPath) ||
                    hostPath.Segments.Count(segment => segment.SortOrder == hostSegmentSortOrder) != 1)
                {
                    reason = $"Opening '{binding.SourceEntityRef}' host '{hostPathId:D}:{hostSegmentSortOrder}' is stale or does not identify exactly one structural wall segment.";
                    return false;
                }
            }
        }

        var ordered = candidates
            .OrderBy(binding => binding.SourceEntityRef, StringComparer.Ordinal)
            .ThenBy(binding => binding.Kind)
            .ThenBy(binding => binding.GeometryPathId)
            .ThenBy(binding => binding.SegmentSortOrder)
            .ThenBy(binding => binding.HostGeometryPathId)
            .ThenBy(binding => binding.HostSegmentSortOrder)
            .ToArray();
        auxiliaries = ordered;
        immutableRefs = ordered
            .Where(binding => binding.IsImmutableSize)
            .Select(binding => binding.SourceEntityRef)
            .OrderBy(entityRef => entityRef, StringComparer.Ordinal)
            .ToArray();
        protectedRefs = ordered
            .Where(binding => binding.IsProtected)
            .Select(binding => binding.SourceEntityRef)
            .OrderBy(entityRef => entityRef, StringComparer.Ordinal)
            .ToArray();
        reason = string.Empty;
        return true;
    }

    private static bool TryCompileVariable(
        ValidatedVariable variable,
        decimal sourceToMillimetersFactor,
        decimal coordinateTolerance,
        CanonicalCuration curation,
        IReadOnlyList<CommissionExistingCurationAuxiliaryEntityBinding> auxiliaries,
        ISet<string> actionIds,
        out CommissionedAdaptationVariable commissionedVariable,
        out string reason)
    {
        commissionedVariable = default!;
        var actions = new List<AdjustmentRecipeStretchActionDto>();
        var expectedAxisTag = AxisTag(variable.Selection.Axis);
        foreach (var selection in variable.Groups)
        {
            var groupMarkers = curation.Markers
                .Where(marker => marker.PinchGroupId == selection.Group.PinchGroupId)
                .ToArray();
            if (groupMarkers.Any(marker =>
                    !string.Equals(marker.AxisTag, expectedAxisTag, StringComparison.OrdinalIgnoreCase)))
            {
                reason = $"Selected pinch group '{selection.Group.PinchGroupId:D}' contains wrong-axis markers.";
                return false;
            }

            if (groupMarkers.Length < 2 || groupMarkers.Length % 2 != 0)
            {
                reason = $"Selected pinch group '{selection.Group.PinchGroupId:D}' requires an even marker count of at least two.";
                return false;
            }

            if (!TryMaximumGroupCapacity(
                    groupMarkers,
                    sourceToMillimetersFactor,
                    out var maximumCapacity,
                    out var capacityReason))
            {
                reason = $"Selected pinch group '{selection.Group.PinchGroupId:D}' is invalid: {capacityReason}";
                return false;
            }

            var compilation = CadStretchRecipeCompiler.CompileGroup(new CadStretchRecipeCompilationRequest(
                selection.Group.PinchGroupId,
                expectedAxisTag,
                selection.Edge,
                maximumCapacity,
                sourceToMillimetersFactor,
                coordinateTolerance,
                groupMarkers,
                curation.AcceptedWallCandidates,
                curation.GeometryPaths));
            if (!compilation.Succeeded || compilation.Actions is null || compilation.Actions.Count == 0)
            {
                reason = $"Selected pinch group '{selection.Group.PinchGroupId:D}' could not compile at maximum capacity: {compilation.RejectionReason ?? "Unknown rejection."}";
                return false;
            }

            decimal compiledCapacity = 0m;
            foreach (var action in compilation.Actions)
            {
                if (!TryValidateMaximumAction(
                        action,
                        expectedAxisTag,
                        selection.Edge,
                        coordinateTolerance,
                        actionIds,
                        out var actionReason))
                {
                    reason = $"Selected pinch group '{selection.Group.PinchGroupId:D}' emitted an invalid action: {actionReason}";
                    return false;
                }

                try
                {
                    compiledCapacity = checked(compiledCapacity + action.MaxDeltaSourceUnits);
                }
                catch (OverflowException)
                {
                    reason = $"Selected pinch group '{selection.Group.PinchGroupId:D}' compiled capacity overflowed.";
                    return false;
                }

                if (!TryAddAuxiliaryRoles(action, auxiliaries, out var augmented, out var auxiliaryReason))
                {
                    reason = $"Action '{action.ActionId}' rejected an auxiliary entity: {auxiliaryReason}";
                    return false;
                }

                actions.Add(augmented);
            }

            if (compiledCapacity != maximumCapacity)
            {
                reason = $"Selected pinch group '{selection.Group.PinchGroupId:D}' did not compile its exact maximum paired-wall capacity.";
                return false;
            }
        }

        commissionedVariable = new CommissionedAdaptationVariable(
            variable.Selection.Id,
            variable.Selection.Name,
            variable.Selection.Axis,
            variable.Selection.Priority,
            actions.ToArray());
        reason = string.Empty;
        return true;
    }

    private static bool TryMaximumGroupCapacity(
        IReadOnlyList<PinchMarkerDto> markers,
        decimal sourceToMillimetersFactor,
        out decimal capacity,
        out string reason)
    {
        capacity = 0m;
        try
        {
            for (var index = 0; index < markers.Count; index += 2)
            {
                var first = markers[index];
                var second = markers[index + 1];
                if (first.GeometryPathId == second.GeometryPathId)
                {
                    reason = $"Marker pair {index / 2} requires two distinct wall paths.";
                    return false;
                }

                var pairCapacity = Math.Min(
                    first.MaxTrimMm / sourceToMillimetersFactor,
                    second.MaxTrimMm / sourceToMillimetersFactor);
                if (pairCapacity <= 0m)
                {
                    reason = $"Marker pair {index / 2} has no positive paired-wall capacity.";
                    return false;
                }

                capacity = checked(capacity + pairCapacity);
            }
        }
        catch (OverflowException)
        {
            reason = "Maximum paired-wall capacity cannot be represented safely.";
            return false;
        }

        reason = string.Empty;
        return capacity > 0m;
    }

    private static bool TryValidateMaximumAction(
        AdjustmentRecipeStretchActionDto? action,
        string expectedAxisTag,
        string expectedEdge,
        decimal expectedTolerance,
        ISet<string> actionIds,
        out string reason)
    {
        if (action is null || string.IsNullOrWhiteSpace(action.ActionId) || !actionIds.Add(action.ActionId))
        {
            reason = "Compiled action ids must be nonblank and unique.";
            return false;
        }

        if (!string.Equals(action.AxisTag, expectedAxisTag, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(action.Edge, expectedEdge, StringComparison.OrdinalIgnoreCase) ||
            action.CoordinateTolerance != expectedTolerance)
        {
            reason = "Compiled action axis, edge, or tolerance changed unexpectedly.";
            return false;
        }

        if (action.MaxDeltaSourceUnits <= 0m || action.DeltaSourceUnits != action.MaxDeltaSourceUnits)
        {
            reason = "Compiled action was not emitted at its maximum paired-wall capacity.";
            return false;
        }

        if (action.TargetSpans is null || action.CanonicalEntityRoles is null || action.CanonicalSourceBounds is null)
        {
            reason = "Compiled action is missing targets, source bounds, or canonical roles.";
            return false;
        }

        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in action.CanonicalEntityRoles)
        {
            if (role is null ||
                string.IsNullOrWhiteSpace(role.EntityRef) ||
                !IsSupportedRole(role.Role) ||
                role.VertexIndices is null ||
                !role.GeometryPathId.HasValue ||
                role.GeometryPathId.Value == Guid.Empty ||
                !refs.Add(role.EntityRef))
            {
                reason = "Compiled action contains duplicate, invalid, or unsupported canonical roles.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryAddAuxiliaryRoles(
        AdjustmentRecipeStretchActionDto action,
        IReadOnlyList<CommissionExistingCurationAuxiliaryEntityBinding> auxiliaries,
        out AdjustmentRecipeStretchActionDto augmented,
        out string reason)
    {
        augmented = default!;
        var roles = action.CanonicalEntityRoles.ToList();
        var representedRefs = action.TargetSpans
            .Select(target => target.SourceEntityRef)
            .Concat(roles.Select(role => role.EntityRef))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var representedPathIds = action.TargetSpans
            .Select(target => target.GeometryPathId)
            .Concat(roles.Where(role => role.GeometryPathId.HasValue).Select(role => role.GeometryPathId!.Value))
            .ToHashSet();

        foreach (var auxiliary in auxiliaries)
        {
            if (!representedRefs.Add(auxiliary.SourceEntityRef) ||
                (auxiliary.GeometryPathId.HasValue && !representedPathIds.Add(auxiliary.GeometryPathId.Value)))
            {
                reason = $"Auxiliary entity '{auxiliary.SourceEntityRef}' duplicates an action target/ref.";
                return false;
            }

            if (!TryClassifyAuxiliary(action, auxiliary, out var role, out var classificationReason))
            {
                reason = classificationReason;
                return false;
            }

            roles.Add(new AdjustmentRecipeEntityRoleDto(
                auxiliary.SourceEntityRef,
                auxiliary.GeometryPathId,
                auxiliary.SegmentSortOrder,
                role,
                [],
                "Commissioned from explicit auxiliary source bounds.")
            {
                HostGeometryPathId = auxiliary.HostGeometryPathId,
                HostSegmentSortOrder = auxiliary.HostSegmentSortOrder
            });
        }

        augmented = action with { CanonicalEntityRoles = roles.ToArray() };
        reason = string.Empty;
        return true;
    }

    private static bool TryClassifyAuxiliary(
        AdjustmentRecipeStretchActionDto action,
        CommissionExistingCurationAuxiliaryEntityBinding auxiliary,
        out string role,
        out string reason)
    {
        role = string.Empty;
        var bounds = auxiliary.SourceBounds!;
        var isWidth = string.Equals(action.AxisTag, "Width", StringComparison.OrdinalIgnoreCase);
        var minimum = isWidth ? bounds.MinX : bounds.MinY;
        var maximum = isWidth ? bounds.MaxX : bounds.MaxY;
        decimal fixedBoundary;
        decimal movingBoundary;
        try
        {
            fixedBoundary = checked(action.CutCoordinate - action.CoordinateTolerance);
            movingBoundary = checked(action.CutCoordinate + action.CoordinateTolerance);
        }
        catch (OverflowException)
        {
            reason = $"Auxiliary entity '{auxiliary.SourceEntityRef}' cannot be classified against the action cut safely.";
            return false;
        }

        var positiveClosingSide = string.Equals(action.Edge, "Right", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(action.Edge, "Top", StringComparison.OrdinalIgnoreCase);
        var whollyFixed = positiveClosingSide ? maximum < fixedBoundary : minimum > movingBoundary;
        var whollyMoving = positiveClosingSide ? minimum > movingBoundary : maximum < fixedBoundary;
        if (whollyFixed || whollyMoving)
        {
            role = whollyMoving ? "RigidMove" : "Fixed";
            reason = string.Empty;
            return true;
        }

        var crossesCut = minimum < fixedBoundary && maximum > movingBoundary;
        reason = crossesCut
            ? $"Auxiliary entity '{auxiliary.SourceEntityRef}' would stretch across action cut {action.CutCoordinate}."
            : $"Auxiliary entity '{auxiliary.SourceEntityRef}' touches or overlaps action cut {action.CutCoordinate} ambiguously.";
        return false;
    }

    private static bool IsValidBounds(AdjustmentRecipeBoundsDto? bounds)
        => bounds is not null &&
           bounds.MinX <= bounds.MaxX &&
           bounds.MinY <= bounds.MaxY &&
           (bounds.MinX < bounds.MaxX || bounds.MinY < bounds.MaxY);

    private static bool TryNormalizeEdge(
        HouseAdaptationAxis axis,
        string? candidate,
        out string edge)
    {
        edge = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var value = candidate.Trim();
        if (axis == HouseAdaptationAxis.Width)
        {
            if (value.Equals("Left", StringComparison.OrdinalIgnoreCase))
            {
                edge = "Left";
                return true;
            }

            if (value.Equals("Right", StringComparison.OrdinalIgnoreCase))
            {
                edge = "Right";
                return true;
            }

            return false;
        }

        if (value.Equals("Top", StringComparison.OrdinalIgnoreCase))
        {
            edge = "Top";
            return true;
        }

        if (value.Equals("Bottom", StringComparison.OrdinalIgnoreCase))
        {
            edge = "Bottom";
            return true;
        }

        return false;
    }

    private static string AxisTag(HouseAdaptationAxis axis)
        => axis == HouseAdaptationAxis.Width ? "Width" : "Height";

    private static bool IsSupportedRole(string? role)
        => string.Equals(role, "Stretch", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(role, "RigidMove", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(role, "Fixed", StringComparison.OrdinalIgnoreCase);

    private static CommissionExistingCurationProfileCompilationResult Reject(string reason)
        => new(false, null, reason);

    private sealed record CanonicalCuration(
        IReadOnlyDictionary<Guid, PinchGroupDto> PinchGroupsById,
        IReadOnlyList<PinchMarkerDto> Markers,
        IReadOnlyList<WallCandidateDto> AcceptedWallCandidates,
        IReadOnlyList<GeometryPathDto> GeometryPaths,
        IReadOnlyDictionary<Guid, GeometryPathDto> GeometryPathsById);

    private sealed record ValidatedVariable(
        CommissionExistingCurationVariableSelection Selection,
        IReadOnlyList<ValidatedGroupSelection> Groups);

    private sealed record ValidatedGroupSelection(
        PinchGroupDto Group,
        string Edge);
}
