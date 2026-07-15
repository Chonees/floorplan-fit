using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Geometry;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

[assembly: InternalsVisibleTo("FloorplanFit.Infrastructure.Tests")]

namespace FloorplanFit.Infrastructure.Dxf;

public sealed class DxfElectricalFloorRegistrationEstimator : IElectricalFloorRegistrationEstimator
{
    private const decimal Tolerance = DominantAxisAlignedOutlineSelector.CoordinateTolerance;
    private const double DxfNumericTolerance = 1e-9d;

    private static readonly int[] QuarterTurns = [0, 90, 180, 270];
    private static readonly HashSet<string> StructuralLayerTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "WALL", "WALLS", "EXTERIOR", "STRUCT", "STRUCTURAL"
    };
    private static readonly Regex LayerTokenSeparator = new(
        "[^A-Za-z0-9]+",
        RegexOptions.CultureInvariant);

    public Task<ElectricalFloorRegistrationEstimate> EstimateAsync(
        string canonicalFloorSourcePath,
        string electricalSourcePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var canonicalPath = ValidatePath(
            canonicalFloorSourcePath,
            nameof(canonicalFloorSourcePath));
        var electricalPath = ValidatePath(
            electricalSourcePath,
            nameof(electricalSourcePath));
        var canonicalHashBefore = ComputeSha256(canonicalPath);
        var dependentHashBefore = ComputeSha256(electricalPath);

        var canonicalExtraction = ExtractStructuralSegments(
            canonicalPath,
            "Canonical FloorPlan",
            cancellationToken);
        var electricalExtraction = ExtractStructuralSegments(
            electricalPath,
            "ElectricalPlan",
            cancellationToken);
        var canonicalHashAfter = ComputeSha256(canonicalPath);
        var dependentHashAfter = ComputeSha256(electricalPath);
        if (!string.Equals(canonicalHashBefore, canonicalHashAfter, StringComparison.Ordinal) ||
            !string.Equals(dependentHashBefore, dependentHashAfter, StringComparison.Ordinal))
        {
            return Task.FromResult(Insufficient(
                "A registration source changed while it was being read; automatic registration was rejected."));
        }

        if (canonicalExtraction.Failure is not null || electricalExtraction.Failure is not null)
        {
            var failures = new[]
                {
                    canonicalExtraction.Failure,
                    electricalExtraction.Failure
                }
                .OfType<string>();
            return Task.FromResult(Insufficient(string.Join(" ", failures)));
        }

        try
        {
            var estimate = EstimateExtracted(
                canonicalExtraction.Segments,
                electricalExtraction.Segments,
                cancellationToken);
            return Task.FromResult(estimate with
            {
                CanonicalSourceSha256 = canonicalHashAfter,
                DependentSourceSha256 = dependentHashAfter
            });
        }
        catch (OverflowException exception)
        {
            return Task.FromResult(Insufficient(
                $"Registration scale/outline/residual arithmetic overflowed decimal range; automatic registration was rejected ({exception.Message})."));
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static ElectricalFloorRegistrationEstimate EstimateExtracted(
        IReadOnlyList<StructuralSegment> canonicalSegments,
        IReadOnlyList<StructuralSegment> electricalSegments,
        CancellationToken cancellationToken)
    {
        var canonicalFrames = SelectConnectedFrames(canonicalSegments);
        var electricalFrames = SelectConnectedFrames(electricalSegments);
        var selectorEvidence = BuildSelectorEvidence(
            canonicalFrames,
            electricalFrames);

        if (!canonicalFrames.HasCandidates || !electricalFrames.HasCandidates)
        {
            return Insufficient(
                $"Connected structural frame selection failed closed. {selectorEvidence}");
        }

        var evaluations = new List<CandidateEvaluation>();
        var scaleRejectionDiagnostics = new Dictionary<int, CandidateEvaluation>();
        var canonicalInteriorCache = new Dictionary<DominantAxisAlignedOutline, IReadOnlyList<StructuralSegment>>();
        var electricalInteriorCache = new Dictionary<DominantAxisAlignedOutline, IReadOnlyList<StructuralSegment>>();
        ScaleDiagnostic? scaleDiagnostic = null;
        var scaleCompatibleCandidates = 0;
        foreach (var canonicalFrame in canonicalFrames.Candidates)
        {
            var canonicalBounds = Bounds.From(canonicalFrame.Outline);
            foreach (var electricalFrame in electricalFrames.Candidates)
            {
                var electricalOutline = electricalFrame.Outline;
                foreach (var rotation in QuarterTurns)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rotatedElectricalBounds = RotateBounds(electricalOutline, rotation);
                    var scaleFit = FitUniformScale(canonicalBounds, rotatedElectricalBounds);
                    var placement = AlignCenters(
                        canonicalBounds,
                        rotatedElectricalBounds,
                        scaleFit.TrialScale);
                    if (scaleDiagnostic is null ||
                        scaleFit.Score < scaleDiagnostic.ScaleFit.Score ||
                        (scaleFit.Score == scaleDiagnostic.ScaleFit.Score &&
                         rotation < scaleDiagnostic.RotationDegrees))
                    {
                        scaleDiagnostic = new ScaleDiagnostic(rotation, scaleFit);
                    }

                    if (!scaleFit.Accepted)
                    {
                        if (!scaleRejectionDiagnostics.TryGetValue(rotation, out var existing) ||
                            scaleFit.Score < existing.ScaleFitScore)
                        {
                            scaleRejectionDiagnostics[rotation] = CreateScaleRejectedEvaluation(
                                rotation,
                                scaleFit,
                                placement,
                                BuildFramePairEvidence(
                                    canonicalFrame,
                                    electricalFrame,
                                    selectorEvidence));
                        }

                        continue;
                    }

                    scaleCompatibleCandidates++;
                    var pairEvidence = BuildFramePairEvidence(
                        canonicalFrame,
                        electricalFrame,
                        selectorEvidence);
                    if (!canonicalInteriorCache.TryGetValue(canonicalFrame.Outline, out var canonicalInterior))
                    {
                        canonicalInterior = ClipInteriorSegments(
                            canonicalSegments,
                            canonicalFrame.Outline);
                        canonicalInteriorCache.Add(canonicalFrame.Outline, canonicalInterior);
                    }

                    if (!electricalInteriorCache.TryGetValue(electricalOutline, out var electricalInterior))
                    {
                        electricalInterior = ClipInteriorSegments(
                            electricalSegments,
                            electricalOutline);
                        electricalInteriorCache.Add(electricalOutline, electricalInterior);
                    }

                    evaluations.Add(EvaluateCandidate(
                        rotation,
                        canonicalInterior,
                        electricalInterior,
                        canonicalInterior.Count == 0 && electricalInterior.Count == 0,
                        pairEvidence,
                        scaleFit,
                        placement));
                }
            }
        }

        if (evaluations.Count == 0)
        {
            evaluations.AddRange(scaleRejectionDiagnostics.Values.OrderBy(candidate => candidate.Evidence.RotationDegrees));
        }

        var accepted = evaluations
            .Where(evaluation => evaluation.Evidence.Accepted)
            .ToArray();
        var evidenceComplete = accepted
            .Where(evaluation => evaluation.EvidenceComplete)
            .ToArray();
        var transformGroups = GroupCandidateEquivalentTransforms(evidenceComplete);
        var globalEvaluations = new List<GlobalTransformEvaluation>(transformGroups.Count);
        foreach (var group in transformGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            globalEvaluations.Add(EvaluateGlobalTransform(
                group,
                canonicalSegments,
                electricalSegments));
        }

        var survivingTransforms = globalEvaluations
            .Where(evaluation => evaluation.EvidenceComplete)
            .ToArray();

        CandidateEvaluation? estimated = null;
        GlobalTransformEvaluation? estimatedGlobal = null;
        ElectricalFloorRegistrationEstimateStatus status;
        if (survivingTransforms.Length > 1)
        {
            status = ElectricalFloorRegistrationEstimateStatus.Ambiguous;
        }
        else if (survivingTransforms.Length == 1)
        {
            status = ElectricalFloorRegistrationEstimateStatus.Estimated;
            estimatedGlobal = survivingTransforms[0];
            estimated = estimatedGlobal.Group.Representative;
        }
        else
        {
            status = ElectricalFloorRegistrationEstimateStatus.InsufficientEvidence;
        }

        var selectedScaleDiagnostic = scaleDiagnostic!;
        var metricDiagnostic = estimated ?? SelectMetricDiagnostic(evaluations, accepted);
        var confidence = estimatedGlobal?.Confidence ?? metricDiagnostic.Confidence;
        var horizontalCoverage = estimatedGlobal?.Horizontal?.Coverage ??
                                 metricDiagnostic.Evidence.HorizontalCoverage;
        var verticalCoverage = estimatedGlobal?.Vertical?.Coverage ??
                               metricDiagnostic.Evidence.VerticalCoverage;
        var rootMeanSquareResidual = estimatedGlobal?.Residuals.RootMeanSquare ??
                                     metricDiagnostic.Evidence.RootMeanSquareResidual;
        var maximumResidual = estimatedGlobal?.Residuals.Maximum ??
                              metricDiagnostic.Evidence.MaximumResidual;
        var transform = estimated is null
            ? null
            : new SheetRegistrationTransform(
                estimated.Evidence.Scale.GetValueOrDefault(),
                estimated.Evidence.RotationDegrees,
                estimated.Evidence.TranslateX.GetValueOrDefault(),
                estimated.Evidence.TranslateY.GetValueOrDefault());

        return new ElectricalFloorRegistrationEstimate(
            status,
            transform,
            confidence,
            selectedScaleDiagnostic.ScaleFit.ObservedScaleX,
            selectedScaleDiagnostic.ScaleFit.ObservedScaleY,
            horizontalCoverage,
            verticalCoverage,
            rootMeanSquareResidual,
            maximumResidual,
            BuildSummary(
                status,
                globalEvaluations,
                selectedScaleDiagnostic,
                metricDiagnostic.Rationale,
                selectorEvidence,
                (long)canonicalFrames.Candidates.Count * electricalFrames.Candidates.Count,
                scaleCompatibleCandidates),
            evaluations.Select(evaluation => evaluation.Evidence).ToArray());
    }

    private static string ValidatePath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A DXF source path is required.", parameterName);
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ArgumentException("The DXF source path is invalid.", parameterName, exception);
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The DXF source file was not found.", fullPath);
        }

        return fullPath;
    }

    private static SegmentExtraction ExtractStructuralSegments(
        string path,
        string sourceLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dxf = DxfFile.Load(path);
            cancellationToken.ThrowIfCancellationRequested();

            var segments = new List<StructuralSegment>();
            foreach (var entity in dxf.Entities)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsStructuralLayer(entity.Layer))
                {
                    continue;
                }

                var failure = AddEntitySegments(entity, segments);
                if (failure is not null)
                {
                    return new SegmentExtraction(
                        [],
                        $"{sourceLabel} structural entity extraction failed closed: {failure}");
                }
            }

            return new SegmentExtraction(segments, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new SegmentExtraction(
                [],
                $"{sourceLabel} DXF could not be read; automatic registration was rejected ({exception.GetType().Name}: {exception.Message}).");
        }
    }

    private static bool IsStructuralLayer(string? layer)
        => !string.IsNullOrWhiteSpace(layer) &&
           LayerTokenSeparator.Split(layer).Any(StructuralLayerTokens.Contains);

    private static string? AddEntitySegments(
        DxfEntity entity,
        ICollection<StructuralSegment> segments)
        => entity switch
        {
            DxfLine line => AddLine(line, segments),
            DxfLwPolyline polyline => AddLwPolyline(polyline, segments),
            DxfSolid solid => AddSolid(solid, segments),
            Dxf3DFace face => Add3dFace(face, segments),
            // ponytail: curves are not axis-aligned wall-run evidence; missing straight evidence still fails below.
            _ when string.Equals(entity.EntityTypeString, "ARC", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entity.EntityTypeString, "CIRCLE", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entity.EntityTypeString, "ELLIPSE", StringComparison.OrdinalIgnoreCase) => null,
            _ => $"Unsupported structural DXF entity type {entity.EntityTypeString}."
        };

    private static string? AddLine(
        DxfLine line,
        ICollection<StructuralSegment> segments)
    {
        if (!double.IsFinite(line.P1.X) ||
            !double.IsFinite(line.P1.Y) ||
            !double.IsFinite(line.P1.Z) ||
            !double.IsFinite(line.P2.X) ||
            !double.IsFinite(line.P2.Y) ||
            !double.IsFinite(line.P2.Z))
        {
            return "LINE contains nonfinite coordinates.";
        }

        if (!TryPoint(line.P1.X, line.P1.Y, out var start) ||
            !TryPoint(line.P2.X, line.P2.Y, out var end))
        {
            return "LINE coordinate is outside decimal range.";
        }

        AddSegment(start, end, segments);
        return null;
    }

    private static string? AddLwPolyline(
        DxfLwPolyline polyline,
        ICollection<StructuralSegment> segments)
    {
        if (!IsDefaultExtrusion(polyline.ExtrusionDirection))
        {
            return "LWPOLYLINE uses unsupported non-default extrusion/OCS; expected approximately (0,0,1).";
        }

        if (!double.IsFinite(polyline.Elevation))
        {
            return "LWPOLYLINE elevation is nonfinite.";
        }

        if (polyline.Vertices.Count < 2)
        {
            return "LWPOLYLINE requires at least two vertices.";
        }

        var vertices = new List<(Point Point, double Bulge)>(polyline.Vertices.Count);
        foreach (var vertex in polyline.Vertices)
        {
            if (!double.IsFinite(vertex.Bulge))
            {
                return "LWPOLYLINE contains a nonfinite bulge.";
            }

            if (!double.IsFinite(vertex.X) || !double.IsFinite(vertex.Y))
            {
                return "LWPOLYLINE contains a nonfinite vertex coordinate.";
            }

            if (!TryPoint(vertex.X, vertex.Y, out var point))
            {
                return "LWPOLYLINE vertex coordinate is outside decimal range.";
            }

            vertices.Add((point, vertex.Bulge));
        }

        var edgeCount = polyline.IsClosed ? vertices.Count : vertices.Count - 1;
        for (var index = 0; index < edgeCount; index++)
        {
            // ponytail: a finite bulge is a curve, not a straight wall run; never flatten it into a chord.
            if (vertices[index].Bulge != 0d)
            {
                continue;
            }

            AddSegment(vertices[index].Point, vertices[(index + 1) % vertices.Count].Point, segments);
        }

        return null;
    }

    private static string? AddSolid(
        DxfSolid solid,
        ICollection<StructuralSegment> segments)
    {
        if (!IsDefaultExtrusion(solid.ExtrusionDirection))
        {
            return "SOLID uses unsupported non-default extrusion/OCS; expected approximately (0,0,1).";
        }

        var failure = GetPlanarPoints(
            "SOLID",
            [solid.FirstCorner, solid.SecondCorner, solid.ThirdCorner, solid.FourthCorner],
            out var points,
            out var isNonPlanar);
        if (failure is not null)
        {
            return failure;
        }

        if (isNonPlanar)
        {
            return "SOLID is non-planar; all corner/endpoint Z values must agree.";
        }

        IReadOnlyList<Point> perimeter = AreCoincident(points[2], points[3])
            ? [points[0], points[1], points[2]]
            : [points[0], points[1], points[3], points[2]];
        AddConnectedSegments(perimeter, close: true, segments: segments);
        return null;
    }

    private static string? Add3dFace(
        Dxf3DFace face,
        ICollection<StructuralSegment> segments)
    {
        var failure = GetPlanarPoints(
            "3DFACE",
            [face.FirstCorner, face.SecondCorner, face.ThirdCorner, face.FourthCorner],
            out var points,
            out var isNonPlanar);
        if (failure is not null)
        {
            return failure;
        }

        // ponytail: nonplanar faces cannot be trusted as 2D wall runs; retain no projected edges.
        if (isNonPlanar)
        {
            return null;
        }

        if (!face.IsFirstEdgeInvisible)
        {
            AddSegment(points[0], points[1], segments);
        }

        if (!face.IsSecondEdgeInvisible)
        {
            AddSegment(points[1], points[2], segments);
        }

        if (!face.IsThirdEdgeInvisible)
        {
            AddSegment(points[2], points[3], segments);
        }

        if (!face.IsFourthEdgeInvisible)
        {
            AddSegment(points[3], points[0], segments);
        }

        return null;
    }

    private static string? GetPlanarPoints(
        string entityType,
        IReadOnlyList<DxfPoint> source,
        out Point[] points,
        out bool isNonPlanar)
    {
        points = new Point[source.Count];
        isNonPlanar = false;
        for (var index = 0; index < source.Count; index++)
        {
            var current = source[index];
            if (!double.IsFinite(current.X) ||
                !double.IsFinite(current.Y) ||
                !double.IsFinite(current.Z))
            {
                points = [];
                return $"{entityType} contains nonfinite coordinates.";
            }

            if (!TryPoint(current.X, current.Y, out points[index]))
            {
                points = [];
                return $"{entityType} coordinate is outside decimal range.";
            }
        }

        var elevation = source[0].Z;
        for (var index = 1; index < source.Count; index++)
        {
            var current = source[index];
            if (Math.Abs(current.Z - elevation) > DxfNumericTolerance)
            {
                points = [];
                isNonPlanar = true;
                return null;
            }
        }

        return null;
    }

    private static bool IsDefaultExtrusion(DxfVector extrusion)
        => double.IsFinite(extrusion.X) &&
           double.IsFinite(extrusion.Y) &&
           double.IsFinite(extrusion.Z) &&
           Math.Abs(extrusion.X) <= DxfNumericTolerance &&
           Math.Abs(extrusion.Y) <= DxfNumericTolerance &&
           Math.Abs(extrusion.Z - 1d) <= DxfNumericTolerance;

    private static void AddConnectedSegments(
        IReadOnlyList<Point> points,
        bool close,
        ICollection<StructuralSegment> segments)
    {
        for (var index = 0; index + 1 < points.Count; index++)
        {
            AddSegment(points[index], points[index + 1], segments);
        }

        if (close && points.Count > 2 && !AreCoincident(points[^1], points[0]))
        {
            AddSegment(points[^1], points[0], segments);
        }
    }

    private static void AddSegment(
        Point start,
        Point end,
        ICollection<StructuralSegment> segments)
    {
        var deltaX = Math.Abs(end.X - start.X);
        var deltaY = Math.Abs(end.Y - start.Y);
        if (deltaY <= Tolerance && deltaX > Tolerance)
        {
            var y = (start.Y + end.Y) / 2m;
            segments.Add(new StructuralSegment(
                new Point(Math.Min(start.X, end.X), y),
                new Point(Math.Max(start.X, end.X), y),
                SegmentOrientation.Horizontal));
        }
        else if (deltaX <= Tolerance && deltaY > Tolerance)
        {
            var x = (start.X + end.X) / 2m;
            segments.Add(new StructuralSegment(
                new Point(x, Math.Min(start.Y, end.Y)),
                new Point(x, Math.Max(start.Y, end.Y)),
                SegmentOrientation.Vertical));
        }
    }

    private static bool TryPoint(double x, double y, out Point point)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            point = default;
            return false;
        }

        try
        {
            point = new Point((decimal)x, (decimal)y);
            return true;
        }
        catch (OverflowException)
        {
            point = default;
            return false;
        }
    }

    private static bool AreCoincident(Point left, Point right)
        => Math.Abs(left.X - right.X) <= Tolerance &&
           Math.Abs(left.Y - right.Y) <= Tolerance;

    private static DominantAxisAlignedOutlineCandidateSet SelectConnectedFrames(
        IReadOnlyList<StructuralSegment> segments)
        => DominantAxisAlignedOutlineSelector.SelectConnectedFrames(
            segments.Select(segment => new AxisAlignedStructuralSegment(
                segment.Start.X,
                segment.Start.Y,
                segment.End.X,
                segment.End.Y)));

    private static IReadOnlyList<StructuralSegment> ClipInteriorSegments(
        IEnumerable<StructuralSegment> segments,
        DominantAxisAlignedOutline outline)
    {
        var interior = new List<StructuralSegment>();
        foreach (var segment in segments)
        {
            if (segment.Orientation == SegmentOrientation.Horizontal)
            {
                var y = segment.AxisCoordinate;
                if (y < outline.MinY - Tolerance ||
                    y > outline.MaxY + Tolerance ||
                    Math.Abs(y - outline.MinY) <= Tolerance ||
                    Math.Abs(y - outline.MaxY) <= Tolerance)
                {
                    continue;
                }

                var start = Math.Max(segment.IntervalStart, outline.MinX);
                var end = Math.Min(segment.IntervalEnd, outline.MaxX);
                if (end - start > Tolerance)
                {
                    interior.Add(new StructuralSegment(
                        new Point(start, y),
                        new Point(end, y),
                        SegmentOrientation.Horizontal));
                }
            }
            else
            {
                var x = segment.AxisCoordinate;
                if (x < outline.MinX - Tolerance ||
                    x > outline.MaxX + Tolerance ||
                    Math.Abs(x - outline.MinX) <= Tolerance ||
                    Math.Abs(x - outline.MaxX) <= Tolerance)
                {
                    continue;
                }

                var start = Math.Max(segment.IntervalStart, outline.MinY);
                var end = Math.Min(segment.IntervalEnd, outline.MaxY);
                if (end - start > Tolerance)
                {
                    interior.Add(new StructuralSegment(
                        new Point(x, start),
                        new Point(x, end),
                        SegmentOrientation.Vertical));
                }
            }
        }

        return interior;
    }

    private static CandidateEvaluation EvaluateCandidate(
        int rotation,
        IReadOnlyList<StructuralSegment> canonicalInterior,
        IReadOnlyList<StructuralSegment> electricalInterior,
        bool outlineOnly,
        string selectorEvidence,
        ScaleFit scaleFit,
        Placement placement)
    {
        if (outlineOnly)
        {
            var outlineOnlyRationale =
                $"Rejected {rotation} degree outline candidate with uniform scale {Format(scaleFit.TrialScale)} and four-edge residuals within tolerance, because no interior structural anchors remain after clipping and excluding the four outline edges; outline-only evidence cannot prove orientation.";
            return CreateEvaluation(
                rotation,
                accepted: false,
                evidenceComplete: false,
                scaleFit.TrialScale,
                placement.TranslateX,
                placement.TranslateY,
                horizontal: null,
                vertical: null,
                residuals: ResidualEvidence.Empty,
                placement,
                outlineOnlyRationale,
                selectorEvidence,
                scaleFit,
                confidence: 0m);
        }

        var transformedElectrical = electricalInterior
            .Select(segment => TransformSegment(
                segment,
                rotation,
                scaleFit.TrialScale,
                placement.TranslateX,
                placement.TranslateY))
            .ToArray();
        var horizontal = EvaluateOrientation(
            canonicalInterior,
            transformedElectrical,
            SegmentOrientation.Horizontal);
        var vertical = EvaluateOrientation(
            canonicalInterior,
            transformedElectrical,
            SegmentOrientation.Vertical);
        var residuals = CombineResiduals(horizontal, vertical);
        var confidence = horizontal is null || vertical is null
            ? 0m
            : Math.Clamp(
                Math.Min(horizontal.Coverage, vertical.Coverage),
                0m,
                1m);

        if (horizontal is null || vertical is null)
        {
            var missing = new List<string>();
            if (horizontal is null)
            {
                missing.Add("horizontal");
            }

            if (vertical is null)
            {
                missing.Add("vertical");
            }

            var missingEvidenceRationale =
                $"Rejected {rotation} degree candidate: interior structural evidence is missing for {string.Join(" and ", missing)} orientation after clipping and excluding outline edges; both orientations are required to prove rotation.";
            return CreateEvaluation(
                rotation,
                accepted: false,
                evidenceComplete: false,
                scaleFit.TrialScale,
                placement.TranslateX,
                placement.TranslateY,
                horizontal,
                vertical,
                residuals,
                placement,
                missingEvidenceRationale,
                selectorEvidence,
                scaleFit,
                confidence);
        }

        var accepted = WholePlanRegistrationAcceptancePolicy.IsAccepted(
            WholePlanRegistrationAcceptancePolicy.CurrentVersion,
            horizontal.Coverage,
            vertical.Coverage,
            residuals.RootMeanSquare,
            residuals.Maximum);
        var rationale = accepted
            ? $"Accepted {rotation} degree candidate: uniform scale {Format(scaleFit.TrialScale)}, centered four-edge fit, horizontal bidirectional coverage {Format(horizontal.Coverage)}, vertical bidirectional coverage {Format(vertical.Coverage)}, RMS perpendicular residual {Format(residuals.RootMeanSquare)}, and maximum residual {Format(residuals.Maximum)} satisfy the thresholds."
            : $"Rejected {rotation} degree candidate: horizontal bidirectional coverage {Format(horizontal.Coverage)} and vertical bidirectional coverage {Format(vertical.Coverage)} must each be at least {Format(WholePlanRegistrationAcceptancePolicy.MinimumCoverage)}, with RMS/maximum perpendicular residuals within {Format(WholePlanRegistrationAcceptancePolicy.MaximumResidualInches)} in; observed RMS={Format(residuals.RootMeanSquare)} and maximum={Format(residuals.Maximum)}.";

        return CreateEvaluation(
            rotation,
            accepted,
            evidenceComplete: accepted,
            scaleFit.TrialScale,
            placement.TranslateX,
            placement.TranslateY,
            horizontal,
            vertical,
            residuals,
            placement,
            rationale,
            selectorEvidence,
            scaleFit,
            confidence);
    }

    private static GlobalTransformEvaluation EvaluateGlobalTransform(
        EquivalentTransformGroup group,
        IReadOnlyList<StructuralSegment> canonicalSegments,
        IReadOnlyList<StructuralSegment> electricalSegments)
    {
        var transform = group.Representative.Evidence;
        var transformedElectrical = electricalSegments
            .Select(segment => TransformSegment(
                segment,
                (int)transform.RotationDegrees,
                transform.Scale.GetValueOrDefault(),
                transform.TranslateX.GetValueOrDefault(),
                transform.TranslateY.GetValueOrDefault()))
            .ToArray();
        var horizontal = EvaluateDirectionalOrientation(
            canonicalSegments,
            transformedElectrical,
            SegmentOrientation.Horizontal);
        var vertical = EvaluateDirectionalOrientation(
            canonicalSegments,
            transformedElectrical,
            SegmentOrientation.Vertical);
        var residuals = CombineResiduals(horizontal, vertical);
        var confidence = horizontal is null || vertical is null
            ? 0m
            : Math.Clamp(Math.Min(horizontal.Coverage, vertical.Coverage), 0m, 1m);
        var evidenceComplete = MeetsWholePlanEvidenceThresholds(
            horizontal?.Coverage,
            vertical?.Coverage,
            residuals.RootMeanSquare,
            residuals.Maximum);

        return new GlobalTransformEvaluation(
            group,
            horizontal,
            vertical,
            residuals,
            evidenceComplete,
            confidence);
    }

    internal static bool MeetsWholePlanEvidenceThresholds(
        decimal? horizontalCoverage,
        decimal? verticalCoverage,
        decimal? rootMeanSquareResidual,
        decimal? maximumResidual)
        => WholePlanRegistrationAcceptancePolicy.IsAccepted(
            WholePlanRegistrationAcceptancePolicy.CurrentVersion,
            horizontalCoverage,
            verticalCoverage,
            rootMeanSquareResidual,
            maximumResidual);

    private static CandidateEvaluation CreateScaleRejectedEvaluation(
        int rotation,
        ScaleFit scaleFit,
        Placement placement,
        string selectorEvidence)
    {
        var scaleRejectionRationale =
            $"Rejected {rotation} degree candidate: observed scale X={Format(scaleFit.ObservedScaleX)} and Y={Format(scaleFit.ObservedScaleY)} cannot be represented by one uniform scale within {Format(Tolerance)} in; least-squares scale {Format(scaleFit.TrialScale)} leaves width residual {Format(scaleFit.WidthResidual)} and height residual {Format(scaleFit.HeightResidual)}.";
        return CreateEvaluation(
            rotation,
            accepted: false,
            evidenceComplete: false,
            scale: null,
            translateX: null,
            translateY: null,
            horizontal: null,
            vertical: null,
            residuals: ResidualEvidence.Empty,
            placement,
            scaleRejectionRationale,
            selectorEvidence,
            scaleFit,
            confidence: 0m);
    }

    private static CandidateEvaluation CreateEvaluation(
        int rotation,
        bool accepted,
        bool evidenceComplete,
        decimal? scale,
        decimal? translateX,
        decimal? translateY,
        OrientationEvidence? horizontal,
        OrientationEvidence? vertical,
        ResidualEvidence residuals,
        Placement placement,
        string rationale,
        string selectorEvidence,
        ScaleFit scaleFit,
        decimal confidence)
        => new(
            new ElectricalFloorRegistrationCandidateEvidence(
                rotation,
                accepted,
                scale,
                translateX,
                translateY,
                horizontal?.Coverage,
                vertical?.Coverage,
                residuals.RootMeanSquare,
                residuals.Maximum,
                placement.LeftResidual,
                placement.RightResidual,
                placement.BottomResidual,
                placement.TopResidual,
                $"{rationale} {selectorEvidence}"),
            scaleFit.ObservedScaleX,
            scaleFit.ObservedScaleY,
            scaleFit.Score,
            evidenceComplete,
            Math.Clamp(confidence, 0m, 1m),
            rationale);

    private static Bounds RotateBounds(
        DominantAxisAlignedOutline outline,
        int rotation)
    {
        var corners = new[]
        {
            new Point(outline.MinX, outline.MinY),
            new Point(outline.MaxX, outline.MinY),
            new Point(outline.MaxX, outline.MaxY),
            new Point(outline.MinX, outline.MaxY)
        };
        var rotated = corners.Select(point => Rotate(point, rotation)).ToArray();
        return new Bounds(
            rotated.Min(point => point.X),
            rotated.Min(point => point.Y),
            rotated.Max(point => point.X),
            rotated.Max(point => point.Y));
    }

    private static ScaleFit FitUniformScale(Bounds canonical, Bounds electrical)
    {
        var observedScaleX = canonical.Width / electrical.Width;
        var observedScaleY = canonical.Height / electrical.Height;
        var dimensionsAlreadyAgree =
            Math.Abs(canonical.Width - electrical.Width) <= Tolerance &&
            Math.Abs(canonical.Height - electrical.Height) <= Tolerance;
        var trialScale = dimensionsAlreadyAgree
            ? 1m
            : ((canonical.Width * electrical.Width) +
               (canonical.Height * electrical.Height)) /
              ((electrical.Width * electrical.Width) +
               (electrical.Height * electrical.Height));
        var widthResidual = Math.Abs((electrical.Width * trialScale) - canonical.Width);
        var heightResidual = Math.Abs((electrical.Height * trialScale) - canonical.Height);

        return new ScaleFit(
            observedScaleX,
            observedScaleY,
            trialScale,
            widthResidual,
            heightResidual,
            trialScale > 0m &&
            widthResidual <= Tolerance &&
            heightResidual <= Tolerance,
            (widthResidual * widthResidual) + (heightResidual * heightResidual));
    }

    private static Placement AlignCenters(
        Bounds canonical,
        Bounds rotatedElectrical,
        decimal scale)
    {
        var translateX = canonical.CenterX - (rotatedElectrical.CenterX * scale);
        var translateY = canonical.CenterY - (rotatedElectrical.CenterY * scale);
        var transformedMinX = (rotatedElectrical.MinX * scale) + translateX;
        var transformedMaxX = (rotatedElectrical.MaxX * scale) + translateX;
        var transformedMinY = (rotatedElectrical.MinY * scale) + translateY;
        var transformedMaxY = (rotatedElectrical.MaxY * scale) + translateY;

        return new Placement(
            translateX,
            translateY,
            Math.Abs(canonical.MinX - transformedMinX),
            Math.Abs(canonical.MaxX - transformedMaxX),
            Math.Abs(canonical.MinY - transformedMinY),
            Math.Abs(canonical.MaxY - transformedMaxY));
    }

    private static StructuralSegment TransformSegment(
        StructuralSegment segment,
        int rotation,
        decimal scale,
        decimal translateX,
        decimal translateY)
    {
        var start = TransformPoint(segment.Start, rotation, scale, translateX, translateY);
        var end = TransformPoint(segment.End, rotation, scale, translateX, translateY);
        if (start.Y == end.Y)
        {
            return new StructuralSegment(
                new Point(Math.Min(start.X, end.X), start.Y),
                new Point(Math.Max(start.X, end.X), start.Y),
                SegmentOrientation.Horizontal);
        }

        return new StructuralSegment(
            new Point(start.X, Math.Min(start.Y, end.Y)),
            new Point(start.X, Math.Max(start.Y, end.Y)),
            SegmentOrientation.Vertical);
    }

    private static Point TransformPoint(
        Point point,
        int rotation,
        decimal scale,
        decimal translateX,
        decimal translateY)
    {
        var rotated = Rotate(point, rotation);
        return new Point(
            (rotated.X * scale) + translateX,
            (rotated.Y * scale) + translateY);
    }

    private static Point Rotate(Point point, int rotation)
        => rotation switch
        {
            0 => point,
            90 => new Point(-point.Y, point.X),
            180 => new Point(-point.X, -point.Y),
            270 => new Point(point.Y, -point.X),
            _ => throw new ArgumentOutOfRangeException(nameof(rotation))
        };

    private static OrientationEvidence? EvaluateOrientation(
        IReadOnlyList<StructuralSegment> canonical,
        IReadOnlyList<StructuralSegment> electrical,
        SegmentOrientation orientation)
    {
        var canonicalRuns = MergeCollinear(canonical, orientation);
        var electricalRuns = MergeCollinear(electrical, orientation);
        if (canonicalRuns.Count == 0 || electricalRuns.Count == 0)
        {
            return null;
        }

        var forward = MeasureCoverage(canonicalRuns, electricalRuns);
        var reverse = MeasureCoverage(electricalRuns, canonicalRuns);
        var residualWeight = forward.MatchedLength + reverse.MatchedLength;
        var weightedSquaredResidual =
            forward.WeightedSquaredResidual + reverse.WeightedSquaredResidual;
        var maximum = new[] { forward.MaximumResidual, reverse.MaximumResidual }
            .Where(value => value.HasValue)
            .Select(value => value.GetValueOrDefault())
            .DefaultIfEmpty()
            .Max();

        return new OrientationEvidence(
            Math.Clamp(Math.Min(forward.Coverage, reverse.Coverage), 0m, 1m),
            residualWeight,
            weightedSquaredResidual,
            residualWeight > 0m ? maximum : null);
    }

    private static OrientationEvidence? EvaluateDirectionalOrientation(
        IReadOnlyList<StructuralSegment> canonical,
        IReadOnlyList<StructuralSegment> electrical,
        SegmentOrientation orientation)
    {
        var canonicalRuns = MergeCollinear(canonical, orientation);
        var electricalRuns = MergeCollinear(electrical, orientation);
        if (canonicalRuns.Count == 0 || electricalRuns.Count == 0)
        {
            return null;
        }

        var forward = MeasureCoverage(canonicalRuns, electricalRuns);
        return new OrientationEvidence(
            forward.Coverage,
            forward.MatchedLength,
            forward.WeightedSquaredResidual,
            forward.MaximumResidual);
    }

    private static IReadOnlyList<LineInterval> MergeCollinear(
        IEnumerable<StructuralSegment> segments,
        SegmentOrientation orientation)
    {
        var intervals = segments
            .Where(segment => segment.Orientation == orientation)
            .Select(segment => new LineInterval(
                segment.AxisCoordinate,
                segment.IntervalStart,
                segment.IntervalEnd))
            .OrderBy(interval => interval.Axis)
            .ThenBy(interval => interval.Start)
            .ToArray();
        var clusters = new List<List<LineInterval>>();
        foreach (var interval in intervals)
        {
            if (clusters.Count == 0 ||
                interval.Axis - clusters[^1][0].Axis > Tolerance)
            {
                clusters.Add([interval]);
            }
            else
            {
                clusters[^1].Add(interval);
            }
        }

        var merged = new List<LineInterval>();
        foreach (var cluster in clusters)
        {
            var totalLength = cluster.Sum(interval => interval.Length);
            var axis = cluster.Sum(interval => interval.Axis * interval.Length) / totalLength;
            decimal? start = null;
            var end = 0m;
            foreach (var interval in cluster.OrderBy(interval => interval.Start))
            {
                if (!start.HasValue)
                {
                    start = interval.Start;
                    end = interval.End;
                }
                else if (interval.Start <= end + Tolerance)
                {
                    end = Math.Max(end, interval.End);
                }
                else
                {
                    merged.Add(new LineInterval(axis, start.Value, end));
                    start = interval.Start;
                    end = interval.End;
                }
            }

            if (start.HasValue)
            {
                merged.Add(new LineInterval(axis, start.Value, end));
            }
        }

        return merged;
    }

    private static DirectionalEvidence MeasureCoverage(
        IReadOnlyList<LineInterval> source,
        IReadOnlyList<LineInterval> target)
    {
        var totalLength = source.Sum(interval => interval.Length);
        var matchedLength = 0m;
        var weightedSquaredResidual = 0m;
        decimal? maximumResidual = null;

        foreach (var expected in source)
        {
            var candidates = target
                .Where(candidate =>
                    Math.Abs(candidate.Axis - expected.Axis) <= Tolerance &&
                    candidate.End > expected.Start &&
                    candidate.Start < expected.End)
                .ToArray();
            if (candidates.Length == 0)
            {
                continue;
            }

            var breakpoints = new List<decimal> { expected.Start, expected.End };
            foreach (var candidate in candidates)
            {
                breakpoints.Add(Math.Max(expected.Start, candidate.Start));
                breakpoints.Add(Math.Min(expected.End, candidate.End));
            }

            var ordered = breakpoints.Distinct().Order().ToArray();
            for (var index = 0; index + 1 < ordered.Length; index++)
            {
                var start = ordered[index];
                var end = ordered[index + 1];
                if (end <= start)
                {
                    continue;
                }

                var midpoint = (start + end) / 2m;
                var residuals = candidates
                    .Where(candidate =>
                        candidate.Start <= midpoint && candidate.End >= midpoint)
                    .Select(candidate => Math.Abs(candidate.Axis - expected.Axis))
                    .ToArray();
                if (residuals.Length == 0)
                {
                    continue;
                }

                var length = end - start;
                var residual = residuals.Min();
                matchedLength += length;
                weightedSquaredResidual += length * residual * residual;
                maximumResidual = maximumResidual.HasValue
                    ? Math.Max(maximumResidual.Value, residual)
                    : residual;
            }
        }

        return new DirectionalEvidence(
            totalLength <= 0m
                ? 0m
                : Math.Clamp(matchedLength / totalLength, 0m, 1m),
            matchedLength,
            weightedSquaredResidual,
            maximumResidual);
    }

    private static ResidualEvidence CombineResiduals(
        OrientationEvidence? horizontal,
        OrientationEvidence? vertical)
    {
        var evidence = new[] { horizontal, vertical }
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();
        var totalWeight = evidence.Sum(item => item.ResidualWeight);
        if (totalWeight <= 0m)
        {
            return ResidualEvidence.Empty;
        }

        var weightedSquaredResidual = evidence.Sum(item => item.WeightedSquaredResidual);
        var maximum = evidence
            .Where(item => item.MaximumResidual.HasValue)
            .Max(item => item.MaximumResidual.GetValueOrDefault());
        return new ResidualEvidence(
            SquareRoot(weightedSquaredResidual / totalWeight),
            maximum);
    }

    private static CandidateEvaluation SelectMetricDiagnostic(
        IReadOnlyList<CandidateEvaluation> evaluations,
        IReadOnlyList<CandidateEvaluation> accepted)
    {
        var pool = accepted.Count > 0 ? accepted : evaluations;
        return pool
            .OrderByDescending(evaluation => evaluation.Confidence)
            .ThenByDescending(evaluation =>
                (evaluation.Evidence.HorizontalCoverage ?? 0m) +
                (evaluation.Evidence.VerticalCoverage ?? 0m))
            .ThenBy(evaluation => evaluation.ScaleFitScore)
            .ThenBy(evaluation => evaluation.Evidence.RotationDegrees)
            .First();
    }

    private static string BuildSummary(
        ElectricalFloorRegistrationEstimateStatus status,
        IReadOnlyList<GlobalTransformEvaluation> globalEvaluations,
        ScaleDiagnostic scaleDiagnostic,
        string metricDiagnostic,
        string selectorEvidence,
        long candidatePairCount,
        int scaleCompatibleCandidates)
    {
        var conclusion = status switch
        {
            ElectricalFloorRegistrationEstimateStatus.Estimated =>
                "Exactly one transform group passed directional whole-plan canonical-to-transformed-Electrical proof after local connected-frame proof.",
            ElectricalFloorRegistrationEstimateStatus.Ambiguous =>
                "Multiple non-equivalent transform groups passed directional whole-plan canonical-to-transformed-Electrical proof, so automatic registration remains ambiguous.",
            _ when globalEvaluations.Count > 0 =>
                "No locally evidence-complete transform group passed directional whole-plan canonical-to-transformed-Electrical proof; manual review is required.",
            _ =>
                $"No connected-frame pair supplied evidence-complete two-axis local proof. Lowest scale-fit diagnostic: {Format(scaleDiagnostic.RotationDegrees)} degrees, observed scale X={Format(scaleDiagnostic.ScaleFit.ObservedScaleX)}, Y={Format(scaleDiagnostic.ScaleFit.ObservedScaleY)}. Diagnostic: {metricDiagnostic} Manual review is required."
        };
        var groupedTransforms = globalEvaluations.Count == 0
            ? "No evidence-complete transform groups reached whole-plan evaluation."
            : $"Whole-plan transform groups: {string.Join("; ", globalEvaluations.Select(FormatGlobalTransform))}.";

        return $"{conclusion} {groupedTransforms} Evaluated {candidatePairCount} connected-frame pairs; {scaleCompatibleCandidates} quarter-turn candidates survived uniform-scale dimension pruning before interior/residual evaluation. {selectorEvidence}";
    }

    private static string FormatGlobalTransform(GlobalTransformEvaluation evaluation)
    {
        var transform = evaluation.Group.Representative.Evidence;
        var outcome = evaluation.EvidenceComplete ? "passed" : "rejected";
        return $"scale {Format(transform.Scale)}, {Format(transform.RotationDegrees)} degrees, translation ({Format(transform.TranslateX)}, {Format(transform.TranslateY)}), support {evaluation.Group.SupportCount} frame-pair evaluations, global canonical-to-transformed-Electrical H coverage {Format(evaluation.Horizontal?.Coverage)}, V coverage {Format(evaluation.Vertical?.Coverage)}, RMS residual {Format(evaluation.Residuals.RootMeanSquare)}, maximum residual {Format(evaluation.Residuals.Maximum)} ({outcome})";
    }

    private static string BuildSelectorEvidence(
        DominantAxisAlignedOutlineCandidateSet canonical,
        DominantAxisAlignedOutlineCandidateSet electrical)
        => $"Dominant connected-frame selector evidence: canonical [{canonical.Status}, {canonical.Candidates.Count} candidates: {canonical.Reason}] electrical [{electrical.Status}, {electrical.Candidates.Count} candidates: {electrical.Reason}].";

    private static string BuildFramePairEvidence(
        DominantAxisAlignedOutlineCandidate canonical,
        DominantAxisAlignedOutlineCandidate electrical,
        string selectorEvidence)
        => $"Connected-frame pair evidence: canonical {FormatOutline(canonical.Outline)} " +
           $"(balanced support {Format(canonical.BalancedSupport)}, support balance {Format(canonical.SupportBalance)}, shared coverage {Format(canonical.SharedCoverage)}, minimum continuity {Format(canonical.MinimumContinuity)}) and electrical {FormatOutline(electrical.Outline)} " +
           $"(balanced support {Format(electrical.BalancedSupport)}, support balance {Format(electrical.SupportBalance)}, shared coverage {Format(electrical.SharedCoverage)}, minimum continuity {Format(electrical.MinimumContinuity)}). {selectorEvidence}";

    private static IReadOnlyList<EquivalentTransformGroup> GroupCandidateEquivalentTransforms(
        IReadOnlyList<CandidateEvaluation> evidenceComplete)
    {
        var hypotheses = evidenceComplete
            .Select((candidate, index) => (Candidate: candidate, Index: index))
            .Where(item =>
                item.Candidate.Evidence.Scale.HasValue &&
                item.Candidate.Evidence.TranslateX.HasValue &&
                item.Candidate.Evidence.TranslateY.HasValue)
            .Select(item => new TransformHypothesis(
                item.Index,
                item.Candidate.Evidence.RotationDegrees,
                item.Candidate.Evidence.Scale.GetValueOrDefault(),
                item.Candidate.Evidence.TranslateX.GetValueOrDefault(),
                item.Candidate.Evidence.TranslateY.GetValueOrDefault()))
            .ToArray();

        return GroupEquivalentTransforms(hypotheses)
            .Select(group =>
            {
                var representative = evidenceComplete[group[0].CandidateIndex];
                foreach (var hypothesis in group.Skip(1))
                {
                    var candidate = evidenceComplete[hypothesis.CandidateIndex];
                    if (IsPreferredRepresentative(candidate, representative))
                    {
                        representative = candidate;
                    }
                }

                return new EquivalentTransformGroup(representative, group.Count);
            })
            .ToArray();
    }

    internal static IReadOnlyList<IReadOnlyList<TransformHypothesis>> GroupEquivalentTransforms(
        IReadOnlyList<TransformHypothesis> hypotheses)
    {
        var groups = new List<TransformHypothesisCluster>();
        foreach (var hypothesis in hypotheses
                     .OrderBy(item => item.RotationDegrees)
                     .ThenBy(item => item.Scale)
                     .ThenBy(item => item.TranslateX)
                     .ThenBy(item => item.TranslateY))
        {
            var matchingIndex = groups.FindIndex(group => group.CanInclude(hypothesis));
            if (matchingIndex < 0)
            {
                groups.Add(new TransformHypothesisCluster(hypothesis));
                continue;
            }

            groups[matchingIndex].Add(hypothesis);
        }

        return groups
            .Select(group => (IReadOnlyList<TransformHypothesis>)group.Hypotheses.ToArray())
            .ToArray();
    }

    private static bool IsPreferredRepresentative(
        CandidateEvaluation candidate,
        CandidateEvaluation current)
    {
        var candidateCoverage =
            (candidate.Evidence.HorizontalCoverage ?? 0m) +
            (candidate.Evidence.VerticalCoverage ?? 0m);
        var currentCoverage =
            (current.Evidence.HorizontalCoverage ?? 0m) +
            (current.Evidence.VerticalCoverage ?? 0m);
        return candidate.Confidence > current.Confidence ||
               (candidate.Confidence == current.Confidence &&
                (candidateCoverage > currentCoverage ||
                 (candidateCoverage == currentCoverage &&
                  candidate.ScaleFitScore < current.ScaleFitScore)));
    }

    private static string FormatOutline(DominantAxisAlignedOutline outline)
        => $"[{Format(outline.MinX)}, {Format(outline.MinY)}] - [{Format(outline.MaxX)}, {Format(outline.MaxY)}]";

    private static ElectricalFloorRegistrationEstimate Ambiguous(string summary)
        => Inconclusive(ElectricalFloorRegistrationEstimateStatus.Ambiguous, summary);

    private static ElectricalFloorRegistrationEstimate Insufficient(string summary)
        => Inconclusive(ElectricalFloorRegistrationEstimateStatus.InsufficientEvidence, summary);

    private static ElectricalFloorRegistrationEstimate Inconclusive(
        ElectricalFloorRegistrationEstimateStatus status,
        string summary)
        => new(
            status,
            Transform: null,
            Confidence: 0m,
            ObservedScaleX: null,
            ObservedScaleY: null,
            HorizontalCoverage: null,
            VerticalCoverage: null,
            RootMeanSquareResidual: null,
            MaximumResidual: null,
            EvidenceSummary: summary,
            Candidates: []);

    private static decimal SquareRoot(decimal value)
        => value <= 0m ? 0m : (decimal)Math.Sqrt((double)value);

    private static string Format(decimal value)
        => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string Format(decimal? value)
        => value.HasValue ? Format(value.Value) : "n/a";

    private enum SegmentOrientation
    {
        Horizontal,
        Vertical
    }

    private readonly record struct Point(decimal X, decimal Y);

    private readonly record struct StructuralSegment(
        Point Start,
        Point End,
        SegmentOrientation Orientation)
    {
        public decimal AxisCoordinate =>
            Orientation == SegmentOrientation.Horizontal ? Start.Y : Start.X;

        public decimal IntervalStart =>
            Orientation == SegmentOrientation.Horizontal ? Start.X : Start.Y;

        public decimal IntervalEnd =>
            Orientation == SegmentOrientation.Horizontal ? End.X : End.Y;
    }

    private sealed record SegmentExtraction(
        IReadOnlyList<StructuralSegment> Segments,
        string? Failure);

    private readonly record struct Bounds(
        decimal MinX,
        decimal MinY,
        decimal MaxX,
        decimal MaxY)
    {
        public decimal Width => MaxX - MinX;

        public decimal Height => MaxY - MinY;

        public decimal CenterX => (MinX + MaxX) / 2m;

        public decimal CenterY => (MinY + MaxY) / 2m;

        public static Bounds From(DominantAxisAlignedOutline outline)
            => new(outline.MinX, outline.MinY, outline.MaxX, outline.MaxY);
    }

    private sealed record ScaleFit(
        decimal ObservedScaleX,
        decimal ObservedScaleY,
        decimal TrialScale,
        decimal WidthResidual,
        decimal HeightResidual,
        bool Accepted,
        decimal Score);

    private sealed record ScaleDiagnostic(
        int RotationDegrees,
        ScaleFit ScaleFit);

    internal readonly record struct TransformHypothesis(
        int CandidateIndex,
        decimal RotationDegrees,
        decimal Scale,
        decimal TranslateX,
        decimal TranslateY);

    private sealed class TransformHypothesisCluster
    {
        private readonly decimal _rotationDegrees;
        private decimal _minimumScale;
        private decimal _maximumScale;
        private decimal _minimumTranslateX;
        private decimal _maximumTranslateX;
        private decimal _minimumTranslateY;
        private decimal _maximumTranslateY;

        public TransformHypothesisCluster(TransformHypothesis first)
        {
            _rotationDegrees = first.RotationDegrees;
            _minimumScale = first.Scale;
            _maximumScale = first.Scale;
            _minimumTranslateX = first.TranslateX;
            _maximumTranslateX = first.TranslateX;
            _minimumTranslateY = first.TranslateY;
            _maximumTranslateY = first.TranslateY;
            Hypotheses = [first];
        }

        public List<TransformHypothesis> Hypotheses { get; }

        public bool CanInclude(TransformHypothesis hypothesis)
            => hypothesis.RotationDegrees == _rotationDegrees &&
               Math.Max(_maximumScale, hypothesis.Scale) -
               Math.Min(_minimumScale, hypothesis.Scale) <= Tolerance &&
               Math.Max(_maximumTranslateX, hypothesis.TranslateX) -
               Math.Min(_minimumTranslateX, hypothesis.TranslateX) <= Tolerance &&
               Math.Max(_maximumTranslateY, hypothesis.TranslateY) -
               Math.Min(_minimumTranslateY, hypothesis.TranslateY) <= Tolerance;

        public void Add(TransformHypothesis hypothesis)
        {
            _minimumScale = Math.Min(_minimumScale, hypothesis.Scale);
            _maximumScale = Math.Max(_maximumScale, hypothesis.Scale);
            _minimumTranslateX = Math.Min(_minimumTranslateX, hypothesis.TranslateX);
            _maximumTranslateX = Math.Max(_maximumTranslateX, hypothesis.TranslateX);
            _minimumTranslateY = Math.Min(_minimumTranslateY, hypothesis.TranslateY);
            _maximumTranslateY = Math.Max(_maximumTranslateY, hypothesis.TranslateY);
            Hypotheses.Add(hypothesis);
        }
    }

    private sealed record EquivalentTransformGroup(
        CandidateEvaluation Representative,
        int SupportCount);

    private sealed record GlobalTransformEvaluation(
        EquivalentTransformGroup Group,
        OrientationEvidence? Horizontal,
        OrientationEvidence? Vertical,
        ResidualEvidence Residuals,
        bool EvidenceComplete,
        decimal Confidence);

    private sealed record Placement(
        decimal TranslateX,
        decimal TranslateY,
        decimal LeftResidual,
        decimal RightResidual,
        decimal BottomResidual,
        decimal TopResidual);

    private readonly record struct LineInterval(
        decimal Axis,
        decimal Start,
        decimal End)
    {
        public decimal Length => End - Start;
    }

    private sealed record DirectionalEvidence(
        decimal Coverage,
        decimal MatchedLength,
        decimal WeightedSquaredResidual,
        decimal? MaximumResidual);

    private sealed record OrientationEvidence(
        decimal Coverage,
        decimal ResidualWeight,
        decimal WeightedSquaredResidual,
        decimal? MaximumResidual);

    private sealed record ResidualEvidence(
        decimal? RootMeanSquare,
        decimal? Maximum)
    {
        public static ResidualEvidence Empty { get; } = new(null, null);
    }

    private sealed record CandidateEvaluation(
        ElectricalFloorRegistrationCandidateEvidence Evidence,
        decimal ObservedScaleX,
        decimal ObservedScaleY,
        decimal ScaleFitScore,
        bool EvidenceComplete,
        decimal Confidence,
        string Rationale);
}
