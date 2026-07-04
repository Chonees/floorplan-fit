using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace FloorplanFit.Desktop.Controls.Preview;

internal sealed class PreviewCollectionObserverHub
{
    private readonly Action<PreviewObservedCollectionSlot> collectionChanged;
    private readonly Dictionary<PreviewObservedCollectionSlot, INotifyCollectionChanged> observed = [];

    internal enum PreviewObservedCollectionSlot
    {
        SitePlanGeometryPaths,
        GeometryPaths,
        PinchMarkers,
        RoomLabels,
        WallCandidates,
        OpeningCandidates,
        OpeningLabels,
        Dimensions,
        DimensionBindings,
        DimensionAssociations,
        MeasurementCorridors,
        MeasurementNodes,
        DimensionIntervalBindings,
        ArticulationBands,
        FixedPlanComponents,
        ProtectedDetailAssemblies,
        CuratedPlanArtifacts,
        SitePlanRenderPaths,
        SitePlanTexts
    }

    internal readonly record struct PreviewObservedCollections(
        IEnumerable? GeometryPaths,
        IEnumerable? PinchMarkers,
        IEnumerable? RoomLabels,
        IEnumerable? WallCandidates,
        IEnumerable? OpeningCandidates,
        IEnumerable? OpeningLabels,
        IEnumerable? Dimensions,
        IEnumerable? DimensionBindings,
        IEnumerable? DimensionAssociations,
        IEnumerable? MeasurementCorridors,
        IEnumerable? MeasurementNodes,
        IEnumerable? DimensionIntervalBindings,
        IEnumerable? ArticulationBands,
        IEnumerable? FixedPlanComponents,
        IEnumerable? ProtectedDetailAssemblies,
        IEnumerable? CuratedPlanArtifacts,
        IEnumerable? SitePlanGeometryPaths = null,
        IEnumerable? SitePlanRenderPaths = null,
        IEnumerable? SitePlanTexts = null);

    public PreviewCollectionObserverHub(Action invalidateVisual)
        : this(_ => invalidateVisual())
    {
    }

    public PreviewCollectionObserverHub(Action<PreviewObservedCollectionSlot> collectionChanged)
    {
        this.collectionChanged = collectionChanged;
    }

    public void AttachAll(PreviewObservedCollections collections)
    {
        Replace(PreviewObservedCollectionSlot.SitePlanGeometryPaths, collections.SitePlanGeometryPaths);
        Replace(PreviewObservedCollectionSlot.GeometryPaths, collections.GeometryPaths);
        Replace(PreviewObservedCollectionSlot.PinchMarkers, collections.PinchMarkers);
        Replace(PreviewObservedCollectionSlot.RoomLabels, collections.RoomLabels);
        Replace(PreviewObservedCollectionSlot.WallCandidates, collections.WallCandidates);
        Replace(PreviewObservedCollectionSlot.OpeningCandidates, collections.OpeningCandidates);
        Replace(PreviewObservedCollectionSlot.OpeningLabels, collections.OpeningLabels);
        Replace(PreviewObservedCollectionSlot.Dimensions, collections.Dimensions);
        Replace(PreviewObservedCollectionSlot.DimensionBindings, collections.DimensionBindings);
        Replace(PreviewObservedCollectionSlot.DimensionAssociations, collections.DimensionAssociations);
        Replace(PreviewObservedCollectionSlot.MeasurementCorridors, collections.MeasurementCorridors);
        Replace(PreviewObservedCollectionSlot.MeasurementNodes, collections.MeasurementNodes);
        Replace(PreviewObservedCollectionSlot.DimensionIntervalBindings, collections.DimensionIntervalBindings);
        Replace(PreviewObservedCollectionSlot.ArticulationBands, collections.ArticulationBands);
        Replace(PreviewObservedCollectionSlot.FixedPlanComponents, collections.FixedPlanComponents);
        Replace(PreviewObservedCollectionSlot.ProtectedDetailAssemblies, collections.ProtectedDetailAssemblies);
        Replace(PreviewObservedCollectionSlot.CuratedPlanArtifacts, collections.CuratedPlanArtifacts);
        Replace(PreviewObservedCollectionSlot.SitePlanRenderPaths, collections.SitePlanRenderPaths);
        Replace(PreviewObservedCollectionSlot.SitePlanTexts, collections.SitePlanTexts);
    }

    public void Replace(PreviewObservedCollectionSlot slot, IEnumerable? value)
    {
        if (observed.TryGetValue(slot, out var existing) && ReferenceEquals(existing, value))
        {
            return;
        }

        Detach(slot);
        if (value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observed[slot] = notifyCollectionChanged;
        notifyCollectionChanged.CollectionChanged += OnObservedCollectionChanged;
    }

    public void DetachAll()
    {
        foreach (var slot in observed.Keys.ToArray())
        {
            Detach(slot);
        }
    }

    private void Detach(PreviewObservedCollectionSlot slot)
    {
        if (!observed.TryGetValue(slot, out var existing))
        {
            return;
        }

        existing.CollectionChanged -= OnObservedCollectionChanged;
        observed.Remove(slot);
    }

    private void OnObservedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in observed)
        {
            if (ReferenceEquals(item.Value, sender))
            {
                collectionChanged(item.Key);
                return;
            }
        }
    }
}
