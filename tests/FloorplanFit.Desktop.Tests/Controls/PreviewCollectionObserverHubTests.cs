using System.Collections.ObjectModel;
using FloorplanFit.Desktop.Controls.Preview;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class PreviewCollectionObserverHubTests
{
    [Fact]
    public void AttachAll_subscribes_observable_collections_and_invalidates_on_change()
    {
        var invalidations = 0;
        var hub = new PreviewCollectionObserverHub(() => invalidations++);
        var geometry = new ObservableCollection<int>();

        hub.AttachAll(new PreviewCollectionObserverHub.PreviewObservedCollections(
            GeometryPaths: geometry,
            PinchMarkers: null,
            RoomLabels: null,
            WallCandidates: null,
            OpeningCandidates: null,
            OpeningLabels: null,
            Dimensions: null,
            DimensionAssociations: null,
            FixedPlanComponents: null,
            ProtectedDetailAssemblies: null,
            CuratedPlanArtifacts: null));

        geometry.Add(1);

        Assert.Equal(1, invalidations);
    }

    [Fact]
    public void Replace_detaches_previous_collection_before_subscribing_new_one()
    {
        var invalidations = 0;
        var hub = new PreviewCollectionObserverHub(() => invalidations++);
        var oldCollection = new ObservableCollection<int>();
        var newCollection = new ObservableCollection<int>();

        hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, oldCollection);
        hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, newCollection);

        oldCollection.Add(1);
        newCollection.Add(1);

        Assert.Equal(1, invalidations);
    }

    [Fact]
    public void DetachAll_removes_all_active_subscriptions()
    {
        var invalidations = 0;
        var hub = new PreviewCollectionObserverHub(() => invalidations++);
        var geometry = new ObservableCollection<int>();

        hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, geometry);
        hub.DetachAll();
        geometry.Add(1);

        Assert.Equal(0, invalidations);
    }

    [Fact]
    public void Replace_ignores_non_observable_lists_without_throwing()
    {
        var invalidations = 0;
        var hub = new PreviewCollectionObserverHub(() => invalidations++);

        hub.Replace(
            PreviewCollectionObserverHub.PreviewObservedCollectionSlot.RoomLabels,
            new[] { 1, 2, 3 });

        Assert.Equal(0, invalidations);
    }

    [Fact]
    public void Replace_same_instance_does_not_double_subscribe()
    {
        var invalidations = 0;
        var hub = new PreviewCollectionObserverHub(() => invalidations++);
        var geometry = new ObservableCollection<int>();

        hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, geometry);
        hub.Replace(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, geometry);
        geometry.Add(1);

        Assert.Equal(1, invalidations);
    }
}
