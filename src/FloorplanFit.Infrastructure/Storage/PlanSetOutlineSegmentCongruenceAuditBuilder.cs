using System.Globalization;
using System.Text;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Infrastructure.Geometry;

namespace FloorplanFit.Infrastructure.Storage;

internal sealed record PlanSetCongruenceVerificationEvidence(
    Guid SheetId,
    PlanSetVerificationCheckStatus Status,
    string Reason);

internal static class PlanSetOutlineSegmentCongruenceAuditBuilder
{
    private const decimal Tolerance = 0.05m;
    private const decimal CenterlineTolerance = 0.5m;
    private const decimal OutlineBandTolerance = 4m;
    private const decimal OutlineMatchTolerance = 2m;
    private const decimal FinalOutputTolerance = 0.5m;
    private const decimal MinimumSegmentLength = 24m;
    private const decimal WallPairMaxWidth = 8m;
    private const decimal WallPairOverlapRatio = 0.8m;
    private const int MaxMismatchSamples = 20;
    private const string BinaryDxfSentinel = "AutoCAD Binary DXF\r\n\u001A\0";

    public static object Build(MultiSheetExportAuditDto audit)
    {
        var floor = audit.Sheets.FirstOrDefault(sheet => sheet.SheetKind == "CanonicalFloorPlan");
        return new
        {
            stage = "outline-segment-congruence",
            audit.ExportId,
            audit.CanonicalAdjustmentId,
            toleranceInches = Tolerance,
            sheets = audit.Sheets
                .Where(IsElectricalSheet)
                .Select(sheet => BuildSheet(floor, sheet).Artifact)
                .ToArray()
        };
    }

    public static IReadOnlyList<PlanSetCongruenceVerificationEvidence> BuildVerification(
        MultiSheetExportAuditDto audit)
    {
        var floor = audit.Sheets.FirstOrDefault(sheet => sheet.SheetKind == "CanonicalFloorPlan");
        return audit.Sheets
            .Where(IsElectricalSheet)
            .Select(sheet => BuildSheet(floor, sheet).Verification)
            .ToArray();
    }

    public static object BuildFinalOutput(
        MultiSheetExportAuditDto audit,
        bool reportStoragePaths = false)
    {
        var floor = audit.Sheets.FirstOrDefault(sheet => sheet.SheetKind == "CanonicalFloorPlan");
        return new
        {
            stage = "final-output-congruence",
            audit.ExportId,
            audit.CanonicalAdjustmentId,
            toleranceInches = FinalOutputTolerance,
            sheets = audit.Sheets
                .Where(IsElectricalSheet)
                .Select(sheet => BuildFinalOutputSheet(floor, sheet, reportStoragePaths).Artifact)
                .ToArray()
        };
    }

    public static IReadOnlyList<PlanSetCongruenceVerificationEvidence> BuildFinalOutputVerification(
        MultiSheetExportAuditDto audit)
    {
        var floor = audit.Sheets.FirstOrDefault(sheet => sheet.SheetKind == "CanonicalFloorPlan");
        return audit.Sheets
            .Where(IsElectricalSheet)
            .Select(sheet => BuildFinalOutputSheet(floor, sheet).Verification)
            .ToArray();
    }

    private static CongruenceAuditBuildResult BuildSheet(
        ExportedPlanSheetDto? floor,
        ExportedPlanSheetDto electrical)
    {
        try
        {
            var floorVerificationPath = floor?.VerificationPath ?? floor?.StoragePath;
            if (string.IsNullOrWhiteSpace(floorVerificationPath) || !File.Exists(floorVerificationPath))
            {
                return Insufficient(electrical, "Canonical FloorPlan DXF was not available for segment comparison.");
            }

            var electricalVerificationPath = electrical.VerificationPath ?? electrical.StoragePath;
            if (string.IsNullOrWhiteSpace(electricalVerificationPath) || !File.Exists(electricalVerificationPath))
            {
                return Insufficient(electrical, "ElectricalPlan DXF was not available for segment comparison.");
            }

            var floorRawSegments = ExtractSegments(floorVerificationPath, includeElectricalWalls: false);
            var electricalRawSegments = ExtractSegments(electricalVerificationPath, includeElectricalWalls: true);
            if (floorRawSegments.Count == 0 || electricalRawSegments.Count == 0)
            {
                return Insufficient(electrical, "Structural line/polyline segments were not detected on comparable layers.");
            }

            var floorOutline = SelectDominantOutline(floorRawSegments);
            if (!floorOutline.IsSelected)
            {
                return Insufficient(electrical, $"Canonical FloorPlan dominant structural outline selection failed: {floorOutline.Reason}");
            }

            var electricalOutline = SelectDominantOutline(electricalRawSegments);
            if (!electricalOutline.IsSelected)
            {
                return Insufficient(electrical, $"ElectricalPlan dominant structural outline selection failed: {electricalOutline.Reason}");
            }

            var floorSegments = NormalizeSegments(floorRawSegments, floorOutline.Outline!);
            var electricalSegments = NormalizeSegments(electricalRawSegments, electricalOutline.Outline!);
            if (floorSegments.Count == 0 || electricalSegments.Count == 0)
            {
                return Insufficient(electrical, "Dominant structural outlines did not contain comparable wall runs.");
            }

            var floorOutlineSegments = SelectOutlineSegments(floorSegments);
            if (floorOutlineSegments.Count == 0)
            {
                return Insufficient(electrical, "Structural outline wall runs were not detected on the canonical FloorPlan.");
            }

            var outlineEdges = BuildOutlineEdgeCoverage(floorSegments, electricalSegments);
            var cornerCoverage = BuildCornerCoverage(floorSegments, electricalSegments);
            var missing = outlineEdges
                .SelectMany(edge => edge.Mismatches)
                .Concat(cornerCoverage.Where(corner => !corner.IsCovered).Select(corner => new
                {
                    Kind = "MissingCornerInElectrical",
                    Axis = "Corner",
                    FloorSegment = (object?)null,
                    ElectricalCandidate = (object?)null,
                    DeltaX = corner.DeltaX,
                    DeltaY = corner.DeltaY,
                    Overlap = 0m,
                    Layer = "outline",
                    Reason = $"Corner {corner.Corner} is not covered within {OutlineMatchTolerance}\"."
                }))
                .ToArray();
            var advisoryMissing = FindMissing(floorSegments, electricalSegments, "AdvisoryMissingInternalWallRun", CenterlineTolerance).ToArray();
            var advisoryExtra = FindMissing(electricalSegments, floorSegments, "AdvisoryExtraElectricalWallRun", CenterlineTolerance).ToArray();
            var status = missing.Length == 0
                ? "SegmentCongruent"
                : "SegmentMismatchRequiresManualReview";
            var reason = status == "SegmentCongruent"
                ? "Required structural outline wall runs are covered after bbox-normalized comparison; internal wall-run differences are advisory because dependent sheets can draft walls differently."
                : "Bbox is not enough: required structural outline wall runs differ after bbox-normalized comparison; manual CAD review or recipe remap is required.";

            var artifact = new
            {
                electrical.SheetId,
                electrical.ProjectionId,
                electrical.Status,
                segmentCongruence = new
                {
                    Status = status,
                    Reason = reason,
                    ToleranceInches = Tolerance,
                    CenterlineToleranceInches = CenterlineTolerance,
                    OutlineMatchToleranceInches = OutlineMatchTolerance,
                    ComparisonMode = "StructuralOutlineCoverageWithWallRunAdvisory",
                    FloorSegmentCount = floorSegments.Count,
                    ElectricalSegmentCount = electricalSegments.Count,
                    RequiredOutlineSegmentCount = floorOutlineSegments.Count,
                    OutlineEdges = outlineEdges,
                    CornerCoverage = cornerCoverage,
                    MissingInElectricalCount = missing.Length,
                    ExtraInElectricalCount = 0,
                    AdvisoryMissingInternalWallRunCount = advisoryMissing.Length,
                    AdvisoryExtraElectricalWallRunCount = advisoryExtra.Length,
                    Mismatches = missing.Take(MaxMismatchSamples).ToArray(),
                    AdvisoryMismatches = advisoryMissing.Concat(advisoryExtra).Take(MaxMismatchSamples).ToArray()
                }
            };
            return new CongruenceAuditBuildResult(
                artifact,
                new PlanSetCongruenceVerificationEvidence(
                    electrical.SheetId,
                    status == "SegmentCongruent"
                        ? PlanSetVerificationCheckStatus.Passed
                        : PlanSetVerificationCheckStatus.Failed,
                    reason));
        }
        catch (Exception exception)
        {
            return Insufficient(electrical, $"Segment audit failed: {exception.Message}");
        }
    }

    private static CongruenceAuditBuildResult Insufficient(
        ExportedPlanSheetDto electrical,
        string reason)
    {
        var artifact = new
        {
            electrical.SheetId,
            electrical.ProjectionId,
            electrical.Status,
            segmentCongruence = new
            {
                Status = "InsufficientData",
                Reason = reason,
                ToleranceInches = Tolerance,
                CenterlineToleranceInches = CenterlineTolerance,
                OutlineMatchToleranceInches = OutlineMatchTolerance,
                ComparisonMode = "StructuralOutlineCoverageWithWallRunAdvisory",
                FloorSegmentCount = 0,
                ElectricalSegmentCount = 0,
                RequiredOutlineSegmentCount = 0,
                OutlineEdges = Array.Empty<object>(),
                CornerCoverage = Array.Empty<object>(),
                MissingInElectricalCount = 0,
                ExtraInElectricalCount = 0,
                AdvisoryMissingInternalWallRunCount = 0,
                AdvisoryExtraElectricalWallRunCount = 0,
                AdvisoryMismatches = Array.Empty<object>(),
                Mismatches = Array.Empty<object>()
            }
        };
        return new CongruenceAuditBuildResult(
            artifact,
            new PlanSetCongruenceVerificationEvidence(
                electrical.SheetId,
                PlanSetVerificationCheckStatus.InsufficientData,
                reason));
    }

    private static CongruenceAuditBuildResult BuildFinalOutputSheet(
        ExportedPlanSheetDto? floor,
        ExportedPlanSheetDto electrical,
        bool reportStoragePaths = false)
    {
        var floorVerificationPath = floor?.VerificationPath ?? floor?.StoragePath;
        var electricalVerificationPath = electrical.VerificationPath ?? electrical.StoragePath;
        var floorOutputPath = reportStoragePaths ? floor?.StoragePath : floorVerificationPath;
        var electricalOutputPath = reportStoragePaths ? electrical.StoragePath : electricalVerificationPath;

        try
        {
            if (string.IsNullOrWhiteSpace(floorVerificationPath) || !File.Exists(floorVerificationPath))
            {
                return InsufficientFinalOutput(
                    electrical,
                    "Canonical FloorPlan output DXF was not available for final output comparison.",
                    floorOutputPath,
                    electricalOutputPath);
            }

            if (string.IsNullOrWhiteSpace(electricalVerificationPath) || !File.Exists(electricalVerificationPath))
            {
                return InsufficientFinalOutput(
                    electrical,
                    "ElectricalPlan output DXF was not available for final output comparison.",
                    floorOutputPath,
                    electricalOutputPath);
            }

            var floorSegments = ExtractSegments(floorVerificationPath, includeElectricalWalls: false);
            var electricalSegments = ExtractSegments(electricalVerificationPath, includeElectricalWalls: true);
            if (floorSegments.Count == 0 || electricalSegments.Count == 0)
            {
                return InsufficientFinalOutput(
                    electrical,
                    "Comparable structural segments were not detected in both final output DXFs.",
                    floorOutputPath,
                    electricalOutputPath);
            }

            var floorRawBounds = RawSegmentBounds.From(floorSegments);
            var electricalRawBounds = RawSegmentBounds.From(electricalSegments);
            var floorSelection = SelectDominantOutline(floorSegments);
            if (!floorSelection.IsSelected)
            {
                return InsufficientFinalOutput(
                    electrical,
                    $"Canonical FloorPlan final native dominant structural outline selection failed: {floorSelection.Reason}",
                    floorOutputPath,
                    electricalOutputPath);
            }

            var electricalSelection = SelectDominantOutline(electricalSegments);
            if (!electricalSelection.IsSelected)
            {
                return InsufficientFinalOutput(
                    electrical,
                    $"ElectricalPlan final native dominant structural outline selection failed: {electricalSelection.Reason}",
                    floorOutputPath,
                    electricalOutputPath);
            }

            var floorBounds = ToBounds(floorSelection.Outline!);
            var electricalBounds = ToBounds(electricalSelection.Outline!);

            var widthMismatch = electricalBounds.Width - floorBounds.Width;
            var heightMismatch = electricalBounds.Height - floorBounds.Height;
            var edgeDeltas = new[]
            {
                BuildFinalOutputEdge("Left", floorBounds.MinX, electricalBounds.MinX),
                BuildFinalOutputEdge("Right", floorBounds.MaxX, electricalBounds.MaxX),
                BuildFinalOutputEdge("Bottom", floorBounds.MinY, electricalBounds.MinY),
                BuildFinalOutputEdge("Top", floorBounds.MaxY, electricalBounds.MaxY)
            };
            var sizeOk = Math.Abs(widthMismatch) <= FinalOutputTolerance &&
                         Math.Abs(heightMismatch) <= FinalOutputTolerance;
            var failingEdges = edgeDeltas.Where(edge => !edge.IsCovered).ToArray();
            var edgesOk = failingEdges.Length == 0;
            var isCongruent = sizeOk && edgesOk;
            var status = isCongruent
                ? "FinalOutputCongruent"
                : "FinalOutputMismatchRequiresReview";
            var reason = isCongruent
                ? "Final exported FloorPlan and ElectricalPlan dominant structural outlines match in final native coordinates: width, height, and all four native edges are within tolerance."
                : $"Final exported dominant structural outlines do not match in final native coordinates (width residual {widthMismatch}, height residual {heightMismatch}, failing native edges {failingEdges.Length}); do not trust automatic overlay.";

            var artifact = new
            {
                electrical.SheetId,
                electrical.ProjectionId,
                electrical.Status,
                finalOutputCongruence = new
                {
                    Status = status,
                    Reason = reason,
                    ToleranceInches = FinalOutputTolerance,
                    ComparisonMode = "FinalNativeDominantStructuralOutlineEdgesAndSize",
                    FloorOutputPath = floorOutputPath,
                    ElectricalOutputPath = electricalOutputPath,
                    FloorStructuralSegmentCount = floorSegments.Count,
                    ElectricalStructuralSegmentCount = electricalSegments.Count,
                    FloorStructuralBounds = floorBounds.ToAuditDto(),
                    ElectricalStructuralBounds = electricalBounds.ToAuditDto(),
                    FloorRawStructuralBounds = floorRawBounds.ToAuditDto(),
                    ElectricalRawStructuralBounds = electricalRawBounds.ToAuditDto(),
                    RawWidthMismatchInches = electricalRawBounds.Width - floorRawBounds.Width,
                    RawHeightMismatchInches = electricalRawBounds.Height - floorRawBounds.Height,
                    WidthMismatchInches = widthMismatch,
                    HeightMismatchInches = heightMismatch,
                    SizeResidualsWithinTolerance = sizeOk,
                    NativeEdgeResidualsWithinTolerance = edgesOk,
                    EdgeDeltas = edgeDeltas,
                    MissingInElectricalCount = failingEdges.Count(IsInwardElectricalEdge),
                    ExtraInElectricalCount = failingEdges.Count(edge => !IsInwardElectricalEdge(edge)),
                    Mismatches = isCongruent
                        ? Array.Empty<object>()
                        : failingEdges
                            .Select(edge => (object)new
                            {
                                Kind = IsInwardElectricalEdge(edge)
                                    ? "FinalOutputMissingNativeEdge"
                                    : "FinalOutputExtraNativeEdge",
                                edge.Edge,
                                edge.FloorCoordinate,
                                edge.ElectricalCoordinate,
                                edge.Delta,
                                Reason = $"Final native output {edge.Edge} edge differs by {edge.Delta}\"."
                            })
                            .ToArray()
                }
            };
            return new CongruenceAuditBuildResult(
                artifact,
                new PlanSetCongruenceVerificationEvidence(
                    electrical.SheetId,
                    status == "FinalOutputCongruent"
                        ? PlanSetVerificationCheckStatus.Passed
                        : PlanSetVerificationCheckStatus.Failed,
                    reason));
        }
        catch (Exception exception)
        {
            return InsufficientFinalOutput(
                electrical,
                $"Final output congruence audit failed: {exception.Message}",
                floorOutputPath,
                electricalOutputPath);
        }
    }

    private static CongruenceAuditBuildResult InsufficientFinalOutput(
        ExportedPlanSheetDto electrical,
        string reason,
        string? floorOutputPath,
        string? electricalOutputPath)
    {
        var artifact = new
        {
            electrical.SheetId,
            electrical.ProjectionId,
            electrical.Status,
            finalOutputCongruence = new
            {
                Status = "InsufficientFinalOutputCongruenceData",
                Reason = reason,
                ToleranceInches = FinalOutputTolerance,
                ComparisonMode = "FinalNativeDominantStructuralOutlineEdgesAndSize",
                FloorOutputPath = floorOutputPath,
                ElectricalOutputPath = electricalOutputPath,
                FloorStructuralSegmentCount = 0,
                ElectricalStructuralSegmentCount = 0,
                FloorStructuralBounds = (object?)null,
                ElectricalStructuralBounds = (object?)null,
                WidthMismatchInches = (decimal?)null,
                HeightMismatchInches = (decimal?)null,
                EdgeDeltas = Array.Empty<object>(),
                MissingInElectricalCount = 0,
                ExtraInElectricalCount = 0,
                Mismatches = Array.Empty<object>()
            }
        };
        return new CongruenceAuditBuildResult(
            artifact,
            new PlanSetCongruenceVerificationEvidence(
                electrical.SheetId,
                PlanSetVerificationCheckStatus.InsufficientData,
                reason));
    }

    private static bool IsElectricalSheet(ExportedPlanSheetDto sheet)
        => sheet.SheetKind.Contains("Electrical", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(
               sheet.ProjectionMethod,
               "ElectricalWholeSheetSimilarity",
               StringComparison.Ordinal);

    private static FinalOutputEdgeDelta BuildFinalOutputEdge(string edge, decimal floorCoordinate, decimal electricalCoordinate)
    {
        var delta = electricalCoordinate - floorCoordinate;
        return new FinalOutputEdgeDelta(
            edge,
            floorCoordinate,
            electricalCoordinate,
            delta,
            Math.Abs(delta) <= FinalOutputTolerance);
    }

    private static bool IsInwardElectricalEdge(FinalOutputEdgeDelta edge)
        => edge.Edge switch
        {
            "Left" or "Bottom" => edge.Delta > 0m,
            "Right" or "Top" => edge.Delta < 0m,
            _ => false
        };

    private static DominantAxisAlignedOutlineSelection SelectDominantOutline(
        IReadOnlyList<RawSegment> segments)
        => DominantAxisAlignedOutlineSelector.Select(
            segments.Select(segment => new AxisAlignedStructuralSegment(
                segment.X1,
                segment.Y1,
                segment.X2,
                segment.Y2)));

    private static Bounds ToBounds(DominantAxisAlignedOutline outline)
        => new(outline.MinX, outline.MinY, outline.MaxX, outline.MaxY);

    private static IReadOnlyList<ComparableSegment> NormalizeSegments(
        IReadOnlyList<RawSegment> segments,
        DominantAxisAlignedOutline outline)
    {
        var bounds = ToBounds(outline);

        var comparableSegments = segments
            .Where(segment => IsInside(segment, bounds))
            .Select(segment => segment.Normalize(bounds))
            .Where(segment => segment.Length >= MinimumSegmentLength)
            .ToArray();

        return MergeCollinear(NormalizeWallCenterlines(comparableSegments));
    }

    private static IReadOnlyList<ComparableSegment> NormalizeWallCenterlines(IReadOnlyList<ComparableSegment> segments)
        => segments
            .Select(segment =>
            {
                var companion = segments
                    .Where(candidate => !ReferenceEquals(candidate, segment) &&
                                        candidate.Orientation == segment.Orientation &&
                                        Math.Abs(candidate.AxisCoordinate - segment.AxisCoordinate) > Tolerance &&
                                        Math.Abs(candidate.AxisCoordinate - segment.AxisCoordinate) <= WallPairMaxWidth &&
                                        segment.Overlap(candidate) >= segment.Length * WallPairOverlapRatio)
                    .OrderBy(candidate => Math.Abs(candidate.AxisCoordinate - segment.AxisCoordinate))
                    .FirstOrDefault();

                return companion is null
                    ? segment
                    : segment.WithAxisCoordinate(Snap((segment.AxisCoordinate + companion.AxisCoordinate) / 2m));
            })
            .ToArray();

    private static IReadOnlyList<ComparableSegment> MergeCollinear(IEnumerable<ComparableSegment> segments)
    {
        var merged = new List<ComparableSegment>();
        foreach (var group in segments.GroupBy(segment => (segment.Orientation, segment.AxisCoordinate, segment.Layer)))
        {
            decimal? start = null;
            var end = 0m;
            foreach (var segment in group.OrderBy(segment => segment.Start))
            {
                if (start is null)
                {
                    start = segment.Start;
                    end = segment.End;
                    continue;
                }

                if (segment.Start <= end + Tolerance)
                {
                    end = Math.Max(end, segment.End);
                    continue;
                }

                merged.Add(ComparableSegment.Create(group.Key.Orientation, group.Key.AxisCoordinate, start.Value, end, group.Key.Layer));
                start = segment.Start;
                end = segment.End;
            }

            if (start is not null)
            {
                merged.Add(ComparableSegment.Create(group.Key.Orientation, group.Key.AxisCoordinate, start.Value, end, group.Key.Layer));
            }
        }

        return merged.Where(segment => segment.Length >= MinimumSegmentLength).ToArray();
    }

    private static IReadOnlyList<ComparableSegment> SelectOutlineSegments(IReadOnlyList<ComparableSegment> segments)
    {
        if (segments.Count == 0)
        {
            return [];
        }

        var minX = segments.Min(segment => Math.Min(segment.X1, segment.X2));
        var maxX = segments.Max(segment => Math.Max(segment.X1, segment.X2));
        var minY = segments.Min(segment => Math.Min(segment.Y1, segment.Y2));
        var maxY = segments.Max(segment => Math.Max(segment.Y1, segment.Y2));

        return segments
            .Where(segment =>
                segment.Orientation == "V" &&
                (Math.Abs(segment.AxisCoordinate - minX) <= OutlineBandTolerance ||
                 Math.Abs(segment.AxisCoordinate - maxX) <= OutlineBandTolerance) ||
                segment.Orientation == "H" &&
                (Math.Abs(segment.AxisCoordinate - minY) <= OutlineBandTolerance ||
                 Math.Abs(segment.AxisCoordinate - maxY) <= OutlineBandTolerance))
            .ToArray();
    }

    private static IReadOnlyList<OutlineEdgeCoverage> BuildOutlineEdgeCoverage(
        IReadOnlyList<ComparableSegment> floorSegments,
        IReadOnlyList<ComparableSegment> electricalSegments)
    {
        var floorBounds = SegmentBounds.From(floorSegments);
        var edges = new[]
        {
            ("Left", EdgeSegments(floorSegments, "V", floorBounds.MinX).ToArray()),
            ("Right", EdgeSegments(floorSegments, "V", floorBounds.MaxX).ToArray()),
            ("Bottom", EdgeSegments(floorSegments, "H", floorBounds.MinY).ToArray()),
            ("Top", EdgeSegments(floorSegments, "H", floorBounds.MaxY).ToArray())
        };

        return edges
            .Select(edge =>
            {
                var mismatches = FindMissing(
                    edge.Item2,
                    electricalSegments,
                    $"Missing{edge.Item1}OutlineInElectrical",
                    OutlineMatchTolerance).ToArray();

                return new OutlineEdgeCoverage(edge.Item1, edge.Item2.Length, mismatches.Length, mismatches);
            })
            .ToArray();
    }

    private static IEnumerable<ComparableSegment> EdgeSegments(
        IEnumerable<ComparableSegment> segments,
        string orientation,
        decimal axisCoordinate)
        => segments.Where(segment =>
            segment.Orientation == orientation &&
            Math.Abs(segment.AxisCoordinate - axisCoordinate) <= OutlineBandTolerance);

    private static IReadOnlyList<CornerCoverage> BuildCornerCoverage(
        IReadOnlyList<ComparableSegment> floorSegments,
        IReadOnlyList<ComparableSegment> electricalSegments)
    {
        var floorBounds = SegmentBounds.From(floorSegments);
        var electricalBounds = SegmentBounds.From(electricalSegments);
        return
        [
            BuildCornerCoverage("BottomLeft", floorBounds.MinX, floorBounds.MinY, electricalBounds.MinX, electricalBounds.MinY),
            BuildCornerCoverage("BottomRight", floorBounds.MaxX, floorBounds.MinY, electricalBounds.MaxX, electricalBounds.MinY),
            BuildCornerCoverage("TopLeft", floorBounds.MinX, floorBounds.MaxY, electricalBounds.MinX, electricalBounds.MaxY),
            BuildCornerCoverage("TopRight", floorBounds.MaxX, floorBounds.MaxY, electricalBounds.MaxX, electricalBounds.MaxY)
        ];
    }

    private static CornerCoverage BuildCornerCoverage(
        string corner,
        decimal floorX,
        decimal floorY,
        decimal electricalX,
        decimal electricalY)
    {
        var deltaX = electricalX - floorX;
        var deltaY = electricalY - floorY;
        return new CornerCoverage(
            corner,
            floorX,
            floorY,
            electricalX,
            electricalY,
            deltaX,
            deltaY,
            Math.Abs(deltaX) <= OutlineMatchTolerance && Math.Abs(deltaY) <= OutlineMatchTolerance);
    }

    private static IEnumerable<object> FindMissing(
        IReadOnlyList<ComparableSegment> expected,
        IReadOnlyList<ComparableSegment> actual,
        string kind,
        decimal axisTolerance)
    {
        foreach (var segment in expected)
        {
            var overlap = actual
                .Where(candidate => candidate.Orientation == segment.Orientation &&
                                    Math.Abs(candidate.AxisCoordinate - segment.AxisCoordinate) <= axisTolerance)
                .Sum(candidate => segment.Overlap(candidate));
            if (overlap >= segment.Length * 0.8m)
            {
                continue;
            }

            var nearest = actual
                .Where(candidate => candidate.Orientation == segment.Orientation)
                .OrderBy(candidate => Math.Abs(candidate.AxisCoordinate - segment.AxisCoordinate))
                .FirstOrDefault();
            yield return new
            {
                Kind = kind,
                Axis = segment.Orientation == "H" ? "Horizontal" : "Vertical",
                FloorSegment = segment.ToAuditDto(),
                ElectricalCandidate = nearest?.ToAuditDto(),
                DeltaX = nearest is null || segment.Orientation == "H" ? 0m : nearest.AxisCoordinate - segment.AxisCoordinate,
                DeltaY = nearest is null || segment.Orientation == "V" ? 0m : nearest.AxisCoordinate - segment.AxisCoordinate,
                Overlap = Math.Min(overlap, segment.Length),
                Layer = segment.Layer,
                Reason = $"{kind}: no structural wall run covered at least 80% of this run within {axisTolerance}\"."
            };
        }
    }

    private static bool IsInside(RawSegment segment, Bounds bounds)
        => segment.MinX >= bounds.MinX - Tolerance &&
           segment.MaxX <= bounds.MaxX + Tolerance &&
           segment.MinY >= bounds.MinY - Tolerance &&
           segment.MaxY <= bounds.MaxY + Tolerance;

    private static IReadOnlyList<RawSegment> ExtractSegments(string path, bool includeElectricalWalls)
    {
        var pairs = ReadPairs(path);
        var segments = new List<RawSegment>();
        var section = string.Empty;
        string? polylineLayer = null;
        var polylineVertices = new List<Point>();
        var polylineClosed = false;

        for (var index = 0; index < pairs.Count;)
        {
            var pair = pairs[index];
            var value = pair.Value.Trim();
            if (pair.Code != "0")
            {
                index++;
                continue;
            }

            if (value.Equals("SECTION", StringComparison.OrdinalIgnoreCase) && index + 1 < pairs.Count)
            {
                index++;
                if (pairs[index].Code == "2")
                {
                    section = pairs[index].Value.Trim().ToUpperInvariant();
                }

                index++;
                continue;
            }

            if (value.Equals("ENDSEC", StringComparison.OrdinalIgnoreCase))
            {
                section = string.Empty;
                index++;
                continue;
            }

            var end = index + 1;
            while (end < pairs.Count && pairs[end].Code != "0")
            {
                end++;
            }

            if (section != "ENTITIES")
            {
                index = end;
                continue;
            }

            var entityType = value.ToUpperInvariant();
            var entityPairs = pairs.Skip(index + 1).Take(end - index - 1).ToArray();
            var layer = ResolveValue(entityPairs, "8") ?? polylineLayer ?? string.Empty;
            if (entityType == "POLYLINE")
            {
                polylineLayer = layer;
                polylineVertices.Clear();
                polylineClosed = IsClosedPolyline(entityPairs);
            }
            else if (entityType == "VERTEX")
            {
                AddVertex(entityPairs, layer, includeElectricalWalls, polylineVertices);
            }
            else if (entityType == "SEQEND")
            {
                AddConnectedSegments(polylineVertices, segments, close: polylineClosed);
                polylineLayer = null;
                polylineVertices.Clear();
                polylineClosed = false;
            }
            else if (IsStructuralLayer(layer, includeElectricalWalls))
            {
                AddEntitySegments(entityType, entityPairs, layer, segments);
            }

            index = end;
        }

        return segments
            .Where(segment => segment.IsHorizontal || segment.IsVertical)
            .Where(segment => segment.Length >= 6m)
            .ToArray();
    }

    private static void AddVertex(
        IReadOnlyList<DxfPair> pairs,
        string layer,
        bool includeElectricalWalls,
        ICollection<Point> vertices)
    {
        if (!IsStructuralLayer(layer, includeElectricalWalls))
        {
            return;
        }

        if (TryReadPoint(pairs, "10", "20", out var point))
        {
            vertices.Add(point);
        }
    }

    private static void AddEntitySegments(
        string entityType,
        IReadOnlyList<DxfPair> pairs,
        string layer,
        ICollection<RawSegment> segments)
    {
        if (entityType == "LINE" && TryReadPoint(pairs, "10", "20", out var start) && TryReadPoint(pairs, "11", "21", out var end))
        {
            segments.Add(new RawSegment(start.X, start.Y, end.X, end.Y, layer));
            return;
        }

        if (entityType is "LWPOLYLINE" or "SOLID" or "3DFACE")
        {
            var points = ReadEntityPoints(pairs);
            if (entityType == "SOLID" && points.Count == 4)
            {
                points = points[2] == points[3]
                    ? new[] { points[0], points[1], points[2] }
                    : new[] { points[0], points[1], points[3], points[2] };
            }

            var close = entityType is "SOLID" or "3DFACE" ||
                        entityType == "LWPOLYLINE" && IsClosedPolyline(pairs);
            AddConnectedSegments(
                points.Select(point => point with { Layer = layer }).ToArray(),
                segments,
                close);
        }
    }

    private static bool IsClosedPolyline(IReadOnlyList<DxfPair> pairs)
        => int.TryParse(
               ResolveValue(pairs, "70"),
               NumberStyles.Integer,
               CultureInfo.InvariantCulture,
               out var flags) &&
           (flags & 1) != 0;

    private static void AddConnectedSegments(
        IReadOnlyList<Point> points,
        ICollection<RawSegment> segments,
        bool close = false)
    {
        for (var index = 0; index + 1 < points.Count; index++)
        {
            segments.Add(new RawSegment(points[index].X, points[index].Y, points[index + 1].X, points[index + 1].Y, points[index].Layer));
        }

        if (!close || points.Count <= 2)
        {
            return;
        }

        var first = points[0];
        var last = points[^1];
        if (Math.Abs(last.X - first.X) <= Tolerance &&
            Math.Abs(last.Y - first.Y) <= Tolerance)
        {
            return;
        }

        segments.Add(new RawSegment(last.X, last.Y, first.X, first.Y, last.Layer));
    }

    private static IReadOnlyList<Point> ReadEntityPoints(IReadOnlyList<DxfPair> pairs)
    {
        var points = new List<Point>();
        for (var index = 0; index + 1 < pairs.Count; index++)
        {
            var xCode = pairs[index].Code;
            var yCode = xCode switch
            {
                "10" => "20",
                "11" => "21",
                "12" => "22",
                "13" => "23",
                _ => null
            };
            if (yCode is not null &&
                pairs[index + 1].Code == yCode &&
                TryParse(pairs[index].Value, out var x) &&
                TryParse(pairs[index + 1].Value, out var y))
            {
                points.Add(new Point(x, y, string.Empty));
            }
        }

        return points;
    }

    private static bool TryReadPoint(IReadOnlyList<DxfPair> pairs, string xCode, string yCode, out Point point)
    {
        var xValue = ResolveValue(pairs, xCode);
        var yValue = ResolveValue(pairs, yCode);
        if (TryParse(xValue, out var x) && TryParse(yValue, out var y))
        {
            point = new Point(x, y, string.Empty);
            return true;
        }

        point = null!;
        return false;
    }

    private static string? ResolveValue(IEnumerable<DxfPair> pairs, string code)
        => pairs.FirstOrDefault(pair => pair.Code == code)?.Value;

    private static bool IsStructuralLayer(string layer, bool includeElectricalWalls)
    {
        if (string.IsNullOrWhiteSpace(layer))
        {
            return false;
        }

        var normalized = layer.ToUpperInvariant();
        if (!includeElectricalWalls && normalized.Contains("ELECTRICAL", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return normalized.Contains("WALL", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("EXTERIOR", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("STRUCT", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<DxfPair> ReadPairs(string path)
        => IsBinaryDxf(path) ? ReadBinaryPairs(path) : ReadTextPairs(path);

    private static bool IsBinaryDxf(string path)
    {
        var sentinel = Encoding.ASCII.GetBytes(BinaryDxfSentinel);
        var buffer = new byte[sentinel.Length];
        using var stream = File.OpenRead(path);
        return stream.Read(buffer, 0, buffer.Length) == buffer.Length && buffer.SequenceEqual(sentinel);
    }

    private static IReadOnlyList<DxfPair> ReadTextPairs(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.Latin1);
        var pairs = new List<DxfPair>(lines.Length / 2);
        for (var index = 0; index + 1 < lines.Length; index += 2)
        {
            pairs.Add(new DxfPair(lines[index].Trim(), lines[index + 1].Trim()));
        }

        return pairs;
    }

    private static IReadOnlyList<DxfPair> ReadBinaryPairs(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path), Encoding.Latin1);
        reader.ReadBytes(Encoding.ASCII.GetByteCount(BinaryDxfSentinel));
        var pairs = new List<DxfPair>();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var code = reader.ReadInt16();
            var value = ReadBinaryValue(reader, code);
            pairs.Add(new DxfPair(code.ToString(CultureInfo.InvariantCulture), value));
            if (code == 0 && value.Equals("EOF", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return pairs;
    }

    private static string ReadBinaryValue(BinaryReader reader, short code)
        => GetBinaryValueKind(code) switch
        {
            BinaryValueKind.String => ReadNullTerminatedString(reader),
            BinaryValueKind.Double => reader.ReadDouble().ToString("G17", CultureInfo.InvariantCulture),
            BinaryValueKind.Int16 => reader.ReadInt16().ToString(CultureInfo.InvariantCulture),
            BinaryValueKind.Int32 => reader.ReadInt32().ToString(CultureInfo.InvariantCulture),
            BinaryValueKind.Int64 => reader.ReadInt64().ToString(CultureInfo.InvariantCulture),
            BinaryValueKind.Boolean => reader.ReadByte() == 0 ? "0" : "1",
            BinaryValueKind.BinaryChunk => Convert.ToHexString(reader.ReadBytes(reader.ReadByte())),
            _ => throw new InvalidDataException($"Unsupported binary DXF group code {code}.")
        };

    private static string ReadNullTerminatedString(BinaryReader reader)
    {
        var bytes = new List<byte>();
        while (true)
        {
            var next = reader.ReadByte();
            if (next == 0)
            {
                return Encoding.Latin1.GetString(bytes.ToArray());
            }

            bytes.Add(next);
        }
    }

    private static BinaryValueKind GetBinaryValueKind(short code)
        => code switch
        {
            >= 0 and <= 9 => BinaryValueKind.String,
            >= 10 and <= 59 => BinaryValueKind.Double,
            >= 60 and <= 79 => BinaryValueKind.Int16,
            >= 90 and <= 99 => BinaryValueKind.Int32,
            >= 100 and <= 109 => BinaryValueKind.String,
            >= 110 and <= 149 => BinaryValueKind.Double,
            >= 160 and <= 169 => BinaryValueKind.Int64,
            >= 170 and <= 179 => BinaryValueKind.Int16,
            >= 210 and <= 239 => BinaryValueKind.Double,
            >= 270 and <= 289 => BinaryValueKind.Int16,
            >= 290 and <= 299 => BinaryValueKind.Boolean,
            >= 300 and <= 309 => BinaryValueKind.String,
            >= 310 and <= 319 => BinaryValueKind.BinaryChunk,
            >= 320 and <= 369 => BinaryValueKind.String,
            >= 370 and <= 389 => BinaryValueKind.Int16,
            >= 390 and <= 399 => BinaryValueKind.String,
            >= 400 and <= 409 => BinaryValueKind.Int16,
            >= 410 and <= 419 => BinaryValueKind.String,
            >= 420 and <= 429 => BinaryValueKind.Int32,
            >= 430 and <= 439 => BinaryValueKind.String,
            >= 440 and <= 459 => BinaryValueKind.Int32,
            >= 460 and <= 469 => BinaryValueKind.Double,
            >= 470 and <= 481 => BinaryValueKind.String,
            999 => BinaryValueKind.String,
            >= 1000 and <= 1003 => BinaryValueKind.String,
            1004 => BinaryValueKind.BinaryChunk,
            1005 => BinaryValueKind.String,
            >= 1010 and <= 1059 => BinaryValueKind.Double,
            >= 1060 and <= 1070 => BinaryValueKind.Int16,
            1071 => BinaryValueKind.Int32,
            _ => throw new InvalidDataException($"Unsupported binary DXF group code {code}.")
        };

    private static bool TryParse(string? value, out decimal number)
        => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

    private static decimal Snap(decimal value)
        => Math.Round(value / Tolerance, 0, MidpointRounding.AwayFromZero) * Tolerance;

    private sealed record CongruenceAuditBuildResult(
        object Artifact,
        PlanSetCongruenceVerificationEvidence Verification);

    private sealed record DxfPair(string Code, string Value);

    private sealed record Point(decimal X, decimal Y, string Layer);

    private sealed record Bounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY)
    {
        public decimal Width => MaxX - MinX;

        public decimal Height => MaxY - MinY;

        public object ToAuditDto()
            => new
            {
                MinX,
                MinY,
                MaxX,
                MaxY,
                Width,
                Height
            };
    }

    private sealed record OutlineEdgeCoverage(string Edge, int RequiredCount, int MissingCount, object[] Mismatches);

    private sealed record CornerCoverage(
        string Corner,
        decimal FloorX,
        decimal FloorY,
        decimal ElectricalX,
        decimal ElectricalY,
        decimal DeltaX,
        decimal DeltaY,
        bool IsCovered);

    private sealed record SegmentBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY)
    {
        public static SegmentBounds From(IReadOnlyList<ComparableSegment> segments)
            => new(
                segments.Min(segment => Math.Min(segment.X1, segment.X2)),
                segments.Min(segment => Math.Min(segment.Y1, segment.Y2)),
                segments.Max(segment => Math.Max(segment.X1, segment.X2)),
                segments.Max(segment => Math.Max(segment.Y1, segment.Y2)));
    }

    private sealed record RawSegmentBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY)
    {
        public decimal Width => MaxX - MinX;

        public decimal Height => MaxY - MinY;

        public static RawSegmentBounds From(IReadOnlyList<RawSegment> segments)
            => new(
                segments.Min(segment => segment.MinX),
                segments.Min(segment => segment.MinY),
                segments.Max(segment => segment.MaxX),
                segments.Max(segment => segment.MaxY));

        public object ToAuditDto()
            => new
            {
                MinX,
                MinY,
                MaxX,
                MaxY,
                Width,
                Height
            };
    }

    private sealed record FinalOutputEdgeDelta(
        string Edge,
        decimal FloorCoordinate,
        decimal ElectricalCoordinate,
        decimal Delta,
        bool IsCovered);

    private sealed record RawSegment(decimal X1, decimal Y1, decimal X2, decimal Y2, string Layer)
    {
        public bool IsHorizontal => Math.Abs(Y1 - Y2) <= Tolerance;

        public bool IsVertical => Math.Abs(X1 - X2) <= Tolerance;

        public decimal Length => IsHorizontal ? Math.Abs(X2 - X1) : IsVertical ? Math.Abs(Y2 - Y1) : 0m;

        public decimal MinX => Math.Min(X1, X2);

        public decimal MaxX => Math.Max(X1, X2);

        public decimal MinY => Math.Min(Y1, Y2);

        public decimal MaxY => Math.Max(Y1, Y2);

        public ComparableSegment Normalize(Bounds bounds)
            => new(
                Snap(X1 - bounds.MinX),
                Snap(Y1 - bounds.MinY),
                Snap(X2 - bounds.MinX),
                Snap(Y2 - bounds.MinY),
                Layer);
    }

    private sealed record ComparableSegment(decimal X1, decimal Y1, decimal X2, decimal Y2, string Layer)
    {
        public string Orientation => Math.Abs(Y1 - Y2) <= Tolerance ? "H" : "V";

        public decimal AxisCoordinate => Orientation == "H" ? (Y1 + Y2) / 2m : (X1 + X2) / 2m;

        public decimal Start => Orientation == "H" ? Math.Min(X1, X2) : Math.Min(Y1, Y2);

        public decimal End => Orientation == "H" ? Math.Max(X1, X2) : Math.Max(Y1, Y2);

        public decimal Length => End - Start;

        public string Key => $"{Orientation}:{AxisCoordinate}:{Start}:{End}";

        public decimal Overlap(ComparableSegment other)
            => Math.Max(0m, Math.Min(End, other.End) - Math.Max(Start, other.Start));

        public ComparableSegment WithAxisCoordinate(decimal axis)
            => Create(Orientation, axis, Start, End, Layer);

        public static ComparableSegment Create(string orientation, decimal axis, decimal start, decimal end, string layer)
            => orientation == "H"
                ? new ComparableSegment(start, axis, end, axis, layer)
                : new ComparableSegment(axis, start, axis, end, layer);

        public object ToAuditDto()
            => new
            {
                X1,
                Y1,
                X2,
                Y2,
                Length,
                Layer
            };
    }

    private enum BinaryValueKind
    {
        String,
        Double,
        Int16,
        Int32,
        Int64,
        Boolean,
        BinaryChunk
    }
}
