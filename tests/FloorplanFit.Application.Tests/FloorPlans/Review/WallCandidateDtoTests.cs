using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Review;

public sealed class WallCandidateDtoTests
{
    [Fact]
    public void AssemblyHint_returns_likely_two_by_four_when_thickness_is_near_four_inches()
    {
        var candidate = CreateCandidate(101.6m);

        Assert.Equal("Likely 2x4 wall (4\")", candidate.AssemblyHint);
    }

    [Fact]
    public void AssemblyHint_returns_likely_two_by_six_when_thickness_is_near_six_inches()
    {
        var candidate = CreateCandidate(152.4m);

        Assert.Equal("Likely 2x6 wall (6\")", candidate.AssemblyHint);
    }

    [Fact]
    public void AssemblyHint_returns_unknown_when_thickness_was_not_inferred()
    {
        var candidate = CreateCandidate(null);

        Assert.Equal("Thickness unknown", candidate.AssemblyHint);
    }

    private static WallCandidateDto CreateCandidate(decimal? thicknessMm)
    {
        return new WallCandidateDto(
            Guid.NewGuid(),
            "LINE:1",
            "WALLS",
            "Pending",
            0.95m,
            thicknessMm,
            null,
            Guid.NewGuid(),
            1);
    }
}
