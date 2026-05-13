using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace FloorplanFit.Desktop.Controls.Preview;

internal sealed class PreviewCollectionObserverHub
{
    private readonly Action invalidateVisual;
    private readonly Dictionary<PreviewObservedCollectionSlot, INotifyCollectionChanged> observed = [];

    internal enum PreviewObservedCollectionSlot
    {
        GeometryPaths,
        PinchMarkers,
        RoomLabels,
        WallCandidates,
        OpeningCandidates,
        OpeningLabels,
        Dimensions,
        DimensionAssociations,
        FixedPlanComponents,
        ProtectedDetailAssemblies,
        CuratedPlanArtifacts
    }

    internal readonly record struct PreviewObservedCollections(
        IEnumerable? GeometryPaths,
        IEnumerable? PinchMarkers,
        IEnumerable? RoomLabels,
        IEnumerable? WallCandidates,
        IEnumerable? OpeningCandidates,
        IEnumerable? OpeningLabels,
        IEnumerable? Dimensions,
        IEnumerable? DimensionAssociations,
        IEnumerable? FixedPlanComponents,
        IEnumerable? ProtectedDetailAssemblies,
        IEnumerable? CuratedPlanArtifacts);

    public PreviewCollectionObserverHub(Action invalidateVisual)
    {
        this.invalidateVisual = invalidateVisual;
    }

    public void AttachAll(PreviewObservedCollections collections)
    {
        Replace(PreviewObservedCollectionSlot.GeometryPaths, collections.GeometryPaths);
        Replace(PreviewObservedCollectionSlot.PinchMarkers, collections.PinchMarkers);
        Replace(PreviewObservedCollectionSlot.RoomLabels, collections.RoomLabels);
        Replace(PreviewObservedCollectionSlot.WallCandidates, collections.WallCandidates);
        Replace(PreviewObservedCollectionSlot.OpeningCandidates, collections.OpeningCandidates);
        Replace(PreviewObservedCollectionSlot.OpeningLabels, collections.OpeningLabels);
        Replace(PreviewObservedCollectionSlot.Dimensions, collections.Dimensions);
        Replace(PreviewObservedCollectionSlot.DimensionAssociations, collections.DimensionAssociations);
        Replace(PreviewObservedCollectionSlot.FixedPlanComponents, collections.FixedPlanComponents);
        Replace(PreviewObservedCollectionSlot.ProtectedDetailAssemblies, collections.ProtectedDetailAssemblies);
        Replace(PreviewObservedCollectionSlot.CuratedPlanArtifacts, collections.CuratedPlanArtifacts);
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
        invalidateVisual();
    }
}
