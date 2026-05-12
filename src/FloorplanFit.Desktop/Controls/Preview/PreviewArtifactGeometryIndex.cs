using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal sealed class PreviewArtifactGeometryIndex
{
    private PreviewArtifactGeometryIndex(
        IReadOnlySet<Guid> openingGeometryPathIds,
        IReadOnlySet<Guid> fixedPlanComponentGeometryPathIds,
        IReadOnlySet<Guid> protectedDetailGeometryPathIds)
    {
        OpeningGeometryPathIds = openingGeometryPathIds;
        FixedPlanComponentGeometryPathIds = fixedPlanComponentGeometryPathIds;
        ProtectedDetailGeometryPathIds = protectedDetailGeometryPathIds;
    }

    public IReadOnlySet<Guid> OpeningGeometryPathIds { get; }

    public IReadOnlySet<Guid> FixedPlanComponentGeometryPathIds { get; }

    public IReadOnlySet<Guid> ProtectedDetailGeometryPathIds { get; }

    public bool Contains(Guid geometryPathId)
    {
        return OpeningGeometryPathIds.Contains(geometryPathId) ||
               FixedPlanComponentGeometryPathIds.Contains(geometryPathId) ||
               ProtectedDetailGeometryPathIds.Contains(geometryPathId);
    }

    public static PreviewArtifactGeometryIndex Create(
        IReadOnlyList<OpeningCandidateDto>? openingCandidates,
        IReadOnlyList<FixedPlanComponentDto>? fixedPlanComponents,
        IReadOnlyList<ProtectedDetailAssemblyDto>? protectedDetailAssemblies = null)
    {
        var openingGeometryPathIds = openingCandidates is { Count: > 0 }
            ? openingCandidates
                .Where(item => item.GeometryPathId is not null)
                .Select(item => item.GeometryPathId!.Value)
                .ToHashSet()
            : new HashSet<Guid>();

        var fixedPlanComponentGeometryPathIds = fixedPlanComponents is { Count: > 0 }
            ? fixedPlanComponents
                .SelectMany(item => item.GeometryPathIds)
                .ToHashSet()
            : new HashSet<Guid>();

        var protectedDetailGeometryPathIds = protectedDetailAssemblies is { Count: > 0 }
            ? protectedDetailAssemblies
                .SelectMany(item => item.GeometryPathIds)
                .ToHashSet()
            : new HashSet<Guid>();

        return new PreviewArtifactGeometryIndex(
            openingGeometryPathIds,
            fixedPlanComponentGeometryPathIds,
            protectedDetailGeometryPathIds);
    }

    public static PreviewArtifactGeometryIndex Create(IReadOnlyList<CuratedPlanArtifactDto>? curatedPlanArtifacts)
    {
        var openingGeometryPathIds = new HashSet<Guid>();
        var fixedPlanComponentGeometryPathIds = new HashSet<Guid>();
        var protectedDetailGeometryPathIds = new HashSet<Guid>();

        if (curatedPlanArtifacts is not { Count: > 0 })
        {
            return new PreviewArtifactGeometryIndex(
                openingGeometryPathIds,
                fixedPlanComponentGeometryPathIds,
                protectedDetailGeometryPathIds);
        }

        foreach (var artifact in curatedPlanArtifacts)
        {
            switch (artifact.ResolvedFamily)
            {
                case var family when string.Equals(family, FloorPlanArtifactTaxonomy.ProtectedFamily, StringComparison.Ordinal):
                    protectedDetailGeometryPathIds.UnionWith(artifact.GeometryPathIds);
                    break;
                case var family when string.Equals(family, FloorPlanArtifactTaxonomy.FixedFamily, StringComparison.Ordinal):
                    fixedPlanComponentGeometryPathIds.UnionWith(artifact.GeometryPathIds);
                    break;
                default:
                    openingGeometryPathIds.UnionWith(artifact.GeometryPathIds);
                    break;
            }
        }

        return new PreviewArtifactGeometryIndex(
            openingGeometryPathIds,
            fixedPlanComponentGeometryPathIds,
            protectedDetailGeometryPathIds);
    }

    public IReadOnlyList<GeometryPathDto> OrderForHitTesting(IReadOnlyList<GeometryPathDto>? geometryPaths)
    {
        if (geometryPaths is not { Count: > 0 })
        {
            return [];
        }

        if (OpeningGeometryPathIds.Count == 0 &&
            FixedPlanComponentGeometryPathIds.Count == 0 &&
            ProtectedDetailGeometryPathIds.Count == 0)
        {
            return geometryPaths;
        }

        return geometryPaths
            .OrderByDescending(path => ResolveHitTestPriority(path.Id))
            .ToArray();
    }

    private int ResolveHitTestPriority(Guid geometryPathId)
    {
        if (ProtectedDetailGeometryPathIds.Contains(geometryPathId))
        {
            return 3;
        }

        if (FixedPlanComponentGeometryPathIds.Contains(geometryPathId))
        {
            return 2;
        }

        return OpeningGeometryPathIds.Contains(geometryPathId)
            ? 1
            : 0;
    }
}
