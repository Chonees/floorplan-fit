using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public enum HouseAdaptationAxis
{
    Width,
    Depth
}

public sealed record CommissionedHouseAdaptationProfile(
    Guid FloorPlanVersionId,
    Guid PublishedCurationId,
    decimal SourceToMillimetersFactor,
    IReadOnlyList<CommissionedAdaptationVariable> Variables,
    IReadOnlyList<string> ImmutableSizeEntityRefs,
    IReadOnlyList<string> ProtectedEntityRefs)
{
    public IReadOnlyList<CommissionExistingCurationAuxiliaryEntityBinding> AuxiliaryEntityBindings { get; init; } = [];
}

public sealed record CommissionedAdaptationVariable(
    string Id,
    string Name,
    HouseAdaptationAxis Axis,
    int Priority,
    IReadOnlyList<AdjustmentRecipeStretchActionDto> ActionTemplates);

public sealed record CommissionedHouseFitRequest(
    decimal OriginalWidthInches,
    decimal OriginalDepthInches,
    decimal BuildableWidthInches,
    decimal BuildableDepthInches);

public sealed record CommissionedHouseFitResult(
    bool Succeeded,
    bool IsRigidPlacement,
    decimal WidthReductionInches,
    decimal DepthReductionInches,
    IReadOnlyList<AdjustmentRecipeStretchActionDto> Actions,
    string RejectionReason);

public sealed record CommissionedHouseAdaptationReadinessResult(
    bool IsReady,
    decimal WidthCapacityInches,
    decimal DepthCapacityInches,
    IReadOnlyList<string> Reasons);

public static class CommissionedHouseAdaptationProfileReadiness
{
    private const decimal MillimetersPerInch = 25.4m;

    public static CommissionedHouseAdaptationReadinessResult Evaluate(
        CommissionedHouseAdaptationProfile? profile)
    {
        if (profile is null)
        {
            return new(false, 0m, 0m, ["No commissioned house adaptation profile exists."]);
        }

        if (!CommissionedHouseFitPlanner.TryValidateProfile(profile, out var validationReason))
        {
            return new(false, 0m, 0m, [validationReason]);
        }

        if (!TryValidateAuxiliaryCoverage(profile, out var coverageReason))
        {
            return new(false, 0m, 0m, [coverageReason]);
        }

        try
        {
            var widthCapacity = CapacityInches(profile, HouseAdaptationAxis.Width);
            var depthCapacity = CapacityInches(profile, HouseAdaptationAxis.Depth);
            var reasons = new List<string>(2);
            if (widthCapacity <= 0m)
            {
                reasons.Add("Width commissioned adaptation variable is missing or has no capacity.");
            }

            if (depthCapacity <= 0m)
            {
                reasons.Add("Depth commissioned adaptation variable is missing or has no capacity.");
            }

            return new(reasons.Count == 0, widthCapacity, depthCapacity, reasons);
        }
        catch (OverflowException)
        {
            return new(false, 0m, 0m, ["Commissioned adaptation capacity cannot be represented safely."]);
        }
    }

    private static decimal CapacityInches(
        CommissionedHouseAdaptationProfile profile,
        HouseAdaptationAxis axis)
    {
        var sourceCapacity = profile.Variables
            .Where(variable => variable.Axis == axis)
            .SelectMany(variable => variable.ActionTemplates)
            .Aggregate(0m, (total, action) => checked(total + action.MaxDeltaSourceUnits));

        return checked(sourceCapacity * profile.SourceToMillimetersFactor / MillimetersPerInch);
    }

    internal static bool TryValidateAuxiliaryCoverage(
        CommissionedHouseAdaptationProfile profile,
        out string reason)
    {
        var bindings = profile.AuxiliaryEntityBindings;
        if (bindings is null || bindings.Count == 0)
        {
            reason = "Commissioned auxiliary coverage is empty; bind every supported opening, protected, and rigid auxiliary entity before marking the house Auto-fit ready.";
            return false;
        }

        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in bindings)
        {
            if (binding is null || string.IsNullOrWhiteSpace(binding.SourceEntityRef))
            {
                reason = "Commissioned auxiliary coverage contains an incomplete source reference.";
                return false;
            }

            if (!refs.Add(binding.SourceEntityRef))
            {
                reason = $"Commissioned auxiliary coverage contains duplicate entity '{binding.SourceEntityRef}'.";
                return false;
            }

            if (!Enum.IsDefined(binding.Kind) ||
                !IsValidAuxiliaryBounds(binding.SourceBounds) ||
                binding.GeometryPathId == Guid.Empty ||
                (binding.SegmentSortOrder.HasValue && !binding.GeometryPathId.HasValue))
            {
                reason = $"Auxiliary entity '{binding.SourceEntityRef}' has incomplete commissioning evidence.";
                return false;
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.Opening &&
                !binding.IsImmutableSize)
            {
                reason = $"Opening '{binding.SourceEntityRef}' must be commissioned with immutable-size semantics.";
                return false;
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.Opening &&
                (!binding.GeometryPathId.HasValue || !binding.SegmentSortOrder.HasValue))
            {
                reason = $"Opening '{binding.SourceEntityRef}' requires a complete own-geometry path and segment binding.";
                return false;
            }

            if (binding.HostGeometryPathId == Guid.Empty ||
                binding.HostGeometryPathId.HasValue != binding.HostSegmentSortOrder.HasValue)
            {
                reason = $"Auxiliary entity '{binding.SourceEntityRef}' has incomplete structural wall host evidence.";
                return false;
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.Opening &&
                (!binding.HostGeometryPathId.HasValue || !binding.HostSegmentSortOrder.HasValue))
            {
                reason = $"Opening '{binding.SourceEntityRef}' requires exactly one accepted structural wall segment host.";
                return false;
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.Opening &&
                binding.GeometryPathId == binding.HostGeometryPathId)
            {
                reason = $"Opening '{binding.SourceEntityRef}' conflates its own geometry with its structural wall host.";
                return false;
            }

            if (binding.Kind != CommissionExistingCurationAuxiliaryEntityKind.Opening &&
                binding.HostGeometryPathId.HasValue)
            {
                reason = $"Non-opening auxiliary entity '{binding.SourceEntityRef}' cannot carry a structural wall host.";
                return false;
            }

            if (binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.ProtectedEntity &&
                !binding.IsProtected)
            {
                reason = $"Protected auxiliary entity '{binding.SourceEntityRef}' must retain protected semantics.";
                return false;
            }
        }

        var expectedImmutable = bindings
            .Where(binding => binding.IsImmutableSize)
            .Select(binding => binding.SourceEntityRef)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedProtected = bindings
            .Where(binding => binding.IsProtected)
            .Select(binding => binding.SourceEntityRef)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!expectedImmutable.SetEquals(profile.ImmutableSizeEntityRefs))
        {
            reason = $"Commissioned auxiliary coverage contradicts the immutable-size entity set. Expected [{string.Join(", ", expectedImmutable.OrderBy(entityRef => entityRef, StringComparer.Ordinal))}], actual [{string.Join(", ", profile.ImmutableSizeEntityRefs.OrderBy(entityRef => entityRef, StringComparer.Ordinal))}].";
            return false;
        }

        if (!expectedProtected.SetEquals(profile.ProtectedEntityRefs))
        {
            reason = $"Commissioned auxiliary coverage contradicts the protected entity set. Expected [{string.Join(", ", expectedProtected.OrderBy(entityRef => entityRef, StringComparer.Ordinal))}], actual [{string.Join(", ", profile.ProtectedEntityRefs.OrderBy(entityRef => entityRef, StringComparer.Ordinal))}].";
            return false;
        }

        var actions = profile.Variables
            .SelectMany(variable => variable.ActionTemplates)
            .ToArray();
        var structuralRoles = actions
            .SelectMany(action => action.CanonicalEntityRoles)
            .Where(role => !refs.Contains(role.EntityRef))
            .ToArray();
        var structuralRoleWithHostMetadata = structuralRoles.FirstOrDefault(role =>
            role.HostGeometryPathId.HasValue || role.HostSegmentSortOrder.HasValue);
        if (structuralRoleWithHostMetadata is not null)
        {
            reason = $"Structural wall role '{structuralRoleWithHostMetadata.EntityRef}' cannot carry opening-host metadata.";
            return false;
        }

        foreach (var opening in bindings.Where(binding =>
                     binding.Kind == CommissionExistingCurationAuxiliaryEntityKind.Opening))
        {
            if (structuralRoles.Any(role => role.GeometryPathId == opening.GeometryPathId))
            {
                reason = $"Opening '{opening.SourceEntityRef}' uses an accepted structural wall path as its own geometry identity.";
                return false;
            }

            var hostPathId = opening.HostGeometryPathId!.Value;
            var hostSegmentSortOrder = opening.HostSegmentSortOrder!.Value;
            var hostCandidates = structuralRoles
                .Where(role => role.GeometryPathId == hostPathId)
                .GroupBy(role => role.EntityRef, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.ToArray())
                .ToArray();
            if (hostCandidates.Length == 0)
            {
                reason = $"Opening '{opening.SourceEntityRef}' host '{hostPathId:D}:{hostSegmentSortOrder}' is not an accepted structural wall.";
                return false;
            }

            if (hostCandidates.Length != 1)
            {
                reason = $"Opening '{opening.SourceEntityRef}' host '{hostPathId:D}:{hostSegmentSortOrder}' resolves to {hostCandidates.Length} duplicate or ambiguous structural walls.";
                return false;
            }

            var explicitHostSegments = hostCandidates[0]
                .Where(role => role.SegmentSortOrder.HasValue)
                .Select(role => role.SegmentSortOrder!.Value)
                .Distinct()
                .ToArray();
            if (explicitHostSegments.Length > 0 && !explicitHostSegments.Contains(hostSegmentSortOrder))
            {
                reason = $"Opening '{opening.SourceEntityRef}' host segment '{hostPathId:D}:{hostSegmentSortOrder}' is stale.";
                return false;
            }
        }

        foreach (var action in actions)
        {
            var untrackedSourceOnlyRole = action.CanonicalEntityRoles.FirstOrDefault(role =>
                !role.GeometryPathId.HasValue &&
                !role.SegmentSortOrder.HasValue &&
                role.VertexIndices.Count == 0 &&
                !refs.Contains(role.EntityRef));
            if (untrackedSourceOnlyRole is not null)
            {
                reason = $"Source-only auxiliary role '{untrackedSourceOnlyRole.EntityRef}' in action '{action.ActionId}' is not declared by commissioned auxiliary coverage.";
                return false;
            }

            foreach (var binding in bindings)
            {
                var roles = action.CanonicalEntityRoles
                    .Where(role => string.Equals(
                        role.EntityRef,
                        binding.SourceEntityRef,
                        StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (roles.Length == 0)
                {
                    reason = $"Auxiliary entity '{binding.SourceEntityRef}' is missing its role in action '{action.ActionId}'.";
                    return false;
                }

                if (roles.Length != 1)
                {
                    reason = $"Auxiliary entity '{binding.SourceEntityRef}' has duplicate roles in action '{action.ActionId}'.";
                    return false;
                }

                var role = roles[0];
                if (string.Equals(role.Role, "Stretch", StringComparison.OrdinalIgnoreCase))
                {
                    reason = $"Auxiliary entity '{binding.SourceEntityRef}' cannot deform in action '{action.ActionId}'.";
                    return false;
                }

                if (role.GeometryPathId != binding.GeometryPathId ||
                    role.SegmentSortOrder != binding.SegmentSortOrder ||
                    role.HostGeometryPathId != binding.HostGeometryPathId ||
                    role.HostSegmentSortOrder != binding.HostSegmentSortOrder ||
                    role.VertexIndices.Count != 0)
                {
                    reason = $"Auxiliary entity '{binding.SourceEntityRef}' has a stale host binding in action '{action.ActionId}'.";
                    return false;
                }
            }
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsValidAuxiliaryBounds(AdjustmentRecipeBoundsDto? bounds)
        => bounds is not null &&
           bounds.MinX <= bounds.MaxX &&
           bounds.MinY <= bounds.MaxY &&
           (bounds.MinX < bounds.MaxX || bounds.MinY < bounds.MaxY);
}

public sealed class CommissionedHouseFitPlanner
{
    private const decimal MillimetersPerInch = 25.4m;
    private readonly CommissionedHouseAdaptationProfile profile;

    public CommissionedHouseFitPlanner(CommissionedHouseAdaptationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        this.profile = profile;
    }

    public CommissionedHouseFitResult Plan(CommissionedHouseFitRequest request)
    {
        if (request is null ||
            request.OriginalWidthInches <= 0m ||
            request.OriginalDepthInches <= 0m ||
            request.BuildableWidthInches <= 0m ||
            request.BuildableDepthInches <= 0m)
        {
            return Reject(0m, 0m, "Original and buildable dimensions must be positive.");
        }

        var widthDeficit = Math.Max(0m, request.OriginalWidthInches - request.BuildableWidthInches);
        var depthDeficit = Math.Max(0m, request.OriginalDepthInches - request.BuildableDepthInches);
        if (!TryValidateProfile(profile, out var validationReason))
        {
            return Reject(widthDeficit, depthDeficit, validationReason);
        }

        var requiresAuxiliaryValidation = profile.AuxiliaryEntityBindings.Count > 0 ||
                                          profile.ImmutableSizeEntityRefs.Count > 0 ||
                                          profile.ProtectedEntityRefs.Count > 0 ||
                                          profile.Variables
                                              .SelectMany(variable => variable.ActionTemplates)
                                              .SelectMany(action => action.CanonicalEntityRoles)
                                              .Any(role =>
                                                  !role.GeometryPathId.HasValue &&
                                                  !role.SegmentSortOrder.HasValue &&
                                                  role.VertexIndices.Count == 0);
        if (requiresAuxiliaryValidation &&
            !CommissionedHouseAdaptationProfileReadiness.TryValidateAuxiliaryCoverage(
                profile,
                out var auxiliaryReason))
        {
            return Reject(widthDeficit, depthDeficit, auxiliaryReason);
        }

        if (widthDeficit == 0m && depthDeficit == 0m)
        {
            return new(true, true, 0m, 0m, [], string.Empty);
        }

        decimal widthSourceUnits;
        decimal depthSourceUnits;
        try
        {
            widthSourceUnits = checked(widthDeficit * MillimetersPerInch / profile.SourceToMillimetersFactor);
            depthSourceUnits = checked(depthDeficit * MillimetersPerInch / profile.SourceToMillimetersFactor);
        }
        catch (OverflowException)
        {
            return Reject(widthDeficit, depthDeficit, "Required reduction cannot be represented in source units.");
        }

        var actions = new List<AdjustmentRecipeStretchActionDto>();
        if (!TryAllocate(HouseAdaptationAxis.Width, widthSourceUnits, actions, out var widthReason))
        {
            return Reject(widthDeficit, depthDeficit, widthReason);
        }

        if (!TryAllocate(HouseAdaptationAxis.Depth, depthSourceUnits, actions, out var depthReason))
        {
            return Reject(widthDeficit, depthDeficit, depthReason);
        }

        return new(true, false, widthDeficit, depthDeficit, actions.ToArray(), string.Empty);
    }

    internal static bool TryValidateProfile(
        CommissionedHouseAdaptationProfile profile,
        out string reason)
    {
        if (profile.FloorPlanVersionId == Guid.Empty)
        {
            reason = "The commissioned adaptation profile is incomplete or invalid.";
            return false;
        }

        if (profile.PublishedCurationId == Guid.Empty)
        {
            reason = "The commissioned adaptation profile requires a published curation id.";
            return false;
        }

        if (profile.SourceToMillimetersFactor <= 0m ||
            profile.Variables is null ||
            profile.ImmutableSizeEntityRefs is null ||
            profile.ProtectedEntityRefs is null ||
            profile.AuxiliaryEntityBindings is null)
        {
            reason = "The commissioned adaptation profile is incomplete or invalid.";
            return false;
        }

        if (!TryEntitySet(profile.ImmutableSizeEntityRefs, "immutable-size", out var immutable, out reason) ||
            !TryEntitySet(profile.ProtectedEntityRefs, "protected", out var protectedEntities, out reason))
        {
            return false;
        }

        var variableIds = new HashSet<string>(StringComparer.Ordinal);
        var actionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variable in profile.Variables)
        {
            if (variable is null || string.IsNullOrWhiteSpace(variable.Id) || !variableIds.Add(variable.Id))
            {
                reason = "Commissioned adaptation variable ids must be nonblank and unique.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(variable.Name) ||
                variable.Priority <= 0 ||
                !Enum.IsDefined(variable.Axis) ||
                variable.ActionTemplates is null)
            {
                reason = $"Commissioned variable '{variable.Id}' requires a name, positive priority, valid axis, and action templates.";
                return false;
            }

            foreach (var action in variable.ActionTemplates)
            {
                if (!TryValidateAction(variable, action, actionIds, immutable, protectedEntities, out reason))
                {
                    return false;
                }
            }
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryValidateAction(
        CommissionedAdaptationVariable variable,
        AdjustmentRecipeStretchActionDto action,
        ISet<string> actionIds,
        ISet<string> immutable,
        ISet<string> protectedEntities,
        out string reason)
    {
        if (action is null || string.IsNullOrWhiteSpace(action.ActionId) || !actionIds.Add(action.ActionId))
        {
            reason = "Commissioned action ids must be nonblank and unique.";
            return false;
        }

        if (action.MaxDeltaSourceUnits <= 0m || action.CoordinateTolerance < 0m)
        {
            reason = $"Action '{action.ActionId}' must have positive capacity and nonnegative tolerance.";
            return false;
        }

        var widthAction = string.Equals(action.AxisTag, "Width", StringComparison.OrdinalIgnoreCase);
        var depthAction = string.Equals(action.AxisTag, "Height", StringComparison.OrdinalIgnoreCase);
        var supportedEdge = widthAction
            ? string.Equals(action.Edge, "Left", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(action.Edge, "Right", StringComparison.OrdinalIgnoreCase)
            : depthAction &&
              (string.Equals(action.Edge, "Top", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(action.Edge, "Bottom", StringComparison.OrdinalIgnoreCase));
        var compatible = variable.Axis == HouseAdaptationAxis.Width ? widthAction : depthAction;
        if (!supportedEdge || !compatible)
        {
            reason = $"Action '{action.ActionId}' has unsupported or incompatible axis/edge '{action.AxisTag}/{action.Edge}'.";
            return false;
        }

        if (action.CanonicalSourceBounds is null || action.TargetSpans is null || action.CanonicalEntityRoles is null ||
            action.TargetSpans.Count != 2 ||
            action.TargetSpans.Any(target =>
                target is null ||
                string.IsNullOrWhiteSpace(target.SourceEntityRef) ||
                target.GeometryPathId == Guid.Empty ||
                target.ClosingVertexIndex is < 0 or > 1) ||
            action.TargetSpans.Select(target => target.SourceEntityRef).Distinct(StringComparer.Ordinal).Count() != 2 ||
            action.TargetSpans
                .Select(target => (target.GeometryPathId, target.SegmentSortOrder))
                .Distinct()
                .Count() != 2)
        {
            reason = $"Action '{action.ActionId}' requires exactly two distinct target spans.";
            return false;
        }

        if (action.CanonicalEntityRoles.Any(role => !IsValidCanonicalRole(role)))
        {
            reason = $"Action '{action.ActionId}' contains an invalid or unsupported canonical entity role.";
            return false;
        }

        if (action.CanonicalEntityRoles
            .GroupBy(role => role.EntityRef, StringComparer.Ordinal)
            .Any(group => group.Count() != 1))
        {
            reason = $"Action '{action.ActionId}' contains duplicate canonical entity roles.";
            return false;
        }

        var stretchRoles = action.CanonicalEntityRoles
            .Where(role => string.Equals(role.Role, "Stretch", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (stretchRoles.Length != 2)
        {
            reason = $"Action '{action.ActionId}' target spans require matching Stretch roles.";
            return false;
        }

        foreach (var target in action.TargetSpans)
        {
            var matchingRoles = stretchRoles
                .Where(role =>
                    string.Equals(role.EntityRef, target.SourceEntityRef, StringComparison.Ordinal) &&
                    role.GeometryPathId == target.GeometryPathId &&
                    role.SegmentSortOrder == target.SegmentSortOrder)
                .ToArray();
            if (matchingRoles.Length != 1)
            {
                reason = $"Action '{action.ActionId}' target spans require matching Stretch roles.";
                return false;
            }

            var vertexIndices = matchingRoles[0].VertexIndices;
            if (vertexIndices.Count != 1 || vertexIndices[0] != target.ClosingVertexIndex)
            {
                reason = $"Action '{action.ActionId}' Stretch role for '{target.SourceEntityRef}' must match its target closing vertex.";
                return false;
            }
        }

        var forbidden = stretchRoles.FirstOrDefault(role =>
            immutable.Contains(role.EntityRef) || protectedEntities.Contains(role.EntityRef));
        if (forbidden is not null)
        {
            reason = $"Action '{action.ActionId}' cannot stretch immutable or protected entity '{forbidden.EntityRef}'.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsSupportedRole(string role)
        => string.Equals(role, "Stretch", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(role, "RigidMove", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(role, "Fixed", StringComparison.OrdinalIgnoreCase);

    private static bool IsValidCanonicalRole(AdjustmentRecipeEntityRoleDto? role)
    {
        if (role is null ||
            string.IsNullOrWhiteSpace(role.EntityRef) ||
            string.IsNullOrWhiteSpace(role.Role) ||
            !IsSupportedRole(role.Role) ||
            role.VertexIndices is null ||
            role.HostGeometryPathId == Guid.Empty ||
            role.HostGeometryPathId.HasValue != role.HostSegmentSortOrder.HasValue)
        {
            return false;
        }

        if (string.Equals(role.Role, "Stretch", StringComparison.OrdinalIgnoreCase))
        {
            return role.GeometryPathId is Guid geometryPathId &&
                   geometryPathId != Guid.Empty &&
                   role.SegmentSortOrder.HasValue;
        }

        if (role.GeometryPathId is Guid boundGeometryPathId)
        {
            return boundGeometryPathId != Guid.Empty;
        }

        return !role.SegmentSortOrder.HasValue && role.VertexIndices.Count == 0;
    }

    private bool TryAllocate(
        HouseAdaptationAxis axis,
        decimal requiredSourceUnits,
        ICollection<AdjustmentRecipeStretchActionDto> actions,
        out string reason)
    {
        reason = string.Empty;
        if (requiredSourceUnits == 0m)
        {
            return true;
        }

        var variables = profile.Variables
            .Where(variable => variable.Axis == axis)
            .OrderBy(variable => variable.Priority)
            .ThenBy(variable => variable.Name, StringComparer.Ordinal)
            .ThenBy(variable => variable.Id, StringComparer.Ordinal)
            .ToArray();
        if (variables.Length == 0)
        {
            reason = $"No commissioned adaptation variable exists for required {axis} reduction.";
            return false;
        }

        decimal capacity;
        try
        {
            capacity = variables.SelectMany(variable => variable.ActionTemplates)
                .Aggregate(0m, (total, action) => checked(total + action.MaxDeltaSourceUnits));
        }
        catch (OverflowException)
        {
            reason = $"Commissioned {axis} capacity cannot be represented safely.";
            return false;
        }

        if (capacity < requiredSourceUnits)
        {
            reason = $"Commissioned {axis} capacity ({capacity}) is below the required source-unit reduction ({requiredSourceUnits}).";
            return false;
        }

        var remaining = requiredSourceUnits;
        foreach (var template in variables.SelectMany(variable => variable.ActionTemplates))
        {
            if (remaining == 0m)
            {
                break;
            }

            var delta = Math.Min(remaining, template.MaxDeltaSourceUnits);
            actions.Add(template with { DeltaSourceUnits = delta });
            remaining -= delta;
        }

        return true;
    }

    private static bool TryEntitySet(
        IReadOnlyList<string> refs,
        string kind,
        out HashSet<string> result,
        out string reason)
    {
        result = new HashSet<string>(StringComparer.Ordinal);
        if (refs.Any(string.IsNullOrWhiteSpace))
        {
            reason = $"The commissioned profile contains a blank {kind} entity reference.";
            return false;
        }

        result.UnionWith(refs);
        reason = string.Empty;
        return true;
    }

    private static CommissionedHouseFitResult Reject(
        decimal widthDeficit,
        decimal depthDeficit,
        string reason)
        => new(false, false, widthDeficit, depthDeficit, [], reason);
}
