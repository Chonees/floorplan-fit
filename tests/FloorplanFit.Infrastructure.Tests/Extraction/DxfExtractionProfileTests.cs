using FloorplanFit.Infrastructure.Dxf;

namespace FloorplanFit.Infrastructure.Tests.Extraction;

public sealed class DxfExtractionProfileTests
{
    [Fact]
    public void PointeHomes_profile_names_current_seed_convention_explicitly()
    {
        var profile = DxfExtractionProfile.PointeHomes;

        Assert.Equal("Pointe Homes CAD", profile.Name);
        Assert.Contains("SEMINOLE2000", profile.SeedFloorPlans);
    }

    [Fact]
    public void PointeHomes_profile_resolves_wall_and_room_label_conventions()
    {
        var profile = DxfExtractionProfile.PointeHomes;

        Assert.True(profile.IsWallCandidateLayer("WALLS"));
        Assert.False(profile.IsPhysicalWallLayer("ELECTRICAL WALLS"));
        Assert.True(profile.IsRoomLabelLayer("ROOM LBLS"));
        Assert.True(profile.LooksLikeRoomName("MASTER BATH"));
        Assert.False(profile.LooksLikeRoomName("WALL LEGEND"));
    }

    [Fact]
    public void PointeHomes_profile_resolves_opening_conventions()
    {
        var profile = DxfExtractionProfile.PointeHomes;

        Assert.Equal("Door", profile.ResolveOpeningGeometryKind("DOORS"));
        Assert.Equal("Window", profile.ResolveOpeningGeometryKind("WIN"));
        Assert.Equal("Window", profile.ResolveOpeningGeometryKind("WINS"));
        Assert.Equal("Door", profile.ResolveOpeningLabelKind("DOORTEXT"));
        Assert.Equal("Window", profile.ResolveOpeningLabelKind("WINDWS LBLS"));
        Assert.True(profile.LooksLikeOpeningModelOrSizeLabel("2668"));
        Assert.True(profile.LooksLikeOpeningModelOrSizeLabel("24\"DR."));
        Assert.True(profile.LooksLikeOpeningModelOrSizeLabel("27\" R.O."));
        Assert.False(profile.LooksLikeOpeningModelOrSizeLabel("STAND ALONE TUB"));
    }

    [Fact]
    public void PointeHomes_profile_resolves_fixed_component_conventions()
    {
        var profile = DxfExtractionProfile.PointeHomes;

        Assert.Equal("Cabinet", profile.ResolveFixedComponentLayerKind("CABS"));
        Assert.Equal("Cabinet", profile.ResolveFixedComponentLayerKind("CABS-FLOORPLAN"));
        Assert.Equal("Fixture", profile.ResolveFixedComponentLayerKind("FIXTURES"));
        Assert.Equal("Toilet", profile.ResolveFixedComponentBlockKind("TOILET1"));
        Assert.Equal("Appliance", profile.ResolveFixedComponentBlockKind("STOVE"));
        Assert.Equal("Appliance", profile.ResolveFixedComponentBlockKind("DISHWASHER"));
        Assert.Equal("Fixture", profile.ResolveFixedComponentBlockKind("SINK"));
        Assert.Equal("Fixture", profile.ResolveFixedComponentBlockKind("TUB"));
    }

    [Fact]
    public void PointeHomes_profile_resolves_protected_detail_conventions_without_promoting_every_layer_to_walls()
    {
        var profile = DxfExtractionProfile.PointeHomes;

        Assert.Equal("WetAreaDetail", profile.ResolveProtectedDetailLayerKind("MISC"));
        Assert.Equal("WetAreaDetail", profile.ResolveProtectedDetailLayerKind("HATCH"));
        Assert.Null(profile.ResolveProtectedDetailLayerKind("L1"));
        Assert.Null(profile.ResolveProtectedDetailLayerKind("WALLS"));
        Assert.Null(profile.ResolveProtectedDetailLayerKind("DIMS"));
    }
}
