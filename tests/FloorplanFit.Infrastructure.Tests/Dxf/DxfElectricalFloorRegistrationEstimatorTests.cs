using System.Globalization;
using System.Security.Cryptography;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Dxf;

namespace FloorplanFit.Infrastructure.Tests.Dxf;

public sealed class DxfElectricalFloorRegistrationEstimatorTests
{
    private const string CanonicalWallLayer = "FLOOR-WALL";
    private const string ElectricalWallLayer = "ELECTRICAL-WALL";
    private const decimal CoordinateTolerance = 0.001m;
    private const decimal DominantSelectorTolerance = 0.05m;
    private const decimal MinimumCoverage = 0.75m;

    [Fact]
    public void WholePlanAcceptancePolicy_is_shared_by_estimator_and_persisted_proof()
    {
        Assert.True(WholePlanRegistrationAcceptancePolicy.IsAccepted(
            WholePlanRegistrationAcceptancePolicy.CurrentVersion,
            0.75m,
            0.75m,
            0.05m,
            0.05m));
        Assert.False(WholePlanRegistrationAcceptancePolicy.IsAccepted(
            WholePlanRegistrationAcceptancePolicy.CurrentVersion,
            0.74m,
            1m,
            0m,
            0m));
        Assert.False(WholePlanRegistrationAcceptancePolicy.IsAccepted(
            WholePlanRegistrationAcceptancePolicy.CurrentVersion,
            1m,
            1m,
            0.05m,
            100m));
        Assert.False(WholePlanRegistrationAcceptancePolicy.IsAccepted(
            WholePlanRegistrationAcceptancePolicy.CurrentVersion + 1,
            1m,
            1m,
            0m,
            0m));
        Assert.Equal(
            WholePlanRegistrationAcceptancePolicy.IsAccepted(
                WholePlanRegistrationAcceptancePolicy.CurrentVersion,
                0.75m,
                0.75m,
                0.05m,
                0.05m),
            DxfElectricalFloorRegistrationEstimator.MeetsWholePlanEvidenceThresholds(0.75m, 0.75m, 0.05m, 0.05m));

        var invalidProof = new WholePlanRegistrationProof(
            WholePlanRegistrationProof.CurrentVersion,
            Passed: true,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('a', 64),
            new string('b', 64),
            HorizontalCoverage: 0m,
            VerticalCoverage: 0m,
            RootMeanSquareResidual: 100m,
            MaximumResidual: 100m);

        Assert.False(invalidProof.IsAuthoritative);
    }

    [Fact]
    public async Task EstimateAsync_recovers_identity_orientation_and_ignores_short_outboard_fragments()
    {
        var canonical = CreateAsymmetricStructure();
        var expected = SyntheticTransform.Similarity(1m, 0, 11m, -7m);
        var electrical = InverseGenerate(canonical, expected);
        var fragmentedElectrical = electrical.Concat(CreateShortOutboardFragments(electrical)).ToArray();

        var cleanEstimate = await EstimateAsync(canonical, electrical);
        var fragmentedEstimate = await EstimateAsync(canonical, fragmentedElectrical);

        AssertEstimatedTransform(cleanEstimate, expected);
        AssertEstimatedTransform(fragmentedEstimate, expected);
        Assert.Equal(1m, cleanEstimate.Transform!.Scale);
        Assert.Equal(1m, fragmentedEstimate.Transform!.Scale);
        Assert.Equal(cleanEstimate.Transform.Scale, fragmentedEstimate.Transform.Scale);
        AssertClose(cleanEstimate.Transform.RotationDegrees, fragmentedEstimate.Transform.RotationDegrees);
        AssertClose(cleanEstimate.Transform.TranslateX, fragmentedEstimate.Transform.TranslateX);
        AssertClose(cleanEstimate.Transform.TranslateY, fragmentedEstimate.Transform.TranslateY);
        AssertStrongTwoAxisEvidence(fragmentedEstimate);
        AssertLowResiduals(fragmentedEstimate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public async Task EstimateAsync_recovers_unique_quarter_turn_from_inverse_generated_asymmetric_source(
        int rotationDegrees)
    {
        var canonical = CreateAsymmetricStructure();
        var expected = SyntheticTransform.Similarity(1m, rotationDegrees, -13m, 19m);
        var electrical = InverseGenerate(canonical, expected);

        var estimate = await EstimateAsync(canonical, electrical);

        AssertEstimatedTransform(estimate, expected);
        var accepted = Assert.Single(estimate.Candidates.Where(candidate => candidate.Accepted));
        Assert.Equal((decimal)rotationDegrees, accepted.RotationDegrees);
        AssertStrongTwoAxisEvidence(estimate);
        AssertLowResiduals(estimate);
    }

    [Fact]
    public async Task EstimateAsync_accepts_and_reports_uniform_scale_supported_by_both_axes()
    {
        var canonical = CreateAsymmetricStructure();
        var expected = SyntheticTransform.Similarity(1.25m, 0, 6m, -9m);
        var electrical = InverseGenerate(canonical, expected);

        var estimate = await EstimateAsync(canonical, electrical);

        AssertEstimatedTransform(estimate, expected);
        AssertClose(expected.ScaleX, Require(estimate.ObservedScaleX, nameof(estimate.ObservedScaleX)));
        AssertClose(expected.ScaleY, Require(estimate.ObservedScaleY, nameof(estimate.ObservedScaleY)));
        var accepted = Assert.Single(estimate.Candidates.Where(candidate => candidate.Accepted));
        AssertClose(expected.ScaleX, Require(accepted.Scale, nameof(accepted.Scale)));
        AssertStrongTwoAxisEvidence(estimate);
    }

    [Fact]
    public async Task EstimateAsync_snaps_small_uniform_source_size_drift_to_exact_scale_one()
    {
        var canonical = CreateAsymmetricStructure();
        var electrical = ScaleAboutBoundsCenter(canonical, 1.001m);
        var canonicalBounds = GetBounds(canonical);
        var electricalBounds = GetBounds(electrical);
        var widthDifference = Math.Abs(canonicalBounds.Width - electricalBounds.Width);
        var heightDifference = Math.Abs(canonicalBounds.Height - electricalBounds.Height);

        Assert.True(
            widthDifference is > 0m and < DominantSelectorTolerance,
            $"The fixture width drift must be below selector tolerance; found {widthDifference}.");
        Assert.True(
            heightDifference is > 0m and < DominantSelectorTolerance,
            $"The fixture height drift must be below selector tolerance; found {heightDifference}.");

        var estimate = await EstimateAsync(canonical, electrical);

        AssertEstimatedTransform(
            estimate,
            SyntheticTransform.Similarity(1m, 0, 0m, 0m));
        Assert.NotEqual(1m, Require(estimate.ObservedScaleX, nameof(estimate.ObservedScaleX)));
        Assert.NotEqual(1m, Require(estimate.ObservedScaleY, nameof(estimate.ObservedScaleY)));
        Assert.Equal(1m, estimate.Transform!.Scale);
        var accepted = Assert.Single(estimate.Candidates.Where(candidate => candidate.Accepted));
        Assert.Equal(1m, Require(accepted.Scale, nameof(accepted.Scale)));
        AssertResidualsAtMost(estimate, DominantSelectorTolerance);
    }

    [Fact]
    public async Task EstimateAsync_uses_lower_ranked_connected_frame_when_disconnected_pairs_have_more_support()
    {
        var structure = CreateAsymmetricStructure()
            .Concat(CreateDisconnectedHigherRankedPairs())
            .ToArray();

        var estimate = await EstimateAsync(structure, structure);

        AssertEstimatedTransform(estimate, SyntheticTransform.Similarity(1m, 0, 0m, 0m));
        AssertStrongTwoAxisEvidence(estimate);
        AssertLowResiduals(estimate);
    }

    [Fact]
    public async Task EstimateAsync_rejects_disconnected_pairs_and_a_three_corner_frame()
    {
        var incompleteStructure = CreateDisconnectedHigherRankedPairs()
            .Concat(CreateThreeCornerFrame(0m, 0m, 24m, 15m))
            .ToArray();

        var estimate = await EstimateAsync(incompleteStructure, incompleteStructure);

        AssertInsufficientEstimate(estimate);
        Assert.Contains("connect", estimate.EvidenceSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EstimateAsync_evaluates_compatible_connected_frame_pairs_when_other_frames_are_incompatible()
    {
        var dominantStructure = ScaleCoordinates(CreateAsymmetricStructure(), 10m, 11m);
        var canonical = dominantStructure
            .Concat(CreateOuterRectangle(300m, 300m, 360m, 340m))
            .ToArray();
        var electrical = dominantStructure
            .Concat(CreateOuterRectangle(600m, 600m, 670m, 650m))
            .ToArray();

        var estimate = await EstimateAsync(canonical, electrical);

        AssertEstimatedTransform(estimate, SyntheticTransform.Similarity(1m, 0, 0m, 0m));
        Assert.Equal(1m, estimate.Transform!.Scale);
        AssertStrongTwoAxisEvidence(estimate);
        AssertLowResiduals(estimate);
    }

    [Fact]
    public async Task EstimateAsync_returns_insufficient_when_local_pair_transforms_do_not_cover_the_whole_plan()
    {
        var canonical = CreateAsymmetricStructure()
            .Concat(Translate(CreateAsymmetricStructure(), 100m, 100m))
            .ToArray();
        var electrical = CreateAsymmetricStructure()
            .Concat(Translate(CreateAsymmetricStructure(), 250m, 180m))
            .ToArray();

        var estimate = await EstimateAsync(canonical, electrical);

        AssertInsufficientEstimate(estimate);
        Assert.True(
            estimate.Candidates
                .Where(candidate => candidate.Accepted)
                .Select(candidate => (candidate.Scale, candidate.RotationDegrees, candidate.TranslateX, candidate.TranslateY))
                .Distinct()
                .Count() > 1,
            "The fixture must prove multiple local transforms before whole-plan filtering.");
        Assert.Contains("manual review is required", estimate.EvidenceSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EstimateAsync_returns_ambiguous_when_two_transforms_cover_the_complete_canonical_structure()
    {
        var canonical = CreateAsymmetricStructure();
        var electrical = canonical
            .Concat(Translate(canonical, 100m, 100m))
            .ToArray();

        var estimate = await EstimateAsync(canonical, electrical);

        AssertAmbiguousEstimate(estimate);
        Assert.True(
            estimate.Candidates
                .Where(candidate => candidate.Accepted)
                .Select(candidate => (candidate.Scale, candidate.RotationDegrees, candidate.TranslateX, candidate.TranslateY))
                .Distinct()
                .Count() > 1,
            "Whole-plan symmetry must remain ambiguous instead of selecting one transform.");
        Assert.Equal(
            2,
            estimate.EvidenceSummary.Split(
                "global canonical-to-transformed-Electrical H coverage 1, V coverage 1",
                StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public async Task EstimateAsync_accepts_when_extra_unmatched_electrical_runs_are_not_canonical_evidence()
    {
        var canonical = CreateAsymmetricStructure();
        var unmatchedElectricalRuns = new[]
        {
            new LineSegment(new Point(100m, 100m), new Point(300m, 100m)),
            new LineSegment(new Point(400m, 200m), new Point(400m, 450m))
        };
        var electrical = canonical.Concat(unmatchedElectricalRuns).ToArray();

        var estimate = await EstimateAsync(canonical, electrical);

        AssertEstimatedTransform(estimate, SyntheticTransform.Similarity(1m, 0, 0m, 0m));
        Assert.Equal(1m, Require(estimate.HorizontalCoverage, nameof(estimate.HorizontalCoverage)));
        Assert.Equal(1m, Require(estimate.VerticalCoverage, nameof(estimate.VerticalCoverage)));
        Assert.Contains(
            "canonical-to-transformed-Electrical",
            estimate.EvidenceSummary,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WholePlanEvidenceGate_rejects_locally_proven_transform_when_coverage_passes_but_residual_exceeds_tolerance()
    {
        Assert.True(DxfElectricalFloorRegistrationEstimator.MeetsWholePlanEvidenceThresholds(
            MinimumCoverage,
            MinimumCoverage,
            DominantSelectorTolerance,
            DominantSelectorTolerance));
        Assert.False(DxfElectricalFloorRegistrationEstimator.MeetsWholePlanEvidenceThresholds(
            MinimumCoverage,
            MinimumCoverage,
            DominantSelectorTolerance + 0.001m,
            DominantSelectorTolerance + 0.001m));
    }

    [Fact]
    public async Task EstimateAsync_rejects_nonuniform_axis_scales_with_evidence()
    {
        var canonical = CreateAsymmetricStructure();
        var nonuniform = new SyntheticTransform(1.25m, 0.80m, 0, 5m, -8m);
        var electrical = InverseGenerate(canonical, nonuniform);

        var estimate = await EstimateAsync(canonical, electrical);

        AssertInsufficientEstimate(estimate);
        AssertClose(nonuniform.ScaleX, Require(estimate.ObservedScaleX, nameof(estimate.ObservedScaleX)));
        AssertClose(nonuniform.ScaleY, Require(estimate.ObservedScaleY, nameof(estimate.ObservedScaleY)));

        var rejected = Assert.Single(estimate.Candidates.Where(candidate => candidate.RotationDegrees == 0m));
        Assert.False(rejected.Accepted);
        Assert.Null(rejected.Scale);
        Assert.False(string.IsNullOrWhiteSpace(rejected.Reason));
        Assert.Contains("scale", rejected.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EstimateAsync_requires_interior_anchor_evidence_for_a_symmetric_rectangle()
    {
        var canonical = CreateOuterRectangle(0m, 0m, 16m, 16m);
        var electrical = InverseGenerate(
            canonical,
            SyntheticTransform.Similarity(1m, 0, 0m, 0m));

        var estimate = await EstimateAsync(canonical, electrical);

        AssertInsufficientEstimate(estimate);
        Assert.Empty(estimate.Candidates.Where(candidate => candidate.Accepted));
        Assert.Contains("outline-only", estimate.EvidenceSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EstimateAsync_fails_closed_when_only_one_interior_anchor_orientation_supports_rotation()
    {
        var canonical = CreateOneOrientationAnchorStructure();
        var expectedIfEvidenceWereSufficient = SyntheticTransform.Similarity(1m, 90, 8m, -12m);
        var electrical = InverseGenerate(canonical, expectedIfEvidenceWereSufficient);

        var estimate = await EstimateAsync(canonical, electrical);

        AssertInsufficientEstimate(estimate);

        var quarterTurn = Assert.Single(
            estimate.Candidates.Where(candidate => candidate.RotationDegrees == 90m));
        Assert.False(quarterTurn.Accepted);
        Assert.False(string.IsNullOrWhiteSpace(quarterTurn.Reason));
    }

    [Fact]
    public async Task EstimateAsync_does_not_treat_nonstructural_as_a_structural_layer_token()
    {
        var rectangle = CreateOuterRectangle(0m, 0m, 24m, 15m);

        var estimate = await EstimateAsync(
            rectangle,
            rectangle,
            canonicalLayer: "NONSTRUCTURAL",
            electricalLayer: "NONSTRUCTURAL");

        AssertInsufficientEstimate(estimate);
        Assert.NotEqual(ElectricalFloorRegistrationEstimateStatus.Ambiguous, estimate.Status);
    }

    [Fact]
    public async Task EstimateAsync_accepts_structural_lines_with_different_finite_endpoint_z_elevations()
    {
        var canonical = CreateAsymmetricStructure();
        var electricalRawEntities = CreateRawLinesWithElevatedEndpoint(canonical, ElectricalWallLayer);

        var estimate = await EstimateAsync(
            canonical,
            [],
            electricalRawEntityPairs: electricalRawEntities);

        AssertEstimatedTransform(estimate, SyntheticTransform.Similarity(1m, 0, 0m, 0m));
        Assert.Equal(1m, estimate.Transform!.Scale);
    }

    [Theory]
    [InlineData(UnsupportedStructuralEntityKind.OutOfDecimalRangeLwPolyline, "range")]
    [InlineData(UnsupportedStructuralEntityKind.NonfiniteBulgedLwPolyline, "bulge")]
    [InlineData(UnsupportedStructuralEntityKind.Nonfinite3dFace, "nonfinite")]
    [InlineData(UnsupportedStructuralEntityKind.OutOfDecimalRange3dFace, "range")]
    public async Task EstimateAsync_fails_closed_for_partial_or_unsupported_structural_entities(
        UnsupportedStructuralEntityKind entityKind,
        string evidenceToken)
    {
        var structure = CreateAsymmetricStructure();

        var estimate = await EstimateAsync(
            structure,
            structure,
            electricalRawEntityPairs: CreateUnsupportedStructuralEntity(entityKind));

        AssertInsufficientEstimate(estimate);
        Assert.Contains("ElectricalPlan", estimate.EvidenceSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(evidenceToken, estimate.EvidenceSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EstimateAsync_ignores_structural_arcs_when_straight_wall_evidence_is_sufficient()
    {
        var structure = CreateAsymmetricStructure();
        var canonicalArc = RawEntity(
            "ARC",
            CanonicalWallLayer,
            "10", "12",
            "20", "7.5",
            "30", "0",
            "40", "2",
            "50", "0",
            "51", "90");
        var electricalArc = RawEntity(
            "ARC",
            ElectricalWallLayer,
            "10", "12",
            "20", "7.5",
            "30", "0",
            "40", "2",
            "50", "0",
            "51", "90");

        var estimate = await EstimateAsync(
            structure,
            structure,
            canonicalRawEntityPairs: canonicalArc,
            electricalRawEntityPairs: electricalArc);

        AssertEstimatedTransform(estimate, SyntheticTransform.Similarity(1m, 0, 0m, 0m));
    }

    [Fact]
    public async Task EstimateAsync_ignores_structural_circles_when_straight_wall_evidence_is_sufficient()
    {
        var structure = CreateAsymmetricStructure();
        var canonicalCircle = RawEntity(
            "CIRCLE",
            CanonicalWallLayer,
            "10", "12",
            "20", "7.5",
            "30", "0",
            "40", "2");
        var electricalCircle = RawEntity(
            "CIRCLE",
            ElectricalWallLayer,
            "10", "12",
            "20", "7.5",
            "30", "0",
            "40", "2");

        var estimate = await EstimateAsync(
            structure,
            structure,
            canonicalRawEntityPairs: canonicalCircle,
            electricalRawEntityPairs: electricalCircle);

        AssertEstimatedTransform(estimate, SyntheticTransform.Similarity(1m, 0, 0m, 0m));
    }

    [Fact]
    public async Task EstimateAsync_ignores_structural_ellipses_when_straight_wall_evidence_is_sufficient()
    {
        var structure = CreateAsymmetricStructure();
        var canonicalEllipse = RawEntity(
            "ELLIPSE",
            CanonicalWallLayer,
            "10", "12", "20", "7.5", "30", "0",
            "11", "2", "21", "0", "31", "0",
            "40", "0.5",
            "41", "0", "42", "6.283185307179586");
        var electricalEllipse = RawEntity(
            "ELLIPSE",
            ElectricalWallLayer,
            "10", "12", "20", "7.5", "30", "0",
            "11", "2", "21", "0", "31", "0",
            "40", "0.5",
            "41", "0", "42", "6.283185307179586");

        var estimate = await EstimateAsync(
            structure,
            structure,
            canonicalRawEntityPairs: canonicalEllipse,
            electricalRawEntityPairs: electricalEllipse);

        AssertEstimatedTransform(estimate, SyntheticTransform.Similarity(1m, 0, 0m, 0m));
    }

    [Fact]
    public async Task EstimateAsync_ignores_finite_nonplanar_3dface_when_straight_wall_evidence_is_sufficient()
    {
        var structure = CreateAsymmetricStructure();
        var nonPlanarFace = CreateRaw3dFace(
            ElectricalWallLayer,
            13m,
            7m,
            13.25m,
            7.25m,
            thirdCornerZ: 0.75m);

        var estimate = await EstimateAsync(
            structure,
            structure,
            electricalRawEntityPairs: nonPlanarFace);

        AssertEstimatedTransform(estimate, SyntheticTransform.Similarity(1m, 0, 0m, 0m));
    }

    [Fact]
    public async Task EstimateAsync_ignores_finite_bulged_polyline_edges_and_retains_straight_edges()
    {
        var estimate = await EstimateAsync(
            [],
            [],
            canonicalRawEntityPairs: CreateStraightWallPolylineWithBulgedEdge(CanonicalWallLayer)
                .Concat(CreateRawInteriorAnchors(CanonicalWallLayer))
                .ToArray(),
            electricalRawEntityPairs: CreateStraightWallPolylineWithBulgedEdge(ElectricalWallLayer)
                .Concat(CreateRawInteriorAnchors(ElectricalWallLayer))
                .ToArray());

        AssertEstimatedTransform(estimate, SyntheticTransform.Similarity(1m, 0, 0m, 0m));
    }

    [Fact]
    public async Task EstimateAsync_ignores_fully_invisible_3dface_edges_instead_of_fabricating_an_outline()
    {
        const int allEdgesInvisible = 15;
        var canonicalFace = CreateRaw3dFace(
            CanonicalWallLayer,
            0m,
            0m,
            24m,
            15m,
            invisibleEdgeFlags: allEdgesInvisible);
        var electricalFace = CreateRaw3dFace(
            ElectricalWallLayer,
            0m,
            0m,
            24m,
            15m,
            invisibleEdgeFlags: allEdgesInvisible);

        var estimate = await EstimateAsync(
            [],
            [],
            canonicalRawEntityPairs: canonicalFace,
            electricalRawEntityPairs: electricalFace);

        AssertInsufficientEstimate(estimate);
        Assert.NotEqual(ElectricalFloorRegistrationEstimateStatus.Ambiguous, estimate.Status);
    }

    [Fact]
    public async Task EstimateAsync_estimates_when_many_local_frame_matches_have_one_whole_plan_transform()
    {
        var multipleFrames = CreateAsymmetricStructure()
            .Concat(Translate(CreateAsymmetricStructure(), 100m, 100m))
            .ToArray();

        var estimate = await EstimateAsync(
            multipleFrames,
            multipleFrames);

        AssertEstimatedTransform(
            estimate,
            SyntheticTransform.Similarity(1m, 0, 0m, 0m),
            requireSingleAcceptedCandidate: false);
        Assert.True(estimate.HorizontalCoverage.HasValue);
        Assert.True(estimate.VerticalCoverage.HasValue);
        Assert.True(estimate.RootMeanSquareResidual.HasValue);
        Assert.True(estimate.MaximumResidual.HasValue);
        Assert.True(
            estimate.Candidates
                .Where(candidate => candidate.Accepted)
                .Select(candidate => (candidate.Scale, candidate.RotationDegrees, candidate.TranslateX, candidate.TranslateY))
                .Distinct()
                .Count() > 1,
            "The fixture must retain multiple locally proven transforms before whole-plan filtering.");
        Assert.Contains("Exactly one transform group passed", estimate.EvidenceSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EstimateAsync_ignores_local_vote_count_when_only_lower_support_transform_covers_the_whole_plan()
    {
        var dominantStructure = ScaleCoordinates(CreateAsymmetricStructure(), 10m, 11m);
        var repeatedLocalFrames = new[] { 400m, 500m, 600m }
            .SelectMany(x => Translate(CreateAsymmetricStructure(), x, 300m))
            .ToArray();
        var canonical = dominantStructure.Concat(repeatedLocalFrames).ToArray();
        var electrical = dominantStructure
            .Concat(Translate(repeatedLocalFrames, 1000m, 0m))
            .ToArray();

        var estimate = await EstimateAsync(canonical, electrical);

        AssertEstimatedTransform(
            estimate,
            SyntheticTransform.Similarity(1m, 0, 0m, 0m),
            requireSingleAcceptedCandidate: false);
        Assert.Contains(
            "scale 1, 0 degrees, translation (0, 0), support 1 frame-pair evaluations",
            estimate.EvidenceSummary,
            StringComparison.Ordinal);
        Assert.Contains(
            "scale 1, 0 degrees, translation (-1000, 0), support 3 frame-pair evaluations",
            estimate.EvidenceSummary,
            StringComparison.Ordinal);
        Assert.True(
            Require(estimate.HorizontalCoverage, nameof(estimate.HorizontalCoverage)) is >= MinimumCoverage and < 0.80m);
        Assert.True(
            Require(estimate.VerticalCoverage, nameof(estimate.VerticalCoverage)) is >= MinimumCoverage and < 0.80m);
    }

    [Fact]
    public async Task EstimateAsync_summarizes_equivalent_transform_once_with_support_count()
    {
        var repeatedStructure = CreateAsymmetricStructure()
            .Concat(Translate(CreateAsymmetricStructure(), 100m, 100m))
            .ToArray();

        var estimate = await EstimateAsync(repeatedStructure, repeatedStructure);

        AssertEstimatedTransform(
            estimate,
            SyntheticTransform.Similarity(1m, 0, 0m, 0m),
            requireSingleAcceptedCandidate: false);
        const string groupedIdentity =
            "scale 1, 0 degrees, translation (0, 0), support 2 frame-pair evaluations";
        Assert.Equal(
            1,
            estimate.EvidenceSummary.Split(groupedIdentity, StringSplitOptions.None).Length - 1);
        Assert.Contains(
            $"{groupedIdentity}, global canonical-to-transformed-Electrical H coverage 1, V coverage 1, RMS residual 0, maximum residual 0",
            estimate.EvidenceSummary,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GroupEquivalentTransforms_is_order_independent_and_does_not_single_link_tolerance_chain()
    {
        var hypotheses = new[]
        {
            new DxfElectricalFloorRegistrationEstimator.TransformHypothesis(0, 0m, 1m, 0m, 0m),
            new DxfElectricalFloorRegistrationEstimator.TransformHypothesis(1, 0m, 1m, 0.04m, 0m),
            new DxfElectricalFloorRegistrationEstimator.TransformHypothesis(2, 0m, 1m, 0.08m, 0m)
        };

        var forward = DxfElectricalFloorRegistrationEstimator.GroupEquivalentTransforms(hypotheses);
        var reversed = DxfElectricalFloorRegistrationEstimator.GroupEquivalentTransforms(
            hypotheses.Reverse().ToArray());
        var forwardSummary = SummarizeTransformGroups(forward);
        var reversedSummary = SummarizeTransformGroups(reversed);

        Assert.Equal(new[] { "0,0.04|support 2", "0.08|support 1" }, forwardSummary);
        Assert.Equal(forwardSummary, reversedSummary);
        Assert.All(
            forward,
            group => Assert.True(
                group.Max(hypothesis => hypothesis.TranslateX) -
                group.Min(hypothesis => hypothesis.TranslateX) <= DominantSelectorTolerance));
        Assert.DoesNotContain(
            forward,
            group => group.Any(hypothesis => hypothesis.TranslateX == 0m) &&
                     group.Any(hypothesis => hypothesis.TranslateX == 0.08m));
    }

    [Fact]
    public async Task EstimateAsync_keeps_status_and_summary_stable_when_tolerance_chain_input_order_reverses()
    {
        var canonical = new[] { 0m, 100m, 200m }
            .SelectMany(translateX => Translate(CreateAsymmetricStructure(), translateX, 0m))
            .ToArray();
        var electrical = new[] { 0m, 99.96m, 199.92m }
            .SelectMany(translateX => Translate(CreateAsymmetricStructure(), translateX, 0m))
            .ToArray();

        var forward = await EstimateAsync(canonical, electrical);
        var reversed = await EstimateAsync(
            canonical.Reverse().ToArray(),
            electrical.Reverse().ToArray());

        AssertInsufficientEstimate(forward);
        AssertInsufficientEstimate(reversed);
        Assert.Equal(forward.Status, reversed.Status);
        Assert.Equal(forward.EvidenceSummary, reversed.EvidenceSummary);
        foreach (var expectedTranslation in new[] { 0m, 0.04m, 0.08m })
        {
            Assert.Contains(
                forward.Candidates,
                candidate => candidate.Accepted &&
                             candidate.RotationDegrees == 0m &&
                             candidate.Scale == 1m &&
                             candidate.TranslateX == expectedTranslation &&
                             candidate.TranslateY == 0m);
        }
    }

    [Fact]
    public async Task EstimateAsync_fails_closed_when_large_finite_coordinates_overflow_scale_fit_squaring()
    {
        const decimal largeCoordinate = 10_000_000_000_000_000m;
        var canonical = CreateOuterRectangle(
            0m,
            0m,
            largeCoordinate * 2m,
            largeCoordinate);
        var electrical = CreateOuterRectangle(
            0m,
            0m,
            largeCoordinate,
            largeCoordinate / 2m);

        var estimate = await EstimateAsync(canonical, electrical);

        AssertInsufficientEstimate(estimate);
        Assert.Contains("scale", estimate.EvidenceSummary, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ElectricalFloorRegistrationEstimate> EstimateAsync(
        IReadOnlyList<LineSegment> canonical,
        IReadOnlyList<LineSegment> electrical,
        string canonicalLayer = CanonicalWallLayer,
        string electricalLayer = ElectricalWallLayer,
        IReadOnlyList<string>? canonicalRawEntityPairs = null,
        IReadOnlyList<string>? electricalRawEntityPairs = null)
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            $"floorplan-fit-registration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var canonicalPath = Path.Combine(tempRoot, "canonical-floor.dxf");
        var electricalPath = Path.Combine(tempRoot, "electrical-plan.dxf");

        try
        {
            await WriteAsciiDxfAsync(
                canonicalPath,
                canonicalLayer,
                canonical,
                canonicalRawEntityPairs);
            await WriteAsciiDxfAsync(
                electricalPath,
                electricalLayer,
                electrical,
                electricalRawEntityPairs);
            IElectricalFloorRegistrationEstimator estimator = new DxfElectricalFloorRegistrationEstimator();

            var estimate = await estimator.EstimateAsync(
                canonicalPath,
                electricalPath,
                CancellationToken.None);
            if (estimate.IsConclusive)
            {
                Assert.Equal(await ComputeSha256Async(canonicalPath), estimate.CanonicalSourceSha256);
                Assert.Equal(await ComputeSha256Async(electricalPath), estimate.DependentSourceSha256);
            }

            return estimate;
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static async Task<string> ComputeSha256Async(string path)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream)).ToLowerInvariant();
    }

    private static IReadOnlyList<LineSegment> CreateAsymmetricStructure()
        =>
        [
            .. CreateOuterRectangle(0m, 0m, 24m, 15m),
            new(new Point(2m, 4m), new Point(11m, 4m)),
            new(new Point(9m, 11m), new Point(21m, 11m)),
            new(new Point(5m, 2m), new Point(5m, 9m)),
            new(new Point(18m, 6m), new Point(18m, 14m))
        ];

    private static IReadOnlyList<LineSegment> CreateOneOrientationAnchorStructure()
        =>
        [
            .. CreateOuterRectangle(0m, 0m, 18m, 18m),
            new(new Point(2m, 4m), new Point(13m, 4m)),
            new(new Point(7m, 12m), new Point(16m, 12m))
        ];

    private static IReadOnlyList<LineSegment> CreateDisconnectedHigherRankedPairs()
        =>
        [
            new(new Point(100m, 100m), new Point(100m, 140m)),
            new(new Point(200m, 100m), new Point(200m, 140m)),
            new(new Point(0m, 200m), new Point(60m, 200m)),
            new(new Point(0m, 300m), new Point(60m, 300m))
        ];

    private static IReadOnlyList<LineSegment> CreateThreeCornerFrame(
        decimal minX,
        decimal minY,
        decimal maxX,
        decimal maxY)
        =>
        [
            new(new Point(minX, minY), new Point(maxX, minY)),
            new(new Point(maxX, minY), new Point(maxX, maxY)),
            new(new Point(maxX, maxY), new Point(minX, maxY))
        ];

    private static IReadOnlyList<LineSegment> CreateOuterRectangle(
        decimal minX,
        decimal minY,
        decimal maxX,
        decimal maxY)
        =>
        [
            new(new Point(minX, minY), new Point(maxX, minY)),
            new(new Point(maxX, minY), new Point(maxX, maxY)),
            new(new Point(maxX, maxY), new Point(minX, maxY)),
            new(new Point(minX, maxY), new Point(minX, minY))
        ];

    private static IReadOnlyList<LineSegment> InverseGenerate(
        IReadOnlyList<LineSegment> canonical,
        SyntheticTransform transform)
        => canonical
            .Select(line => new LineSegment(
                transform.Invert(line.Start),
                transform.Invert(line.End)))
            .ToArray();

    private static IReadOnlyList<LineSegment> Translate(
        IReadOnlyList<LineSegment> lines,
        decimal translateX,
        decimal translateY)
        => lines
            .Select(line => new LineSegment(
                new Point(line.Start.X + translateX, line.Start.Y + translateY),
                new Point(line.End.X + translateX, line.End.Y + translateY)))
            .ToArray();

    private static IReadOnlyList<LineSegment> ScaleAboutBoundsCenter(
        IReadOnlyList<LineSegment> lines,
        decimal scale)
    {
        var bounds = GetBounds(lines);

        Point Scale(Point point) => new(
            bounds.CenterX + ((point.X - bounds.CenterX) * scale),
            bounds.CenterY + ((point.Y - bounds.CenterY) * scale));

        return lines
            .Select(line => new LineSegment(Scale(line.Start), Scale(line.End)))
            .ToArray();
    }

    private static IReadOnlyList<LineSegment> ScaleCoordinates(
        IReadOnlyList<LineSegment> lines,
        decimal scaleX,
        decimal scaleY)
        => lines
            .Select(line => new LineSegment(
                new Point(line.Start.X * scaleX, line.Start.Y * scaleY),
                new Point(line.End.X * scaleX, line.End.Y * scaleY)))
            .ToArray();

    private static SyntheticBounds GetBounds(IReadOnlyList<LineSegment> lines)
    {
        var points = lines.SelectMany(line => new[] { line.Start, line.End }).ToArray();
        return new SyntheticBounds(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
    }

    private static IReadOnlyList<LineSegment> CreateShortOutboardFragments(
        IReadOnlyList<LineSegment> lines)
    {
        var bounds = GetBounds(lines);

        return
        [
            new(
                new Point(bounds.MaxX + 5m, bounds.MinY + 2m),
                new Point(bounds.MaxX + 5m, bounds.MinY + 2.25m)),
            new(
                new Point(bounds.MaxX + 6m, bounds.MinY + 2m),
                new Point(bounds.MaxX + 6m, bounds.MinY + 2.25m)),
            new(
                new Point(bounds.MinX + 2m, bounds.MinY - 5m),
                new Point(bounds.MinX + 2.25m, bounds.MinY - 5m)),
            new(
                new Point(bounds.MinX + 2m, bounds.MinY - 6m),
                new Point(bounds.MinX + 2.25m, bounds.MinY - 6m))
        ];
    }

    private static IReadOnlyList<string> CreateUnsupportedStructuralEntity(
        UnsupportedStructuralEntityKind entityKind)
        => entityKind switch
        {
            UnsupportedStructuralEntityKind.OutOfDecimalRangeLwPolyline => RawEntity(
                "LWPOLYLINE",
                ElectricalWallLayer,
                "90", "3",
                "70", "0",
                "10", "2",
                "20", "8",
                "10", "1E100",
                "20", "8",
                "10", "4",
                "20", "8"),
            UnsupportedStructuralEntityKind.NonfiniteBulgedLwPolyline => RawEntity(
                "LWPOLYLINE",
                ElectricalWallLayer,
                "90", "2",
                "70", "0",
                "10", "2",
                "20", "8",
                "42", "1E1000",
                "10", "3",
                "20", "8"),
            UnsupportedStructuralEntityKind.Nonfinite3dFace => RawEntity(
                "3DFACE",
                ElectricalWallLayer,
                "70", "0",
                "10", "13", "20", "7", "30", "0",
                "11", "13.25", "21", "7", "31", "0",
                "12", "13.25", "22", "7.25", "32", "1E1000",
                "13", "13", "23", "7.25", "33", "0"),
            UnsupportedStructuralEntityKind.OutOfDecimalRange3dFace => RawEntity(
                "3DFACE",
                ElectricalWallLayer,
                "70", "0",
                "10", "13", "20", "7", "30", "0",
                "11", "13.25", "21", "7", "31", "0",
                "12", "1E100", "22", "7.25", "32", "0",
                "13", "13", "23", "7.25", "33", "0"),
            _ => throw new ArgumentOutOfRangeException(nameof(entityKind))
        };

    private static IReadOnlyList<string> CreateStraightWallPolylineWithBulgedEdge(string layer)
        => RawEntity(
            "LWPOLYLINE",
            layer,
            "90", "6",
            "70", "0",
            "10", "0", "20", "0", "42", "0",
            "10", "24", "20", "0", "42", "0",
            "10", "24", "20", "15", "42", "0",
            "10", "0", "20", "15", "42", "0",
            "10", "0", "20", "0", "42", "0.5",
            "10", "3", "20", "3", "42", "0");

    private static IReadOnlyList<string> CreateRawInteriorAnchors(string layer)
        =>
        [
            .. RawEntity("LINE", layer, "10", "2", "20", "4", "11", "11", "21", "4"),
            .. RawEntity("LINE", layer, "10", "9", "20", "11", "11", "21", "11"),
            .. RawEntity("LINE", layer, "10", "5", "20", "2", "11", "5", "21", "9"),
            .. RawEntity("LINE", layer, "10", "18", "20", "6", "11", "18", "21", "14")
        ];

    private static IReadOnlyList<string> CreateRaw3dFace(
        string layer,
        decimal minX,
        decimal minY,
        decimal maxX,
        decimal maxY,
        decimal thirdCornerZ = 0m,
        int invisibleEdgeFlags = 0)
        => RawEntity(
            "3DFACE",
            layer,
            "70", invisibleEdgeFlags.ToString(CultureInfo.InvariantCulture),
            "10", Format(minX),
            "20", Format(minY),
            "30", "0",
            "11", Format(maxX),
            "21", Format(minY),
            "31", "0",
            "12", Format(maxX),
            "22", Format(maxY),
            "32", Format(thirdCornerZ),
            "13", Format(minX),
            "23", Format(maxY),
            "33", "0");

    private static IReadOnlyList<string> CreateRawLinesWithElevatedEndpoint(
        IReadOnlyList<LineSegment> lines,
        string layer)
        => lines
            .SelectMany((line, index) => RawEntity(
                "LINE",
                layer,
                "10", Format(line.Start.X),
                "20", Format(line.Start.Y),
                "30", "0",
                "11", Format(line.End.X),
                "21", Format(line.End.Y),
                "31", Format(index == 0 ? 3m : 0m)))
            .ToArray();

    private static IReadOnlyList<string> RawEntity(
        string entityType,
        string layer,
        params string[] bodyPairs)
        => ["0", entityType, "8", layer, .. bodyPairs];

    private static Task WriteAsciiDxfAsync(
        string path,
        string layer,
        IReadOnlyList<LineSegment> lines,
        IReadOnlyList<string>? rawEntityPairs = null)
    {
        var pairs = new List<string>
        {
            "0", "SECTION",
            "2", "HEADER",
            "9", "$ACADVER",
            "1", "AC1027",
            "9", "$INSUNITS",
            "70", "1",
            "0", "ENDSEC",
            "0", "SECTION",
            "2", "ENTITIES"
        };

        foreach (var line in lines)
        {
            pairs.AddRange(
            [
                "0", "LINE",
                "8", layer,
                "10", Format(line.Start.X),
                "20", Format(line.Start.Y),
                "30", "0",
                "11", Format(line.End.X),
                "21", Format(line.End.Y),
                "31", "0"
            ]);
        }

        if (rawEntityPairs is not null)
        {
            pairs.AddRange(rawEntityPairs);
        }

        pairs.AddRange(["0", "ENDSEC", "0", "EOF"]);
        return File.WriteAllTextAsync(
            path,
            string.Join(Environment.NewLine, pairs),
            CancellationToken.None);
    }

    private static void AssertEstimatedTransform(
        ElectricalFloorRegistrationEstimate estimate,
        SyntheticTransform expected,
        bool requireSingleAcceptedCandidate = true)
    {
        Assert.Equal(ElectricalFloorRegistrationEstimateStatus.Estimated, estimate.Status);
        Assert.True(estimate.IsConclusive);
        Assert.NotNull(estimate.Transform);
        AssertClose(expected.ScaleX, estimate.Transform!.Scale);
        AssertClose(expected.RotationDegrees, estimate.Transform.RotationDegrees);
        AssertClose(expected.TranslateX, estimate.Transform.TranslateX);
        AssertClose(expected.TranslateY, estimate.Transform.TranslateY);
        var accepted = estimate.Candidates.Where(candidate => candidate.Accepted).ToArray();
        if (requireSingleAcceptedCandidate)
        {
            Assert.Single(accepted);
        }
        else
        {
            Assert.NotEmpty(accepted);
        }
    }

    private static void AssertInsufficientEstimate(ElectricalFloorRegistrationEstimate estimate)
    {
        Assert.Equal(ElectricalFloorRegistrationEstimateStatus.InsufficientEvidence, estimate.Status);
        Assert.False(estimate.IsConclusive);
        Assert.Null(estimate.Transform);
        Assert.False(string.IsNullOrWhiteSpace(estimate.EvidenceSummary));
    }

    private static void AssertAmbiguousEstimate(ElectricalFloorRegistrationEstimate estimate)
    {
        Assert.Equal(ElectricalFloorRegistrationEstimateStatus.Ambiguous, estimate.Status);
        Assert.False(estimate.IsConclusive);
        Assert.Null(estimate.Transform);
        Assert.False(string.IsNullOrWhiteSpace(estimate.EvidenceSummary));
    }

    private static void AssertStrongTwoAxisEvidence(ElectricalFloorRegistrationEstimate estimate)
    {
        Assert.True(estimate.Confidence >= 0.80m, "Expected strong registration confidence.");
        Assert.True(
            Require(estimate.HorizontalCoverage, nameof(estimate.HorizontalCoverage)) >= MinimumCoverage,
            "Expected strong horizontal structural coverage.");
        Assert.True(
            Require(estimate.VerticalCoverage, nameof(estimate.VerticalCoverage)) >= MinimumCoverage,
            "Expected strong vertical structural coverage.");
    }

    private static void AssertLowResiduals(ElectricalFloorRegistrationEstimate estimate)
        => AssertResidualsAtMost(estimate, CoordinateTolerance);

    private static void AssertResidualsAtMost(
        ElectricalFloorRegistrationEstimate estimate,
        decimal tolerance)
    {
        AssertAtMost(
            estimate.RootMeanSquareResidual,
            nameof(estimate.RootMeanSquareResidual),
            tolerance);
        AssertAtMost(estimate.MaximumResidual, nameof(estimate.MaximumResidual), tolerance);

        var accepted = Assert.Single(estimate.Candidates.Where(candidate => candidate.Accepted));
        AssertAtMost(
            accepted.RootMeanSquareResidual,
            nameof(accepted.RootMeanSquareResidual),
            tolerance);
        AssertAtMost(accepted.MaximumResidual, nameof(accepted.MaximumResidual), tolerance);
        AssertAtMost(accepted.LeftEdgeResidual, nameof(accepted.LeftEdgeResidual), tolerance);
        AssertAtMost(accepted.RightEdgeResidual, nameof(accepted.RightEdgeResidual), tolerance);
        AssertAtMost(accepted.BottomEdgeResidual, nameof(accepted.BottomEdgeResidual), tolerance);
        AssertAtMost(accepted.TopEdgeResidual, nameof(accepted.TopEdgeResidual), tolerance);
    }

    private static void AssertAtMost(decimal? value, string label, decimal tolerance)
    {
        var actual = Require(value, label);
        Assert.True(
            actual <= tolerance,
            $"Expected {label} <= {tolerance}, but found {actual}.");
    }

    private static decimal Require(decimal? value, string label)
    {
        Assert.True(value.HasValue, $"Expected {label} evidence.");
        return value.GetValueOrDefault();
    }

    private static string[] SummarizeTransformGroups(
        IReadOnlyList<IReadOnlyList<DxfElectricalFloorRegistrationEstimator.TransformHypothesis>> groups)
        => groups
            .Select(group =>
                $"{string.Join(",", group.Select(hypothesis => Format(hypothesis.TranslateX)))}|support {group.Count}")
            .ToArray();

    private static void AssertClose(decimal expected, decimal actual)
        => Assert.True(
            Math.Abs(expected - actual) <= CoordinateTolerance,
            $"Expected {expected} +/- {CoordinateTolerance}, but found {actual}.");

    private static string Format(decimal value)
        => value.ToString("0.############################", CultureInfo.InvariantCulture);

    private readonly record struct Point(decimal X, decimal Y);

    private readonly record struct LineSegment(Point Start, Point End);

    private readonly record struct SyntheticBounds(
        decimal MinX,
        decimal MinY,
        decimal MaxX,
        decimal MaxY)
    {
        public decimal Width => MaxX - MinX;

        public decimal Height => MaxY - MinY;

        public decimal CenterX => (MinX + MaxX) / 2m;

        public decimal CenterY => (MinY + MaxY) / 2m;
    }

    public enum UnsupportedStructuralEntityKind
    {
        OutOfDecimalRangeLwPolyline,
        NonfiniteBulgedLwPolyline,
        Nonfinite3dFace,
        OutOfDecimalRange3dFace
    }

    private readonly record struct SyntheticTransform(
        decimal ScaleX,
        decimal ScaleY,
        int RotationDegrees,
        decimal TranslateX,
        decimal TranslateY)
    {
        public static SyntheticTransform Similarity(
            decimal scale,
            int rotationDegrees,
            decimal translateX,
            decimal translateY)
            => new(scale, scale, rotationDegrees, translateX, translateY);

        public Point Invert(Point canonical)
        {
            var translated = new Point(
                canonical.X - TranslateX,
                canonical.Y - TranslateY);
            var unrotated = Rotate(translated, -RotationDegrees);

            return new Point(unrotated.X / ScaleX, unrotated.Y / ScaleY);
        }

        private static Point Rotate(Point point, int rotationDegrees)
            => NormalizeQuarterTurn(rotationDegrees) switch
            {
                0 => point,
                90 => new Point(-point.Y, point.X),
                180 => new Point(-point.X, -point.Y),
                270 => new Point(point.Y, -point.X),
                _ => throw new ArgumentOutOfRangeException(nameof(rotationDegrees))
            };

        private static int NormalizeQuarterTurn(int rotationDegrees)
            => ((rotationDegrees % 360) + 360) % 360;
    }
}
