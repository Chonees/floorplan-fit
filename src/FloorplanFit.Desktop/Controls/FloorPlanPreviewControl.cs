using System.Collections.Specialized;
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
    private INotifyCollectionChanged? observedGeometryPaths;
    private INotifyCollectionChanged? observedPinchMarkers;
    private INotifyCollectionChanged? observedRoomLabels;
    private INotifyCollectionChanged? observedWallCandidates;
    private INotifyCollectionChanged? observedOpeningCandidates;
    private INotifyCollectionChanged? observedOpeningLabels;
    private INotifyCollectionChanged? observedDimensions;
    private INotifyCollectionChanged? observedDimensionAssociations;
    private INotifyCollectionChanged? observedFixedPlanComponents;
    private INotifyCollectionChanged? observedProtectedDetailAssemblies;
    private INotifyCollectionChanged? observedCuratedPlanArtifacts;
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
        AttachGeometryPathsCollectionObserver(GeometryPaths);
        AttachPinchMarkersCollectionObserver(PinchMarkers);
        AttachRoomLabelsCollectionObserver(RoomLabels);
        AttachWallCandidatesCollectionObserver(WallCandidates);
        AttachOpeningCandidatesCollectionObserver(OpeningCandidates);
        AttachOpeningLabelsCollectionObserver(OpeningLabels);
        AttachDimensionsCollectionObserver(Dimensions);
        AttachDimensionAssociationsCollectionObserver(DimensionAssociations);
        AttachFixedPlanComponentsCollectionObserver(FixedPlanComponents);
        AttachProtectedDetailAssembliesCollectionObserver(ProtectedDetailAssemblies);
        AttachCuratedPlanArtifactsCollectionObserver(CuratedPlanArtifacts);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachRoomLabelsCollectionObserver();
        DetachWallCandidatesCollectionObserver();
        DetachOpeningLabelsCollectionObserver();
        DetachOpeningCandidatesCollectionObserver();
        DetachDimensionsCollectionObserver();
        DetachDimensionAssociationsCollectionObserver();
        DetachFixedPlanComponentsCollectionObserver();
        DetachProtectedDetailAssembliesCollectionObserver();
        DetachCuratedPlanArtifactsCollectionObserver();
        DetachPinchMarkersCollectionObserver();
        DetachGeometryPathsCollectionObserver();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pointerProperties = e.GetCurrentPoint(this).Properties;
        if (pointerProperties.IsMiddleButtonPressed)
        {
            isPanningPreview = true;
            panStartPoint = e.GetPosition(this);
            panStartZoomState = previewZoomState;
            activeDragEdge = null;
            activePreviewTrimMm = 0m;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!pointerProperties.IsLeftButtonPressed)
        {
            return;
        }

        var pointerPosition = e.GetPosition(this);
        var axisTag = ParseAxisTag();
        if (!IsPinchPlacementArmed && axisTag is not null)
        {
            var edge = ResolveEdgeDrag(pointerPosition, axisTag.Value);
            if (edge is not null)
            {
                activeDragEdge = edge;
                dragStartPoint = pointerPosition;
                activePreviewTrimMm = 0m;
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }
        }

        var viewport = GetPreviewViewport(axisTag);
        if (viewport is null)
        {
            return;
        }

        if (!IsPinchPlacementArmed &&
            TryResolveDimensionHandleHit(Dimensions, HighlightDimensionId, viewport.Value, pointerPosition, out var selectedHandleHit))
        {
            DimensionClicked?.Invoke(this, new DimensionClickedEventArgs(selectedHandleHit.Dimension.DimensionId));
            activeDimensionEdit = PreviewDimensionEditState.Start(
                selectedHandleHit.Dimension,
                selectedHandleHit.HandleKind,
                pointerPosition);
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        var dimensionHit = !IsPinchPlacementArmed
            ? TryResolveDimensionHit(Dimensions, viewport.Value, pointerPosition, HitTestTolerance)
            : null;
        if (dimensionHit is not null)
        {
            DimensionClicked?.Invoke(this, new DimensionClickedEventArgs(dimensionHit.Value.Dimension.DimensionId));
            activeDimensionEdit = PreviewDimensionEditState.Start(
                dimensionHit.Value.Dimension,
                dimensionHit.Value.SuggestedHandle,
                pointerPosition);
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (!IsPinchPlacementArmed &&
            TryResolveRoomLabelHit(viewport.Value, pointerPosition, out var roomLabel))
        {
            RoomLabelClicked?.Invoke(this, new RoomLabelClickedEventArgs(roomLabel.RoomLabelId));
            activeArtifactMove = PreviewArtifactMoveState.ForAbsolutePoint(
                FloorPlanArtifactPositionSourceKinds.RoomLabel,
                roomLabel.RoomLabelId,
                pointerPosition,
                roomLabel.X,
                roomLabel.Y);
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (!IsPinchPlacementArmed &&
            TryResolveOpeningLabelHit(viewport.Value, pointerPosition, out var openingLabel))
        {
            OpeningLabelClicked?.Invoke(this, new OpeningLabelClickedEventArgs(openingLabel.OpeningLabelId));
            activeArtifactMove = PreviewArtifactMoveState.ForAbsolutePoint(
                FloorPlanArtifactPositionSourceKinds.OpeningLabel,
                openingLabel.OpeningLabelId,
                pointerPosition,
                openingLabel.X,
                openingLabel.Y);
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        var hitTestGeometry = BuildHitTestGeometry(
            GeometryPaths,
            OpeningCandidates,
            FixedPlanComponents,
            ProtectedDetailAssemblies,
            CuratedPlanArtifacts);
        var hit = FloorPlanPreviewGeometry.HitTestPathDetail(hitTestGeometry, viewport.Value, pointerPosition, HitTestTolerance);

        if (hit is null)
        {
            return;
        }

        GeometryPathClicked?.Invoke(this, new GeometryPathClickedEventArgs(hit.Value.GeometryPathId, hit.Value.PositionRatio));
        if (!IsPinchPlacementArmed &&
            TryResolveMovableArtifact(hit.Value.GeometryPathId, out var movableArtifact))
        {
            activeArtifactMove = PreviewArtifactMoveState.ForTranslation(
                movableArtifact.SourceArtifactKind,
                movableArtifact.SourceArtifactId,
                pointerPosition,
                movableArtifact.BaseDx,
                movableArtifact.BaseDy);
            e.Pointer.Capture(this);
            InvalidateVisual();
        }

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (isPanningPreview)
        {
            previewZoomState = ResolvePanStateForDrag(panStartZoomState, panStartPoint, e.GetPosition(this));
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (activeArtifactMove is not null)
        {
            var activeMoveViewport = GetPreviewViewport(ParseAxisTag());
            if (activeMoveViewport is null || activeMoveViewport.Value.Scale <= double.Epsilon)
            {
                return;
            }

            var currentPointerPosition = e.GetPosition(this);
            var delta = ResolveWorldDelta(activeMoveViewport.Value, activeArtifactMove.Value.PointerStart, currentPointerPosition);
            activeArtifactMove = activeArtifactMove.Value with
            {
                CurrentDeltaX = delta.DeltaX,
                CurrentDeltaY = delta.DeltaY
            };
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (activeDimensionEdit is not null)
        {
            var activeMoveViewport = GetPreviewViewport(ParseAxisTag());
            if (activeMoveViewport is null || activeMoveViewport.Value.Scale <= double.Epsilon)
            {
                return;
            }

            var currentPointerPosition = e.GetPosition(this);
            var currentWorld = activeMoveViewport.Value.Unproject(currentPointerPosition);
            var viewportContext = CadViewportContext.Create(activeMoveViewport.Value);
            var snappedWorld = ResolveSnappedWorldPoint(
                currentWorld,
                viewportContext,
                ResolveDimensionSnapAnchors(activeDimensionEdit.Value.BaseDimension, activeDimensionEdit.Value.HandleKind),
                enableGridSnap: true);
            activeDimensionEdit = activeDimensionEdit.Value with
            {
                CurrentWorldPoint = snappedWorld
            };
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (activeDragEdge is null || GeometryPaths is not { Count: > 0 })
        {
            return;
        }

        var viewport = GetPreviewViewport(ParseAxisTag());
        if (viewport is null || viewport.Value.Scale <= double.Epsilon)
        {
            return;
        }

        var current = e.GetPosition(this);
        var pixelDelta = activeDragEdge switch
        {
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Right => Math.Max(0d, dragStartPoint.X - current.X),
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Left => Math.Max(0d, current.X - dragStartPoint.X),
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Top => Math.Max(0d, current.Y - dragStartPoint.Y),
            FloorPlanPreviewGeometry.PreviewCompressionEdge.Bottom => Math.Max(0d, dragStartPoint.Y - current.Y),
            _ => 0d
        };

        activePreviewTrimMm = (decimal)(pixelDelta / viewport.Value.Scale);
        InvalidateVisual();
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

        if (isPanningPreview)
        {
            isPanningPreview = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (activeArtifactMove is not null)
        {
            var move = activeArtifactMove.Value;
            var viewport = GetPreviewViewport(ParseAxisTag());
            var delta = viewport is null || viewport.Value.Scale <= double.Epsilon
                ? (move.CurrentDeltaX, move.CurrentDeltaY)
                : ResolveWorldDelta(viewport.Value, move.PointerStart, e.GetPosition(this));
            var movement = ResolveMovement(move, delta.Item1, delta.Item2);

            activeArtifactMove = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            InvalidateVisual();

            if (movement is not null)
            {
                MovableArtifactMoved?.Invoke(this, movement);
            }

            return;
        }

        if (activeDimensionEdit is not null)
        {
            var edit = activeDimensionEdit.Value;
            var editedDimension = BuildEditedDimensionPreview(
                edit,
                Dimensions?.FirstOrDefault(item => item.DimensionId == edit.BaseDimension.DimensionId) ?? edit.BaseDimension);
            activeDimensionEdit = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            InvalidateVisual();

            if (editedDimension is not null && !editedDimension.Equals(edit.BaseDimension))
            {
                DimensionEdited?.Invoke(this, new DimensionEditedEventArgs(
                    edit.BaseDimension.DimensionId,
                    ResolveSourceDimensionKey(edit.BaseDimension),
                    editedDimension));
            }

            return;
        }

        if (activeDragEdge is null)
        {
            return;
        }

        activeDragEdge = null;
        activePreviewTrimMm = 0m;
        e.Pointer.Capture(null);
        InvalidateVisual();
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
    {
        var safeCurrentZoom = double.IsFinite(currentZoomFactor)
            ? currentZoomFactor
            : 1d;
        var requestedZoom = safeCurrentZoom * Math.Pow(UserZoomStep, wheelDeltaY);
        return Math.Clamp(requestedZoom, MinimumUserZoomFactor, MaximumUserZoomFactor);
    }

    internal static PreviewZoomState ResolveZoomStateForWheel(
        FloorPlanPreviewGeometry.PreviewViewport baseViewport,
        PreviewZoomState currentState,
        Point pointerPosition,
        double wheelDeltaY)
    {
        var currentZoom = Math.Clamp(
            double.IsFinite(currentState.ZoomFactor) ? currentState.ZoomFactor : 1d,
            MinimumUserZoomFactor,
            MaximumUserZoomFactor);
        var currentViewport = baseViewport.WithUserTransform(currentZoom, currentState.PanOffset);
        var worldPoint = currentViewport.Unproject(pointerPosition);
        var nextZoom = CalculateWheelZoomFactor(currentZoom, wheelDeltaY);
        var nextViewportWithoutPan = baseViewport.WithUserTransform(nextZoom, default);
        var projectedWithoutPan = nextViewportWithoutPan.Project(worldPoint.X, worldPoint.Y);
        var nextPanOffset = new Vector(
            pointerPosition.X - projectedWithoutPan.X,
            pointerPosition.Y - projectedWithoutPan.Y);

        return new PreviewZoomState(nextZoom, nextPanOffset);
    }

    internal static PreviewZoomState ResolvePanStateForDrag(
        PreviewZoomState startState,
        Point startPointerPosition,
        Point currentPointerPosition)
    {
        var pointerDelta = currentPointerPosition - startPointerPosition;
        return new PreviewZoomState(
            startState.ZoomFactor,
            startState.PanOffset + pointerDelta);
    }

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

    private static (decimal DeltaX, decimal DeltaY) ResolveWorldDelta(
        FloorPlanPreviewGeometry.PreviewViewport viewport,
        Point startPointer,
        Point currentPointer)
    {
        var startWorld = viewport.Unproject(startPointer);
        var currentWorld = viewport.Unproject(currentPointer);
        return (
            RoundModelValue((decimal)(currentWorld.X - startWorld.X)),
            RoundModelValue((decimal)(currentWorld.Y - startWorld.Y)));
    }

    private static MovableArtifactMovedEventArgs? ResolveMovement(
        PreviewArtifactMoveState move,
        decimal deltaX,
        decimal deltaY)
    {
        if (move.PositionMode == FloorPlanArtifactPositionMode.AbsolutePoint)
        {
            var moved = ApplyAbsolutePointDelta(move.BaseX, move.BaseY, deltaX, deltaY);
            if (!HasMeaningfulDifference(move.BaseX, moved.X) && !HasMeaningfulDifference(move.BaseY, moved.Y))
            {
                return null;
            }

            return new MovableArtifactMovedEventArgs(
                move.SourceArtifactKind,
                move.SourceArtifactId,
                move.PositionMode,
                moved.X,
                moved.Y,
                null,
                null);
        }

        var translated = ApplyTranslationDelta(move.BaseDx, move.BaseDy, deltaX, deltaY);
        if (!HasMeaningfulDifference(move.BaseDx, translated.Dx) && !HasMeaningfulDifference(move.BaseDy, translated.Dy))
        {
            return null;
        }

        return new MovableArtifactMovedEventArgs(
            move.SourceArtifactKind,
            move.SourceArtifactId,
            move.PositionMode,
            null,
            null,
            translated.Dx,
            translated.Dy);
    }

    private static bool HasMeaningfulDifference(decimal original, decimal updated)
    {
        return decimal.Abs(updated - original) >= MovementPersistenceEpsilon;
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
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachGeometryPathsCollectionObserver();
            AttachGeometryPathsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnPinchMarkersChanged(IReadOnlyList<PinchMarkerDto>? oldValue, IReadOnlyList<PinchMarkerDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachPinchMarkersCollectionObserver();
            AttachPinchMarkersCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnRoomLabelsChanged(IReadOnlyList<RoomLabelDto>? oldValue, IReadOnlyList<RoomLabelDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachRoomLabelsCollectionObserver();
            AttachRoomLabelsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnOpeningCandidatesChanged(IReadOnlyList<OpeningCandidateDto>? oldValue, IReadOnlyList<OpeningCandidateDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachOpeningCandidatesCollectionObserver();
            AttachOpeningCandidatesCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnOpeningLabelsChanged(IReadOnlyList<OpeningLabelDto>? oldValue, IReadOnlyList<OpeningLabelDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachOpeningLabelsCollectionObserver();
            AttachOpeningLabelsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnDimensionsChanged(IReadOnlyList<DimensionDto>? oldValue, IReadOnlyList<DimensionDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachDimensionsCollectionObserver();
            AttachDimensionsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnWallCandidatesChanged(IReadOnlyList<WallCandidateDto>? oldValue, IReadOnlyList<WallCandidateDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachWallCandidatesCollectionObserver();
            AttachWallCandidatesCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnDimensionAssociationsChanged(
        IReadOnlyList<DimensionAssociationDto>? oldValue,
        IReadOnlyList<DimensionAssociationDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachDimensionAssociationsCollectionObserver();
            AttachDimensionAssociationsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnFixedPlanComponentsChanged(IReadOnlyList<FixedPlanComponentDto>? oldValue, IReadOnlyList<FixedPlanComponentDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachFixedPlanComponentsCollectionObserver();
            AttachFixedPlanComponentsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnProtectedDetailAssembliesChanged(
        IReadOnlyList<ProtectedDetailAssemblyDto>? oldValue,
        IReadOnlyList<ProtectedDetailAssemblyDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachProtectedDetailAssembliesCollectionObserver();
            AttachProtectedDetailAssembliesCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void OnCuratedPlanArtifactsChanged(
        IReadOnlyList<CuratedPlanArtifactDto>? oldValue,
        IReadOnlyList<CuratedPlanArtifactDto>? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
        {
            DetachCuratedPlanArtifactsCollectionObserver();
            AttachCuratedPlanArtifactsCollectionObserver(newValue);
        }

        InvalidateVisual();
    }

    private void AttachGeometryPathsCollectionObserver(IReadOnlyList<GeometryPathDto>? value)
    {
        if (ReferenceEquals(observedGeometryPaths, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedGeometryPaths = notifyCollectionChanged;
        observedGeometryPaths.CollectionChanged += GeometryPathsCollectionChanged;
    }

    private void DetachGeometryPathsCollectionObserver()
    {
        if (observedGeometryPaths is null)
        {
            return;
        }

        observedGeometryPaths.CollectionChanged -= GeometryPathsCollectionChanged;
        observedGeometryPaths = null;
    }

    private void AttachPinchMarkersCollectionObserver(IReadOnlyList<PinchMarkerDto>? value)
    {
        if (ReferenceEquals(observedPinchMarkers, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedPinchMarkers = notifyCollectionChanged;
        observedPinchMarkers.CollectionChanged += PinchMarkersCollectionChanged;
    }

    private void DetachPinchMarkersCollectionObserver()
    {
        if (observedPinchMarkers is null)
        {
            return;
        }

        observedPinchMarkers.CollectionChanged -= PinchMarkersCollectionChanged;
        observedPinchMarkers = null;
    }

    private void AttachRoomLabelsCollectionObserver(IReadOnlyList<RoomLabelDto>? value)
    {
        if (ReferenceEquals(observedRoomLabels, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedRoomLabels = notifyCollectionChanged;
        observedRoomLabels.CollectionChanged += RoomLabelsCollectionChanged;
    }

    private void DetachRoomLabelsCollectionObserver()
    {
        if (observedRoomLabels is null)
        {
            return;
        }

        observedRoomLabels.CollectionChanged -= RoomLabelsCollectionChanged;
        observedRoomLabels = null;
    }

    private void AttachOpeningCandidatesCollectionObserver(IReadOnlyList<OpeningCandidateDto>? value)
    {
        if (ReferenceEquals(observedOpeningCandidates, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedOpeningCandidates = notifyCollectionChanged;
        observedOpeningCandidates.CollectionChanged += OpeningCandidatesCollectionChanged;
    }

    private void AttachWallCandidatesCollectionObserver(IReadOnlyList<WallCandidateDto>? value)
    {
        if (ReferenceEquals(observedWallCandidates, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedWallCandidates = notifyCollectionChanged;
        observedWallCandidates.CollectionChanged += WallCandidatesCollectionChanged;
    }

    private void DetachOpeningCandidatesCollectionObserver()
    {
        if (observedOpeningCandidates is null)
        {
            return;
        }

        observedOpeningCandidates.CollectionChanged -= OpeningCandidatesCollectionChanged;
        observedOpeningCandidates = null;
    }

    private void DetachWallCandidatesCollectionObserver()
    {
        if (observedWallCandidates is null)
        {
            return;
        }

        observedWallCandidates.CollectionChanged -= WallCandidatesCollectionChanged;
        observedWallCandidates = null;
    }

    private void AttachOpeningLabelsCollectionObserver(IReadOnlyList<OpeningLabelDto>? value)
    {
        if (ReferenceEquals(observedOpeningLabels, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedOpeningLabels = notifyCollectionChanged;
        observedOpeningLabels.CollectionChanged += OpeningLabelsCollectionChanged;
    }

    private void DetachOpeningLabelsCollectionObserver()
    {
        if (observedOpeningLabels is null)
        {
            return;
        }

        observedOpeningLabels.CollectionChanged -= OpeningLabelsCollectionChanged;
        observedOpeningLabels = null;
    }

    private void AttachDimensionsCollectionObserver(IReadOnlyList<DimensionDto>? value)
    {
        if (ReferenceEquals(observedDimensions, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedDimensions = notifyCollectionChanged;
        observedDimensions.CollectionChanged += DimensionsCollectionChanged;
    }

    private void AttachDimensionAssociationsCollectionObserver(IReadOnlyList<DimensionAssociationDto>? value)
    {
        if (ReferenceEquals(observedDimensionAssociations, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedDimensionAssociations = notifyCollectionChanged;
        observedDimensionAssociations.CollectionChanged += DimensionAssociationsCollectionChanged;
    }

    private void AttachFixedPlanComponentsCollectionObserver(IReadOnlyList<FixedPlanComponentDto>? value)
    {
        if (ReferenceEquals(observedFixedPlanComponents, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedFixedPlanComponents = notifyCollectionChanged;
        observedFixedPlanComponents.CollectionChanged += FixedPlanComponentsCollectionChanged;
    }

    private void AttachProtectedDetailAssembliesCollectionObserver(IReadOnlyList<ProtectedDetailAssemblyDto>? value)
    {
        if (ReferenceEquals(observedProtectedDetailAssemblies, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedProtectedDetailAssemblies = notifyCollectionChanged;
        observedProtectedDetailAssemblies.CollectionChanged += ProtectedDetailAssembliesCollectionChanged;
    }

    private void AttachCuratedPlanArtifactsCollectionObserver(IReadOnlyList<CuratedPlanArtifactDto>? value)
    {
        if (ReferenceEquals(observedCuratedPlanArtifacts, value) || value is not INotifyCollectionChanged notifyCollectionChanged)
        {
            return;
        }

        observedCuratedPlanArtifacts = notifyCollectionChanged;
        observedCuratedPlanArtifacts.CollectionChanged += CuratedPlanArtifactsCollectionChanged;
    }

    private void DetachFixedPlanComponentsCollectionObserver()
    {
        if (observedFixedPlanComponents is null)
        {
            return;
        }

        observedFixedPlanComponents.CollectionChanged -= FixedPlanComponentsCollectionChanged;
        observedFixedPlanComponents = null;
    }

    private void DetachDimensionsCollectionObserver()
    {
        if (observedDimensions is null)
        {
            return;
        }

        observedDimensions.CollectionChanged -= DimensionsCollectionChanged;
        observedDimensions = null;
    }

    private void DetachDimensionAssociationsCollectionObserver()
    {
        if (observedDimensionAssociations is null)
        {
            return;
        }

        observedDimensionAssociations.CollectionChanged -= DimensionAssociationsCollectionChanged;
        observedDimensionAssociations = null;
    }

    private void DetachProtectedDetailAssembliesCollectionObserver()
    {
        if (observedProtectedDetailAssemblies is null)
        {
            return;
        }

        observedProtectedDetailAssemblies.CollectionChanged -= ProtectedDetailAssembliesCollectionChanged;
        observedProtectedDetailAssemblies = null;
    }

    private void DetachCuratedPlanArtifactsCollectionObserver()
    {
        if (observedCuratedPlanArtifacts is null)
        {
            return;
        }

        observedCuratedPlanArtifacts.CollectionChanged -= CuratedPlanArtifactsCollectionChanged;
        observedCuratedPlanArtifacts = null;
    }

    private void GeometryPathsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void PinchMarkersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void RoomLabelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OpeningCandidatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void WallCandidatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OpeningLabelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void DimensionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void DimensionAssociationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void FixedPlanComponentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void ProtectedDetailAssembliesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void CuratedPlanArtifactsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
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

    private readonly record struct DimensionHandleHit(DimensionDto Dimension, DimensionHandleKind HandleKind);

    private readonly record struct PreviewArtifactMoveState(
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

    private readonly record struct PreviewMovableArtifactDescriptor(
        string SourceArtifactKind,
        Guid SourceArtifactId,
        FloorPlanArtifactPositionMode PositionMode,
        decimal BaseX,
        decimal BaseY,
        decimal BaseDx,
        decimal BaseDy,
        IReadOnlyList<Guid> GeometryPathIds);

    private readonly record struct PreviewDimensionEditState(
        DimensionDto BaseDimension,
        DimensionHandleKind HandleKind,
        Point PointerStart,
        Point? CurrentWorldPoint)
    {
        public static PreviewDimensionEditState Start(DimensionDto baseDimension, DimensionHandleKind handleKind, Point pointerStart)
            => new(baseDimension, handleKind, pointerStart, null);
    }
}
