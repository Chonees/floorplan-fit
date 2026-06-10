using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.ViewModels;

public sealed partial class SitePlanAdjustmentViewModel : ObservableObject
{
    private const decimal MillimetersPerInch = 25.4m;
    private const decimal EdgeInferenceTolerance = 0.000001m;

    private readonly AutoFitSuggestionFacts? autoFitSuggestionFacts;
    private readonly IAutoFitPlanSuggester? autoFitPlanSuggester;
    private readonly decimal sitePlanToMillimetersFactor;
    private readonly IReadOnlyList<PinchMarkerDto> pinchMarkers;
    private readonly IReadOnlyList<MeasurementCorridorDto> measurementCorridors;
    private readonly IReadOnlyList<MeasurementNodeDto> measurementNodes;
    private readonly IReadOnlyList<DimensionIntervalBindingDto> dimensionIntervalBindings;
    private readonly IReadOnlyList<ArticulationBandDto> articulationBands;
    private IReadOnlyList<GeometryPathDto> autoFitBaselineGeometryPaths = [];
    private IReadOnlyList<RoomLabelDto> autoFitBaselineRoomLabels = [];
    private IReadOnlyList<OpeningLabelDto> autoFitBaselineOpeningLabels = [];
    private IReadOnlyList<DimensionDto> autoFitBaselineDimensions = [];

    public SitePlanAdjustmentViewModel(
        string title,
        string subtitle,
        string previewSelectionLabel,
        string statusMessage,
        IReadOnlyList<GeometryPathDto> sitePlanGeometryPaths,
        IReadOnlyList<SitePlanRenderPathDto> sitePlanRenderPaths,
        IReadOnlyList<SitePlanTextDto> sitePlanTexts,
        IReadOnlyList<GeometryPathDto> floorPlanGeometryPaths,
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<DimensionDto> dimensions,
        AutoFitSuggestionFacts? autoFitSuggestionFacts = null,
        IAutoFitPlanSuggester? autoFitPlanSuggester = null,
        decimal sitePlanToMillimetersFactor = 1m,
        IReadOnlyList<PinchMarkerDto>? pinchMarkers = null,
        IReadOnlyList<MeasurementCorridorDto>? measurementCorridors = null,
        IReadOnlyList<MeasurementNodeDto>? measurementNodes = null,
        IReadOnlyList<DimensionIntervalBindingDto>? dimensionIntervalBindings = null,
        IReadOnlyList<ArticulationBandDto>? articulationBands = null)
    {
        Title = title;
        Subtitle = subtitle;
        PreviewSelectionLabel = previewSelectionLabel;
        StatusMessage = statusMessage;
        this.autoFitSuggestionFacts = autoFitSuggestionFacts;
        this.autoFitPlanSuggester = autoFitPlanSuggester;
        this.sitePlanToMillimetersFactor = sitePlanToMillimetersFactor > 0m
            ? sitePlanToMillimetersFactor
            : 1m;
        this.pinchMarkers = pinchMarkers ?? [];
        this.measurementCorridors = measurementCorridors ?? [];
        this.measurementNodes = measurementNodes ?? [];
        this.dimensionIntervalBindings = dimensionIntervalBindings ?? [];
        this.articulationBands = articulationBands ?? [];
        autoFitBaselineGeometryPaths = floorPlanGeometryPaths.ToArray();
        autoFitBaselineRoomLabels = roomLabels.ToArray();
        autoFitBaselineOpeningLabels = openingLabels.ToArray();
        autoFitBaselineDimensions = dimensions.ToArray();
        ReplaceItems(SitePlanGeometryPaths, sitePlanGeometryPaths);
        ReplaceItems(SitePlanRenderPaths, sitePlanRenderPaths);
        ReplaceItems(SitePlanTexts, sitePlanTexts);
        ReplaceItems(FloorPlanGeometryPaths, autoFitBaselineGeometryPaths);
        ReplaceItems(RoomLabels, autoFitBaselineRoomLabels);
        ReplaceItems(OpeningLabels, autoFitBaselineOpeningLabels);
        ReplaceItems(Dimensions, autoFitBaselineDimensions);
        AutoFitSuggestionStatus = BuildInitialAutoFitStatus(autoFitSuggestionFacts);
    }

    public string Title { get; }

    public string Subtitle { get; }

    public string PreviewSelectionLabel { get; }

    public string StatusMessage { get; }

    public ObservableCollection<GeometryPathDto> SitePlanGeometryPaths { get; } = [];

    public ObservableCollection<SitePlanRenderPathDto> SitePlanRenderPaths { get; } = [];

    public ObservableCollection<SitePlanTextDto> SitePlanTexts { get; } = [];

    public ObservableCollection<GeometryPathDto> FloorPlanGeometryPaths { get; } = [];

    public ObservableCollection<RoomLabelDto> RoomLabels { get; } = [];

    public ObservableCollection<OpeningLabelDto> OpeningLabels { get; } = [];

    public ObservableCollection<DimensionDto> Dimensions { get; } = [];

    public ObservableCollection<AutoFitSuggestionOptionViewModel> AutoFitSuggestionOptions { get; } = [];

    public bool CanSuggestAutoFitPlan =>
        autoFitSuggestionFacts?.NeedsAdjustment == true;

    public string AutoFitCandidateSummary => autoFitSuggestionFacts is null
        ? "No fit facts available yet."
        : FormatCandidateSummary(autoFitSuggestionFacts);

    [ObservableProperty]
    private bool arePreviewDimensionsVisible = true;

    [ObservableProperty]
    private bool isFloorPlanMoveToolActive;

    [ObservableProperty]
    private decimal manualOffsetX;

    [ObservableProperty]
    private decimal manualOffsetY;

    [ObservableProperty]
    private string autoFitSuggestionStatus = "Auto-fit suggestion not generated yet.";

    [ObservableProperty]
    private string autoFitSuggestionSummary = "No fit plan generated.";

    [ObservableProperty]
    private string autoFitSuggestionPlanDetails = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<Guid> changedNumberDimensionIds = [];

    [RelayCommand]
    private void ToggleFloorPlanMoveTool()
    {
        IsFloorPlanMoveToolActive = !IsFloorPlanMoveToolActive;
    }

    [RelayCommand(CanExecute = nameof(CanSuggestAutoFitPlan))]
    public async Task SuggestAutoFitPlanAsync(CancellationToken cancellationToken)
    {
        AutoFitSuggestionOptions.Clear();

        if (autoFitSuggestionFacts is null)
        {
            AutoFitSuggestionStatus = "Fit suggestions are unavailable because fit facts are missing.";
            return;
        }

        if (!autoFitSuggestionFacts.HasRequiredCandidateCapacity)
        {
            AutoFitSuggestionStatus = "OpenAI suggestion is blocked until compatible pinch groups have enough capacity.";
            AutoFitSuggestionSummary = "No valid fit plan generated.";
            AutoFitSuggestionPlanDetails = FormatBlockedSuggestionDetails(autoFitSuggestionFacts);
            return;
        }

        var deterministicPlans = AutoFitSuggestionOptionGenerator.Generate(autoFitSuggestionFacts);
        if (deterministicPlans.Count == 0)
        {
            AutoFitSuggestionStatus = "No deterministic fit options were generated.";
            AutoFitSuggestionSummary = "No valid fit plan generated.";
            AutoFitSuggestionPlanDetails = "Review named pinch groups and capacities before asking OpenAI to rank options.";
            return;
        }

        if (autoFitPlanSuggester is null)
        {
            PopulateAutoFitOptions(deterministicPlans);
            AutoFitSuggestionStatus = "Generated deterministic fit options.";
            AutoFitSuggestionSummary = FormatOptionSummary(AutoFitSuggestionOptions.Count);
            AutoFitSuggestionPlanDetails = "OpenAI ranking is unavailable, so options are shown in deterministic order.";
            return;
        }

        AutoFitSuggestionStatus = "Asking OpenAI to rank deterministic fit options...";
        var response = await autoFitPlanSuggester.SuggestAsync(autoFitSuggestionFacts, deterministicPlans, cancellationToken);
        if (response.Succeeded && response.Plans.Count > 0)
        {
            PopulateAutoFitOptions(response.Plans);
            AutoFitSuggestionStatus = "OpenAI ranked deterministic fit options.";
            AutoFitSuggestionSummary = FormatOptionSummary(AutoFitSuggestionOptions.Count);
            AutoFitSuggestionPlanDetails = "Pick the option you prefer; Apply only reduces the listed pinch groups.";
            return;
        }

        PopulateAutoFitOptions(deterministicPlans);
        AutoFitSuggestionStatus = "OpenAI ranking failed; showing deterministic fit options.";
        AutoFitSuggestionSummary = FormatOptionSummary(AutoFitSuggestionOptions.Count);
        AutoFitSuggestionPlanDetails = FormatFailureDetails(response);
    }

    [RelayCommand]
    public void ApplyAutoFitPlan(AutoFitSuggestionOptionViewModel? option)
    {
        if (option is null || autoFitSuggestionFacts is null)
        {
            return;
        }

        var geometry = autoFitBaselineGeometryPaths.ToArray();
        var roomLabels = autoFitBaselineRoomLabels.ToArray();
        var openingLabels = autoFitBaselineOpeningLabels.ToArray();
        var dimensions = autoFitBaselineDimensions.ToArray();

        foreach (var step in option.Plan.Steps)
        {
            var transform = BuildCompressionTransform(step, geometry);
            if (transform is null)
            {
                AutoFitSuggestionStatus = $"Could not apply option {option.OptionNumber}: no compatible pinch markers were found for {step.GroupName}.";
                return;
            }

            var sourceGeometryBeforeStep = geometry;
            geometry = geometry.Select(path => TransformPath(path, transform)).ToArray();
            roomLabels = roomLabels.Select(label => TransformRoomLabel(label, transform)).ToArray();
            openingLabels = openingLabels.Select(label => TransformOpeningLabel(label, transform)).ToArray();
            dimensions = BuildReactiveDimensionsForAppliedStep(
                dimensions,
                geometry,
                sourceGeometryBeforeStep,
                transform).ToArray();
        }

        ReplaceItems(FloorPlanGeometryPaths, geometry);
        ReplaceItems(RoomLabels, roomLabels);
        ReplaceItems(OpeningLabels, openingLabels);
        ReplaceItems(Dimensions, dimensions);
        ChangedNumberDimensionIds = ResolveChangedNumberDimensionIds(autoFitBaselineDimensions, dimensions);

        foreach (var autoFitOption in AutoFitSuggestionOptions)
        {
            autoFitOption.IsApplied = autoFitOption.OptionNumber == option.OptionNumber;
        }

        if (!AutoFitSuggestionOptions.Contains(option))
        {
            option.IsApplied = true;
        }

        AutoFitSuggestionStatus = $"Applied option {option.OptionNumber} to the preview.";
        AutoFitSuggestionSummary = option.Title;
        AutoFitSuggestionPlanDetails = "Selected option is highlighted below; preview geometry and related dimensions were updated.";
    }

    public void MoveFloorPlanBy(decimal deltaX, decimal deltaY)
    {
        if (deltaX == 0m && deltaY == 0m)
        {
            return;
        }

        var projection = SitePlanAdjustmentPreviewProjector.Translate(
            FloorPlanGeometryPaths,
            RoomLabels,
            OpeningLabels,
            Dimensions,
            deltaX,
            deltaY);
        var baselineProjection = SitePlanAdjustmentPreviewProjector.Translate(
            autoFitBaselineGeometryPaths,
            autoFitBaselineRoomLabels,
            autoFitBaselineOpeningLabels,
            autoFitBaselineDimensions,
            deltaX,
            deltaY);

        ReplaceItems(FloorPlanGeometryPaths, projection.FloorPlanGeometryPaths);
        ReplaceItems(RoomLabels, projection.RoomLabels);
        ReplaceItems(OpeningLabels, projection.OpeningLabels);
        ReplaceItems(Dimensions, projection.Dimensions);
        autoFitBaselineGeometryPaths = baselineProjection.FloorPlanGeometryPaths.ToArray();
        autoFitBaselineRoomLabels = baselineProjection.RoomLabels.ToArray();
        autoFitBaselineOpeningLabels = baselineProjection.OpeningLabels.ToArray();
        autoFitBaselineDimensions = baselineProjection.Dimensions.ToArray();
        ManualOffsetX = Round(ManualOffsetX + deltaX);
        ManualOffsetY = Round(ManualOffsetY + deltaY);
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private static decimal Round(decimal value)
        => decimal.Round(value, 6, MidpointRounding.AwayFromZero);

    private static string BuildInitialAutoFitStatus(AutoFitSuggestionFacts? facts)
    {
        if (facts is null)
        {
            return "Auto-fit facts are not available yet.";
        }

        return facts.NeedsAdjustment
            ? facts.HasRequiredCandidateCapacity
                ? "Auto-fit facts are ready. Review candidates, then ask OpenAI for a bounded suggestion."
                : "Auto-fit needs compatible pinch groups with enough capacity before OpenAI can suggest a valid plan."
            : "The projected floor plan already fits inside the buildable area.";
    }

    private static string FormatCandidateSummary(AutoFitSuggestionFacts facts)
    {
        if (!facts.NeedsAdjustment)
        {
            return "Fits: no Width or Height deficit detected.";
        }

        var deficit = $"Deficit: Width {facts.Deficit.WidthInches:0.###}\"; Height {facts.Deficit.HeightInches:0.###}\".";
        if (facts.CandidateGroups.Count == 0)
        {
            return $"{deficit} No candidate pinch groups are available for the required axes.";
        }

        var groups = string.Join(
            "; ",
            facts.CandidateGroups.Select(group =>
                $"{group.Name} ({group.AxisTag}, cap {group.CapacityInches:0.###}\")"));
        var warnings = facts.Warnings.Count == 0
            ? string.Empty
            : $" Warnings: {string.Join(" ", facts.Warnings)}";
        return $"{deficit} Candidates: {groups}.{warnings}";
    }

    internal static string FormatPlanDetails(AutoFitSuggestionPlan plan)
    {
        var lines = plan.Steps
            .Select(step => $"- {step.GroupName} ({step.AxisTag}): {step.ReductionInches:0.###}\" — {step.Reason}")
            .ToList();

        if (!string.IsNullOrWhiteSpace(plan.Explanation))
        {
            lines.Add(plan.Explanation);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatFailureDetails(AutoFitSuggestionPlanResponse response)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
        {
            lines.Add(response.ErrorMessage);
        }

        lines.AddRange(response.Validation.Errors);
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatBlockedSuggestionDetails(AutoFitSuggestionFacts facts)
    {
        if (facts.Warnings.Count > 0)
        {
            return string.Join(Environment.NewLine, facts.Warnings);
        }

        return "Create compatible Width/Height pinch groups with enough available trim before asking OpenAI.";
    }

    private void PopulateAutoFitOptions(IReadOnlyList<AutoFitSuggestionPlan> plans)
    {
        AutoFitSuggestionOptions.Clear();
        for (var index = 0; index < plans.Count; index++)
        {
            AutoFitSuggestionOptions.Add(new AutoFitSuggestionOptionViewModel(index + 1, plans[index]));
        }
    }

    private static string FormatOptionSummary(int count)
        => count == 1
            ? "1 fit option available. Pick it to apply the preview reduction."
            : $"{count} fit options available. Pick the one you prefer to apply the preview reduction.";

    private static IReadOnlyList<Guid> ResolveChangedNumberDimensionIds(
        IReadOnlyList<DimensionDto> baselineDimensions,
        IReadOnlyList<DimensionDto> appliedDimensions)
    {
        if (baselineDimensions.Count == 0 || appliedDimensions.Count == 0)
        {
            return [];
        }

        var baselineById = baselineDimensions.ToDictionary(dimension => dimension.DimensionId);
        return appliedDimensions
            .Where(dimension =>
                baselineById.TryGetValue(dimension.DimensionId, out var baselineDimension) &&
                !string.Equals(
                    NormalizeVisibleDimensionText(baselineDimension.DisplayText),
                    NormalizeVisibleDimensionText(dimension.DisplayText),
                    StringComparison.Ordinal))
            .Select(dimension => dimension.DimensionId)
            .ToArray();
    }

    private static string NormalizeVisibleDimensionText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();

    private AutoFitCompressionTransform? BuildCompressionTransform(
        AutoFitSuggestionStep step,
        IReadOnlyList<GeometryPathDto> geometry)
    {
        var candidate = autoFitSuggestionFacts?.CandidateGroups.SingleOrDefault(group =>
            string.Equals(group.Name, step.GroupName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(group.AxisTag, step.AxisTag, StringComparison.OrdinalIgnoreCase));
        if (candidate is null)
        {
            return null;
        }

        var groupMarkers = pinchMarkers
            .Where(marker => marker.PinchGroupId == candidate.PinchGroupId)
            .Where(marker => string.Equals(marker.AxisTag, step.AxisTag, StringComparison.OrdinalIgnoreCase))
            .OrderBy(marker => marker.SortOrder)
            .ToArray();
        if (groupMarkers.Length == 0)
        {
            return null;
        }

        var geometryLookup = geometry.ToDictionary(path => path.Id);
        var requestedTrimSourceUnits = (step.ReductionInches * MillimetersPerInch) / sitePlanToMillimetersFactor;
        var markerTransforms = new List<AutoFitCompressionMarker>();

        foreach (var marker in groupMarkers)
        {
            if (!geometryLookup.TryGetValue(marker.GeometryPathId, out var path))
            {
                continue;
            }

            var point = TryGetPointAtRatio(path, marker.PositionRatio);
            if (point is null)
            {
                continue;
            }

            var coordinate = IsHeight(step.AxisTag)
                ? point.Value.Y
                : point.Value.X;
            var maxTrimSourceUnits = marker.MaxTrimMm / sitePlanToMillimetersFactor;
            markerTransforms.Add(new AutoFitCompressionMarker(
                coordinate,
                decimal.Min(maxTrimSourceUnits, requestedTrimSourceUnits / groupMarkers.Length)));
        }

        if (markerTransforms.Count == 0)
        {
            return null;
        }

        var orderedMarkers = markerTransforms.OrderBy(marker => marker.Coordinate).ToArray();

        return new AutoFitCompressionTransform(
            candidate.PinchGroupId,
            NormalizeAxis(step.AxisTag),
            ResolveCompressionEdge(step.AxisTag, autoFitSuggestionFacts!.Deficit, orderedMarkers, geometry),
            orderedMarkers);
    }

    private IReadOnlyList<DimensionDto> BuildReactiveDimensionsForAppliedStep(
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<GeometryPathDto> previewGeometry,
        IReadOnlyList<GeometryPathDto> sourceGeometryBeforeStep,
        AutoFitCompressionTransform transform)
    {
        if (measurementCorridors.Count == 0 ||
            measurementNodes.Count == 0 ||
            dimensionIntervalBindings.Count == 0 ||
            articulationBands.Count == 0)
        {
            return dimensions.Select(dimension => TransformDimension(dimension, transform)).ToArray();
        }

        return DimensionIntervalReactiveProjector.Project(
            dimensions,
            previewGeometry,
            measurementCorridors,
            measurementNodes,
            dimensionIntervalBindings,
            articulationBands,
            transform.PinchGroupId,
            sourceGeometryBeforeStep);
    }

    private static AutoFitCompressionEdge ResolveCompressionEdge(
        string axisTag,
        AutoFitEnvelopeDeficitDto deficit)
    {
        if (IsHeight(axisTag))
        {
            return deficit.TopInches >= deficit.BottomInches
                ? AutoFitCompressionEdge.Top
                : AutoFitCompressionEdge.Bottom;
        }

        return deficit.RightInches >= deficit.LeftInches
            ? AutoFitCompressionEdge.Right
            : AutoFitCompressionEdge.Left;
    }

    private static AutoFitCompressionEdge ResolveCompressionEdge(
        string axisTag,
        AutoFitEnvelopeDeficitDto deficit,
        IReadOnlyList<AutoFitCompressionMarker> markers,
        IReadOnlyList<GeometryPathDto> geometry)
    {
        var fallbackEdge = ResolveCompressionEdge(axisTag, deficit);
        var bounds = TryGetGeometryBounds(geometry);
        if (bounds is null || markers.Count == 0)
        {
            return fallbackEdge;
        }

        var averageMarkerCoordinate = markers.Average(marker => marker.Coordinate);
        if (IsHeight(axisTag))
        {
            var centerY = (bounds.Value.MinY + bounds.Value.MaxY) / 2m;
            if (averageMarkerCoordinate < centerY - EdgeInferenceTolerance)
            {
                return AutoFitCompressionEdge.Bottom;
            }

            if (averageMarkerCoordinate > centerY + EdgeInferenceTolerance)
            {
                return AutoFitCompressionEdge.Top;
            }

            return fallbackEdge;
        }

        var centerX = (bounds.Value.MinX + bounds.Value.MaxX) / 2m;
        if (averageMarkerCoordinate < centerX - EdgeInferenceTolerance)
        {
            return AutoFitCompressionEdge.Left;
        }

        if (averageMarkerCoordinate > centerX + EdgeInferenceTolerance)
        {
            return AutoFitCompressionEdge.Right;
        }

        return fallbackEdge;
    }

    private static GeometryPathDto TransformPath(
        GeometryPathDto path,
        AutoFitCompressionTransform transform)
        => new(
            path.Id,
            path.IsClosed,
            path.Segments
                .Select(segment =>
                {
                    var start = transform.Apply(segment.StartX, segment.StartY);
                    var end = transform.Apply(segment.EndX, segment.EndY);
                    return new GeometrySegmentDto(
                        segment.GeometryPathId,
                        segment.SortOrder,
                        start.X,
                        start.Y,
                        end.X,
                        end.Y);
                })
                .ToArray());

    private static RoomLabelDto TransformRoomLabel(
        RoomLabelDto label,
        AutoFitCompressionTransform transform)
    {
        var point = transform.Apply(label.X, label.Y);
        var detectedPoint = label.DetectedX.HasValue && label.DetectedY.HasValue
            ? transform.Apply(label.DetectedX.Value, label.DetectedY.Value)
            : (AutoFitPoint?)null;

        return label with
        {
            X = point.X,
            Y = point.Y,
            DetectedX = detectedPoint?.X,
            DetectedY = detectedPoint?.Y
        };
    }

    private static OpeningLabelDto TransformOpeningLabel(
        OpeningLabelDto label,
        AutoFitCompressionTransform transform)
    {
        var point = transform.Apply(label.X, label.Y);
        var detectedPoint = label.DetectedX.HasValue && label.DetectedY.HasValue
            ? transform.Apply(label.DetectedX.Value, label.DetectedY.Value)
            : (AutoFitPoint?)null;

        return label with
        {
            X = point.X,
            Y = point.Y,
            DetectedX = detectedPoint?.X,
            DetectedY = detectedPoint?.Y
        };
    }

    private static DimensionDto TransformDimension(
        DimensionDto dimension,
        AutoFitCompressionTransform transform)
    {
        var defPoint = transform.Apply(dimension.DefPointX, dimension.DefPointY);
        var defPoint2 = transform.Apply(dimension.DefPoint2X, dimension.DefPoint2Y);
        var defPoint3 = transform.Apply(dimension.DefPoint3X, dimension.DefPoint3Y);
        var renderTextPoint = dimension.RenderTextX.HasValue && dimension.RenderTextY.HasValue
            ? transform.Apply(dimension.RenderTextX.Value, dimension.RenderTextY.Value)
            : (AutoFitPoint?)null;

        return dimension with
        {
            DefPointX = defPoint.X,
            DefPointY = defPoint.Y,
            DefPoint2X = defPoint2.X,
            DefPoint2Y = defPoint2.Y,
            DefPoint3X = defPoint3.X,
            DefPoint3Y = defPoint3.Y,
            RenderTextX = renderTextPoint?.X,
            RenderTextY = renderTextPoint?.Y,
            LineSegments = dimension.LineSegments
                .Select(segment =>
                {
                    var start = transform.Apply(segment.StartX, segment.StartY);
                    var end = transform.Apply(segment.EndX, segment.EndY);
                    return new DimensionLineSegmentDto(start.X, start.Y, end.X, end.Y);
                })
                .ToArray(),
            LinePrimitives = dimension.LinePrimitives
                .Select(line =>
                {
                    var start = transform.Apply(line.StartX, line.StartY);
                    var end = transform.Apply(line.EndX, line.EndY);
                    return line with
                    {
                        StartX = start.X,
                        StartY = start.Y,
                        EndX = end.X,
                        EndY = end.Y
                    };
                })
                .ToArray(),
            TextPrimitives = dimension.TextPrimitives
                .Select(text =>
                {
                    var point = transform.Apply(text.X, text.Y);
                    return text with { X = point.X, Y = point.Y };
                })
                .ToArray(),
            InsertPrimitives = dimension.InsertPrimitives
                .Select(insert =>
                {
                    var point = transform.Apply(insert.X, insert.Y);
                    return insert with { X = point.X, Y = point.Y };
                })
                .ToArray(),
            CirclePrimitives = dimension.CirclePrimitives
                .Select(circle =>
                {
                    var point = transform.Apply(circle.CenterX, circle.CenterY);
                    return circle with { CenterX = point.X, CenterY = point.Y };
                })
                .ToArray(),
            ArcPrimitives = dimension.ArcPrimitives
                .Select(arc =>
                {
                    var point = transform.Apply(arc.CenterX, arc.CenterY);
                    return arc with { CenterX = point.X, CenterY = point.Y };
                })
                .ToArray(),
            SolidPrimitives = dimension.SolidPrimitives
                .Select(solid =>
                {
                    var point1 = transform.Apply(solid.Point1X, solid.Point1Y);
                    var point2 = transform.Apply(solid.Point2X, solid.Point2Y);
                    var point3 = transform.Apply(solid.Point3X, solid.Point3Y);
                    var point4 = transform.Apply(solid.Point4X, solid.Point4Y);
                    return solid with
                    {
                        Point1X = point1.X,
                        Point1Y = point1.Y,
                        Point2X = point2.X,
                        Point2Y = point2.Y,
                        Point3X = point3.X,
                        Point3Y = point3.Y,
                        Point4X = point4.X,
                        Point4Y = point4.Y
                    };
                })
                .ToArray()
        };
    }

    private static AutoFitPoint? TryGetPointAtRatio(GeometryPathDto path, decimal positionRatio)
    {
        if (path.Segments.Count == 0)
        {
            return null;
        }

        var clampedRatio = decimal.Clamp(positionRatio, 0m, 1m);
        var totalLength = path.Segments.Sum(GetSegmentLength);
        if (totalLength <= double.Epsilon)
        {
            var first = path.Segments[0];
            return new AutoFitPoint(first.StartX, first.StartY);
        }

        var targetLength = totalLength * (double)clampedRatio;
        var traversedLength = 0d;

        foreach (var segment in path.Segments)
        {
            var segmentLength = GetSegmentLength(segment);
            if (traversedLength + segmentLength >= targetLength)
            {
                var localRatio = segmentLength <= double.Epsilon
                    ? 0d
                    : (targetLength - traversedLength) / segmentLength;
                return new AutoFitPoint(
                    Round(segment.StartX + ((segment.EndX - segment.StartX) * decimal.CreateChecked(localRatio))),
                    Round(segment.StartY + ((segment.EndY - segment.StartY) * decimal.CreateChecked(localRatio))));
            }

            traversedLength += segmentLength;
        }

        var last = path.Segments[^1];
        return new AutoFitPoint(last.EndX, last.EndY);
    }

    private static double GetSegmentLength(GeometrySegmentDto segment)
    {
        var deltaX = (double)(segment.EndX - segment.StartX);
        var deltaY = (double)(segment.EndY - segment.StartY);
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    private static AutoFitGeometryBounds? TryGetGeometryBounds(IReadOnlyList<GeometryPathDto> geometry)
    {
        var hasPoint = false;
        var minX = 0m;
        var minY = 0m;
        var maxX = 0m;
        var maxY = 0m;

        foreach (var segment in geometry.SelectMany(path => path.Segments))
        {
            Include(segment.StartX, segment.StartY);
            Include(segment.EndX, segment.EndY);
        }

        return hasPoint
            ? new AutoFitGeometryBounds(minX, minY, maxX, maxY)
            : null;

        void Include(decimal x, decimal y)
        {
            if (!hasPoint)
            {
                minX = x;
                maxX = x;
                minY = y;
                maxY = y;
                hasPoint = true;
                return;
            }

            minX = decimal.Min(minX, x);
            minY = decimal.Min(minY, y);
            maxX = decimal.Max(maxX, x);
            maxY = decimal.Max(maxY, y);
        }
    }

    private static bool IsHeight(string axisTag)
        => string.Equals(axisTag, "Height", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeAxis(string axisTag)
        => IsHeight(axisTag) ? "Height" : "Width";

    private readonly record struct AutoFitGeometryBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY);
}

public sealed partial class AutoFitSuggestionOptionViewModel : ObservableObject
{
    public AutoFitSuggestionOptionViewModel(int optionNumber, AutoFitSuggestionPlan plan)
    {
        OptionNumber = optionNumber;
        Plan = plan;
        Title = plan.Summary;
        Details = SitePlanAdjustmentViewModel.FormatPlanDetails(plan);
    }

    public int OptionNumber { get; }

    public AutoFitSuggestionPlan Plan { get; }

    public string Title { get; }

    public string Details { get; }

    public string ApplyLabel => $"Apply option {OptionNumber}";

    [ObservableProperty]
    private bool isApplied;
}

internal sealed record AutoFitCompressionTransform(
    Guid PinchGroupId,
    string AxisTag,
    AutoFitCompressionEdge Edge,
    IReadOnlyList<AutoFitCompressionMarker> Markers)
{
    public AutoFitPoint Apply(decimal x, decimal y)
    {
        var deltaX = 0m;
        var deltaY = 0m;

        foreach (var marker in Markers)
        {
            if (string.Equals(AxisTag, "Width", StringComparison.OrdinalIgnoreCase))
            {
                if (Edge == AutoFitCompressionEdge.Right && x >= marker.Coordinate)
                {
                    deltaX -= marker.TrimSourceUnits;
                }
                else if (Edge == AutoFitCompressionEdge.Left && x <= marker.Coordinate)
                {
                    deltaX += marker.TrimSourceUnits;
                }

                continue;
            }

            if (Edge == AutoFitCompressionEdge.Top && y >= marker.Coordinate)
            {
                deltaY -= marker.TrimSourceUnits;
            }
            else if (Edge == AutoFitCompressionEdge.Bottom && y <= marker.Coordinate)
            {
                deltaY += marker.TrimSourceUnits;
            }
        }

        return new AutoFitPoint(
            decimal.Round(x + deltaX, 6, MidpointRounding.AwayFromZero),
            decimal.Round(y + deltaY, 6, MidpointRounding.AwayFromZero));
    }
}

internal sealed record AutoFitCompressionMarker(decimal Coordinate, decimal TrimSourceUnits);

internal readonly record struct AutoFitPoint(decimal X, decimal Y);

internal enum AutoFitCompressionEdge
{
    Left,
    Right,
    Top,
    Bottom
}

internal static class SitePlanAdjustmentPreviewProjector
{
    public static SitePlanAdjustmentViewModel Build(
        FloorPlanLibraryItemDto libraryItem,
        FloorPlanLibraryVersionDto version,
        FloorPlanReviewViewModel reviewViewModel,
        SitePlanPreviewDto sitePlan,
        IAutoFitPlanSuggester? autoFitPlanSuggester = null)
    {
        var floorPlanPlacementGeometryPathIds = reviewViewModel.WallCandidates
            .Select(candidate => candidate.GeometryPathId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();
        var projection = Project(
            reviewViewModel.GeometryPaths,
            reviewViewModel.RoomLabels,
            reviewViewModel.OpeningLabels,
            reviewViewModel.Dimensions,
            reviewViewModel.MeasurementContext,
            sitePlan,
            floorPlanPlacementGeometryPathIds);
        var filteredSitePlan = FilterSitePlanForAdjustment(sitePlan);
        var autoFitGeometryPaths = ResolveAutoFitGeometryPaths(
            projection.FloorPlanGeometryPaths,
            floorPlanPlacementGeometryPathIds);
        var autoFitFacts = AutoFitSuggestionFactBuilder.Build(
            autoFitGeometryPaths,
            sitePlan.BuildableArea,
            sitePlan.ToMillimetersFactor,
            reviewViewModel.PinchGroups,
            reviewViewModel.ArticulationBands,
            reviewViewModel.MeasurementCorridors,
            reviewViewModel.DimensionIntervalBindings);

        return new SitePlanAdjustmentViewModel(
            "Adjust to Site Plan",
            $"{libraryItem.Code} v{version.VersionNumber} over {sitePlan.FileName}",
            $"Centered in buildable area: {Format(sitePlan.BuildableArea.MinX)}, {Format(sitePlan.BuildableArea.MinY)} -> {Format(sitePlan.BuildableArea.MaxX)}, {Format(sitePlan.BuildableArea.MaxY)}",
            "Preview only. Showing lot/terrain and setbacks; use Move Floor Plan for fine placement.",
            filteredSitePlan.GeometryPaths,
            filteredSitePlan.RenderPaths,
            filteredSitePlan.Texts,
            projection.FloorPlanGeometryPaths,
            projection.RoomLabels,
            projection.OpeningLabels,
            projection.Dimensions,
            autoFitFacts,
            autoFitPlanSuggester,
            sitePlan.ToMillimetersFactor,
            reviewViewModel.PinchMarkers,
            reviewViewModel.MeasurementCorridors,
            reviewViewModel.MeasurementNodes,
            reviewViewModel.DimensionIntervalBindings,
            reviewViewModel.ArticulationBands);
    }

    internal static SitePlanAdjustmentSitePlanDisplay FilterSitePlanForAdjustment(SitePlanPreviewDto sitePlan)
    {
        var renderPaths = sitePlan.RenderPaths
            .Where(IsTerrainOrSetbackPath)
            .ToArray();

        if (renderPaths.Length == 0)
        {
            renderPaths = sitePlan.RenderPaths.ToArray();
        }

        var geometryPaths = renderPaths
            .Select(path => new GeometryPathDto(path.Id, path.IsClosed, path.Segments))
            .ToArray();
        var texts = sitePlan.Texts
            .Where(IsSetbackText)
            .ToArray();

        return new SitePlanAdjustmentSitePlanDisplay(geometryPaths, renderPaths, texts);
    }

    private static bool IsTerrainOrSetbackPath(SitePlanRenderPathDto path)
        => path.IsSetback || ContainsTerrainLayerToken(path.SourceLayer);

    private static bool IsSetbackText(SitePlanTextDto text)
        => text.IsSetback || ContainsSetbackToken(text.Text) || ContainsSetbackToken(text.SourceLayer);

    private static bool ContainsTerrainLayerToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("PROP", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("PROPERTY", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("LOT", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("BOUND", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("PARCEL", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsSetbackToken(string? value)
        => value?.Contains("SETBACK", StringComparison.OrdinalIgnoreCase) == true;

    internal static SitePlanAdjustmentProjection Project(
        IReadOnlyList<GeometryPathDto> floorPlanGeometryPaths,
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<DimensionDto> dimensions,
        MeasurementContextDto? floorPlanMeasurementContext,
        SitePlanPreviewDto sitePlan,
        IReadOnlySet<Guid>? floorPlanPlacementGeometryPathIds = null)
    {
        var floorBounds = TryBoundsOf(ResolvePlacementGeometryPaths(
            floorPlanGeometryPaths,
            floorPlanPlacementGeometryPathIds));
        if (floorBounds is null)
        {
            return new SitePlanAdjustmentProjection([], [], [], [], 1m, 0m, 0m);
        }

        var scale = ResolveFloorToSiteScale(floorPlanMeasurementContext, sitePlan);
        var floorCenterX = ((floorBounds.Value.MinX + floorBounds.Value.MaxX) / 2m) * scale;
        var floorCenterY = ((floorBounds.Value.MinY + floorBounds.Value.MaxY) / 2m) * scale;
        var offsetX = sitePlan.BuildableArea.CenterX - floorCenterX;
        var offsetY = sitePlan.BuildableArea.CenterY - floorCenterY;
        var transform = new CoordinateTransform(scale, offsetX, offsetY);

        return new SitePlanAdjustmentProjection(
            floorPlanGeometryPaths.Select(path => TransformPath(path, transform)).ToArray(),
            roomLabels.Select(label => TransformRoomLabel(label, transform)).ToArray(),
            openingLabels.Select(label => TransformOpeningLabel(label, transform)).ToArray(),
            dimensions.Select(dimension => TransformDimension(dimension, transform)).ToArray(),
            scale,
            offsetX,
            offsetY);
    }

    internal static SitePlanAdjustmentProjection Translate(
        IReadOnlyList<GeometryPathDto> floorPlanGeometryPaths,
        IReadOnlyList<RoomLabelDto> roomLabels,
        IReadOnlyList<OpeningLabelDto> openingLabels,
        IReadOnlyList<DimensionDto> dimensions,
        decimal deltaX,
        decimal deltaY)
    {
        var transform = new CoordinateTransform(1m, deltaX, deltaY);

        return new SitePlanAdjustmentProjection(
            floorPlanGeometryPaths.Select(path => TransformPath(path, transform)).ToArray(),
            roomLabels.Select(label => TransformRoomLabel(label, transform)).ToArray(),
            openingLabels.Select(label => TransformOpeningLabel(label, transform)).ToArray(),
            dimensions.Select(dimension => TransformDimension(dimension, transform)).ToArray(),
            1m,
            deltaX,
            deltaY);
    }

    private static IReadOnlyList<GeometryPathDto> ResolvePlacementGeometryPaths(
        IReadOnlyList<GeometryPathDto> floorPlanGeometryPaths,
        IReadOnlySet<Guid>? floorPlanPlacementGeometryPathIds)
    {
        if (floorPlanPlacementGeometryPathIds is null || floorPlanPlacementGeometryPathIds.Count == 0)
        {
            return floorPlanGeometryPaths;
        }

        var placementGeometryPaths = floorPlanGeometryPaths
            .Where(path => floorPlanPlacementGeometryPathIds.Contains(path.Id))
            .ToArray();

        return placementGeometryPaths.Length > 0
            ? placementGeometryPaths
            : floorPlanGeometryPaths;
    }

    internal static IReadOnlyList<GeometryPathDto> ResolveAutoFitGeometryPaths(
        IReadOnlyList<GeometryPathDto> projectedFloorPlanGeometryPaths,
        IReadOnlySet<Guid>? floorPlanPlacementGeometryPathIds)
        => ResolvePlacementGeometryPaths(projectedFloorPlanGeometryPaths, floorPlanPlacementGeometryPathIds);

    private static GeometryPathDto TransformPath(GeometryPathDto path, CoordinateTransform transform)
        => new(
            path.Id,
            path.IsClosed,
            path.Segments
                .Select(segment => new GeometrySegmentDto(
                    segment.GeometryPathId,
                    segment.SortOrder,
                    transform.X(segment.StartX),
                    transform.Y(segment.StartY),
                    transform.X(segment.EndX),
                    transform.Y(segment.EndY)))
                .ToArray());

    private static RoomLabelDto TransformRoomLabel(RoomLabelDto label, CoordinateTransform transform)
        => label with
        {
            X = transform.X(label.X),
            Y = transform.Y(label.Y),
            TextHeight = TransformNullableLength(label.TextHeight, transform.Scale),
            DetectedX = TransformNullableX(label.DetectedX, transform),
            DetectedY = TransformNullableY(label.DetectedY, transform),
            DetectedTextHeight = TransformNullableLength(label.DetectedTextHeight, transform.Scale)
        };

    private static OpeningLabelDto TransformOpeningLabel(OpeningLabelDto label, CoordinateTransform transform)
        => label with
        {
            X = transform.X(label.X),
            Y = transform.Y(label.Y),
            TextHeight = TransformNullableLength(label.TextHeight, transform.Scale),
            DetectedX = TransformNullableX(label.DetectedX, transform),
            DetectedY = TransformNullableY(label.DetectedY, transform),
            DetectedTextHeight = TransformNullableLength(label.DetectedTextHeight, transform.Scale)
        };

    private static DimensionDto TransformDimension(DimensionDto dimension, CoordinateTransform transform)
        => dimension with
        {
            DefPointX = transform.X(dimension.DefPointX),
            DefPointY = transform.Y(dimension.DefPointY),
            DefPoint2X = transform.X(dimension.DefPoint2X),
            DefPoint2Y = transform.Y(dimension.DefPoint2Y),
            DefPoint3X = transform.X(dimension.DefPoint3X),
            DefPoint3Y = transform.Y(dimension.DefPoint3Y),
            RenderTextX = TransformNullableX(dimension.RenderTextX, transform),
            RenderTextY = TransformNullableY(dimension.RenderTextY, transform),
            RenderTextHeight = TransformNullableLength(dimension.RenderTextHeight, transform.Scale),
            LineSegments = dimension.LineSegments
                .Select(segment => new DimensionLineSegmentDto(
                    transform.X(segment.StartX),
                    transform.Y(segment.StartY),
                    transform.X(segment.EndX),
                    transform.Y(segment.EndY)))
                .ToArray(),
            LinePrimitives = dimension.LinePrimitives
                .Select(line => line with
                {
                    StartX = transform.X(line.StartX),
                    StartY = transform.Y(line.StartY),
                    EndX = transform.X(line.EndX),
                    EndY = transform.Y(line.EndY)
                })
                .ToArray(),
            TextPrimitives = dimension.TextPrimitives
                .Select(text => text with
                {
                    X = transform.X(text.X),
                    Y = transform.Y(text.Y),
                    Height = TransformLength(text.Height, transform.Scale)
                })
                .ToArray(),
            InsertPrimitives = dimension.InsertPrimitives
                .Select(insert => insert with
                {
                    X = transform.X(insert.X),
                    Y = transform.Y(insert.Y)
                })
                .ToArray(),
            CirclePrimitives = dimension.CirclePrimitives
                .Select(circle => circle with
                {
                    CenterX = transform.X(circle.CenterX),
                    CenterY = transform.Y(circle.CenterY),
                    Radius = TransformLength(circle.Radius, transform.Scale)
                })
                .ToArray(),
            ArcPrimitives = dimension.ArcPrimitives
                .Select(arc => arc with
                {
                    CenterX = transform.X(arc.CenterX),
                    CenterY = transform.Y(arc.CenterY),
                    Radius = TransformLength(arc.Radius, transform.Scale)
                })
                .ToArray(),
            SolidPrimitives = dimension.SolidPrimitives
                .Select(solid => solid with
                {
                    Point1X = transform.X(solid.Point1X),
                    Point1Y = transform.Y(solid.Point1Y),
                    Point2X = transform.X(solid.Point2X),
                    Point2Y = transform.Y(solid.Point2Y),
                    Point3X = transform.X(solid.Point3X),
                    Point3Y = transform.Y(solid.Point3Y),
                    Point4X = transform.X(solid.Point4X),
                    Point4Y = transform.Y(solid.Point4Y)
                })
                .ToArray()
        };

    private static decimal ResolveFloorToSiteScale(MeasurementContextDto? floorPlanMeasurementContext, SitePlanPreviewDto sitePlan)
    {
        var floorFactor = floorPlanMeasurementContext?.ToMillimetersFactor;
        var siteFactor = sitePlan.ToMillimetersFactor;

        return floorFactor is > 0m && siteFactor > 0m
            ? floorFactor.Value / siteFactor
            : 1m;
    }

    private static GeometryBounds? TryBoundsOf(IEnumerable<GeometryPathDto> paths)
    {
        var points = paths
            .SelectMany(path => path.Segments)
            .SelectMany(segment => new[]
            {
                (X: segment.StartX, Y: segment.StartY),
                (X: segment.EndX, Y: segment.EndY)
            })
            .ToArray();

        if (points.Length == 0)
        {
            return null;
        }

        return new GeometryBounds(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
    }

    private static decimal? TransformNullableX(decimal? value, CoordinateTransform transform)
        => value.HasValue ? transform.X(value.Value) : null;

    private static decimal? TransformNullableY(decimal? value, CoordinateTransform transform)
        => value.HasValue ? transform.Y(value.Value) : null;

    private static decimal? TransformNullableLength(decimal? value, decimal scale)
        => value.HasValue ? TransformLength(value.Value, scale) : null;

    private static decimal TransformLength(decimal value, decimal scale)
        => Round(value * scale);

    private static string Format(decimal value)
        => value.ToString("0.###");

    private static decimal Round(decimal value)
        => decimal.Round(value, 6, MidpointRounding.AwayFromZero);

    private readonly record struct CoordinateTransform(decimal Scale, decimal OffsetX, decimal OffsetY)
    {
        public decimal X(decimal value) => Round((value * Scale) + OffsetX);

        public decimal Y(decimal value) => Round((value * Scale) + OffsetY);
    }

    private readonly record struct GeometryBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY);
}

internal sealed record SitePlanAdjustmentProjection(
    IReadOnlyList<GeometryPathDto> FloorPlanGeometryPaths,
    IReadOnlyList<RoomLabelDto> RoomLabels,
    IReadOnlyList<OpeningLabelDto> OpeningLabels,
    IReadOnlyList<DimensionDto> Dimensions,
    decimal Scale,
    decimal OffsetX,
    decimal OffsetY);

internal sealed record SitePlanAdjustmentSitePlanDisplay(
    IReadOnlyList<GeometryPathDto> GeometryPaths,
    IReadOnlyList<SitePlanRenderPathDto> RenderPaths,
    IReadOnlyList<SitePlanTextDto> Texts);
