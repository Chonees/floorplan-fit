using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Desktop.Controls;

public sealed class FloorPlanPreviewControl : Control
{
    private const double PreviewPadding = 48d;
    private const double HitTestTolerance = 8d;
    private const double UserZoomStep = 1.12d;
    internal const decimal MovementPersistenceEpsilon = 0.001m;
    private const double DimensionHandleHitTolerance = 10d;
    internal const double MinimumUserZoomFactor = 0.35d;
    internal const double MaximumUserZoomFactor = 6d;
    private readonly PreviewCollectionObserverHub collectionObserverHub;
    private FloorPlanPreviewGeometry.PreviewCompressionEdge? activeDragEdge;
    private PreviewArtifactMoveState? activeArtifactMove;
    private Point dragStartPoint;
    private decimal activePreviewTrimMm;
    private PreviewZoomState previewZoomState = PreviewZoomState.Default;
    private bool isPanningPreview;
    private Point panStartPoint;
    private PreviewZoomState panStartZoomState;
    private PreviewDimensionEditState? activeDimensionEdit;

    static FloorPlanPreviewControl()
    {
        AffectsRender<FloorPlanPreviewControl>(
            GeometryPathsProperty,
            HighlightGeometryPathIdProperty,
            PinchMarkersProperty,
            RoomLabelsProperty,
            HighlightRoomLabelIdProperty,
            WallCandidatesProperty,
            OpeningCandidatesProperty,
            OpeningLabelsProperty,
            HighlightOpeningLabelIdProperty,
            DimensionsProperty,
            DimensionAssociationsProperty,
            HighlightDimensionIdProperty,
            FixedPlanComponentsProperty,
            ProtectedDetailAssembliesProperty,
            CuratedPlanArtifactsProperty,
            PreviewPinchGroupIdProperty,
            PreviewAxisTagProperty,
            IsPinchPlacementArmedProperty);
        GeometryPathsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnGeometryPathsChanged(
                args.GetOldValue<IReadOnlyList<GeometryPathDto>?>(),
                args.GetNewValue<IReadOnlyList<GeometryPathDto>?>()));
        PinchMarkersProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnPinchMarkersChanged(
                args.GetOldValue<IReadOnlyList<PinchMarkerDto>?>(),
                args.GetNewValue<IReadOnlyList<PinchMarkerDto>?>()));
        RoomLabelsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnRoomLabelsChanged(
                args.GetOldValue<IReadOnlyList<RoomLabelDto>?>(),
                args.GetNewValue<IReadOnlyList<RoomLabelDto>?>()));
        WallCandidatesProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnWallCandidatesChanged(
                args.GetOldValue<IReadOnlyList<WallCandidateDto>?>(),
                args.GetNewValue<IReadOnlyList<WallCandidateDto>?>()));
        OpeningCandidatesProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnOpeningCandidatesChanged(
                args.GetOldValue<IReadOnlyList<OpeningCandidateDto>?>(),
                args.GetNewValue<IReadOnlyList<OpeningCandidateDto>?>()));
        OpeningLabelsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnOpeningLabelsChanged(
                args.GetOldValue<IReadOnlyList<OpeningLabelDto>?>(),
                args.GetNewValue<IReadOnlyList<OpeningLabelDto>?>()));
        DimensionsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnDimensionsChanged(
                args.GetOldValue<IReadOnlyList<DimensionDto>?>(),
                args.GetNewValue<IReadOnlyList<DimensionDto>?>()));
        DimensionAssociationsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnDimensionAssociationsChanged(
                args.GetOldValue<IReadOnlyList<DimensionAssociationDto>?>(),
                args.GetNewValue<IReadOnlyList<DimensionAssociationDto>?>()));
        FixedPlanComponentsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnFixedPlanComponentsChanged(
                args.GetOldValue<IReadOnlyList<FixedPlanComponentDto>?>(),
                args.GetNewValue<IReadOnlyList<FixedPlanComponentDto>?>()));
        ProtectedDetailAssembliesProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnProtectedDetailAssembliesChanged(
                args.GetOldValue<IReadOnlyList<ProtectedDetailAssemblyDto>?>(),
                args.GetNewValue<IReadOnlyList<ProtectedDetailAssemblyDto>?>()));
        CuratedPlanArtifactsProperty.Changed.AddClassHandler<FloorPlanPreviewControl>((control, args) =>
            control.OnCuratedPlanArtifactsChanged(
                args.GetOldValue<IReadOnlyList<CuratedPlanArtifactDto>?>(),
                args.GetNewValue<IReadOnlyList<CuratedPlanArtifactDto>?>()));
    }

    public FloorPlanPreviewControl()
    {
        collectionObserverHub = new PreviewCollectionObserverHub(InvalidateVisual);
    }

    public static readonly StyledProperty<IReadOnlyList<GeometryPathDto>?> GeometryPathsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<GeometryPathDto>?>(nameof(GeometryPaths));

    public static readonly StyledProperty<Guid?> HighlightGeometryPathIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightGeometryPathId));

    public static readonly StyledProperty<IReadOnlyList<PinchMarkerDto>?> PinchMarkersProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<PinchMarkerDto>?>(nameof(PinchMarkers));

    public static readonly StyledProperty<IReadOnlyList<RoomLabelDto>?> RoomLabelsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<RoomLabelDto>?>(nameof(RoomLabels));

    public static readonly StyledProperty<Guid?> HighlightRoomLabelIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightRoomLabelId));

    public static readonly StyledProperty<IReadOnlyList<WallCandidateDto>?> WallCandidatesProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<WallCandidateDto>?>(nameof(WallCandidates));

    public static readonly StyledProperty<IReadOnlyList<OpeningCandidateDto>?> OpeningCandidatesProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<OpeningCandidateDto>?>(nameof(OpeningCandidates));

    public static readonly StyledProperty<IReadOnlyList<OpeningLabelDto>?> OpeningLabelsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<OpeningLabelDto>?>(nameof(OpeningLabels));

    public static readonly StyledProperty<Guid?> HighlightOpeningLabelIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightOpeningLabelId));

    public static readonly StyledProperty<IReadOnlyList<DimensionDto>?> DimensionsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<DimensionDto>?>(nameof(Dimensions));

    public static readonly StyledProperty<IReadOnlyList<DimensionAssociationDto>?> DimensionAssociationsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<DimensionAssociationDto>?>(nameof(DimensionAssociations));

    public static readonly StyledProperty<Guid?> HighlightDimensionIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(HighlightDimensionId));

    public static readonly StyledProperty<IReadOnlyList<FixedPlanComponentDto>?> FixedPlanComponentsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<FixedPlanComponentDto>?>(nameof(FixedPlanComponents));

    public static readonly StyledProperty<IReadOnlyList<ProtectedDetailAssemblyDto>?> ProtectedDetailAssembliesProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<ProtectedDetailAssemblyDto>?>(nameof(ProtectedDetailAssemblies));

    public static readonly StyledProperty<IReadOnlyList<CuratedPlanArtifactDto>?> CuratedPlanArtifactsProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, IReadOnlyList<CuratedPlanArtifactDto>?>(nameof(CuratedPlanArtifacts));

    public static readonly StyledProperty<Guid?> PreviewPinchGroupIdProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, Guid?>(nameof(PreviewPinchGroupId));

    public static readonly StyledProperty<string?> PreviewAxisTagProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, string?>(nameof(PreviewAxisTag));

    public static readonly StyledProperty<bool> IsPinchPlacementArmedProperty =
        AvaloniaProperty.Register<FloorPlanPreviewControl, bool>(nameof(IsPinchPlacementArmed));

    public IReadOnlyList<GeometryPathDto>? GeometryPaths
    {
        get => GetValue(GeometryPathsProperty);
        set => SetValue(GeometryPathsProperty, value);
    }

    public Guid? HighlightGeometryPathId
    {
        get => GetValue(HighlightGeometryPathIdProperty);
        set => SetValue(HighlightGeometryPathIdProperty, value);
    }

    public IReadOnlyList<PinchMarkerDto>? PinchMarkers
    {
        get => GetValue(PinchMarkersProperty);
        set => SetValue(PinchMarkersProperty, value);
    }

    public IReadOnlyList<RoomLabelDto>? RoomLabels
    {
        get => GetValue(RoomLabelsProperty);
        set => SetValue(RoomLabelsProperty, value);
    }

    public Guid? HighlightRoomLabelId
    {
        get => GetValue(HighlightRoomLabelIdProperty);
        set => SetValue(HighlightRoomLabelIdProperty, value);
    }

    public IReadOnlyList<WallCandidateDto>? WallCandidates
    {
        get => GetValue(WallCandidatesProperty);
        set => SetValue(WallCandidatesProperty, value);
    }

    public IReadOnlyList<OpeningCandidateDto>? OpeningCandidates
    {
        get => GetValue(OpeningCandidatesProperty);
        set => SetValue(OpeningCandidatesProperty, value);
    }

    public IReadOnlyList<OpeningLabelDto>? OpeningLabels
    {
        get => GetValue(OpeningLabelsProperty);
        set => SetValue(OpeningLabelsProperty, value);
    }

    public Guid? HighlightOpeningLabelId
    {
        get => GetValue(HighlightOpeningLabelIdProperty);
        set => SetValue(HighlightOpeningLabelIdProperty, value);
    }

    public IReadOnlyList<DimensionDto>? Dimensions
    {
        get => GetValue(DimensionsProperty);
        set => SetValue(DimensionsProperty, value);
    }

    public IReadOnlyList<DimensionAssociationDto>? DimensionAssociations
    {
        get => GetValue(DimensionAssociationsProperty);
        set => SetValue(DimensionAssociationsProperty, value);
    }

    public Guid? HighlightDimensionId
    {
        get => GetValue(HighlightDimensionIdProperty);
        set => SetValue(HighlightDimensionIdProperty, value);
    }

    public IReadOnlyList<FixedPlanComponentDto>? FixedPlanComponents
    {
        get => GetValue(FixedPlanComponentsProperty);
        set => SetValue(FixedPlanComponentsProperty, value);
    }

    public IReadOnlyList<ProtectedDetailAssemblyDto>? ProtectedDetailAssemblies
    {
        get => GetValue(ProtectedDetailAssembliesProperty);
        set => SetValue(ProtectedDetailAssembliesProperty, value);
    }

    public IReadOnlyList<CuratedPlanArtifactDto>? CuratedPlanArtifacts
    {
        get => GetValue(CuratedPlanArtifactsProperty);
        set => SetValue(CuratedPlanArtifactsProperty, value);
    }

    public Guid? PreviewPinchGroupId
    {
        get => GetValue(PreviewPinchGroupIdProperty);
        set => SetValue(PreviewPinchGroupIdProperty, value);
    }

    public string? PreviewAxisTag
    {
        get => GetValue(PreviewAxisTagProperty);
        set => SetValue(PreviewAxisTagProperty, value);
    }

    public bool IsPinchPlacementArmed
    {
        get => GetValue(IsPinchPlacementArmedProperty);
        set => SetValue(IsPinchPlacementArmedProperty, value);
    }

    public event EventHandler<GeometryPathClickedEventArgs>? GeometryPathClicked;
    public event EventHandler<RoomLabelClickedEventArgs>? RoomLabelClicked;
    public event EventHandler<OpeningLabelClickedEventArgs>? OpeningLabelClicked;
    public event EventHandler<MovableArtifactMovedEventArgs>? MovableArtifactMoved;
    public event EventHandler<DimensionClickedEventArgs>? DimensionClicked;
    public event EventHandler<DimensionEditedEventArgs>? DimensionEdited;

    internal static Rect GetLocalRenderBounds(Rect layoutBounds)
    {
        return new Rect(0d, 0d, Math.Max(0d, layoutBounds.Width), Math.Max(0d, layoutBounds.Height));
    }

    internal static (decimal X, decimal Y) ApplyAbsolutePointDelta(decimal baseX, decimal baseY, decimal deltaX, decimal deltaY)
    {
        return (RoundModelValue(baseX + deltaX), RoundModelValue(baseY + deltaY));
    }

    internal static (decimal Dx, decimal Dy) ApplyTranslationDelta(decimal baseDx, decimal baseDy, decimal deltaX, decimal deltaY)
    {
        return (RoundModelValue(baseDx + deltaX), RoundModelValue(baseDy + deltaY));
    }

    internal static DimensionDto ApplyDimensionHandleDelta(
        DimensionDto dimension,
        DimensionHandleKind handleKind,
        decimal deltaX,
        decimal deltaY)
    {
        return NativeDimensionEditor.ApplyHandleDelta(dimension, handleKind, deltaX, deltaY);
    }

    internal static DimensionHit? TryResolveDimensionHit(
        IReadOnlyList<DimensionDto>? dimensions,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        double hitTolerancePixels)
    {
        return NativeDimensionHitTester.TryResolveDimensionHit(
            dimensions,
            viewport,
            pointerPosition,
            hitTolerancePixels,
            out var dimension,
            out var suggestedHandle)
            ? new DimensionHit(dimension, suggestedHandle)
            : null;
    }

    internal static Point ResolveSnappedWorldPoint(
        Point worldPoint,
        CadViewportContext viewportContext,
        IReadOnlyList<Point> anchorPoints,
        bool enableGridSnap)
    {
        return NativeDimensionEditor.ResolveSnappedWorldPoint(worldPoint, viewportContext, anchorPoints, enableGridSnap);
    }

    internal static IReadOnlyList<GeometryPathDto> BuildHitTestGeometry(
        IReadOnlyList<GeometryPathDto>? geometryPaths,
        IReadOnlyList<OpeningCandidateDto>? openings = null,
        IReadOnlyList<FixedPlanComponentDto>? fixedPlanComponents = null,
        IReadOnlyList<ProtectedDetailAssemblyDto>? protectedDetailAssemblies = null,
        IReadOnlyList<CuratedPlanArtifactDto>? curatedArtifacts = null)
    {
        if (geometryPaths is not { Count: > 0 })
        {
            return [];
        }

        if (curatedArtifacts is { Count: > 0 })
        {
            return PreviewArtifactGeometryIndex
                .Create(curatedArtifacts)
                .OrderForHitTesting(geometryPaths);
        }

        return PreviewArtifactGeometryIndex
            .Create(openings, fixedPlanComponents, protectedDetailAssemblies)
            .OrderForHitTesting(geometryPaths);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        collectionObserverHub.AttachAll(CaptureObservedCollections());
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        collectionObserverHub.DetachAll();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pointerProperties = e.GetCurrentPoint(this).Properties;
        if (pointerProperties.IsMiddleButtonPressed)
        {
            var panStart = PreviewInteractionCoordinator.HandleMiddleButtonPressed(
                PreviewInteractionCoordinator.CreatePointerPressedRequestForPan(
                    e.GetPosition(this),
                    previewZoomState));
            isPanningPreview = panStart.IsPanningPreview;
            panStartPoint = panStart.PanStartPoint;
            panStartZoomState = panStart.PanStartZoomState;
            activeDragEdge = null;
            activePreviewTrimMm = 0m;
            if (panStart.CapturePointer)
            {
                e.Pointer.Capture(this);
            }

            e.Handled = panStart.Handled;
            return;
        }

        if (!pointerProperties.IsLeftButtonPressed)
        {
            return;
        }

        var pointerPosition = e.GetPosition(this);
        var axisTag = ParseAxisTag();
        var viewport = GetPreviewViewport(axisTag);
        if (viewport is null)
        {
            return;
        }
        var pressOutcome = PreviewInteractionCoordinator.HandleLeftButtonPressed(
            new PreviewInteractionCoordinator.LeftButtonPressRequest(
                pointerPosition,
                axisTag,
                IsPinchPlacementArmed,
                ResolveEdgeDrag,
                point => TryResolveDimensionHandleHit(Dimensions, HighlightDimensionId, viewport.Value, point, out var handleHit)
                    ? handleHit
                    : null,
                point => !IsPinchPlacementArmed
                    ? TryResolveDimensionHit(Dimensions, viewport.Value, point, HitTestTolerance)
                    : null,
                point => TryResolveRoomLabelHit(viewport.Value, point, out var roomLabel)
                    ? roomLabel
                    : null,
                point => TryResolveOpeningLabelHit(viewport.Value, point, out var openingLabel)
                    ? openingLabel
                    : null,
                point =>
                {
                    var hitTestGeometry = BuildHitTestGeometry(
                        GeometryPaths,
                        OpeningCandidates,
                        FixedPlanComponents,
                        ProtectedDetailAssemblies,
                        CuratedPlanArtifacts);
                    var hit = FloorPlanPreviewGeometry.HitTestPathDetail(hitTestGeometry, viewport.Value, point, HitTestTolerance);
                    return hit is null
                        ? null
                        : new PreviewInteractionCoordinator.GeometryHit(hit.Value.GeometryPathId, hit.Value.PositionRatio);
                },
                geometryPathId => TryResolveMovableArtifact(geometryPathId, out var movableArtifact)
                    ? movableArtifact
                    : null));

        if (!pressOutcome.Handled)
        {
            return;
        }

        if (pressOutcome.DimensionClickedId is { } dimensionId)
        {
            DimensionClicked?.Invoke(this, new DimensionClickedEventArgs(dimensionId));
        }

        if (pressOutcome.RoomLabelClickedId is { } roomLabelId)
        {
            RoomLabelClicked?.Invoke(this, new RoomLabelClickedEventArgs(roomLabelId));
        }

        if (pressOutcome.OpeningLabelClickedId is { } openingLabelId)
        {
            OpeningLabelClicked?.Invoke(this, new OpeningLabelClickedEventArgs(openingLabelId));
        }

        if (pressOutcome.GeometryClick is { } geometryClick)
        {
            GeometryPathClicked?.Invoke(this, new GeometryPathClickedEventArgs(geometryClick.GeometryPathId, geometryClick.PositionRatio));
        }

        if (pressOutcome.StartedEdgeDrag is { } edge)
        {
            activeDragEdge = edge;
            dragStartPoint = pointerPosition;
            activePreviewTrimMm = 0m;
        }

        if (pressOutcome.StartedDimensionEdit is { } dimensionEdit)
        {
            activeDimensionEdit = dimensionEdit;
        }

        if (pressOutcome.StartedArtifactMove is { } artifactMove)
        {
            activeArtifactMove = artifactMove;
        }

        if (pressOutcome.CapturePointer)
        {
            e.Pointer.Capture(this);
        }

        if (pressOutcome.InvalidateVisual)
        {
            InvalidateVisual();
        }

        e.Handled = pressOutcome.Handled;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var axisTag = ParseAxisTag();
        var viewport = GetPreviewViewport(axisTag);
        var pointerPosition = e.GetPosition(this);
        Func<PreviewDimensionEditState, Point, Point>? resolveSnappedDimensionWorldPoint = null;
        if (viewport is { } activeViewport)
        {
            resolveSnappedDimensionWorldPoint = (edit, currentPointerPosition)
                => ResolveSnappedDimensionEditWorldPoint(edit, activeViewport, currentPointerPosition);
        }

        var moveOutcome = PreviewInteractionCoordinator.HandlePointerMoved(
            new PreviewInteractionCoordinator.PointerMovedRequest(
                CurrentState: CaptureInteractionState(),
                PointerPosition: pointerPosition,
                Viewport: viewport,
                ResolveSnappedDimensionWorldPoint: resolveSnappedDimensionWorldPoint));

        ApplyInteractionState(moveOutcome.NextState);
        if (moveOutcome.InvalidateVisual)
        {
            InvalidateVisual();
        }

        if (moveOutcome.Handled)
        {
            e.Handled = true;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (GeometryPaths is not { Count: > 0 })
        {
            return;
        }

        var axisTag = ParseAxisTag();
        var baseViewport = FloorPlanPreviewGeometry.CalculateViewport(GeometryPaths, GetGeometryViewportBounds(axisTag), PreviewPadding);
        if (baseViewport is null)
        {
            return;
        }

        previewZoomState = ResolveZoomStateForWheel(
            baseViewport.Value,
            previewZoomState,
            e.GetPosition(this),
            e.Delta.Y);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var releaseOutcome = PreviewInteractionCoordinator.HandlePointerReleased(
            new PreviewInteractionCoordinator.PointerReleasedRequest(
                CurrentState: CaptureInteractionState(),
                PointerPosition: e.GetPosition(this),
                Viewport: GetPreviewViewport(ParseAxisTag()),
                BuildDimensionEditedEventArgs: BuildDimensionEditedEventArgs));

        ApplyInteractionState(releaseOutcome.NextState);
        if (releaseOutcome.ReleasePointerCapture)
        {
            e.Pointer.Capture(null);
        }

        if (releaseOutcome.Handled)
        {
            e.Handled = true;
        }

        if (releaseOutcome.InvalidateVisual)
        {
            InvalidateVisual();
        }

        if (releaseOutcome.CommittedArtifactMove is { } movement)
        {
            MovableArtifactMoved?.Invoke(this, movement);
        }

        if (releaseOutcome.CommittedDimensionEdit is { } editedDimension)
        {
            DimensionEdited?.Invoke(this, editedDimension);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = GetLocalRenderBounds(Bounds);
        var axisTag = ParseAxisTag();
        var viewport = GetPreviewViewport(axisTag);
        PreviewWorkspaceRenderer.Render(context, bounds, viewport);
        context.DrawRectangle(new Pen(Brushes.Gainsboro, 1), bounds.Deflate(0.5));

        if (axisTag is not null)
        {
            CompressionHandlePreviewLayerRenderer.Render(context, bounds, axisTag.Value, IsPinchPlacementArmed);
        }

        if (GeometryPaths is not { Count: > 0 })
        {
            return;
        }

        if (viewport is null)
        {
            return;
        }

        var previewGeometry = BuildPreviewGeometry(axisTag);
        previewGeometry = ApplyActiveArtifactMoveToGeometry(previewGeometry);
        var roomLabels = BuildRenderedRoomLabels();
        var openingLabels = BuildRenderedOpeningLabels();
        var dimensions = BuildRenderedDimensions();
        var artifactIndex = CuratedPlanArtifacts is { Count: > 0 }
            ? PreviewArtifactGeometryIndex.Create(CuratedPlanArtifacts)
            : PreviewArtifactGeometryIndex.Create(OpeningCandidates, FixedPlanComponents, ProtectedDetailAssemblies);
        var orderedPaths = previewGeometry
            .Where(path => !artifactIndex.OpeningGeometryPathIds.Contains(path.Id))
            .Where(path => !artifactIndex.FixedPlanComponentGeometryPathIds.Contains(path.Id))
            .Where(path => !artifactIndex.ProtectedDetailGeometryPathIds.Contains(path.Id))
            .OrderBy(path => FloorPlanPreviewGeometry.GetPathStyle(path.Id, HighlightGeometryPathId).IsHighlighted)
            .ToArray();

        foreach (var path in orderedPaths)
        {
            var style = FloorPlanPreviewGeometry.GetPathStyle(path.Id, HighlightGeometryPathId);
            var pen = new Pen(new SolidColorBrush(style.Color), style.Thickness);

            foreach (var segment in path.Segments)
            {
                context.DrawLine(
                    pen,
                    viewport.Value.Project(segment.StartX, segment.StartY),
                    viewport.Value.Project(segment.EndX, segment.EndY));
            }
        }

        if (CuratedPlanArtifacts is { Count: > 0 })
        {
            CuratedArtifactPreviewLayerRenderer.Render(
                context,
                viewport.Value,
                previewGeometry,
                CuratedPlanArtifacts,
                HighlightGeometryPathId);
        }
        else
        {
            OpeningPreviewLayerRenderer.Render(
                context,
                viewport.Value,
                previewGeometry,
                OpeningCandidates,
                artifactIndex.OpeningGeometryPathIds,
                HighlightGeometryPathId);
            FixedPlanComponentPreviewLayerRenderer.Render(
                context,
                viewport.Value,
                previewGeometry,
                FixedPlanComponents,
                artifactIndex.FixedPlanComponentGeometryPathIds,
                HighlightGeometryPathId);
            ProtectedDetailPreviewLayerRenderer.Render(
                context,
                viewport.Value,
                previewGeometry,
                ProtectedDetailAssemblies,
                artifactIndex.ProtectedDetailGeometryPathIds,
                HighlightGeometryPathId);
        }
        DimensionPreviewLayerRenderer.Render(context, viewport.Value, dimensions, HighlightDimensionId);
        CadTextPreviewLayerRenderer.RenderRoomLabels(context, viewport.Value, roomLabels, HighlightRoomLabelId);
        CadTextPreviewLayerRenderer.RenderOpeningLabels(context, viewport.Value, openingLabels, HighlightOpeningLabelId);
        CadTextPreviewLayerRenderer.RenderDimensions(context, viewport.Value, dimensions, HighlightDimensionId);
        PinchMarkerPreviewLayerRenderer.Render(
            context,
            viewport.Value,
            previewGeometry,
            PinchMarkers,
            PreviewPinchGroupId,
            PreviewAxisTag);
        DimensionPreviewLayerRenderer.RenderHandles(
            context,
            viewport.Value,
            dimensions,
            HighlightDimensionId,
            activeDimensionEdit?.HandleKind);
    }

    internal static double CalculateWheelZoomFactor(double currentZoomFactor, double wheelDeltaY)
        => PreviewInteractionCoordinator.CalculateWheelZoomFactor(currentZoomFactor, wheelDeltaY);

    internal static PreviewZoomState ResolveZoomStateForWheel(
        FloorPlanPreviewGeometry.PreviewViewport baseViewport,
        PreviewZoomState currentState,
        Point pointerPosition,
        double wheelDeltaY)
        => PreviewInteractionCoordinator.ResolveZoomStateForWheel(baseViewport, currentState, pointerPosition, wheelDeltaY);

    internal static PreviewZoomState ResolvePanStateForDrag(
        PreviewZoomState startState,
        Point startPointerPosition,
        Point currentPointerPosition)
        => PreviewInteractionCoordinator.ResolvePanStateForDrag(startState, startPointerPosition, currentPointerPosition);

    private IReadOnlyList<GeometryPathDto> BuildPreviewGeometry(PinchAxisTag? axisTag)
    {
        if (axisTag is null || activeDragEdge is null)
        {
            return GeometryPaths ?? [];
        }

        return FloorPlanPreviewGeometry.CreatePreviewGeometry(
            GeometryPaths,
            axisTag.Value,
            PinchMarkerPreviewLayerRenderer.FilterForPreviewGroup(PinchMarkers, PreviewPinchGroupId),
            activePreviewTrimMm,
            activeDragEdge.Value);
    }

    private FloorPlanPreviewGeometry.PreviewCompressionEdge? ResolveEdgeDrag(Point pointerPosition, PinchAxisTag axisTag)
    {
        return FloorPlanPreviewGeometry.TryResolveCompressionHandle(pointerPosition, GetLocalRenderBounds(Bounds), axisTag);
    }

    private IReadOnlyList<GeometryPathDto> ApplyActiveArtifactMoveToGeometry(IReadOnlyList<GeometryPathDto> geometryPaths)
    {
        if (activeArtifactMove is not { PositionMode: FloorPlanArtifactPositionMode.Translation } move ||
            geometryPaths.Count == 0)
        {
            return geometryPaths;
        }

        var pathIds = ResolveGeometryPathIds(move.SourceArtifactKind, move.SourceArtifactId);
        if (pathIds.Count == 0)
        {
            return geometryPaths;
        }

        return geometryPaths
            .Select(path =>
            {
                if (!pathIds.Contains(path.Id))
                {
                    return path;
                }

                return new GeometryPathDto(
                    path.Id,
                    path.IsClosed,
                    path.Segments
                        .Select(segment => new GeometrySegmentDto(
                            segment.GeometryPathId,
                            segment.SortOrder,
                            segment.StartX + move.CurrentDeltaX,
                            segment.StartY + move.CurrentDeltaY,
                            segment.EndX + move.CurrentDeltaX,
                            segment.EndY + move.CurrentDeltaY))
                        .ToArray());
            })
            .ToArray();
    }

    private IReadOnlyList<RoomLabelDto> BuildRenderedRoomLabels()
    {
        if (RoomLabels is not { Count: > 0 } ||
            activeArtifactMove is not { PositionMode: FloorPlanArtifactPositionMode.AbsolutePoint } move ||
            !string.Equals(move.SourceArtifactKind, FloorPlanArtifactPositionSourceKinds.RoomLabel, StringComparison.Ordinal))
        {
            return RoomLabels ?? [];
        }

        var movedPoint = ApplyAbsolutePointDelta(move.BaseX, move.BaseY, move.CurrentDeltaX, move.CurrentDeltaY);
        return RoomLabels
            .Select(label => label.RoomLabelId == move.SourceArtifactId
                ? label with { X = movedPoint.X, Y = movedPoint.Y }
                : label)
            .ToArray();
    }

    private IReadOnlyList<OpeningLabelDto> BuildRenderedOpeningLabels()
    {
        if (OpeningLabels is not { Count: > 0 } ||
            activeArtifactMove is not { PositionMode: FloorPlanArtifactPositionMode.AbsolutePoint } move ||
            !string.Equals(move.SourceArtifactKind, FloorPlanArtifactPositionSourceKinds.OpeningLabel, StringComparison.Ordinal))
        {
            return OpeningLabels ?? [];
        }

        var movedPoint = ApplyAbsolutePointDelta(move.BaseX, move.BaseY, move.CurrentDeltaX, move.CurrentDeltaY);
        return OpeningLabels
            .Select(label => label.OpeningLabelId == move.SourceArtifactId
                ? label with { X = movedPoint.X, Y = movedPoint.Y }
                : label)
            .ToArray();
    }

    private IReadOnlyList<DimensionDto> BuildRenderedDimensions()
    {
        return DimensionPreviewProjector.BuildRenderedDimensions(
            Dimensions,
            activeDimensionEdit is { } edit
                ? new DimensionPreviewProjector.DimensionPreviewEditRequest(
                    edit.BaseDimension.DimensionId,
                    edit.BaseDimension,
                    edit.HandleKind,
                    edit.CurrentWorldPoint)
                : null);
    }

    private DimensionDto? BuildEditedDimensionPreview(PreviewDimensionEditState edit, DimensionDto baseDimension)
    {
        var rendered = DimensionPreviewProjector.BuildRenderedDimensions(
            [baseDimension],
            new DimensionPreviewProjector.DimensionPreviewEditRequest(
                edit.BaseDimension.DimensionId,
                edit.BaseDimension,
                edit.HandleKind,
                edit.CurrentWorldPoint));

        return rendered.FirstOrDefault();
    }

    private PreviewInteractionCoordinator.InteractionState CaptureInteractionState()
        => new(
            previewZoomState,
            isPanningPreview,
            panStartPoint,
            panStartZoomState,
            activeDragEdge,
            dragStartPoint,
            activePreviewTrimMm,
            activeArtifactMove,
            activeDimensionEdit);

    private void ApplyInteractionState(PreviewInteractionCoordinator.InteractionState state)
    {
        previewZoomState = state.PreviewZoomState;
        isPanningPreview = state.IsPanningPreview;
        panStartPoint = state.PanStartPoint;
        panStartZoomState = state.PanStartZoomState;
        activeDragEdge = state.ActiveDragEdge;
        dragStartPoint = state.DragStartPoint;
        activePreviewTrimMm = state.ActivePreviewTrimMm;
        activeArtifactMove = state.ActiveArtifactMove;
        activeDimensionEdit = state.ActiveDimensionEdit;
    }

    private Point ResolveSnappedDimensionEditWorldPoint(
        PreviewDimensionEditState edit,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition)
    {
        var currentWorld = viewport.Unproject(pointerPosition);
        var viewportContext = CadViewportContext.Create(viewport);
        return ResolveSnappedWorldPoint(
            currentWorld,
            viewportContext,
            ResolveDimensionSnapAnchors(edit.BaseDimension, edit.HandleKind),
            enableGridSnap: true);
    }

    private DimensionEditedEventArgs? BuildDimensionEditedEventArgs(PreviewDimensionEditState edit)
    {
        var editedDimension = BuildEditedDimensionPreview(
            edit,
            Dimensions?.FirstOrDefault(item => item.DimensionId == edit.BaseDimension.DimensionId) ?? edit.BaseDimension);
        return editedDimension is not null && !editedDimension.Equals(edit.BaseDimension)
            ? new DimensionEditedEventArgs(
                edit.BaseDimension.DimensionId,
                ResolveSourceDimensionKey(edit.BaseDimension),
                editedDimension)
            : null;
    }

    private Rect GetGeometryViewportBounds(PinchAxisTag? axisTag)
    {
        var bounds = GetLocalRenderBounds(Bounds);

        return axisTag is null
            ? bounds
            : FloorPlanPreviewGeometry.GetGeometryViewportBounds(bounds, axisTag.Value);
    }

    private bool TryResolveRoomLabelHit(
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        out RoomLabelDto roomLabel)
    {
        if (RoomLabels is not { Count: > 0 })
        {
            roomLabel = null!;
            return false;
        }

        foreach (var item in RoomLabels.Reverse())
        {
            if (string.IsNullOrWhiteSpace(item.Text))
            {
                continue;
            }

            if (!CadTextPreviewLayerRenderer.GetRoomLabelBounds(item, viewport).Contains(pointerPosition))
            {
                continue;
            }

            roomLabel = item;
            return true;
        }

        roomLabel = null!;
        return false;
    }

    private bool TryResolveOpeningLabelHit(
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        out OpeningLabelDto openingLabel)
    {
        if (OpeningLabels is not { Count: > 0 })
        {
            openingLabel = null!;
            return false;
        }

        foreach (var item in OpeningLabels.Reverse())
        {
            if (string.IsNullOrWhiteSpace(item.Text))
            {
                continue;
            }

            if (!CadTextPreviewLayerRenderer.GetOpeningLabelBounds(item, viewport).Contains(pointerPosition))
            {
                continue;
            }

            openingLabel = item;
            return true;
        }

        openingLabel = null!;
        return false;
    }

    private static bool TryResolveDimensionHandleHit(
        IReadOnlyList<DimensionDto>? dimensions,
        Guid? highlightedDimensionId,
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point pointerPosition,
        out DimensionHandleHit handleHit)
    {
        if (NativeDimensionHitTester.TryResolveHandleHit(
                dimensions,
                highlightedDimensionId,
                viewport,
                pointerPosition,
                DimensionHandleHitTolerance,
                out var dimension,
                out var handleKind))
        {
            handleHit = new DimensionHandleHit(dimension, handleKind);
            return true;
        }

        handleHit = default;
        return false;
    }

    private static IReadOnlyList<Point> ResolveDimensionSnapAnchors(DimensionDto dimension, DimensionHandleKind activeHandleKind)
    {
        return NativeDimensionEditor.ResolveSnapAnchors(dimension, activeHandleKind);
    }

    private FloorPlanPreviewGeometry.PreviewViewport? GetPreviewViewport(PinchAxisTag? axisTag)
    {
        var baseViewport = FloorPlanPreviewGeometry.CalculateViewport(GeometryPaths, GetGeometryViewportBounds(axisTag), PreviewPadding);
        return baseViewport?.WithUserTransform(previewZoomState.ZoomFactor, previewZoomState.PanOffset);
    }

    private PinchAxisTag? ParseAxisTag()
    {
        return Enum.TryParse<PinchAxisTag>(PreviewAxisTag, out var axisTag)
            ? axisTag
            : null;
    }

    private bool TryResolveMovableArtifact(Guid geometryPathId, out PreviewMovableArtifactDescriptor artifact)
    {
        if (CuratedPlanArtifacts is { Count: > 0 })
        {
            var curated = CuratedPlanArtifacts.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
            if (curated is not null)
            {
                artifact = new PreviewMovableArtifactDescriptor(
                    curated.SourceArtifactKind,
                    curated.SourceArtifactId,
                    FloorPlanArtifactPositionMode.Translation,
                    0m,
                    0m,
                    curated.TranslationDx,
                    curated.TranslationDy,
                    curated.GeometryPathIds);
                return true;
            }
        }

        if (OpeningCandidates is { Count: > 0 })
        {
            var opening = OpeningCandidates.FirstOrDefault(item => item.GeometryPathId == geometryPathId);
            if (opening is not null)
            {
                artifact = new PreviewMovableArtifactDescriptor(
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    opening.OpeningCandidateId,
                    FloorPlanArtifactPositionMode.Translation,
                    0m,
                    0m,
                    0m,
                    0m,
                    opening.GeometryPathId is Guid pathId ? [pathId] : []);
                return true;
            }
        }

        if (FixedPlanComponents is { Count: > 0 })
        {
            var fixedPlanComponent = FixedPlanComponents.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
            if (fixedPlanComponent is not null)
            {
                artifact = new PreviewMovableArtifactDescriptor(
                    FloorPlanArtifactSourceKinds.FixedPlanComponent,
                    fixedPlanComponent.FixedPlanComponentId,
                    FloorPlanArtifactPositionMode.Translation,
                    0m,
                    0m,
                    0m,
                    0m,
                    fixedPlanComponent.GeometryPathIds);
                return true;
            }
        }

        if (ProtectedDetailAssemblies is { Count: > 0 })
        {
            var protectedAssembly = ProtectedDetailAssemblies.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
            if (protectedAssembly is not null)
            {
                artifact = new PreviewMovableArtifactDescriptor(
                    FloorPlanArtifactSourceKinds.ProtectedDetailAssembly,
                    protectedAssembly.ProtectedDetailAssemblyId,
                    FloorPlanArtifactPositionMode.Translation,
                    0m,
                    0m,
                    0m,
                    0m,
                    protectedAssembly.GeometryPathIds);
                return true;
            }
        }

        artifact = default;
        return false;
    }

    private IReadOnlySet<Guid> ResolveGeometryPathIds(string sourceArtifactKind, Guid sourceArtifactId)
    {
        if (CuratedPlanArtifacts is { Count: > 0 })
        {
            var curated = CuratedPlanArtifacts.FirstOrDefault(item =>
                item.SourceArtifactId == sourceArtifactId &&
                string.Equals(item.SourceArtifactKind, sourceArtifactKind, StringComparison.Ordinal));
            if (curated is not null)
            {
                return curated.GeometryPathIds.ToHashSet();
            }
        }

        if (string.Equals(sourceArtifactKind, FloorPlanArtifactSourceKinds.OpeningCandidate, StringComparison.Ordinal))
        {
            return OpeningCandidates?
                       .FirstOrDefault(item => item.OpeningCandidateId == sourceArtifactId)?
                       .GeometryPathId is Guid geometryPathId
                ? new HashSet<Guid> { geometryPathId }
                : new HashSet<Guid>();
        }

        if (string.Equals(sourceArtifactKind, FloorPlanArtifactSourceKinds.FixedPlanComponent, StringComparison.Ordinal))
        {
            return FixedPlanComponents?
                       .FirstOrDefault(item => item.FixedPlanComponentId == sourceArtifactId)?
                       .GeometryPathIds
                       .ToHashSet()
                   ?? new HashSet<Guid>();
        }

        if (string.Equals(sourceArtifactKind, FloorPlanArtifactSourceKinds.ProtectedDetailAssembly, StringComparison.Ordinal))
        {
            return ProtectedDetailAssemblies?
                       .FirstOrDefault(item => item.ProtectedDetailAssemblyId == sourceArtifactId)?
                       .GeometryPathIds
                       .ToHashSet()
                   ?? new HashSet<Guid>();
        }

        return new HashSet<Guid>();
    }

    private static string ResolveSourceDimensionKey(DimensionDto dimension)
    {
        return !string.IsNullOrWhiteSpace(dimension.SourceHandle)
            ? dimension.SourceHandle
            : dimension.SourceEntityRef;
    }

    private static decimal RoundModelValue(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    private void OnGeometryPathsChanged(IReadOnlyList<GeometryPathDto>? oldValue, IReadOnlyList<GeometryPathDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.GeometryPaths, oldValue, newValue);

    private void OnPinchMarkersChanged(IReadOnlyList<PinchMarkerDto>? oldValue, IReadOnlyList<PinchMarkerDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.PinchMarkers, oldValue, newValue);

    private void OnRoomLabelsChanged(IReadOnlyList<RoomLabelDto>? oldValue, IReadOnlyList<RoomLabelDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.RoomLabels, oldValue, newValue);

    private void OnOpeningCandidatesChanged(IReadOnlyList<OpeningCandidateDto>? oldValue, IReadOnlyList<OpeningCandidateDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.OpeningCandidates, oldValue, newValue);

    private void OnOpeningLabelsChanged(IReadOnlyList<OpeningLabelDto>? oldValue, IReadOnlyList<OpeningLabelDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.OpeningLabels, oldValue, newValue);

    private void OnDimensionsChanged(IReadOnlyList<DimensionDto>? oldValue, IReadOnlyList<DimensionDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.Dimensions, oldValue, newValue);

    private void OnWallCandidatesChanged(IReadOnlyList<WallCandidateDto>? oldValue, IReadOnlyList<WallCandidateDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.WallCandidates, oldValue, newValue);

    private void OnDimensionAssociationsChanged(
        IReadOnlyList<DimensionAssociationDto>? oldValue,
        IReadOnlyList<DimensionAssociationDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.DimensionAssociations, oldValue, newValue);

    private void OnFixedPlanComponentsChanged(IReadOnlyList<FixedPlanComponentDto>? oldValue, IReadOnlyList<FixedPlanComponentDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.FixedPlanComponents, oldValue, newValue);

    private void OnProtectedDetailAssembliesChanged(
        IReadOnlyList<ProtectedDetailAssemblyDto>? oldValue,
        IReadOnlyList<ProtectedDetailAssemblyDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.ProtectedDetailAssemblies, oldValue, newValue);

    private void OnCuratedPlanArtifactsChanged(
        IReadOnlyList<CuratedPlanArtifactDto>? oldValue,
        IReadOnlyList<CuratedPlanArtifactDto>? newValue)
        => ReplaceObservedCollection(PreviewCollectionObserverHub.PreviewObservedCollectionSlot.CuratedPlanArtifacts, oldValue, newValue);

    private PreviewCollectionObserverHub.PreviewObservedCollections CaptureObservedCollections()
        => new(
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
            CuratedPlanArtifacts);

    private void ReplaceObservedCollection<T>(
        PreviewCollectionObserverHub.PreviewObservedCollectionSlot slot,
        IReadOnlyList<T>? oldValue,
        IReadOnlyList<T>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            collectionObserverHub.Replace(slot, newValue);
        }

        InvalidateVisual();
    }

    public sealed class GeometryPathClickedEventArgs : EventArgs
    {
        public GeometryPathClickedEventArgs(Guid geometryPathId, decimal positionRatio)
        {
            GeometryPathId = geometryPathId;
            PositionRatio = positionRatio;
        }

        public Guid GeometryPathId { get; }

        public decimal PositionRatio { get; }
    }

    public sealed class RoomLabelClickedEventArgs : EventArgs
    {
        public RoomLabelClickedEventArgs(Guid roomLabelId)
        {
            RoomLabelId = roomLabelId;
        }

        public Guid RoomLabelId { get; }
    }

    public sealed class OpeningLabelClickedEventArgs : EventArgs
    {
        public OpeningLabelClickedEventArgs(Guid openingLabelId)
        {
            OpeningLabelId = openingLabelId;
        }

        public Guid OpeningLabelId { get; }
    }

    public sealed class DimensionClickedEventArgs : EventArgs
    {
        public DimensionClickedEventArgs(Guid dimensionId)
        {
            DimensionId = dimensionId;
        }

        public Guid DimensionId { get; }
    }

    public sealed class DimensionEditedEventArgs : EventArgs
    {
        public DimensionEditedEventArgs(Guid dimensionId, string sourceDimensionKey, DimensionDto dimension)
        {
            DimensionId = dimensionId;
            SourceDimensionKey = sourceDimensionKey;
            Dimension = dimension;
        }

        public Guid DimensionId { get; }

        public string SourceDimensionKey { get; }

        public DimensionDto Dimension { get; }
    }

    public sealed class MovableArtifactMovedEventArgs : EventArgs
    {
        public MovableArtifactMovedEventArgs(
            string sourceArtifactKind,
            Guid sourceArtifactId,
            FloorPlanArtifactPositionMode positionMode,
            decimal? resolvedX,
            decimal? resolvedY,
            decimal? translationDx,
            decimal? translationDy)
        {
            SourceArtifactKind = sourceArtifactKind;
            SourceArtifactId = sourceArtifactId;
            PositionMode = positionMode;
            ResolvedX = resolvedX;
            ResolvedY = resolvedY;
            TranslationDx = translationDx;
            TranslationDy = translationDy;
        }

        public string SourceArtifactKind { get; }

        public Guid SourceArtifactId { get; }

        public FloorPlanArtifactPositionMode PositionMode { get; }

        public decimal? ResolvedX { get; }

        public decimal? ResolvedY { get; }

        public decimal? TranslationDx { get; }

        public decimal? TranslationDy { get; }
    }

    internal readonly record struct PreviewZoomState(double ZoomFactor, Vector PanOffset)
    {
        public static PreviewZoomState Default { get; } = new(1d, default);
    }

    internal enum DimensionHandleKind
    {
        FirstDefinitionPoint = 1,
        SecondDefinitionPoint = 2,
        DimensionLinePoint = 3,
        TextAnchor = 4
    }

    internal readonly record struct DimensionHit(DimensionDto Dimension, DimensionHandleKind SuggestedHandle);

    internal readonly record struct DimensionHandleHit(DimensionDto Dimension, DimensionHandleKind HandleKind);

    internal readonly record struct PreviewArtifactMoveState(
        string SourceArtifactKind,
        Guid SourceArtifactId,
        FloorPlanArtifactPositionMode PositionMode,
        Point PointerStart,
        decimal BaseX,
        decimal BaseY,
        decimal BaseDx,
        decimal BaseDy,
        decimal CurrentDeltaX,
        decimal CurrentDeltaY)
    {
        public static PreviewArtifactMoveState ForAbsolutePoint(
            string sourceArtifactKind,
            Guid sourceArtifactId,
            Point pointerStart,
            decimal baseX,
            decimal baseY)
            => new(
                sourceArtifactKind,
                sourceArtifactId,
                FloorPlanArtifactPositionMode.AbsolutePoint,
                pointerStart,
                baseX,
                baseY,
                0m,
                0m,
                0m,
                0m);

        public static PreviewArtifactMoveState ForTranslation(
            string sourceArtifactKind,
            Guid sourceArtifactId,
            Point pointerStart,
            decimal baseDx,
            decimal baseDy)
            => new(
                sourceArtifactKind,
                sourceArtifactId,
                FloorPlanArtifactPositionMode.Translation,
                pointerStart,
                0m,
                0m,
                baseDx,
                baseDy,
                0m,
                0m);
    }

    internal readonly record struct PreviewMovableArtifactDescriptor(
        string SourceArtifactKind,
        Guid SourceArtifactId,
        FloorPlanArtifactPositionMode PositionMode,
        decimal BaseX,
        decimal BaseY,
        decimal BaseDx,
        decimal BaseDy,
        IReadOnlyList<Guid> GeometryPathIds);

    internal readonly record struct PreviewDimensionEditState(
        DimensionDto BaseDimension,
        DimensionHandleKind HandleKind,
        Point PointerStart,
        Point? CurrentWorldPoint)
    {
        public static PreviewDimensionEditState Start(DimensionDto baseDimension, DimensionHandleKind handleKind, Point pointerStart)
            => new(baseDimension, handleKind, pointerStart, null);
    }
}
