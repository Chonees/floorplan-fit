namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public enum CadStretchRole
{
    Fixed,
    Stretch,
    RigidMove,
    Rejected
}

public sealed record CadStretchPoint(decimal X, decimal Y);

public sealed record CadStretchEntity(
    string EntityId,
    string EntityType,
    IReadOnlyList<CadStretchPoint> Vertices,
    bool SupportsVertexStretch = true);

public sealed record CadStretchEntityRole(
    string EntityId,
    CadStretchRole Role,
    IReadOnlyList<int> VertexIndices,
    string? Reason = null);

public sealed record CadStretchAction(
    string ActionId,
    string AxisTag,
    string Edge,
    decimal DeltaSourceUnits,
    decimal MaxDeltaSourceUnits,
    IReadOnlyList<string> TargetEntityIds,
    decimal Tolerance);

public sealed record CadStretchVertexEdit(
    int VertexIndex,
    decimal DeltaX,
    decimal DeltaY);

public sealed record CadStretchEntityEdit(
    string EntityId,
    CadStretchRole Role,
    IReadOnlyList<CadStretchVertexEdit> Vertices);

public sealed record CadStretchAudit(
    decimal RequestedDeltaSourceUnits,
    decimal MeasuredDeltaSourceUnits,
    decimal PairSpacingBeforeSourceUnits,
    decimal PairSpacingAfterSourceUnits,
    int StretchedEntityCount,
    int RigidMovedEntityCount,
    int FixedEntityCount,
    int RejectedEntityCount);

public sealed record CadStretchDeformationResult(
    bool Succeeded,
    string? RejectionReason,
    IReadOnlyList<CadStretchEntityEdit> Edits,
    CadStretchAudit Audit);

public static class CadStretchDeformationEngine
{
    public static CadStretchDeformationResult Apply(
        CadStretchAction action,
        IReadOnlyList<CadStretchEntity> entities,
        IReadOnlyList<CadStretchEntityRole> roles)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(roles);

        var emptyAudit = EmptyAudit(action.DeltaSourceUnits, entities.Count);
        if (string.IsNullOrWhiteSpace(action.ActionId))
        {
            return Reject("Stretch action id is required.", emptyAudit);
        }

        if (action.DeltaSourceUnits <= 0m ||
            action.MaxDeltaSourceUnits <= 0m ||
            action.DeltaSourceUnits > action.MaxDeltaSourceUnits)
        {
            return Reject("Stretch action delta must be positive and within its maximum capacity.", emptyAudit);
        }

        if (action.Tolerance < 0m)
        {
            return Reject("Stretch action tolerance cannot be negative.", emptyAudit);
        }

        if (!TryResolveVector(action, out var deltaX, out var deltaY, out var vectorError))
        {
            return Reject(vectorError, emptyAudit);
        }

        var targetIds = action.TargetEntityIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (targetIds.Length != 2 || targetIds.Length != action.TargetEntityIds.Count)
        {
            return Reject("A paired-wall stretch action requires exactly two distinct target entity ids.", emptyAudit);
        }

        if (!TryIndexUnique(entities, entity => entity.EntityId, "entity", out var entitiesById, out var entityError))
        {
            return Reject(entityError, emptyAudit);
        }

        if (!TryIndexUnique(roles, role => role.EntityId, "role", out var rolesById, out var roleError))
        {
            return Reject(roleError, emptyAudit);
        }

        var rejected = roles.FirstOrDefault(role => role.Role == CadStretchRole.Rejected);
        if (rejected is not null)
        {
            var reason = string.IsNullOrWhiteSpace(rejected.Reason)
                ? $"Entity '{rejected.EntityId}' was rejected by stretch preflight."
                : rejected.Reason;
            return Reject(reason!, emptyAudit with { RejectedEntityCount = roles.Count(role => role.Role == CadStretchRole.Rejected) });
        }

        foreach (var role in roles)
        {
            if (!entitiesById.ContainsKey(role.EntityId))
            {
                return Reject($"Role references missing entity '{role.EntityId}'.", emptyAudit);
            }

            if (role.Role == CadStretchRole.Stretch && !targetIds.Contains(role.EntityId, StringComparer.Ordinal))
            {
                return Reject($"Stretch role for '{role.EntityId}' is not one of the paired target spans.", emptyAudit);
            }
        }

        var targetEntities = new CadStretchEntity[2];
        var targetRoles = new CadStretchEntityRole[2];
        for (var index = 0; index < targetIds.Length; index++)
        {
            var targetId = targetIds[index];
            if (!entitiesById.TryGetValue(targetId, out var entity))
            {
                return Reject($"Target span entity '{targetId}' is missing.", emptyAudit);
            }

            if (!rolesById.TryGetValue(targetId, out var role) || role.Role != CadStretchRole.Stretch)
            {
                return Reject($"Target span entity '{targetId}' requires one Stretch role.", emptyAudit);
            }

            if (!entity.SupportsVertexStretch)
            {
                return Reject($"Target span entity '{targetId}' does not support vertex stretch.", emptyAudit);
            }

            if (!TryValidateTarget(entity, role, action, out var targetError))
            {
                return Reject(targetError, emptyAudit);
            }

            targetEntities[index] = entity;
            targetRoles[index] = role;
        }

        var beforeLengths = targetEntities.Select(entity => AxisLength(entity, action.AxisTag)).ToArray();
        var spacingBefore = PairSpacing(targetEntities[0], targetEntities[1], action.AxisTag);
        var edits = new List<CadStretchEntityEdit>();

        foreach (var role in roles)
        {
            if (role.Role == CadStretchRole.Fixed)
            {
                if (role.VertexIndices.Count != 0)
                {
                    return Reject($"Fixed entity '{role.EntityId}' cannot declare moved vertices.", emptyAudit);
                }

                continue;
            }

            var entity = entitiesById[role.EntityId];
            IReadOnlyList<int> vertexIndices;
            if (role.Role == CadStretchRole.Stretch)
            {
                vertexIndices = role.VertexIndices.Distinct().Order().ToArray();
            }
            else if (role.Role == CadStretchRole.RigidMove)
            {
                if (role.VertexIndices.Count != 0)
                {
                    return Reject($"RigidMove entity '{role.EntityId}' must move every vertex and cannot declare a subset.", emptyAudit);
                }

                vertexIndices = Enumerable.Range(0, entity.Vertices.Count).ToArray();
            }
            else
            {
                continue;
            }

            edits.Add(new CadStretchEntityEdit(
                role.EntityId,
                role.Role,
                vertexIndices.Select(vertexIndex => new CadStretchVertexEdit(vertexIndex, deltaX, deltaY)).ToArray()));
        }

        var afterTargets = targetEntities
            .Select((entity, index) => ApplyEdits(entity, edits.Single(edit => edit.EntityId == targetIds[index])))
            .ToArray();
        var afterLengths = afterTargets.Select(entity => AxisLength(entity, action.AxisTag)).ToArray();
        var measuredDeltas = beforeLengths.Zip(afterLengths, (before, after) => before - after).ToArray();
        if (measuredDeltas.Any(delta => !Within(delta, action.DeltaSourceUnits, action.Tolerance)))
        {
            return Reject(
                $"Paired target spans did not each shorten by the requested delta {action.DeltaSourceUnits}.",
                emptyAudit);
        }

        var spacingAfter = PairSpacing(afterTargets[0], afterTargets[1], action.AxisTag);
        if (!Within(spacingAfter, spacingBefore, action.Tolerance))
        {
            return Reject("Paired wall-face spacing changed during stretch.", emptyAudit);
        }

        var editedIds = edits.Select(edit => edit.EntityId).ToHashSet(StringComparer.Ordinal);
        var audit = new CadStretchAudit(
            action.DeltaSourceUnits,
            measuredDeltas[0],
            spacingBefore,
            spacingAfter,
            edits.Count(edit => edit.Role == CadStretchRole.Stretch),
            edits.Count(edit => edit.Role == CadStretchRole.RigidMove),
            entities.Count(entity => !editedIds.Contains(entity.EntityId)),
            RejectedEntityCount: 0);

        return new CadStretchDeformationResult(true, null, edits, audit);
    }

    private static bool TryValidateTarget(
        CadStretchEntity entity,
        CadStretchEntityRole role,
        CadStretchAction action,
        out string error)
    {
        if (entity.Vertices.Count < 2)
        {
            error = $"Target span entity '{entity.EntityId}' requires at least two vertices.";
            return false;
        }

        var vertexIndices = role.VertexIndices.Distinct().ToArray();
        if (vertexIndices.Length == 0 || vertexIndices.Length != role.VertexIndices.Count)
        {
            error = $"Target span entity '{entity.EntityId}' requires distinct closing vertex indices.";
            return false;
        }

        if (vertexIndices.Any(index => index < 0 || index >= entity.Vertices.Count))
        {
            error = $"Target span entity '{entity.EntityId}' has an invalid closing vertex index.";
            return false;
        }

        if (!IsAxisAligned(entity, action.AxisTag, action.Tolerance))
        {
            error = $"Target span entity '{entity.EntityId}' is not aligned with action axis '{action.AxisTag}'.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsAxisAligned(CadStretchEntity entity, string axisTag, decimal tolerance)
    {
        var first = entity.Vertices[0];
        var last = entity.Vertices[^1];
        return IsWidth(axisTag)
            ? Within(first.Y, last.Y, tolerance)
            : Within(first.X, last.X, tolerance);
    }

    private static decimal AxisLength(CadStretchEntity entity, string axisTag)
    {
        var values = IsWidth(axisTag)
            ? entity.Vertices.Select(point => point.X)
            : entity.Vertices.Select(point => point.Y);
        return values.Max() - values.Min();
    }

    private static decimal PairSpacing(CadStretchEntity first, CadStretchEntity second, string axisTag)
    {
        static decimal Average(IEnumerable<decimal> values)
        {
            var materialized = values.ToArray();
            return materialized.Sum() / materialized.Length;
        }

        var firstMidpoint = IsWidth(axisTag)
            ? Average(first.Vertices.Select(point => point.Y))
            : Average(first.Vertices.Select(point => point.X));
        var secondMidpoint = IsWidth(axisTag)
            ? Average(second.Vertices.Select(point => point.Y))
            : Average(second.Vertices.Select(point => point.X));
        return Math.Abs(firstMidpoint - secondMidpoint);
    }

    private static CadStretchEntity ApplyEdits(CadStretchEntity entity, CadStretchEntityEdit edit)
    {
        var byIndex = edit.Vertices.ToDictionary(vertex => vertex.VertexIndex);
        var vertices = entity.Vertices
            .Select((point, index) => byIndex.TryGetValue(index, out var delta)
                ? new CadStretchPoint(point.X + delta.DeltaX, point.Y + delta.DeltaY)
                : point)
            .ToArray();
        return entity with { Vertices = vertices };
    }

    private static bool TryResolveVector(
        CadStretchAction action,
        out decimal deltaX,
        out decimal deltaY,
        out string error)
    {
        deltaX = 0m;
        deltaY = 0m;
        error = string.Empty;

        if (IsWidth(action.AxisTag))
        {
            if (string.Equals(action.Edge, "Right", StringComparison.OrdinalIgnoreCase))
            {
                deltaX = -action.DeltaSourceUnits;
                return true;
            }

            if (string.Equals(action.Edge, "Left", StringComparison.OrdinalIgnoreCase))
            {
                deltaX = action.DeltaSourceUnits;
                return true;
            }
        }
        else if (string.Equals(action.AxisTag, "Height", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(action.Edge, "Top", StringComparison.OrdinalIgnoreCase))
            {
                deltaY = -action.DeltaSourceUnits;
                return true;
            }

            if (string.Equals(action.Edge, "Bottom", StringComparison.OrdinalIgnoreCase))
            {
                deltaY = action.DeltaSourceUnits;
                return true;
            }
        }

        error = $"Unsupported stretch axis/edge '{action.AxisTag}/{action.Edge}'.";
        return false;
    }

    private static bool IsWidth(string axisTag)
        => string.Equals(axisTag, "Width", StringComparison.OrdinalIgnoreCase);

    private static bool Within(decimal actual, decimal expected, decimal tolerance)
        => Math.Abs(actual - expected) <= tolerance;

    private static bool TryIndexUnique<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        string kind,
        out IReadOnlyDictionary<string, T> byId,
        out string error)
    {
        var dictionary = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var key = keySelector(item);
            if (string.IsNullOrWhiteSpace(key) || !dictionary.TryAdd(key, item))
            {
                byId = dictionary;
                error = $"Each stretch {kind} requires one unique non-empty id; duplicate '{key}'.";
                return false;
            }
        }

        byId = dictionary;
        error = string.Empty;
        return true;
    }

    private static CadStretchAudit EmptyAudit(decimal requestedDelta, int fixedEntityCount)
        => new(
            requestedDelta,
            MeasuredDeltaSourceUnits: 0m,
            PairSpacingBeforeSourceUnits: 0m,
            PairSpacingAfterSourceUnits: 0m,
            StretchedEntityCount: 0,
            RigidMovedEntityCount: 0,
            fixedEntityCount,
            RejectedEntityCount: 0);

    private static CadStretchDeformationResult Reject(string reason, CadStretchAudit audit)
        => new(false, reason, [], audit);
}
